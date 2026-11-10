using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared.Enum
{
    public enum ModuleStage
    {
        Teorica = 1,
        ObservacaoPassiva = 2,
        ObservacaoParticipada = 3,
        PraticaAssistida = 4,
        PraticaSupervisionada = 5,
        //Avaliacao = 6
    }
}

namespace GamP_SCPeriop.Shared.Enum
{
    public static class ModuleStageExtensions
    {
        /// <summary>
        /// Name shown on screen and in notifications
        /// </summary>
        public static string DisplayName(this ModuleStage stage) => stage switch
        {
            ModuleStage.Teorica => "Componente Teórica",
            ModuleStage.ObservacaoPassiva => "Observação Passiva",
            ModuleStage.ObservacaoParticipada => "Observação Participada",
            ModuleStage.PraticaAssistida => "Prática Assistida",
            ModuleStage.PraticaSupervisionada => "Prática Supervisionada",
            _ => stage.ToString()
        };
    }
}
