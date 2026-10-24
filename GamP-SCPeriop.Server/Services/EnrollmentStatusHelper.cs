namespace GamP_SCPeriop.Server.Services
{
    /// <summary>
    /// Decides the status label shown for a student's enrollment in a pathway
    /// </summary>
    public static class EnrollmentStatusHelper
    {
        public const string EmCurso = "Em curso";
        public const string PorIniciar = "Por iniciar";
        public const string Pendente = "Pendente";
        public const string Aprovado = "Concluído (Aprovado)";
        public const string Reprovado = "Concluído (Reprovado)";

        public static string GetStatus(
            int progressPercentage,
            bool isArchived,
            int minimumApprovalScore,
            int totalModules,
            int scheduledModules,
            int totalEvaluations,
            int completedEvaluations,
            DateTime? minStartDate,
            DateTime? maxEndDate,
            DateTime now)
        {
            bool hasModules = totalModules > 0;
            bool isFullyScheduled = hasModules && totalModules == scheduledModules;

            bool hasStartedEvaluations = completedEvaluations > 0;
            bool isFullyEvaluated = (totalEvaluations > 0 && completedEvaluations == totalEvaluations) || progressPercentage >= 100;

            // REGRA 1 — dates are authoritative, but only once every module is actually scheduled
            if (isFullyEvaluated
                || isArchived
                || (isFullyScheduled && maxEndDate.HasValue && maxEndDate < now))
            {
                var minScore = minimumApprovalScore > 0 ? minimumApprovalScore : 65;
                return progressPercentage >= minScore ? Aprovado : Reprovado;
            }

            // REGRA 2 — not fully scheduled, but work has started
            if (hasStartedEvaluations) return EmCurso;

            // REGRA 3 — not fully scheduled, nothing started
            if (!hasModules || !isFullyScheduled) return Pendente;

            if (minStartDate.HasValue && minStartDate > now) return PorIniciar;

            return EmCurso;
        }

        /// <summary>
        /// Sort order used when listing a student's pathways
        /// </summary>
        public static int GetSortOrder(string status) => status switch
        {
            EmCurso => 1,
            PorIniciar => 2,
            Pendente => 3,
            Aprovado => 4,
            _ => 5 // Concluído (Reprovado) ou outros
        };
    }
}
