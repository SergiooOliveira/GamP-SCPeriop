using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared.Entity.Model
{
    public class StudentDashboardCardDTO
    {
        public int EnrollmentId { get; set; }
        public int PathwayId { get; set; }        
        public string PathwayTitle { get; set; } = string.Empty;
        public string ProfessorName { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; } // Para as abas (Em Curso / Por Iniciar)
        public DateTime? LimitDate { get; set; } // Para desenhar no cartão
        public int ProgressPercentage { get; set; }
        public int MinimumApprovalScore { get; set; }
        public bool IsStarred { get; set; }
        public bool IsHidden { get; set; }
        public bool IsArchived { get; set; }
        public bool IsFullyEvaluated { get; set; }
    }
}
