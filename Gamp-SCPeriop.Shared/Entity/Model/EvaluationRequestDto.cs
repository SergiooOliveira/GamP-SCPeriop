using GamP_SCPeriop.Shared.Enum;

namespace GamP_SCPeriop.Shared.Entity.Model
{
    public class EvaluationRequestDto
    {
        public int EnrollmentId { get; set; }
        public int ModuleComponentId { get; set; }
        public ComponentStatus Status { get; set; }
    }
}
