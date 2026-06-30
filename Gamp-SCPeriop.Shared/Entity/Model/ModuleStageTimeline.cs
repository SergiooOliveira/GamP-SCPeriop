using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Enum;

namespace GamP_SCPeriop.Shared.Entity.Model
{
    public class ModuleStageTimeline
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ModuleId { get; set; }

        [ForeignKey("ModuleId")]
        public Module? Module { get; set; }

        [Required]
        public ModuleStage Stage { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);
    }
}
