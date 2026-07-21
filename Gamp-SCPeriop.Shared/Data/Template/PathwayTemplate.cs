using System.Collections.Generic;

namespace GamP_SCPeriop.Shared.Data.Template
{
    public class PathwayTemplate
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int MinimumApprovalScore { get; set; } = 100;

        public bool IsAdminBase { get; set; } = true;
        public int? SupervisorOwnerId { get; set; }

        // Relação com os Módulos do Template
        public List<ModuleTemplate> ModuleTemplates { get; set; } = new();
    }
}
