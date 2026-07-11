using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared.Entity.Model
{
    public class BuilderModuleDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;

        // A lista universal de componentes
        public List<BuilderComponentDto> Components { get; set; } = new();

        // Mantemos a timeline aqui. Se for Template, o componente simplesmente ignora isto.
        public List<ModuleStageTimeline>? StageTimelines { get; set; }
    }
}
