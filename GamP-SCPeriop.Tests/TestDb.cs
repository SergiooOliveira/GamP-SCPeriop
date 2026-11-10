using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GamP_SCPeriop.Tests
{
    /// <summary>
    /// Helpers shared by the tests: a fresh in-memory database and a logged-in user
    /// </summary>
    public static class TestDb
    {
        public static AppDbContext Create()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        public static ClaimsPrincipal Principal(int userId, UserRole role) =>
            new(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role.ToString())
            }, "Test"));
    }
}
