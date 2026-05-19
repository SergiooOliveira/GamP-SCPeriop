using GamP_SCPeriop.Shared.Enum;

namespace GamP_SCPeriop.Helpers
{
    public static class UIHelpers
    {
        public static string GetBadgeText(ComponentStatus status) => status switch
        {
            ComponentStatus.Pending => "Pendente",
            ComponentStatus.EmProgresso => "Em Curso",
            ComponentStatus.Inconsistente => "Inconsistente",
            ComponentStatus.AbaixoDaMedia => "Abaixo da Média",
            ComponentStatus.AcimaDaMedia => "Acima da Média",
            ComponentStatus.Consistente => "Consistente",
            _ => status.ToString()
        };

        public static string GetBadgeCss(ComponentStatus status) => status switch
        {
            ComponentStatus.Pending => "bg-secondary text-white",
            ComponentStatus.EmProgresso => "bg-primary text-white",
            ComponentStatus.Inconsistente => "bg-danger text-white", // Vermelho
            ComponentStatus.AbaixoDaMedia => "bg-warning text-dark border-warning", // Laranja/Amarelo forte
            ComponentStatus.AcimaDaMedia => "bg-warning bg-opacity-50 text-dark", // Amarelo claro
            ComponentStatus.Consistente => "bg-success text-white", // Verde
            _ => "bg-light text-dark"
        };
    }
}
