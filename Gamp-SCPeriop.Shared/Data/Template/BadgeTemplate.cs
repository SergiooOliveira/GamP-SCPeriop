using GamP_SCPeriop.Shared.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared.Data.Template
{
    // 3. O Modelo do Cartão
    public class BadgeTemplate
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PathwayTemplateId { get; set; }

        // Visuais
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "bi-trophy"; // Ícone Bootstrap por defeito
        public string ColorCode { get; set; } = "#ffffff";
        public BadgeTier Tier { get; set; } = BadgeTier.Common;

        // Regras (Modulares)
        public BadgeTriggerType TriggerType { get; set; }

        // O segredo da modularidade: guarda o ID do módulo, os dias, ou a % consoante o TriggerType
        public string TriggerValue { get; set; } = string.Empty;
    }
}
