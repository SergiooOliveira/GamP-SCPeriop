using GamP_SCPeriop.Server.Services;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Tests
{
    public class SupportAccountBootstrapperTests
    {
        [Fact]
        public async Task Creates_support_when_none_exists()
        {
            using var db = TestDb.Create();

            var changed = await SupportAccountBootstrapper.EnsureAsync(db, "suporte@gamp.pt", "uma-password-longa", "Suporte");

            Assert.True(changed);
            var user = await db.Users.SingleAsync();
            Assert.Equal(UserRole.Support, user.Role);
            Assert.Equal("suporte@gamp.pt", user.Email);
            Assert.True(BCrypt.Net.BCrypt.Verify("uma-password-longa", user.Password));
        }

        [Fact]
        public async Task Promotes_existing_account_with_same_email()
        {
            using var db = TestDb.Create();
            var existing = new User { Email = "Prof@Gamp.pt", FullName = "Prof", Role = UserRole.Teacher, Password = "hash-antigo" };
            db.Users.Add(existing);
            await db.SaveChangesAsync();

            var changed = await SupportAccountBootstrapper.EnsureAsync(db, "prof@gamp.pt", "uma-password-longa", null);

            Assert.True(changed);
            var user = await db.Users.SingleAsync();
            Assert.Equal(existing.Id, user.Id);
            Assert.Equal(UserRole.Support, user.Role);
            Assert.Equal("hash-antigo", user.Password);
        }

        [Fact]
        public async Task Does_nothing_when_a_support_exists()
        {
            using var db = TestDb.Create();
            db.Users.Add(new User { Email = "a@gamp.pt", Role = UserRole.Support, Password = "x" });
            await db.SaveChangesAsync();

            var changed = await SupportAccountBootstrapper.EnsureAsync(db, "b@gamp.pt", "uma-password-longa", null);

            Assert.False(changed);
            Assert.Equal(1, await db.Users.CountAsync());
        }

        [Theory]
        [InlineData(null, "uma-password-longa")]
        [InlineData("a@gamp.pt", null)]
        [InlineData("a@gamp.pt", "1234567")]
        public async Task Does_nothing_without_config_or_short_password(string? email, string? password)
        {
            using var db = TestDb.Create();

            var changed = await SupportAccountBootstrapper.EnsureAsync(db, email, password, null);

            Assert.False(changed);
            Assert.Equal(0, await db.Users.CountAsync());
        }
    }
}
