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
    }
}
