using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Server.Services;
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
    [Authorize(Roles = Roles.Staff)]
    public class ModuleComponentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AccessService _access;

        public ModuleComponentController(AppDbContext context, AccessService access)
        {
            _context = context;
            _access = access;
        }

        // --- 1. CREATE (POST) ---
        [HttpPost]
        public async Task<ActionResult<ModuleComponent>> CreateModuleComponent(ModuleComponentCreateDto dto)
        {
            if (!await _access.CanManageModuleAsync(User, dto.ModuleId)) return Forbid();

            // A sub-task must hang from a component of the same module
            if (dto.ParentComponentId.HasValue &&
                !await _context.ModuleComponents.AnyAsync(c => c.Id == dto.ParentComponentId.Value && c.ModuleId == dto.ModuleId))
                return BadRequest("O componente pai não pertence a este módulo.");

            var component = new ModuleComponent
            {
                ModuleId = dto.ModuleId,
                Title = dto.Title,
                Description = dto.Description,
                Stage = dto.Stage,
                ParentComponentId = dto.ParentComponentId,
                Weight = dto.Weight,
                OrderIndex = dto.OrderIndex,
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
            if (!await _access.CanManageComponentAsync(User, id)) return Forbid();

            if (id != updatedComponent.Id) return BadRequest("ID mismatch.");

            var existingComponent = await _context.ModuleComponents.FindAsync(id);
            if (existingComponent == null) return NotFound();

            // Atualiza apenas os campos permitidos
            existingComponent.Title = updatedComponent.Title;
            existingComponent.Description = updatedComponent.Description;
            existingComponent.PdfFilePath = updatedComponent.PdfFilePath ?? string.Empty;
            existingComponent.Weight = updatedComponent.Weight;
            existingComponent.OrderIndex = updatedComponent.OrderIndex;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // --- 3. DELETE ---
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComponent(int id)
        {
            if (!await _access.CanManageComponentAsync(User, id)) return Forbid();

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
            if (!await _access.CanManageComponentAsync(User, id)) return Forbid();

            var component = await _context.ModuleComponents.FindAsync(id);
            if (component == null) return NotFound();
            return component;
        }
    }
}