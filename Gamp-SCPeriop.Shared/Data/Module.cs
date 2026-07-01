using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Shared.Enum;
using System.ComponentModel.DataAnnotations.Schema;

namespace GamP_SCPeriop.Shared.Data
{
    public class Module
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public List<ModuleComponent> Components { get; set; } = new();
        public int PathwayId { get; set; }
        public List<ModuleStageTimeline>? StageTimelines { get; set; }
        public int Weight { get; set; } = 1;

        [NotMapped]
        public int ProgressPercentage
        {
            get
            {
                var praticas = Components?.Where(c => c.Stage != ModuleStage.Teorica &&
                                                    (c.SubComponents == null || !c.SubComponents.Any())).ToList();

                if (praticas == null || !praticas.Any()) return 0;

                var completedCount = praticas.Count(c =>
                            c.Status == ComponentStatus.AcimaDaMedia ||
                            c.Status == ComponentStatus.Consistente);

                return (int)((double)completedCount / praticas.Count * 100);
            }
        }

        [NotMapped]
        public string Status
        {
            get
            {
                if (ProgressPercentage == 100) return "Concluído";

                // Verifica se há alguma componente prática que já tenha sido avaliada (ou seja, diferente de Pendente)
                bool hasStarted = Components != null &&
                                  Components.Any(c => c.Stage != ModuleStage.Teorica && c.Status != ComponentStatus.Pending);

                return hasStarted || ProgressPercentage > 0 ? "Em curso" : "Por iniciar";
            }
        }
    }
}
