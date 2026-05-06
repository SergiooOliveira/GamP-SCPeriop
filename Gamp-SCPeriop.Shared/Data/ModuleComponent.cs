using GamP_SCPeriop.Shared.Enum;

namespace GamP_SCPeriop.Shared
{
    public class ModuleComponent
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int ModuleId { get; set; }
        public ComponentStatus Status { get; set; } = ComponentStatus.Pending;
        public string? PdfFilePath { get; set; }
        public ModuleStage Stage { get; set; }
    }
}
