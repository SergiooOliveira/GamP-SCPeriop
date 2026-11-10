using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared.Entity.Model
{
    public class EnrollmentDto
    {
        public int StudentId { get; set; }
        public int PathwayId { get; set; }

        // Optional: the supervisor who will grade this student (can also be set later)
        public int? SupervisorId { get; set; }
    }
}