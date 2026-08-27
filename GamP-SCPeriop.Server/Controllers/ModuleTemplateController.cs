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
        public async Task<ActionResult<ModuleTemplate>> GetTemplate(int id)
        {
            var template = await _context.ModuleTemplates
                .Include(m => m.ComponentTemplates)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (template == null)
            {
                return NotFound();
            }

            return Ok(template);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateModuleTemplate(int id, ModuleTemplate updatedTemplate)
        {
            if (id != updatedTemplate.Id)
                return BadRequest("O ID da rota não corresponde ao ID do objeto.");

            // Vai procurar o módulo existente à base de dados
            var existingTemplate = await _context.ModuleTemplates.FindAsync(id);

            if (existingTemplate == null)
                return NotFound("Módulo não encontrado.");

            // Atualiza apenas os campos que interessam (protege o resto da estrutura)
            existingTemplate.Title = updatedTemplate.Title;
            existingTemplate.Weight = updatedTemplate.Weight;
            existingTemplate.OrderIndex = updatedTemplate.OrderIndex;

            // Guarda as alterações
            await _context.SaveChangesAsync();

            return NoContent(); // Retorna 204 (Sucesso, sem conteúdo para devolver)
        }
    }
}