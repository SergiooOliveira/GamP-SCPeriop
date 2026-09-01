using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Services
{
    public class BadgeService
    {
        private readonly AppDbContext _context;

        public BadgeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task EvaluateModuleBadgeAsync(int studentId, int professorId, int moduleId, int pathwayId, float moduleProgress)
        {
            Console.WriteLine($"Testing Module Badge Evaluation for\n" +
                $"studentId: {studentId}\n" +
                $"professorId: {professorId}\n" +
                $"moduleId: {moduleId}\n" +
                $"pathwayId: {pathwayId}\n" +
                $"moduleProgress: {moduleProgress}");

            if (moduleProgress < 65) return;

            //if (moduleProgress >= 85)
            //{

            //}

            var allBadges = await _context.Badges.Where(b => b.PathwayId == pathwayId).ToListAsync();
            Console.WriteLine($"--- DIAGNÓSTICO: Encontradas {allBadges.Count} badges para o Pathway {pathwayId} ---");
            foreach (var b in allBadges)
            {
                Console.WriteLine($"- Nome: {b.Name} | Tipo: {(int)b.TriggerType} ({b.TriggerType}) | Valor: '{b.TriggerValue}'");
            }
            Console.WriteLine("--------------------------------------------------");

            var moduleBadge = await _context.Badges
                .FirstOrDefaultAsync(b => 
                    b.PathwayId == pathwayId &&
                    b.TriggerType == BadgeTriggerType.ModuleCompletion &&
                    b.TriggerValue == moduleId.ToString());

            Console.WriteLine($"Module Badge Query Result: {moduleBadge?.Name ?? "No Badge Found"}");

            if (moduleBadge == null) return;

            bool alreadyHasModuleBadge = await _context.UserBadges
                .AnyAsync(ub => ub.UserId == studentId && ub.BadgeId == moduleBadge.Id);

            Console.WriteLine($"Module Badge Found: {moduleBadge.Name}, Already Has Badge: {alreadyHasModuleBadge}");

            if (alreadyHasModuleBadge) return;

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
