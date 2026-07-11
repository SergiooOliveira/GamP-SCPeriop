using GamP_SCPeriop.Shared.Enum;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GamP_SCPeriop.Shared.Data.Template
{
    public class ComponentTemplate
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        // A fase a que pertence (Observação, Prática, etc.)
        public ModuleStage Stage { get; set; }

        public int Weight { get; set; } = 1; // Para o cálculo de percentagens do teu print

        // Chave Estrangeira para o Módulo
        public int ModuleTemplateId { get; set; }

        [JsonIgnore]
        public ModuleTemplate? ModuleTemplate { get; set; }

        // Relação Pai/Filho (Self-referencing) para permitir indentação
        public int? ParentComponentTemplateId { get; set; }

        [JsonIgnore]
        public ComponentTemplate? ParentComponentTemplate { get; set; }
        public List<ComponentTemplate> SubComponents { get; set; } = new();
    }
}
