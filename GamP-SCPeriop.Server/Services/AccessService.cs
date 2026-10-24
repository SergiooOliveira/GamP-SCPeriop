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
        public const string Admin = nameof(UserRole.Admin);
        public const string Supervisor = nameof(UserRole.Supervisor);
        public const string Student = nameof(UserRole.Supervisionado);
        public const string Staff = Admin + "," + Supervisor;
    }

    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user) =>
            int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(Roles.Admin);
    }

    /// <summary>
    /// Ownership rules: admins can access everything, supervisors only their own pathways
    /// (and the enrollments/modules inside them), students only their own data.
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
        /// The user themselves, an admin, or a supervisor with this student in one of their pathways
        /// </summary>
        public async Task<bool> CanViewStudentAsync(ClaimsPrincipal user, int studentId)
        {
            if (user.IsAdmin() || user.GetUserId() == studentId) return true;
            if (!user.IsInRole(Roles.Supervisor)) return false;

            var supervisorId = user.GetUserId();
            return await _context.Enrollments.AnyAsync(e => e.StudentId == studentId && e.Pathway!.ProfessorId == supervisorId);
        }

        public async Task<bool> CanManagePathwayAsync(ClaimsPrincipal user, int pathwayId)
        {
            if (user.IsAdmin()) return await _context.Pathways.AnyAsync(p => p.Id == pathwayId);

            var userId = user.GetUserId();
            return user.IsInRole(Roles.Supervisor)
                && await _context.Pathways.AnyAsync(p => p.Id == pathwayId && p.ProfessorId == userId);
        }

        /// <summary>
        /// Supervisor of the enrollment's pathway (or admin): edit the plan, grade, etc.
        /// </summary>
        public async Task<bool> CanManageEnrollmentAsync(ClaimsPrincipal user, int enrollmentId)
        {
            if (user.IsAdmin()) return await _context.Enrollments.AnyAsync(e => e.Id == enrollmentId);

            var userId = user.GetUserId();
            return user.IsInRole(Roles.Supervisor)
                && await _context.Enrollments.AnyAsync(e => e.Id == enrollmentId && e.Pathway!.ProfessorId == userId);
        }

        /// <summary>
        /// The enrolled student themselves, or whoever manages the enrollment
        /// </summary>
        public async Task<bool> CanViewEnrollmentAsync(ClaimsPrincipal user, int enrollmentId)
        {
            var userId = user.GetUserId();
            return await _context.Enrollments.AnyAsync(e => e.Id == enrollmentId && e.StudentId == userId)
                || await CanManageEnrollmentAsync(user, enrollmentId);
        }

        /// <summary>
        /// Base modules belong to a pathway; student copies belong to an enrollment. Either way the pathway's supervisor manages it.
        /// </summary>
        public async Task<bool> CanManageModuleAsync(ClaimsPrincipal user, int moduleId)
        {
            var module = await _context.Modules
                .Where(m => m.Id == moduleId)
                .Select(m => new { m.PathwayId })
                .FirstOrDefaultAsync();

            if (module == null) return false;
            if (module.PathwayId.HasValue) return await CanManagePathwayAsync(user, module.PathwayId.Value);

            var enrollmentId = await _context.EnrollmentModules
                .Where(em => em.ModuleId == moduleId)
                .Select(em => (int?)em.EnrollmentId)
                .FirstOrDefaultAsync();

            // A module that belongs to neither a pathway nor an enrollment is orphaned: only admins touch it
            return enrollmentId.HasValue
                ? await CanManageEnrollmentAsync(user, enrollmentId.Value)
                : user.IsAdmin();
        }

        /// <summary>
        /// Managers of the module, or the student whose enrollment holds this module copy
        /// </summary>
        public async Task<bool> CanViewModuleAsync(ClaimsPrincipal user, int moduleId)
        {
            var userId = user.GetUserId();
            return await _context.EnrollmentModules.AnyAsync(em => em.ModuleId == moduleId && em.Enrollment!.StudentId == userId)
                || await CanManageModuleAsync(user, moduleId);
        }

        public async Task<bool> CanManageComponentAsync(ClaimsPrincipal user, int componentId)
        {
            var moduleId = await _context.ModuleComponents
                .Where(c => c.Id == componentId)
                .Select(c => (int?)c.ModuleId)
                .FirstOrDefaultAsync();

            return moduleId.HasValue && await CanManageModuleAsync(user, moduleId.Value);
        }
    }
}
