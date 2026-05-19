namespace GamP_SCPeriop.Shared.Data
{
    public class Notification
    {
        public int Id { get; set; }

        // Quem recebe a notificação (O Aluno)
        public int ReceiverId { get; set; }

        // Quem enviou a notificação (O Docente)
        public int SenderId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;

        // Ex: "/module/3" -> para onde o aluno vai ao clicar
        public string TargetUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}