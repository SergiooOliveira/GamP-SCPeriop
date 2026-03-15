using System.ComponentModel.DataAnnotations;

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
    }
}
