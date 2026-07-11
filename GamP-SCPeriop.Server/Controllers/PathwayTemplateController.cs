// Substitui pelo namespace correto do teu DbContext se for diferente
using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data.Template;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PathwayTemplateController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PathwayTemplateController(AppDbContext context)
        {
            _context = context;
        }

        // 1. LER todos os Templates (já com os módulos e componentes)
        [HttpGet]
        public async Task<ActionResult<List<PathwayTemplate>>> GetPathwayTemplates()
        {
            var templates = await _context.PathwayTemplates
                .Include(p => p.ModuleTemplates)
                    .ThenInclude(m => m.ComponentTemplates)
                .ToListAsync();

            return Ok(templates);
        }

        // 2. CRIAR um novo Template Base
        [HttpPost]
        public async Task<ActionResult<PathwayTemplate>> CreatePathwayTemplate(PathwayTemplate template)
        {
            if (template == null)
                return BadRequest("O template não pode ser nulo.");

            _context.PathwayTemplates.Add(template);
            await _context.SaveChangesAsync();

            return Ok(template);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePathwayTemplate(int id)
        {
            var template = await _context.PathwayTemplates.FindAsync(id);
            if (template == null)
                return NotFound("Molde não encontrado.");

            _context.PathwayTemplates.Remove(template);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}