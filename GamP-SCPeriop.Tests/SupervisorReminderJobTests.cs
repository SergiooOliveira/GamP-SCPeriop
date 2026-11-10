using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Server.Services;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Tests
{
    public class SupervisorReminderJobTests : IDisposable
    {
        private static readonly DateTime Today = new(2026, 11, 10);

        private readonly AppDbContext db = TestDb.Create();
        private readonly User teacher, supervisor, student;
        private readonly Pathway pathway;

        public SupervisorReminderJobTests()
        {
            teacher = new User { FullName = "Prof Rui", Email = "t@t.pt", Role = UserRole.Teacher };
            supervisor = new User { FullName = "Sup Vera", Email = "v@t.pt", Role = UserRole.Supervisor };
            student = new User { FullName = "Ana Aluna", Email = "a@t.pt", Role = UserRole.Student };
            db.Users.AddRange(teacher, supervisor, student);
            db.SaveChanges();

            pathway = new Pathway { Title = "Bloco", ProfessorId = teacher.Id };
            db.Pathways.Add(pathway);
            db.SaveChanges();
        }

        /// <summary>
        /// An enrollment whose module copy's first stage starts on the given day (null = no dates at all)
        /// </summary>
        private Enrollment AddEnrollment(DateTime? start, int? supervisorId = null, Pathway? inPathway = null, int level = 0)
        {
            var enrollment = new Enrollment
            {
                StudentId = student.Id,
                PathwayId = (inPathway ?? pathway).Id,
                SupervisorId = supervisorId,
                MissingSupervisorWarningLevel = level
            };
            db.Enrollments.Add(enrollment);

            var module = new Module
            {
                Title = "Módulo",
                StageTimelines = new List<ModuleStageTimelineDto>
                {
                    new() { Stage = ModuleStage.ObservacaoPassiva, StartDate = start, EndDate = start?.AddDays(4) },
                    new() { Stage = ModuleStage.PraticaAssistida, StartDate = start?.AddDays(5), EndDate = start?.AddDays(10) }
                }
            };
            db.Modules.Add(module);
            db.SaveChanges();

            db.EnrollmentModules.Add(new EnrollmentModule { EnrollmentId = enrollment.Id, ModuleId = module.Id });
            db.SaveChanges();
            return enrollment;
        }

        private async Task<int> LevelOf(Enrollment e) =>
            (await db.Enrollments.AsNoTracking().SingleAsync(x => x.Id == e.Id)).MissingSupervisorWarningLevel;

        [Fact]
        public async Task Sends_7_day_warning_once()
        {
            var enrollment = AddEnrollment(Today.AddDays(7));

            Assert.Equal(1, await SupervisorReminderJob.RunAsync(db, Today));
            Assert.Equal(1, await LevelOf(enrollment));
            Assert.Equal(0, await SupervisorReminderJob.RunAsync(db, Today));

            var notification = await db.Notifications.SingleAsync();
            Assert.Equal(teacher.Id, notification.ReceiverId);
            Assert.Equal("Estudante sem supervisor", notification.Title);
            Assert.Equal($"/pathway/{pathway.Id}/students", notification.TargetUrl);
            Assert.Contains("17/11/2026", notification.Message);
        }

        [Fact]
        public async Task Sends_1_day_then_start_day()
        {
            var enrollment = AddEnrollment(Today.AddDays(1), level: 1);

            Assert.Equal(1, await SupervisorReminderJob.RunAsync(db, Today));
            Assert.Equal(2, await LevelOf(enrollment));

            Assert.Equal(1, await SupervisorReminderJob.RunAsync(db, Today.AddDays(1)));
            Assert.Equal(3, await LevelOf(enrollment));
            var last = await db.Notifications.OrderByDescending(n => n.Id).FirstAsync();
            Assert.Equal("Percurso não pode começar", last.Title);

            Assert.Equal(0, await SupervisorReminderJob.RunAsync(db, Today.AddDays(5)));
        }

        [Fact]
        public async Task Jumps_straight_to_start_level()
        {
            var enrollment = AddEnrollment(Today.AddDays(-10));

            Assert.Equal(1, await SupervisorReminderJob.RunAsync(db, Today));
            Assert.Equal(3, await LevelOf(enrollment));
            Assert.Single(db.Notifications);
        }

        [Fact]
        public async Task Ignores_far_future_no_dates_archived_or_assigned()
        {
            var archived = new Pathway { Title = "Antigo", ProfessorId = teacher.Id, IsArchived = true };
            db.Pathways.Add(archived);
            db.SaveChanges();

            AddEnrollment(Today.AddDays(20));
            AddEnrollment(null);
            AddEnrollment(Today.AddDays(1), inPathway: archived);
            AddEnrollment(Today.AddDays(1), supervisorId: supervisor.Id);

            Assert.Equal(0, await SupervisorReminderJob.RunAsync(db, Today));
            Assert.Empty(db.Notifications);
        }

        public void Dispose() => db.Dispose();
    }
}
