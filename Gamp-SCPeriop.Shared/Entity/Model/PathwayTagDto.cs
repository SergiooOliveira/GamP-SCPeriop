using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared.Entity.Model
{
    public class PathwayTagDto
    {
        public int PathwayId { get; set; }
        public int EnrollmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        // Supervisor of this enrollment (null = none assigned yet)
        public int? SupervisorId { get; set; }
        public string? SupervisorName { get; set; }
    }
}
