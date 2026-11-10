using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Services
{
    /// <summary>
    /// Warns the Teacher about students whose pathway is about to start (or should have started) with no supervisor:
    /// 7 days before, 1 day before, and on/after the start date. Each warning is sent once per enrollment.
    /// </summary>
    public static class SupervisorReminderJob
    {
        public const int NoWarning = 0, WeekBefore = 1, DayBefore = 2, StartDay = 3;

        /// <returns>How many notifications were sent</returns>
        public static async Task<int> RunAsync(AppDbContext db, DateTime today)
        {
            today = today.Date;

            var candidates = await db.Enrollments
                .Where(e => e.SupervisorId == null && !e.Pathway!.IsArchived && e.MissingSupervisorWarningLevel < StartDay)
                .Select(e => new
                {
                    Enrollment = e,
                    TeacherId = e.Pathway!.ProfessorId,
                    PathwayTitle = e.Pathway.Title,
                    StudentName = e.Student!.FullName,
                    // The first stage of any of the student's module copies
                    Start = db.EnrollmentModules
                        .Where(em => em.EnrollmentId == e.Id)
                        .SelectMany(em => em.Module!.StageTimelines!)
                        .Where(t => t.StartDate != null)
                        .Min(t => t.StartDate)
                })
                .ToListAsync();

            var sent = 0;
            foreach (var c in candidates.Where(c => c.Start != null))
            {
                var start = c.Start!.Value.Date;
                var days = (start - today).Days;
                var needed = days <= 0 ? StartDay : days <= 1 ? DayBefore : days <= 7 ? WeekBefore : NoWarning;
                if (needed <= c.Enrollment.MissingSupervisorWarningLevel) continue;

                var cannotStart = needed == StartDay;
                db.Notifications.Add(new Notification
                {
                    ReceiverId = c.TeacherId,
                    SenderId = c.TeacherId, // system message: there is no other sender
                    Title = cannotStart ? "Percurso não pode começar" : "Estudante sem supervisor",
                    Message = cannotStart
                        ? $"O percurso \"{c.PathwayTitle}\" de {c.StudentName} devia ter começado a {start:dd/MM/yyyy}, mas não tem supervisor atribuído."
                        : $"{c.StudentName} começa \"{c.PathwayTitle}\" a {start:dd/MM/yyyy} e ainda não tem supervisor atribuído.",
                    TargetUrl = $"/pathway/{c.Enrollment.PathwayId}/students",
                    CreatedAt = DateTime.UtcNow
                });
                c.Enrollment.MissingSupervisorWarningLevel = needed;
                sent++;
            }

            if (sent > 0) await db.SaveChangesAsync();
            return sent;
        }
    }
}
