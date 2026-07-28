using GamP_SCPeriop.Shared.Enum;

namespace GamP_SCPeriop.Shared.Entity.Model
{
    public class BadgeTemplateCreateDto
    {
        public int PathwayTemplateId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "bi-trophy";
        public BadgeTier Tier { get; set; } = BadgeTier.Common;

        public BadgeTriggerType TriggerType { get; set; }

        // Vai guardar o ID do módulo, o nome da fase, ou a percentagem
        public string TriggerValue { get; set; } = string.Empty;
    }
}
