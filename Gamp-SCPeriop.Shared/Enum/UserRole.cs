namespace GamP_SCPeriop.Shared.Enum
{
    /// <summary>
    /// Stored as a number: never change the values (existing accounts would change role)
    /// </summary>
    public enum UserRole
    {
        Supervisor = 0, // internship supervisor: grades the students assigned to them
        Student = 1,
        Support = 2,    // support/maintenance: can do everything
        Teacher = 3     // school professor: owns pathways, dates, templates, badges
    }

    public static class UserRoleExtensions
    {
        /// <summary>
        /// Name shown on screen
        /// </summary>
        public static string DisplayName(this UserRole role) => role switch
        {
            UserRole.Support => "Suporte",
            UserRole.Teacher => "Professor",
            UserRole.Supervisor => "Supervisor",
            UserRole.Student => "Estudante",
            _ => role.ToString()
        };

        /// <summary>
        /// True for the exact role names used in login tokens. Old tokens ("Admin", "Supervisionado") are not accepted.
        /// </summary>
        public static bool IsKnownRoleName(string? name) =>
            !string.IsNullOrEmpty(name) && System.Enum.GetNames<UserRole>().Contains(name);
    }
}
