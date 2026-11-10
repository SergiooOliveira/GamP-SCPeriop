namespace GamP_SCPeriop.Shared.Entity.Model
{
    /// <summary>
    /// What the logged-in user may do on one student's enrollment (the server decides; screens only follow it)
    /// </summary>
    public class EnrollmentAccessDto
    {
        public bool CanEdit { get; set; }              // pathway structure, dates, supervisor (Teacher/Support)
        public bool CanGrade { get; set; }             // assigned supervisor (or Support), only when a supervisor is set
        public bool CanRequestExtension { get; set; }  // same people who can grade
        public bool HasSupervisor { get; set; }
        public string? SupervisorName { get; set; }
    }
}
