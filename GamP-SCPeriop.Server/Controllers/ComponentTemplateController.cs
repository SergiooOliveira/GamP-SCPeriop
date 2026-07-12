using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data.Template;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComponentTemplateController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ComponentTemplateController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<ComponentTemplate>> CreateComponent([FromBody] ComponentTemplate dto)
        {
            _context.ComponentTemplates.Add(dto);
            await _context.SaveChangesAsync();

            return Ok(dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComponent(int id, [FromBody] ComponentTemplate updatedComponent)
        {
            if (id != updatedComponent.Id) return BadRequest("ID mismatch.");

            var existingComponent = await _context.ComponentTemplates.FindAsync(id);
            if (existingComponent == null) return NotFound();

            // Num molde, só nos interessa atualizar os textos
            existingComponent.Title = updatedComponent.Title;
            existingComponent.Description = updatedComponent.Description;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComponent(int id)
        {
            var component = await _context.ComponentTemplates.FindAsync(id);
            if (component == null) return NotFound();

            // Apaga os sub-parâmetros (filhos) primeiro para evitar erros na BD
            var children = await _context.ComponentTemplates.Where(c => c.ParentComponentTemplateId == id).ToListAsync();
            if (children.Any())
            {
                _context.ComponentTemplates.RemoveRange(children);
            }

            // Apaga a tarefa principal
            _context.ComponentTemplates.Remove(component);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ComponentTemplate>> GetComponentTemplate(int id)
        {
            var component = await _context.ComponentTemplates.FindAsync(id);
            if (component == null) return NotFound();

            return Ok(component);
        }
    }
}