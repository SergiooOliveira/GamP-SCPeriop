namespace GamP_SCPeriop.Shared.Data
{
    public class Enrollment
    {
        public int Id { get; set; }

        public int StudentId { get; set; }
        public User? Student { get; set; }

        public int ProfessorId { get; set; }
        public User? Professor { get; set; }

        public int PathwayId { get; set; }
        public Pathway? Pathway { get; set; }

        public int ProgressPercentage { get; set; } = 0;
        public DateTime EndDate { get; set; }
    }
}
