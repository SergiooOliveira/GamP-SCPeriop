using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared.Entity.Model
{
    public class PathwayCreateDTO
    {
        public string Title { get; set; } = string.Empty;
        public int MinimumPassScore { get; set; }
        public int MinimumApprovalScore { get; set; }
        public int ProfessorId { get; set; }
    }
}
