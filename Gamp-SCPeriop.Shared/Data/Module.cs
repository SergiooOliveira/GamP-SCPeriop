using GamP_SCPeriop.Shared.Enum;
using System.ComponentModel.DataAnnotations.Schema;

namespace GamP_SCPeriop.Shared
{
    public class Module
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public List<ModuleComponent> Components { get; set; } = new();
        public int PathwayId { get; set; }

        [NotMapped]
        public int ProgressPercentage
        {
            get
            {
                if (Components == null || !Components.Any()) return 0;

                var completedCount = Components.Count(c =>
                                    c.Status == ComponentStatus.AcimaDaMedia ||
                                    c.Status == ComponentStatus.Consistente);

                return (int)((double)completedCount / Components.Count * 100);
            }
        }

        [NotMapped]
        public string Status => ProgressPercentage == 100 ? "Concluído" : ProgressPercentage > 0 ? "Em curso" : "Por iniciar";
    }
}
