using GamP_SCPeriop.Server.Services;

namespace GamP_SCPeriop.Tests
{
    public class EnrollmentStatusHelperTests
    {
        private static readonly DateTime Now = new(2026, 11, 10);

        // Fully scheduled pathway (2 of 2 modules), 10 evaluations, given start/end dates
        private static string Status(bool hasSupervisor, DateTime start, DateTime end,
            int completed = 0, int progress = 0, bool archived = false) =>
            EnrollmentStatusHelper.GetStatus(progress, archived, 80, 2, 2, 10, completed, start, end, hasSupervisor, Now);

        [Fact]
        public void No_supervisor_overrides_por_iniciar_and_em_curso()
        {
            Assert.Equal(EnrollmentStatusHelper.SemSupervisor, Status(false, Now.AddDays(5), Now.AddDays(30)));
            Assert.Equal(EnrollmentStatusHelper.SemSupervisor, Status(false, Now.AddDays(-5), Now.AddDays(30)));
            Assert.Equal(EnrollmentStatusHelper.SemSupervisor, Status(false, Now.AddDays(-5), Now.AddDays(30), completed: 3, progress: 30));
        }

        [Fact]
        public void Archived_or_fully_evaluated_still_finishes()
        {
            Assert.Equal(EnrollmentStatusHelper.Reprovado, Status(false, Now.AddDays(-5), Now.AddDays(30), archived: true));
            Assert.Equal(EnrollmentStatusHelper.Aprovado, Status(false, Now.AddDays(-5), Now.AddDays(30), completed: 10, progress: 90));
        }

        [Fact]
        public void With_supervisor_behaves_as_before()
        {
            Assert.Equal(EnrollmentStatusHelper.PorIniciar, Status(true, Now.AddDays(5), Now.AddDays(30)));
            Assert.Equal(EnrollmentStatusHelper.EmCurso, Status(true, Now.AddDays(-5), Now.AddDays(30)));
            Assert.Equal(EnrollmentStatusHelper.EmCurso, Status(true, Now.AddDays(-5), Now.AddDays(30), completed: 3, progress: 30));
        }

        [Fact]
        public void Sort_order_places_sem_supervisor_third()
        {
            var ordered = new[]
            {
                EnrollmentStatusHelper.Reprovado, EnrollmentStatusHelper.SemSupervisor, EnrollmentStatusHelper.Aprovado,
                EnrollmentStatusHelper.EmCurso, EnrollmentStatusHelper.Pendente, EnrollmentStatusHelper.PorIniciar
            }.OrderBy(EnrollmentStatusHelper.GetSortOrder);

            Assert.Equal(new[]
            {
                EnrollmentStatusHelper.EmCurso, EnrollmentStatusHelper.PorIniciar, EnrollmentStatusHelper.SemSupervisor,
                EnrollmentStatusHelper.Pendente, EnrollmentStatusHelper.Aprovado, EnrollmentStatusHelper.Reprovado
            }, ordered);
        }
    }
}
