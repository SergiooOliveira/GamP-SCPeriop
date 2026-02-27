namespace GamP_SCPeriop.Shared
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public bool IsAdmin { get; set; }
        public bool IsSupervisor { get; set; }
        public bool IsSupervised { get; set; }
    }
}
