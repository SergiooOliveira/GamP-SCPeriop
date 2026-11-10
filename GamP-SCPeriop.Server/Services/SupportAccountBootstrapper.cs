using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Services
{
    /// <summary>
    /// Makes sure a Support account exists, from configuration (no default password baked into the code).
    /// If an account with that email already exists it is promoted instead of creating a duplicate.
    /// </summary>
    public static class SupportAccountBootstrapper
    {
        public const int MinPasswordLength = 8;

        /// <returns>true when an account was created or promoted</returns>
        public static async Task<bool> EnsureAsync(AppDbContext db, string? email, string? password, string? fullName)
        {
            if (await db.Users.AnyAsync(u => u.Role == UserRole.Support)) return false;
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength) return false;

            var normalizedEmail = email.Trim().ToLower();
            var existing = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (existing != null)
            {
                existing.Role = UserRole.Support;
            }
            else
            {
                db.Users.Add(new User
                {
                    Email = email.Trim(),
                    FullName = string.IsNullOrWhiteSpace(fullName) ? "Suporte" : fullName,
                    Password = BCrypt.Net.BCrypt.HashPassword(password),
                    Role = UserRole.Support
                });
            }

            await db.SaveChangesAsync();
            return true;
        }
    }
}
