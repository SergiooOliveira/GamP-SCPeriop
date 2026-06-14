using GamP_SCPeriop.Shared.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared.Entity.Model
{
    public class ModuleComponentCreateDTO
    {
        public int ModuleId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description {  get; set; }
        public string? PdfFilePath { get; set; }
        public ModuleStage Stage { get; set; }
        public int? ParentComponentId { get; set; }
    }
}
