using GamP_SCPeriop.Shared;

namespace GamP_SCPeriop.Helpers
{
    public static class UIHelpers
    {
        public static string GetBadgeCss(ComponentStatus status) => status switch
        {
            ComponentStatus.Approved => "bg-success-subtle text-success border-success-subtle",
            ComponentStatus.Passed => "bg-warning-subtle text-warning border-warning-subtle",
            ComponentStatus.InProgress => "bg-primary-subtle text-primary border-primary-subtle",
            _ => "bg-light text-muted border-light"
        };

        public static string GetBadgeText(ComponentStatus status) => status switch
        {
            ComponentStatus.Approved => "Aprovado",
            ComponentStatus.Passed => "Aprovado",
            ComponentStatus.InProgress => "Em curso",
            _ => "Por iniciar"
        };
    }
}
