using GamP_SCPeriop.Shared.Enum;

namespace GamP_SCPeriop.Tests
{
    public class UserRoleTests
    {
        [Theory]
        [InlineData(UserRole.Support, "Suporte")]
        [InlineData(UserRole.Teacher, "Professor")]
        [InlineData(UserRole.Supervisor, "Supervisor")]
        [InlineData(UserRole.Student, "Estudante")]
        public void DisplayName_is_portuguese(UserRole role, string expected) =>
            Assert.Equal(expected, role.DisplayName());

        // Roles are stored as numbers: these must never change, or existing accounts change role
        [Fact]
        public void Enum_values_are_stable()
        {
            Assert.Equal(0, (int)UserRole.Supervisor);
            Assert.Equal(1, (int)UserRole.Student);
            Assert.Equal(2, (int)UserRole.Support);
            Assert.Equal(3, (int)UserRole.Teacher);
        }
    }
}
