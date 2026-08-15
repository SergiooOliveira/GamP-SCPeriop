namespace GamP_SCPeriop.Shared.Data
{
    public class Enrollment
    {
        public int Id { get; set; }

        public int StudentId { get; set; }
        public User? Student { get; set; }

        public int PathwayId { get; set; }
        public Pathway? Pathway { get; set; }

        public int ProgressPercentage { get; set; } = 0;

        // Preferências do Aluno para a sua Dashboard
        public bool IsStarred { get; set; } = false;
        public bool IsHidden { get; set; } = false;
    }
}