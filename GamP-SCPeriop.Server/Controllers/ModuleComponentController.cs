using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<ActionResult<ModuleComponent>> CreateModuleComponent(ModuleComponentCreateDto dto)
        {
            var component = new ModuleComponent
            {
                ModuleId = dto.ModuleId,
                Title = dto.Title,
                Description = dto.Description,
                Stage = dto.Stage,
                ParentComponentId = dto.ParentComponentId,
                Weight = dto.Weight,
                IsFromTemplate = false
            };

            _context.ModuleComponents.Add(component);
            await _context.SaveChangesAsync();

            return Ok(component);
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
            existingComponent.Weight = updatedComponent.Weight;

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
        [HttpGet("{id}")]
        public async Task<ActionResult<ModuleComponent>> GetComponent(int id)
        {
            var component = await _context.ModuleComponents.FindAsync(id);
            if (component == null) return NotFound();
            return component;
        }
    }
}