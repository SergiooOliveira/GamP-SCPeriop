using GamP_SCPeriop.Shared.Enum;

namespace GamP_SCPeriop.Shared.Data
{
    public class Badge
    {
        public int Id { get; set; }

        // Liga à INSTÂNCIA do percurso do aluno, não ao template!
        public int PathwayId { get; set; }
        // public Pathway? Pathway { get; set; } // Descomenta se quiseres a navegação

        // Os dados copiados que ficam congelados no tempo
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public BadgeTier Tier { get; set; } = BadgeTier.Common;
        public BadgeTriggerType TriggerType { get; set; } = BadgeTriggerType.ModuleCompletion;
        public string TriggerValue { get; set; } = string.Empty;
    }
}
