using GamP_SCPeriop.Shared.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared.Entity.Model
{
    public class BuilderComponentDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ModuleStage Stage { get; set; }
        public int Weight { get; set; } = 1;
        public int? ParentComponentId { get; set; }
        public string? PdfFilePath { get; set; }
    }
}
