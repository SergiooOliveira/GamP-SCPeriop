using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace GamP_SCPeriop.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModuleComponentController : Controller
    {
        private readonly AppDbContext _context;

        public ModuleComponentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<Component>> CreateComponent(ModuleComponentCreateDTO dto)
        {
            var component = new ModuleComponent
            {
                Title = dto.Title,
                ModuleId = dto.ModuleId,
                PdfFilePath = dto.PdfFilePath ?? string.Empty,
                Status = 0 // Assuming 0 means "Not Started" or similar
            };

            _context.ModuleComponents.Add(component);
            await _context.SaveChangesAsync();

            return Ok(component);
        }
    }
}
