using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace GamP_SCPeriop.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public FilesController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // 1. THE UPLOAD DOOR (POST)
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Nenhum ficheiro recebido.");

            // Create the secure folder OUTSIDE of wwwroot so it can't be guessed
            var secureFolderPath = Path.Combine(_env.ContentRootPath, "SecureUploads");
            if (!Directory.Exists(secureFolderPath)) Directory.CreateDirectory(secureFolderPath);

            // Generate a unique gibberish name to prevent overwriting
            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(secureFolderPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return the safe API URL that the frontend will use to download this file later
            var downloadUrl = $"api/files/download/{uniqueFileName}";
            return Ok(downloadUrl);
        }

        // 2. THE BOUNCER / DOWNLOAD DOOR (GET)
        // NOTE: Uncomment [Authorize] when your login system is ready!
        // [Authorize] 
        [HttpGet("download/{fileName}")]
        public IActionResult DownloadSecureFile(string fileName)
        {
            var secureFolderPath = Path.Combine(_env.ContentRootPath, "SecureUploads");
            var filePath = Path.Combine(secureFolderPath, fileName);

            if (!System.IO.File.Exists(filePath)) return NotFound("Ficheiro não encontrado ou sem permissões.");

            // Return the file safely as a stream
            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filePath, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return PhysicalFile(filePath, contentType);
        }
    }
}
