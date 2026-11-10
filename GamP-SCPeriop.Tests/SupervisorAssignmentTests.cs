using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Server.Services;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Tests
{
    public class SupervisorAssignmentTests : IDisposable
    {
        private readonly AppDbContext db = TestDb.Create();
        private readonly PathwayService service;
        private readonly User teacher, v1, v2, student;
        private readonly Enrollment enrollment;

        public SupervisorAssignmentTests()
        {
            service = new PathwayService(db);
            teacher = new User { FullName = "Prof", Email = "p@t.pt", Role = UserRole.Teacher };
            v1 = new User { FullName = "Sup Um", Email = "v1@t.pt", Role = UserRole.Supervisor };
            v2 = new User { FullName = "Sup Dois", Email = "v2@t.pt", Role = UserRole.Supervisor };
            student = new User { FullName = "Ana Aluna", Email = "a@t.pt", Role = UserRole.Student };
            db.Users.AddRange(teacher, v1, v2, student);
            db.SaveChanges();

            var pathway = new Pathway { Title = "Bloco", ProfessorId = teacher.Id };
            db.Pathways.Add(pathway);
            db.SaveChanges();

            enrollment = new Enrollment { StudentId = student.Id, PathwayId = pathway.Id };
            db.Enrollments.Add(enrollment);
            db.SaveChanges();
        }

        [Fact]
        public async Task Assigns_and_notifies_new_supervisor()
        {
            var error = await service.AssignSupervisorAsync(enrollment.Id, v1.Id, teacher.Id);

            Assert.Null(error);
            Assert.Equal(v1.Id, (await db.Enrollments.FindAsync(enrollment.Id))!.SupervisorId);
            var notification = await db.Notifications.SingleAsync();
            Assert.Equal(v1.Id, notification.ReceiverId);
            Assert.Equal(teacher.Id, notification.SenderId);
            Assert.Equal($"/evaluate/{enrollment.Id}", notification.TargetUrl);
            Assert.Contains("Ana Aluna", notification.Message);
        }

        [Fact]
        public async Task Rejects_non_supervisor()
        {
            var error = await service.AssignSupervisorAsync(enrollment.Id, teacher.Id, teacher.Id);

            Assert.Equal("O utilizador escolhido não é um supervisor.", error);
            Assert.Null((await db.Enrollments.FindAsync(enrollment.Id))!.SupervisorId);
            Assert.Empty(db.Notifications);
        }

        [Fact]
        public async Task Clearing_does_not_notify()
        {
            enrollment.SupervisorId = v1.Id;
            await db.SaveChangesAsync();

            var error = await service.AssignSupervisorAsync(enrollment.Id, null, teacher.Id);

            Assert.Null(error);
            Assert.Null((await db.Enrollments.FindAsync(enrollment.Id))!.SupervisorId);
            Assert.Empty(db.Notifications);
        }

        [Fact]
        public async Task Changing_resets_warning_level()
        {
            enrollment.MissingSupervisorWarningLevel = 2;
            await db.SaveChangesAsync();

            await service.AssignSupervisorAsync(enrollment.Id, v2.Id, teacher.Id);

            Assert.Equal(0, (await db.Enrollments.FindAsync(enrollment.Id))!.MissingSupervisorWarningLevel);
        }

        [Fact]
        public async Task Same_supervisor_again_does_not_notify_twice()
        {
            await service.AssignSupervisorAsync(enrollment.Id, v1.Id, teacher.Id);
            await service.AssignSupervisorAsync(enrollment.Id, v1.Id, teacher.Id);

            Assert.Single(db.Notifications);
        }

        public void Dispose() => db.Dispose();
    }
}
