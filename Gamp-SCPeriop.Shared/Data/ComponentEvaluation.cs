using GamP_SCPeriop.Shared.Enum;

namespace GamP_SCPeriop.Shared.Data
{
    public class ComponentEvaluation
    {
        public int Id { get; set; }

        // A que inscrição/aluno pertence esta nota?
        public int EnrollmentId { get; set; }
        public Enrollment? Enrollment { get; set; }

        // Qual foi a componente avaliada?
        public int ModuleComponentId { get; set; }
        public ModuleComponent? ModuleComponent { get; set; }

        // Qual foi a avaliação dada pelo professor?
        public ComponentStatus Status { get; set; } = ComponentStatus.Pending;

        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
    }
}
