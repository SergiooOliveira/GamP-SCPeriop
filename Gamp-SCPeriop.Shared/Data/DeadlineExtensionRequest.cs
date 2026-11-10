using GamP_SCPeriop.Shared.Enum;
using System.ComponentModel.DataAnnotations;

namespace GamP_SCPeriop.Shared.Data
{
    /// <summary>
    /// A supervisor asks the pathway's teacher for a later end date on one stage of one student's module
    /// </summary>
    public class DeadlineExtensionRequest
    {
        public const int MaxTextLength = 500;

        public int Id { get; set; }

        public int EnrollmentId { get; set; }
        public Enrollment? Enrollment { get; set; }

        // The student's own copy of the module (each enrollment has its own modules and dates)
        public int ModuleId { get; set; }
        public ModuleStage Stage { get; set; }

        public int RequestedById { get; set; }
        public DateTime CurrentEndDate { get; set; }
        public DateTime RequestedEndDate { get; set; }

        [MaxLength(MaxTextLength)]
        public string? Reason { get; set; }

        public ExtensionRequestStatus Status { get; set; } = ExtensionRequestStatus.Pending;

        public DateTime? DecidedEndDate { get; set; }
        public int? DecidedById { get; set; }
        public DateTime? DecidedAt { get; set; }

        [MaxLength(MaxTextLength)]
        public string? DecisionNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
