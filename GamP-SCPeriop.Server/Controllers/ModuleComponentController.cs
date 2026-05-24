using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.AspNetCore.Mvc;

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
                Status = ComponentStatus.Pending
            };

            _context.ModuleComponents.Add(component);
            await _context.SaveChangesAsync();

            // Returns the object with its new Database ID so the Frontend can edit/delete it immediately
            return CreatedAtAction(nameof(GetComponent), new { id = component.Id }, component);
        }

        // --- 2. EDIT (PUT) ---
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComponent(int id, [FromBody] ModuleComponent updatedComponent)
        {
            if (id != updatedComponent.Id) return BadRequest("ID mismatch.");

            var existingComponent = await _context.ModuleComponents.FindAsync(id);
            if (existingComponent == null) return NotFound();

            // Update only the fields the supervisor is allowed to change
            existingComponent.Title = updatedComponent.Title;
            existingComponent.Description = updatedComponent.Description;
            existingComponent.PdfFilePath = updatedComponent.PdfFilePath ?? string.Empty;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // --- 3. DELETE ---
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComponent(int id)
        {
            var component = await _context.ModuleComponents.FindAsync(id);
            if (component == null) return NotFound();

            _context.ModuleComponents.Remove(component);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // --- 4. GET SINGLE --- (Required for the CreatedAtAction to work properly)
        [HttpGet("{id}")]
        public async Task<ActionResult<ModuleComponent>> GetComponent(int id)
        {
            var component = await _context.ModuleComponents.FindAsync(id);
            if (component == null) return NotFound();
            return component;
        }
    }
}