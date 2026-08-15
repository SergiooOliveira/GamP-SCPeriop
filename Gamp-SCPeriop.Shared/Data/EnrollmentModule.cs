using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared.Data
{
    public class EnrollmentModule
    {
        public int Id { get; set; }

        public int EnrollmentId { get; set; }
        public Enrollment? Enrollment { get; set; }

        public int ModuleId { get; set; }
        public Module? Module { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
