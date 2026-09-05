using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data.Template;
using GamP_SCPeriop.Shared.Entity.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BadgeTemplateController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BadgeTemplateController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBadgeTemplate([FromBody] BadgeTemplateCreateDto dto)
        {
            if (dto == null) return BadRequest("Dados inválidos.");

            var newBadge = new BadgeTemplate
            {
                PathwayTemplateId = dto.PathwayTemplateId,
                Name = dto.Name,
                Description = dto.Description,
                Icon = dto.Icon,
                Tier = dto.Tier, 
                TriggerType = dto.TriggerType,
                TriggerValue = dto.TriggerValue
            };

            _context.BadgeTemplates.Add(newBadge);
            await _context.SaveChangesAsync();

            return Ok(newBadge);
        }

        [HttpGet("pathway/{pathwayTemplateId}")]
        public async Task<IActionResult> GetBadgesByPathway(int pathwayTemplateId)
        {
            var badges = await _context.BadgeTemplates
                .Where(b => b.PathwayTemplateId == pathwayTemplateId)
                .OrderBy(b => b.Tier)
                .ThenBy(b => b.Name)
                .ToListAsync();

            return Ok(badges);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBadgeTemplate(int id, [FromBody] BadgeTemplateCreateDto badgeDto)
        {
            var existingBadge = await _context.BadgeTemplates.FindAsync(id);

            if (existingBadge == null)
            {
                return NotFound("Badge não encontrada.");
            }

            // Map the updated fields from the UI
            existingBadge.Name = badgeDto.Name;
            existingBadge.Description = badgeDto.Description;
            existingBadge.Icon = badgeDto.Icon;
            existingBadge.TriggerType = badgeDto.TriggerType;
            existingBadge.TriggerValue = badgeDto.TriggerValue;
            existingBadge.Tier = badgeDto.Tier;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(existingBadge);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro interno ao atualizar a badge: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBadgeTemplate(int id)
        {
            try
            {
                var badge = await _context.BadgeTemplates.FindAsync(id);

                if (badge == null)
                {
                    return NotFound("Badge não encontrada.");
                }

                _context.BadgeTemplates.Remove(badge);
                await _context.SaveChangesAsync();

                return Ok(); // ou NoContent()
            }
            catch (Exception ex)
            {
                // Se houver problemas (ex: chaves forasteiras, constrangimentos da DB)
                return StatusCode(500, $"Erro interno ao apagar a badge: {ex.Message}");
            }
        }
    }
}
