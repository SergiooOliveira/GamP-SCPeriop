namespace GamP_SCPeriop.Shared
{
    public class Module
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

        public int EnrollmentId { get; set; }
        public List<ModuleComponent> Components { get; set; } = new();
    }
}
