using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GamP_SCPeriop.Shared
{
    public class Pathway
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public int MinimumPassScore { get; set; } = 50;
        public int MinimumApprovalScore { get; set; } = 80;

        public List<Module> Modules { get; set; } = new();

        [NotMapped]
        public int TotalProgress
        {
            get
            {
                if (Modules == null || !Modules.Any()) return 0;

                return (int)Modules.Average(m => m.ProgressPercentage);
            }
        }

        [NotMapped]
        public string Status => TotalProgress == 100 ? "Concluído" : TotalProgress > 0 ? "Em curso" : "Por iniciar";
    }
}
