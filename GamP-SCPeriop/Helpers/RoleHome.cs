using GamP_SCPeriop.Shared.Enum;

namespace GamP_SCPeriop.Helpers
{
    /// <summary>
    /// The page each role lands on after logging in
    /// </summary>
    public static class RoleHome
    {
        public static string For(UserRole role) => role switch
        {
            UserRole.Support => "/admin/dashboard",
            UserRole.Teacher => "/dashboard",
            UserRole.Supervisor => "/dashboard",
            UserRole.Student => "/my-profile",
            _ => "/"
        };
    }
}
