using GamP_SCPeriop.Server.Services;
using GamP_SCPeriop.Shared.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamP_SCPeriop.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FilesController : ControllerBase
    {
        // First bytes of each allowed type, so a renamed .html or .exe is refused
        private static readonly Dictionary<string, byte[]> FileSignatures = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "%PDF-"u8.ToArray(),
            [".docx"] = new byte[] { 0x50, 0x4B, 0x03, 0x04 },                        // ZIP container
            [".doc"] = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }   // OLE container
        };

        private readonly IWebHostEnvironment _env;

        public FilesController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // Folder OUTSIDE of wwwroot, so files are only reachable through the download action below
        private string SecureFolderPath => Path.Combine(_env.ContentRootPath, "SecureUploads");

        // 1. UPLOAD: only staff, only PDF/Word, max 10 MB
        [HttpPost("upload")]
        [Authorize(Roles = Roles.Staff)]
        [RequestSizeLimit(DocumentLinks.MaxUploadBytes + 1024 * 1024)] // + room for the multipart envelope
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Nenhum ficheiro recebido.");
            if (file.Length > DocumentLinks.MaxUploadBytes) return BadRequest("O ficheiro excede o limite de 10 MB.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!FileSignatures.TryGetValue(extension, out var signature))
                return BadRequest("Só são aceites ficheiros PDF ou Word (.pdf, .doc, .docx).");

            // The content must really be what the extension says
            var header = new byte[signature.Length];
            await using (var check = file.OpenReadStream())
            {
                var read = await check.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
                if (read < header.Length || !header.AsSpan().SequenceEqual(signature))
                    return BadRequest("O conteúdo do ficheiro não corresponde a um PDF ou documento Word.");
            }

            Directory.CreateDirectory(SecureFolderPath);

            // Unique name chosen by the server: the client's file name is never used on disk
            var storedName = Guid.NewGuid() + extension;
            await using (var stream = new FileStream(Path.Combine(SecureFolderPath, storedName), FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            // Safe API URL that the frontend stores in the component and uses to open the file later
            return Ok(DocumentLinks.UploadPrefix + storedName);
        }

        // 2. DOWNLOAD: any logged-in user; only names the server generated (no path tricks)
        [HttpGet("download/{fileName}")]
        public IActionResult DownloadSecureFile(string fileName)
        {
            if (!DocumentLinks.IsStoredFileName(fileName)) return NotFound("Ficheiro não encontrado.");

            var filePath = Path.Combine(SecureFolderPath, fileName);
            if (!System.IO.File.Exists(filePath)) return NotFound("Ficheiro não encontrado.");

            var (contentType, inline) = DocumentLinks.AllowedTypes[Path.GetExtension(fileName)];

            // The browser must treat it as exactly this type and never run anything inside it
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Content-Security-Policy"] = "default-src 'none'; sandbox";

            return inline
                ? PhysicalFile(filePath, contentType)
                : PhysicalFile(filePath, contentType, fileName);
        }
    }
}
