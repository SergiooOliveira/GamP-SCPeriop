using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GamP_SCPeriop.Shared.Data.Template
{
    public class ModuleTemplate
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

        // Chave Estrangeira para o PathwayTemplate
        public int PathwayTemplateId { get; set; }

        [JsonIgnore]
        public PathwayTemplate? PathwayTemplate { get; set; }

        // Relação com as Tarefas do Template
        public List<ComponentTemplate> ComponentTemplates { get; set; } = new();
    }
}
