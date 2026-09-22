using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Services
{
    /// <summary>
    /// Which items get graded and how progress is calculated, in one place
    /// (the evaluation page, the progress bars and the "x/y avaliadas" counts all follow these rules).
    /// </summary>
    public static class EvaluationRules
    {
        /// <summary>
        /// What the evaluation page shows as gradable: practical-stage items with a weight,
        /// excluding group headers (their sub-parameters are graded instead). Theory is never graded.
        /// </summary>
        public static bool IsGradable(ModuleComponent component, IEnumerable<ModuleComponent> moduleComponents) =>
            component.Stage != ModuleStage.Teorica
            && component.Weight > 0
            && !moduleComponents.Any(c => c.ParentComponentId == component.Id);

        /// <summary>
        /// Progress only counts the practice stages (assisted and supervised)
        /// </summary>
        public static bool CountsForProgress(ModuleComponent component, IEnumerable<ModuleComponent> moduleComponents) =>
            (component.Stage == ModuleStage.PraticaAssistida || component.Stage == ModuleStage.PraticaSupervisionada)
            && IsGradable(component, moduleComponents);

        /// <summary>
        /// % of practice items graded "Acima da Média" or "Consistente"
        /// </summary>
        public static int CalculateProgress(IReadOnlyCollection<ModuleComponent> components, IEnumerable<ComponentEvaluation> evaluations)
        {
            var countedIds = components.Where(c => CountsForProgress(c, components)).Select(c => c.Id).ToHashSet();
            if (countedIds.Count == 0) return 0;

            int completed = evaluations.Count(e =>
                countedIds.Contains(e.ModuleComponentId) &&
                (e.Status == ComponentStatus.AcimaDaMedia || e.Status == ComponentStatus.Consistente));

            return (int)((double)completed / countedIds.Count * 100);
        }

        /// <summary>
        /// Recomputes an enrollment's stored progress (after grading, or after graded items were deleted)
        /// </summary>
        public static async Task<Enrollment?> RecalculateEnrollmentProgressAsync(AppDbContext context, int enrollmentId)
        {
            var enrollment = await context.Enrollments.FindAsync(enrollmentId);
            if (enrollment == null) return null;

            var components = await context.EnrollmentModules
                .Where(em => em.EnrollmentId == enrollmentId)
                .SelectMany(em => em.Module!.Components)
                .AsNoTracking()
                .ToListAsync();

            var evaluations = await context.ComponentEvaluations
                .Where(ce => ce.EnrollmentId == enrollmentId)
                .AsNoTracking()
                .ToListAsync();

            enrollment.ProgressPercentage = CalculateProgress(components, evaluations);
            return enrollment;
        }
    }
}
