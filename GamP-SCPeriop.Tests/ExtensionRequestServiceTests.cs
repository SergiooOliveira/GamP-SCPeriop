using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Server.Services;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Tests
{
    /// <summary>
    /// Teacher T owns the pathway; supervisor V grades student A; A's module copy has a Prática Assistida stage ending in 3 days.
    /// </summary>
    public class ExtensionRequestServiceTests : IDisposable
    {
        private static readonly DateTime Today = DateTime.Today;

        private readonly AppDbContext db = TestDb.Create();
        private readonly ExtensionRequestService service;
        private readonly User teacher, supervisor, student;
        private readonly Enrollment enrollment;
        private readonly Module copy, otherCopy;
        private readonly ModuleStageTimelineDto timeline;

        public ExtensionRequestServiceTests()
        {
            service = new ExtensionRequestService(db);

            teacher = new User { FullName = "Prof Rui", Email = "t@t.pt", Role = UserRole.Teacher };
            supervisor = new User { FullName = "Sup Vera", Email = "v@t.pt", Role = UserRole.Supervisor };
            student = new User { FullName = "Ana Aluna", Email = "a@t.pt", Role = UserRole.Student };
            db.Users.AddRange(teacher, supervisor, student);
            db.SaveChanges();

            var pathway = new Pathway { Title = "Bloco", ProfessorId = teacher.Id };
            db.Pathways.Add(pathway);
            db.SaveChanges();

            enrollment = new Enrollment { StudentId = student.Id, PathwayId = pathway.Id, SupervisorId = supervisor.Id };
            var otherEnrollment = new Enrollment { StudentId = student.Id, PathwayId = pathway.Id, SupervisorId = supervisor.Id };
            db.Enrollments.AddRange(enrollment, otherEnrollment);

            timeline = new ModuleStageTimelineDto { Stage = ModuleStage.PraticaAssistida, StartDate = Today.AddDays(-5), EndDate = Today.AddDays(3) };
            copy = new Module { Title = "Instrumentação", StageTimelines = new List<ModuleStageTimelineDto> { timeline } };
            otherCopy = new Module { Title = "Outro", StageTimelines = new List<ModuleStageTimelineDto>
            {
                new() { Stage = ModuleStage.PraticaAssistida, StartDate = Today, EndDate = Today.AddDays(3) }
            } };
            db.Modules.AddRange(copy, otherCopy);
            db.SaveChanges();

            db.EnrollmentModules.AddRange(
                new EnrollmentModule { EnrollmentId = enrollment.Id, ModuleId = copy.Id },
                new EnrollmentModule { EnrollmentId = otherEnrollment.Id, ModuleId = otherCopy.Id });
            db.SaveChanges();
        }

        private ExtensionRequestCreateDto Request(int days, int? moduleId = null) => new()
        {
            EnrollmentId = enrollment.Id,
            ModuleId = moduleId ?? copy.Id,
            Stage = ModuleStage.PraticaAssistida,
            RequestedEndDate = Today.AddDays(days),
            Reason = "Faltou a dois turnos"
        };

        private async Task<ExtensionRequestDto> CreateValidAsync(int days = 8)
        {
            var (result, error) = await service.CreateAsync(Request(days), supervisor.Id);
            Assert.Null(error);
            return result!;
        }

        private async Task<DateTime?> CurrentEndAsync() =>
            (await db.ModuleStageTimelines.AsNoTracking().SingleAsync(t => t.Id == timeline.Id)).EndDate;

        [Fact]
        public async Task Create_valid_request_notifies_teacher()
        {
            var request = await CreateValidAsync();

            Assert.Equal(ExtensionRequestStatus.Pending, request.Status);
            Assert.Equal(Today.AddDays(3), request.CurrentEndDate);
            Assert.Equal("Ana Aluna", request.StudentName);
            var notification = await db.Notifications.SingleAsync();
            Assert.Equal(teacher.Id, notification.ReceiverId);
            Assert.Equal(supervisor.Id, notification.SenderId);
            Assert.Equal("/extension-requests", notification.TargetUrl);
            Assert.Contains("Prática Assistida", notification.Message);
        }

        [Fact]
        public async Task Create_rejects_date_not_after_current_end()
        {
            var (_, error) = await service.CreateAsync(Request(3), supervisor.Id);

            Assert.Equal("A nova data tem de ser posterior à data de fim atual.", error);
            Assert.Empty(db.DeadlineExtensionRequests);
        }

        [Fact]
        public async Task Create_rejects_second_pending_for_same_stage()
        {
            var first = await CreateValidAsync();

            var (_, error) = await service.CreateAsync(Request(10), supervisor.Id);
            Assert.Equal("Já existe um pedido pendente para esta fase.", error);

            await service.DecideAsync(first.Id, new ExtensionDecisionDto { Approve = false }, teacher.Id);
            var (again, errorAfter) = await service.CreateAsync(Request(10), supervisor.Id);
            Assert.Null(errorAfter);
            Assert.NotNull(again);
        }

        [Fact]
        public async Task Create_rejects_module_from_another_enrollment()
        {
            var (_, error) = await service.CreateAsync(Request(8, otherCopy.Id), supervisor.Id);

            Assert.NotNull(error);
            Assert.Empty(db.DeadlineExtensionRequests);
        }

        [Fact]
        public async Task Approve_sets_requested_date_and_notifies_supervisor()
        {
            var request = await CreateValidAsync(8);

            var (result, error) = await service.DecideAsync(request.Id, new ExtensionDecisionDto { Approve = true }, teacher.Id);

            Assert.Null(error);
            Assert.Equal(ExtensionRequestStatus.Approved, result!.Status);
            Assert.Equal(Today.AddDays(8), result.DecidedEndDate);
            Assert.Equal(Today.AddDays(8), await CurrentEndAsync());
            var toSupervisor = await db.Notifications.SingleAsync(n => n.ReceiverId == supervisor.Id);
            Assert.Equal("Prolongamento aprovado", toSupervisor.Title);
            Assert.Equal($"/evaluate/{enrollment.Id}", toSupervisor.TargetUrl);
        }

        [Fact]
        public async Task Approve_moves_module_end_date_when_later()
        {
            var link = await db.EnrollmentModules.SingleAsync(em => em.ModuleId == copy.Id);
            link.StartDate = Today.AddDays(-5);
            link.EndDate = Today.AddDays(3);
            await db.SaveChangesAsync();

            var request = await CreateValidAsync(8);
            await service.DecideAsync(request.Id, new ExtensionDecisionDto { Approve = true }, teacher.Id);

            Assert.Equal(Today.AddDays(8), (await db.EnrollmentModules.AsNoTracking().SingleAsync(em => em.ModuleId == copy.Id)).EndDate);
        }

        [Fact]
        public async Task Approve_with_other_date_uses_teacher_date()
        {
            var request = await CreateValidAsync(8);

            var (result, _) = await service.DecideAsync(request.Id,
                new ExtensionDecisionDto { Approve = true, EndDate = Today.AddDays(6) }, teacher.Id);

            Assert.Equal(Today.AddDays(6), result!.DecidedEndDate);
            Assert.Equal(Today.AddDays(6), await CurrentEndAsync());
        }

        [Fact]
        public async Task Approve_reopens_locked_stage()
        {
            timeline.EndDate = Today.AddDays(-1);
            await db.SaveChangesAsync();
            Assert.True(EvaluationWindow.HasEnded(await CurrentEndAsync(), Today));

            var request = await CreateValidAsync(4);
            await service.DecideAsync(request.Id, new ExtensionDecisionDto { Approve = true }, teacher.Id);

            Assert.False(EvaluationWindow.HasEnded(await CurrentEndAsync(), Today));
        }

        [Fact]
        public async Task Approve_checks_current_end_not_stored_one()
        {
            var request = await CreateValidAsync(8);
            timeline.EndDate = Today.AddDays(10); // the teacher moved the date meanwhile
            await db.SaveChangesAsync();

            var (_, error) = await service.DecideAsync(request.Id, new ExtensionDecisionDto { Approve = true }, teacher.Id);

            Assert.Equal("A nova data tem de ser posterior à data de fim atual.", error);
            Assert.Equal(Today.AddDays(10), await CurrentEndAsync());
            Assert.Equal(ExtensionRequestStatus.Pending, (await db.DeadlineExtensionRequests.SingleAsync()).Status);
        }

        [Fact]
        public async Task Reject_keeps_dates_and_stores_note()
        {
            var request = await CreateValidAsync(8);

            var (result, error) = await service.DecideAsync(request.Id,
                new ExtensionDecisionDto { Approve = false, Note = "O estágio termina nessa semana." }, teacher.Id);

            Assert.Null(error);
            Assert.Equal(ExtensionRequestStatus.Rejected, result!.Status);
            Assert.Equal("O estágio termina nessa semana.", result.DecisionNote);
            Assert.Equal(Today.AddDays(3), await CurrentEndAsync());
            Assert.Equal("Prolongamento recusado", (await db.Notifications.SingleAsync(n => n.ReceiverId == supervisor.Id)).Title);
        }

        [Fact]
        public async Task Cannot_decide_twice()
        {
            var request = await CreateValidAsync(8);
            await service.DecideAsync(request.Id, new ExtensionDecisionDto { Approve = true }, teacher.Id);

            var (_, error) = await service.DecideAsync(request.Id, new ExtensionDecisionDto { Approve = false }, teacher.Id);

            Assert.Equal("Este pedido já foi decidido.", error);
        }

        public void Dispose() => db.Dispose();
    }
}
