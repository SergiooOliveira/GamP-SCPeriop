using GamP_SCPeriop.Shared.Enum;

namespace GamP_SCPeriop.Shared.Entity.Model
{
    /// <summary>
    /// Everything the development test panel shows (Development environment only)
    /// </summary>
    public class DevOverviewDto
    {
        public List<DevUserDto> Users { get; set; } = new();

        // Shared password of every seeded account
        public string TestPassword { get; set; } = string.Empty;
    }

    public class DevUserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public int BadgesEarned { get; set; }
        public int UnreadNotifications { get; set; }

        // Students only
        public List<DevEnrollmentDto> Enrollments { get; set; } = new();

        // Supervisors only
        public List<DevPathwaySummaryDto> Pathways { get; set; } = new();
    }

    public class DevEnrollmentDto
    {
        public int EnrollmentId { get; set; }
        public int PathwayId { get; set; }
        public string PathwayTitle { get; set; } = string.Empty;
        public string SupervisorName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ProgressPercentage { get; set; }
        public int MinimumApprovalScore { get; set; }
        public int CompletedEvaluations { get; set; }
        public int TotalEvaluations { get; set; }
        public bool IsArchived { get; set; }
    }

    public class DevPathwaySummaryDto
    {
        public int PathwayId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public int AverageProgress { get; set; }
        public bool IsArchived { get; set; }
    }
}
