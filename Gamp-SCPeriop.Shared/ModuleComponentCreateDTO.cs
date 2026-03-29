using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared
{
    public class ModuleComponentCreateDTO
    {
        public string Title { get; set; } = string.Empty;
        public int ModuleId { get; set; }
        public string? PdfFilePath { get; set; }
    }
}
