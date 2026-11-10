using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Server.Services;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Enum;
using System.Security.Claims;

namespace GamP_SCPeriop.Tests
{
    /// <summary>
    /// Support S, Teachers T1/T2, Supervisors V1/V2, Students A/B.
    /// P1 (T1): eA = A supervised by V1, eB = B without supervisor. P2 (T2): eA2 = A supervised by V2.
    /// Each enrollment has its own module copy; P1 also has a base module.
    /// </summary>
    public class AccessServiceTests : IDisposable
    {
        private readonly AppDbContext db = TestDb.Create();
        private readonly AccessService access;

        private readonly User S, T1, T2, V1, V2, A, B;
        private readonly Pathway P1, P2;
        private readonly Enrollment eA, eB, eA2;
        private readonly Module baseModuleP1, copyA, copyB, copyA2;

        public AccessServiceTests()
        {
            access = new AccessService(db);

            S = AddUser(UserRole.Support); T1 = AddUser(UserRole.Teacher); T2 = AddUser(UserRole.Teacher);
            V1 = AddUser(UserRole.Supervisor); V2 = AddUser(UserRole.Supervisor);
            A = AddUser(UserRole.Student); B = AddUser(UserRole.Student);
            db.SaveChanges();

            P1 = new Pathway { Title = "P1", ProfessorId = T1.Id };
            P2 = new Pathway { Title = "P2", ProfessorId = T2.Id };
            db.Pathways.AddRange(P1, P2);
            db.SaveChanges();

            eA = new Enrollment { StudentId = A.Id, PathwayId = P1.Id, SupervisorId = V1.Id };
            eB = new Enrollment { StudentId = B.Id, PathwayId = P1.Id };
            eA2 = new Enrollment { StudentId = A.Id, PathwayId = P2.Id, SupervisorId = V2.Id };
            db.Enrollments.AddRange(eA, eB, eA2);

            baseModuleP1 = new Module { Title = "Base", PathwayId = P1.Id };
            copyA = new Module { Title = "Copy A" };
            copyB = new Module { Title = "Copy B" };
            copyA2 = new Module { Title = "Copy A2" };
            db.Modules.AddRange(baseModuleP1, copyA, copyB, copyA2);
            db.SaveChanges();

            db.EnrollmentModules.AddRange(
                new EnrollmentModule { EnrollmentId = eA.Id, ModuleId = copyA.Id },
                new EnrollmentModule { EnrollmentId = eB.Id, ModuleId = copyB.Id },
                new EnrollmentModule { EnrollmentId = eA2.Id, ModuleId = copyA2.Id });
            db.SaveChanges();
        }

        private User AddUser(UserRole role)
        {
            var user = new User { FullName = role.ToString(), Email = Guid.NewGuid() + "@t.pt", Role = role, Password = "x" };
            db.Users.Add(user);
            return user;
        }

        private static ClaimsPrincipal As(User user) => TestDb.Principal(user.Id, user.Role);

        [Fact]
        public async Task Teacher_manages_only_own_pathway()
        {
            Assert.True(await access.CanManagePathwayAsync(As(T1), P1.Id));
            Assert.False(await access.CanManagePathwayAsync(As(T1), P2.Id));
            Assert.True(await access.CanManageEnrollmentAsync(As(T1), eA.Id));
            Assert.False(await access.CanManageEnrollmentAsync(As(T1), eA2.Id));
            Assert.True(await access.CanManageModuleAsync(As(T1), copyA.Id));
            Assert.True(await access.CanManageModuleAsync(As(T1), baseModuleP1.Id));
            Assert.False(await access.CanManageModuleAsync(As(T1), copyA2.Id));
        }

        [Fact]
        public async Task Supervisor_never_manages_structure()
        {
            Assert.False(await access.CanManagePathwayAsync(As(V1), P1.Id));
            Assert.False(await access.CanManageEnrollmentAsync(As(V1), eA.Id));
            Assert.False(await access.CanManageModuleAsync(As(V1), copyA.Id));
            Assert.False(await access.CanManageModuleAsync(As(V1), baseModuleP1.Id));
        }

        [Fact]
        public async Task Supervisor_grades_only_assigned()
        {
            Assert.True(await access.CanGradeEnrollmentAsync(As(V1), eA.Id));
            Assert.False(await access.CanGradeEnrollmentAsync(As(V1), eA2.Id));
            Assert.False(await access.CanGradeEnrollmentAsync(As(V2), eA.Id));
        }

