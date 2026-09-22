using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Services
{
    public class BadgeService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BadgeService> _logger;

        public BadgeService(AppDbContext context, ILogger<BadgeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task EvaluateModuleBadgeAsync(int studentId, int professorId, int moduleId, int pathwayId, float moduleProgress)
        {
            if (moduleProgress < 65) return;

            var moduleBadge = await _context.Badges
                .FirstOrDefaultAsync(b => 
                    b.PathwayId == pathwayId &&
                    b.TriggerType == BadgeTriggerType.ModuleCompletion &&
                    b.TriggerValue == moduleId.ToString());

            if (moduleBadge == null) return;

            bool alreadyHasModuleBadge = await _context.UserBadges
                .AnyAsync(ub => ub.UserId == studentId && ub.BadgeId == moduleBadge.Id);

            if (alreadyHasModuleBadge) return;

            _logger.LogInformation("Badge {Badge} awarded to student {StudentId} (module {ModuleId}).", moduleBadge.Name, studentId, moduleId);

            _context.UserBadges.Add(new UserBadge
            {
                UserId = studentId,
                BadgeId = moduleBadge.Id,
                EarnedAt = DateTime.UtcNow
            });

            _context.Notifications.Add(new Notification
            {
                ReceiverId = studentId,
                SenderId = professorId,
                Title = "Nova Conquista! 🏆",
                Message = $"Desbloqueaste a badge '{moduleBadge.Name}' ao concluíres o módulo!",
                TargetUrl = "/badges",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });

            await _context.SaveChangesAsync();            
        }

        public async Task EvaluatePathwayBadgeAsync(int studentId, int professorId, int pathwayId, float pathwayProgress)
        {
            if (pathwayProgress < 100) return;

            var pathwayBadge = await _context.Badges
                .FirstOrDefaultAsync(b =>
                    b.PathwayId == pathwayId &&
                    b.TriggerType == BadgeTriggerType.PathwayMilestone);

            if (pathwayBadge == null) return;

            bool alreadyHasPathwayBadge = await _context.UserBadges
                .AnyAsync(ub => ub.UserId == studentId && ub.BadgeId == pathwayBadge.Id);

            if (alreadyHasPathwayBadge) return;

            _logger.LogInformation("Badge {Badge} awarded to student {StudentId} (pathway {PathwayId}).", pathwayBadge.Name, studentId, pathwayId);

            _context.UserBadges.Add(new UserBadge
            {
                UserId = studentId,
                BadgeId = pathwayBadge.Id,
                EarnedAt = DateTime.UtcNow
            });

            _context.Notifications.Add(new Notification
            {
                ReceiverId = studentId,
                SenderId = professorId,
                Title = "Nova Conquista! 🏆",
                Message = $"Desbloqueaste a badge '{pathwayBadge.Name}'",
                TargetUrl = "/badges",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });

            await _context.SaveChangesAsync();
        }
    }
}
