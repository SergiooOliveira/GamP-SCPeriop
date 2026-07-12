using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data.Template;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModuleTemplateController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ModuleTemplateController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<ModuleTemplate>> CreateModule([FromBody] ModuleTemplate dto)
        {
            _context.ModuleTemplates.Add(dto);
            await _context.SaveChangesAsync();
            return Ok(dto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteModule(int id)
        {
            var module = await _context.ModuleTemplates
                .Include(m => m.ComponentTemplates)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (module == null) return NotFound();

            if (module.ComponentTemplates != null && module.ComponentTemplates.Any())
            {
                _context.ComponentTemplates.RemoveRange(module.ComponentTemplates);
            }

            _context.ModuleTemplates.Remove(module);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PathwayTemplate>> GetTemplate(int id)
        {
            // A magia do Entity Framework: carrega o Molde, inclui os Módulos, e dentro dos Módulos inclui as Tarefas
            var template = await _context.PathwayTemplates
                .Include(p => p.ModuleTemplates)
                    .ThenInclude(m => m.ComponentTemplates)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (template == null)
            {
                return NotFound();
            }

            return Ok(template);
        }
    }
}