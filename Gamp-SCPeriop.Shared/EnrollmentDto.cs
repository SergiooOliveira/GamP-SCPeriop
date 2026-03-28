using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared
{
    public class EnrollmentDto
    {
        public int StudentId { get; set; }
        public int ProfessorId { get; set; }
        public int PathwayId { get; set; }
        public int ProgressPercentage { get; set; }
        public DateTime EndDate { get; set; }
    }
}
