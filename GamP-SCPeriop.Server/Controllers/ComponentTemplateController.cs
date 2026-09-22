using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Server.Services;
using GamP_SCPeriop.Shared.Helpers;
using GamP_SCPeriop.Shared.Data.Template;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.Admin)]
    public class ComponentTemplateController : ControllerBase
    {
        private const string InvalidLinkMessage = "O documento tem de ser um ficheiro carregado ou uma ligação http(s).";

        private readonly AppDbContext _context;

        public ComponentTemplateController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<ComponentTemplate>> CreateComponent([FromBody] ComponentTemplate dto)
        {
            if (!DocumentLinks.IsAllowed(dto.PdfFilePath)) return BadRequest(InvalidLinkMessage);

            // Only this component is created: ignore any id or nested children sent by the client
            dto.Id = 0;
            dto.SubComponents = new();

            _context.ComponentTemplates.Add(dto);
            await _context.SaveChangesAsync();

            return Ok(dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComponent(int id, [FromBody] ComponentTemplate updatedComponent)
        {
            if (id != updatedComponent.Id) return BadRequest("ID mismatch.");
            if (!DocumentLinks.IsAllowed(updatedComponent.PdfFilePath)) return BadRequest(InvalidLinkMessage);

            var existingComponent = await _context.ComponentTemplates.FindAsync(id);
            if (existingComponent == null) return NotFound();

            // Num molde, só nos interessa atualizar os textos
            existingComponent.Title = updatedComponent.Title;
            existingComponent.Description = updatedComponent.Description;
            existingComponent.Weight = updatedComponent.Weight;
            existingComponent.PdfFilePath = updatedComponent.PdfFilePath ?? string.Empty;
            existingComponent.OrderIndex = updatedComponent.OrderIndex;

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