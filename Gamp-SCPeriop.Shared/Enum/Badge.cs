using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared.Enum
{
    // 1. O Enum modular para os triggers
    public enum BadgeTriggerType
    {
        ModuleCompletion, // Conclusão de Módulo
        ExcellenceGrade,  // Avaliação de Excelência
        SpeedFocus,       // Velocidade/Foco
        PathwayMilestone  // Marco do Percurso
    }

    // 2. O Enum para os Tiers (Raridade)
    public enum BadgeTier
    {
        Common = 1,
        Uncommon = 2,
        Rare = 3,
        Epic = 4,
        Legendary = 5
    }
}
