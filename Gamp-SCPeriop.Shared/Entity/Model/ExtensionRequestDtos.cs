using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Enum;
using System.ComponentModel.DataAnnotations;

namespace GamP_SCPeriop.Shared.Entity.Model
{
    /// <summary>
    /// Supervisor asks for a later end date on one stage of a student's module
    /// </summary>
    public class ExtensionRequestCreateDto
    {
        public int EnrollmentId { get; set; }
        public int ModuleId { get; set; }
        public ModuleStage Stage { get; set; }
        public DateTime RequestedEndDate { get; set; }

        [MaxLength(DeadlineExtensionRequest.MaxTextLength)]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Teacher's answer: approve (with the requested date, or EndDate if given) or reject
    /// </summary>
    public class ExtensionDecisionDto
    {
        public bool Approve { get; set; }
        public DateTime? EndDate { get; set; }

        [MaxLength(DeadlineExtensionRequest.MaxTextLength)]
        public string? Note { get; set; }
    }

    public class ExtensionRequestDto
    {
        public int Id { get; set; }
        public int EnrollmentId { get; set; }
        public int ModuleId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string PathwayTitle { get; set; } = string.Empty;
        public string ModuleTitle { get; set; } = string.Empty;
        public ModuleStage Stage { get; set; }
        public string RequestedByName { get; set; } = string.Empty;
        public DateTime CurrentEndDate { get; set; }
        public DateTime RequestedEndDate { get; set; }
        public string? Reason { get; set; }
        public ExtensionRequestStatus Status { get; set; }
        public DateTime? DecidedEndDate { get; set; }
        public string? DecisionNote { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
