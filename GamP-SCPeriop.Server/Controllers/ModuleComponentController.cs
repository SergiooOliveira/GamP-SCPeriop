using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModuleComponentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ModuleComponentController(AppDbContext context)
        {
            _context = context;
        }

        // --- 1. CREATE (POST) ---
        [HttpPost]
        public async Task<ActionResult<ModuleComponent>> CreateComponent([FromBody] ModuleComponentCreateDTO dto)
        {
            var component = new ModuleComponent
            {
                Title = dto.Title,
                ModuleId = dto.ModuleId,
                Stage = dto.Stage,
                Description = dto.Description,
                PdfFilePath = dto.PdfFilePath ?? string.Empty,
                Status = ComponentStatus.Pending,
                ParentComponentId = dto.ParentComponentId,
                Weight = dto.Weight
            };

            _context.ModuleComponents.Add(component);
            await _context.SaveChangesAsync();

            // Devolve o objeto com o novo ID da BD para o Frontend poder editar/apagar imediatamente
            return CreatedAtAction(nameof(GetComponent), new { id = component.Id }, component);
        }

        // --- 2. EDIT (PUT) ---
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComponent(int id, [FromBody] ModuleComponent updatedComponent)
        {
            if (id != updatedComponent.Id) return BadRequest("ID mismatch.");

            var existingComponent = await _context.ModuleComponents.FindAsync(id);
            if (existingComponent == null) return NotFound();

            // Atualiza apenas os campos permitidos
            existingComponent.Title = updatedComponent.Title;
            existingComponent.Description = updatedComponent.Description;
            existingComponent.PdfFilePath = updatedComponent.PdfFilePath ?? string.Empty;
            existingComponent.Weight = updatedComponent.Weight; // <-- CORREÇÃO: O peso é agora atualizado na edição

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // --- 3. DELETE ---
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComponent(int id)
        {
            var component = await _context.ModuleComponents.FindAsync(id);
            if (component == null) return NotFound();

            // Procurar e apagar todos os filhos primeiro (Cascata manual)
            var children = await _context.ModuleComponents.Where(c => c.ParentComponentId == id).ToListAsync();
            if (children.Any())
            {
                _context.ModuleComponents.RemoveRange(children);
            }

            // Apagar o Pai em segurança
            _context.ModuleComponents.Remove(component);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // --- 4. GET SINGLE ---
        // (Necessário para o CreatedAtAction funcionar corretamente no POST)
        [HttpGet("{id}")]
        public async Task<ActionResult<ModuleComponent>> GetComponent(int id)
        {
            var component = await _context.ModuleComponents.FindAsync(id);
            if (component == null) return NotFound();
            return component;
        }
    }
}