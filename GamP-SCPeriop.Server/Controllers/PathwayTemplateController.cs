using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data.Template;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PathwayTemplateController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PathwayTemplateController(AppDbContext context)
        {
            _context = context;
        }

        // Devolve a lista de moldes para a grelha inicial
        [HttpGet]
        public async Task<ActionResult<List<PathwayTemplate>>> GetTemplates()
        {
            return await _context.PathwayTemplates.ToListAsync();
        }

        // Devolve UM molde específico com a estrutura toda (O QUE ESTAVA A FALHAR)
        [HttpGet("{id}")]
        public async Task<ActionResult<PathwayTemplate>> GetTemplate(int id)
        {
            var template = await _context.PathwayTemplates
                .Include(p => p.ModuleTemplates)
                    .ThenInclude(m => m.ComponentTemplates)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (template == null) return NotFound();

            return Ok(template);
        }

        [HttpPost]
        public async Task<ActionResult<PathwayTemplate>> CreateTemplate(PathwayTemplate template)
        {
            _context.PathwayTemplates.Add(template);
            await _context.SaveChangesAsync();
            return Ok(template);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var template = await _context.PathwayTemplates.FindAsync(id);
            if (template == null) return NotFound();

            _context.PathwayTemplates.Remove(template);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}