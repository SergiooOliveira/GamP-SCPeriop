using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GamP_SCPeriop.Server.Services
{
    /// <summary>
    /// Role names for [Authorize(Roles = ...)]
    /// </summary>
    public static class Roles
    {
        public const string Support = nameof(UserRole.Support);
        public const string Teacher = nameof(UserRole.Teacher);
        public const string Supervisor = nameof(UserRole.Supervisor);
        public const string Student = nameof(UserRole.Student);
        public const string TeacherOrSupport = Support + "," + Teacher;
        public const string Staff = Support + "," + Teacher + "," + Supervisor;
    }

    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user) =>
            int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        public static bool IsSupport(this ClaimsPrincipal user) => user.IsInRole(Roles.Support);
    }

    /// <summary>
    /// Ownership rules:
    /// - Support can do everything (except grade an enrollment that has no supervisor — nobody can).
    /// - A Teacher owns their pathways (Pathway.ProfessorId): structure, dates, supervisors, extension decisions.
    ///   Their students are the students enrolled in those pathways.
    /// - A Supervisor grades and sees only the enrollments assigned to them (Enrollment.SupervisorId).
    /// - A Student sees only their own data.
    /// Every check returns false when the item does not exist.
    /// </summary>
    public class AccessService
    {
        private readonly AppDbContext _context;

        public AccessService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// The student themselves, Support, a Teacher with the student in one of their pathways,
        /// or a Supervisor with one of the student's enrollments assigned to them
        /// </summary>
        public async Task<bool> CanViewStudentAsync(ClaimsPrincipal user, int studentId)
        {
            var userId = user.GetUserId();
            if (userId == studentId) return await _context.Users.AnyAsync(u => u.Id == studentId);
            if (user.IsSupport()) return await _context.Users.AnyAsync(u => u.Id == studentId);

            if (user.IsInRole(Roles.Teacher))
                return await _context.Enrollments.AnyAsync(e => e.StudentId == studentId && e.Pathway!.ProfessorId == userId);

            if (user.IsInRole(Roles.Supervisor))
                return await _context.Enrollments.AnyAsync(e => e.StudentId == studentId && e.SupervisorId == userId);

            return false;
        }

        /// <summary>
        /// Change the pathway itself: Support, or the Teacher who owns it
        /// </summary>
        public async Task<bool> CanManagePathwayAsync(ClaimsPrincipal user, int pathwayId)
        {
            if (user.IsSupport()) return await _context.Pathways.AnyAsync(p => p.Id == pathwayId);

            var userId = user.GetUserId();
            return user.IsInRole(Roles.Teacher)
                && await _context.Pathways.AnyAsync(p => p.Id == pathwayId && p.ProfessorId == userId);
        }

        /// <summary>
        /// Read a pathway: whoever manages it, or a Supervisor with a student of theirs in it
        /// </summary>
        public async Task<bool> CanViewPathwayAsync(ClaimsPrincipal user, int pathwayId)
        {
            if (await CanManagePathwayAsync(user, pathwayId)) return true;

            var userId = user.GetUserId();
            return user.IsInRole(Roles.Supervisor)
                && await _context.Enrollments.AnyAsync(e => e.PathwayId == pathwayId && e.SupervisorId == userId);
        }

        /// <summary>
        /// Change a student's plan, dates and supervisor, decide extension requests: Support or the pathway's Teacher
        /// </summary>
        public async Task<bool> CanManageEnrollmentAsync(ClaimsPrincipal user, int enrollmentId)
        {
            if (user.IsSupport()) return await _context.Enrollments.AnyAsync(e => e.Id == enrollmentId);

            var userId = user.GetUserId();
            return user.IsInRole(Roles.Teacher)
                && await _context.Enrollments.AnyAsync(e => e.Id == enrollmentId && e.Pathway!.ProfessorId == userId);
        }

        /// <summary>
        /// Grade (and ask for extensions): the assigned Supervisor, or Support. Never while there is no supervisor.
        /// </summary>
        public async Task<bool> CanGradeEnrollmentAsync(ClaimsPrincipal user, int enrollmentId)
        {
            var userId = user.GetUserId();
            var isSupport = user.IsSupport();
            var isSupervisor = user.IsInRole(Roles.Supervisor);
            if (!isSupport && !isSupervisor) return false;

            return await _context.Enrollments.AnyAsync(e =>
                e.Id == enrollmentId
                && e.SupervisorId != null
                && (isSupport || e.SupervisorId == userId));
        }

        /// <summary>
        /// The enrolled student, whoever manages the enrollment, or its assigned Supervisor
        /// </summary>
        public async Task<bool> CanViewEnrollmentAsync(ClaimsPrincipal user, int enrollmentId)
        {
            var userId = user.GetUserId();
            if (await _context.Enrollments.AnyAsync(e => e.Id == enrollmentId && e.StudentId == userId)) return true;
            if (await CanManageEnrollmentAsync(user, enrollmentId)) return true;

            return user.IsInRole(Roles.Supervisor)
                && await _context.Enrollments.AnyAsync(e => e.Id == enrollmentId && e.SupervisorId == userId);
        }

        /// <summary>
        /// The enrollments of one student that this user may see: all for the student and Support,
        /// only those in their own pathways for a Teacher, only the assigned ones for a Supervisor
        /// </summary>
        public async Task<List<int>> VisibleEnrollmentIdsAsync(ClaimsPrincipal user, int studentId)
        {
            var userId = user.GetUserId();
            var all = user.IsSupport() || userId == studentId;
            var isTeacher = user.IsInRole(Roles.Teacher);
            var isSupervisor = user.IsInRole(Roles.Supervisor);

            return await _context.Enrollments
                .Where(e => e.StudentId == studentId)
                .Where(e => all
                    || (isTeacher && e.Pathway!.ProfessorId == userId)
                    || (isSupervisor && e.SupervisorId == userId))
                .Select(e => e.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Base modules belong to a pathway; student copies belong to an enrollment. Either way the owner manages it.
        /// </summary>
        public async Task<bool> CanManageModuleAsync(ClaimsPrincipal user, int moduleId)
        {
            var owner = await FindModuleOwnerAsync(moduleId);
            if (owner == null) return false;

            if (owner.Value.PathwayId.HasValue) return await CanManagePathwayAsync(user, owner.Value.PathwayId.Value);
            if (owner.Value.EnrollmentId.HasValue) return await CanManageEnrollmentAsync(user, owner.Value.EnrollmentId.Value);

            // A module that belongs to neither a pathway nor an enrollment is orphaned: only Support touches it
            return user.IsSupport();
        }

        /// <summary>
        /// The student whose enrollment holds this module copy, whoever can see that enrollment, or whoever can see the pathway
        /// </summary>
        public async Task<bool> CanViewModuleAsync(ClaimsPrincipal user, int moduleId)
        {
            var owner = await FindModuleOwnerAsync(moduleId);
            if (owner == null) return false;

            if (owner.Value.PathwayId.HasValue) return await CanViewPathwayAsync(user, owner.Value.PathwayId.Value);
            if (owner.Value.EnrollmentId.HasValue) return await CanViewEnrollmentAsync(user, owner.Value.EnrollmentId.Value);

            return user.IsSupport();
        }

        public async Task<bool> CanManageComponentAsync(ClaimsPrincipal user, int componentId)
        {
            var moduleId = await _context.ModuleComponents
                .Where(c => c.Id == componentId)
                .Select(c => (int?)c.ModuleId)
                .FirstOrDefaultAsync();

            return moduleId.HasValue && await CanManageModuleAsync(user, moduleId.Value);
        }

        /// <returns>null when the module does not exist</returns>
        private async Task<(int? PathwayId, int? EnrollmentId)?> FindModuleOwnerAsync(int moduleId)
        {
            var module = await _context.Modules
                .Where(m => m.Id == moduleId)
                .Select(m => new { m.PathwayId })
                .FirstOrDefaultAsync();

            if (module == null) return null;
            if (module.PathwayId.HasValue) return (module.PathwayId, null);

            var enrollmentId = await _context.EnrollmentModules
                .Where(em => em.ModuleId == moduleId)
                .Select(em => (int?)em.EnrollmentId)
                .FirstOrDefaultAsync();

            return (null, enrollmentId);
        }
    }
}
