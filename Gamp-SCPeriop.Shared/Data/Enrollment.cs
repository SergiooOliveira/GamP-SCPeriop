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

        // Internship supervisor who grades this student (chosen by the teacher). No supervisor = cannot start.
        public int? SupervisorId { get; set; }
        public User? Supervisor { get; set; }

        // Missing-supervisor warnings already sent to the teacher: 0 none, 1 = 7 days before, 2 = 1 day before, 3 = start day
        public int MissingSupervisorWarningLevel { get; set; } = 0;

        // Preferências do Aluno para a sua Dashboard
        public bool IsStarred { get; set; } = false;
        public bool IsHidden { get; set; } = false;
    }
}