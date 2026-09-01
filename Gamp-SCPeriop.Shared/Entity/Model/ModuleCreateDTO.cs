using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared.Entity.Model
{
    public class ModuleCreateDto
    {
        public string Title { get; set; } = string.Empty;
        public int? PathwayId { get; set; }
        public int? EnrollmentId { get; set; }
        public float Weight { get; set; }
        public int OrderIndex { get; set; } = 0;
    }
}
