using GamP_SCPeriop.Shared.Enum;
using System.ComponentModel.DataAnnotations.Schema;

namespace GamP_SCPeriop.Shared.Data
{
    public class ModuleComponent
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int ModuleId { get; set; }
        public string? Description { get; set; }
        public string? PdfFilePath { get; set; }
        public ModuleStage Stage { get; set; }

        [NotMapped]
        public ComponentStatus Status { get; set; } = ComponentStatus.Pending;
    }
}