        [Fact]
        public async Task Nobody_grades_without_supervisor()
        {
            Assert.False(await access.CanGradeEnrollmentAsync(As(S), eB.Id));
            Assert.False(await access.CanGradeEnrollmentAsync(As(T1), eB.Id));
            Assert.False(await access.CanGradeEnrollmentAsync(As(V1), eB.Id));
        }

        [Fact]
        public async Task Teacher_cannot_grade()
        {
            Assert.False(await access.CanGradeEnrollmentAsync(As(T1), eA.Id));
            Assert.True(await access.CanViewEnrollmentAsync(As(T1), eA.Id));
        }

        [Fact]
        public async Task Teacher_sees_only_students_in_own_pathways()
        {
            Assert.True(await access.CanViewStudentAsync(As(T1), A.Id));
            Assert.True(await access.CanViewStudentAsync(As(T1), B.Id));
            Assert.False(await access.CanViewStudentAsync(As(T2), B.Id));
            Assert.False(await access.CanViewEnrollmentAsync(As(T1), eA2.Id));
        }

        [Fact]
        public async Task Supervisor_sees_only_assigned_students()
        {
            Assert.True(await access.CanViewStudentAsync(As(V1), A.Id));
            Assert.False(await access.CanViewStudentAsync(As(V1), B.Id));
            Assert.True(await access.CanViewEnrollmentAsync(As(V1), eA.Id));
            Assert.False(await access.CanViewEnrollmentAsync(As(V1), eA2.Id));
            Assert.True(await access.CanViewModuleAsync(As(V1), copyA.Id));
            Assert.False(await access.CanViewModuleAsync(As(V1), copyB.Id));
            Assert.True(await access.CanViewPathwayAsync(As(V1), P1.Id));
            Assert.False(await access.CanViewPathwayAsync(As(V1), P2.Id));
            Assert.True(await access.CanViewModuleAsync(As(V1), baseModuleP1.Id));
        }

        [Fact]
        public async Task Reassigning_supervisor_moves_access()
        {
            eA.SupervisorId = V2.Id;
            await db.SaveChangesAsync();

            Assert.False(await access.CanGradeEnrollmentAsync(As(V1), eA.Id));
            Assert.False(await access.CanViewEnrollmentAsync(As(V1), eA.Id));
            Assert.False(await access.CanViewStudentAsync(As(V1), A.Id));
            Assert.True(await access.CanGradeEnrollmentAsync(As(V2), eA.Id));
            Assert.True(await access.CanViewEnrollmentAsync(As(V2), eA.Id));
        }

        [Fact]
        public async Task Support_can_do_everything_but_grade_unassigned()
        {
            Assert.True(await access.CanManagePathwayAsync(As(S), P1.Id));
            Assert.True(await access.CanManagePathwayAsync(As(S), P2.Id));
            Assert.True(await access.CanManageEnrollmentAsync(As(S), eA2.Id));
            Assert.True(await access.CanViewStudentAsync(As(S), B.Id));
            Assert.True(await access.CanViewEnrollmentAsync(As(S), eB.Id));
            Assert.True(await access.CanManageModuleAsync(As(S), copyA2.Id));
            Assert.True(await access.CanGradeEnrollmentAsync(As(S), eA.Id));
            Assert.False(await access.CanGradeEnrollmentAsync(As(S), eB.Id));
        }

        [Fact]
        public async Task Student_sees_only_own()
        {
            Assert.True(await access.CanViewEnrollmentAsync(As(A), eA.Id));
            Assert.False(await access.CanViewEnrollmentAsync(As(A), eB.Id));
            Assert.False(await access.CanViewStudentAsync(As(A), B.Id));
            Assert.True(await access.CanViewModuleAsync(As(A), copyA.Id));
            Assert.False(await access.CanGradeEnrollmentAsync(As(A), eA.Id));
            Assert.False(await access.CanManageEnrollmentAsync(As(A), eA.Id));
        }

        [Fact]
        public async Task Missing_items_return_false()
        {
            const int missing = 9999;
            foreach (var user in new[] { As(S), As(T1), As(V1) })
            {
                Assert.False(await access.CanManagePathwayAsync(user, missing));
                Assert.False(await access.CanViewPathwayAsync(user, missing));
                Assert.False(await access.CanManageEnrollmentAsync(user, missing));
                Assert.False(await access.CanGradeEnrollmentAsync(user, missing));
                Assert.False(await access.CanViewEnrollmentAsync(user, missing));
                Assert.False(await access.CanManageModuleAsync(user, missing));
                Assert.False(await access.CanViewModuleAsync(user, missing));
                Assert.False(await access.CanManageComponentAsync(user, missing));
            }
        }

        public void Dispose() => db.Dispose();
    }
}
