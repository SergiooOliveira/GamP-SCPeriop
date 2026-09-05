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
    public class BadgesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BadgesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetStudentBadges(int studentId)
        {
            try
            {
                var studentPathwayIds = await _context.Enrollments
                    .Where(e => e.StudentId == studentId)
                    .Select(e => e.PathwayId)
                    .ToListAsync<int>();

                if (!studentPathwayIds.Any())
                    return Ok(new List<StudentBadgeDto>());

                var pathwayBadges = await _context.Badges
                    .Include(b => b.Pathway)
                    .Where(b => studentPathwayIds.Contains(b.PathwayId))
                    .ToListAsync<Badge>();

                var unlockedUserBadges = await _context.UserBadges
                    .Where(ub => ub.UserId == studentId)
                    .ToListAsync<UserBadge>();

                var badgeDtos = pathwayBadges.Select(b =>
                {
                    var unlockData = unlockedUserBadges.FirstOrDefault(ub => ub.BadgeId == b.Id);
                    bool isUnlocked = unlockData != null;

                    return new StudentBadgeDto
                    {
                        Id = b.Id,
                        // O problema costuma estar nestas ligações quando os dados antigos vêm a null
                        PathwayTitle = b.Pathway?.Title ?? "Percurso Desconhecido",
                        Name = b.Name ?? "Sem Nome",
                        Description = b.Description ?? "",
                        Icon = b.Icon ?? "bi-trophy",
                        Tier = b.Tier,
                        IsUnlocked = isUnlocked,
                        UnlockedAt = isUnlocked ? unlockData!.EarnedAt : null,
                        TriggerType = b.TriggerType,
                        TriggerValue = b.TriggerValue ?? "",
                        ModuleName = b.TriggerType == BadgeTriggerType.ModuleCompletion && int.TryParse(b.TriggerValue, out int moduleId)
                            ? _context.Modules.FirstOrDefault(m => m.Id == moduleId)?.Title
                            : null
                    };
                }).ToList();

                return Ok(badgeDtos.OrderByDescending(b => b.IsUnlocked).ThenBy(b => b.Tier).ToList());
            }
            catch (Exception ex)
            {
                // Força a devolver JSON com a causa exata do crash!
                return StatusCode(500, new
                {
                    Erro = ex.Message,
                    Detalhe = ex.InnerException?.Message,
                    Local = ex.StackTrace?.Substring(0, 200)
                });
            }
        }
    }
}
