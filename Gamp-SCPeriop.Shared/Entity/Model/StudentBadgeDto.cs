using GamP_SCPeriop.Shared.Enum;

namespace GamP_SCPeriop.Shared.Entity.Model
{
    public class StudentBadgeDto
    {
        public int Id { get; set; }
        public string PathwayTitle { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public BadgeTier Tier { get; set; } // Para desenharmos as cores certas
        public bool IsUnlocked { get; set; }
        public DateTime? UnlockedAt { get; set; }
    }
}
