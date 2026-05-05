using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared;
using GamP_SCPeriop.Shared.Entity.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrollmentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EnrollmentController(AppDbContext context)
        {
            _context = context;
        }

        #region HttpGet
        [HttpGet("supervisor/{supervisorId}")]
        public async Task<ActionResult<List<Enrollment>>> GetSupervisorEnrollments(int supervisorId)
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Pathway)
                .Where(e => e.ProfessorId == supervisorId)
                .ToListAsync();

            if (!enrollments.Any())
            {
                return NotFound("Nenhum aluno encontrado para este supervisor.");
            }

            return Ok(enrollments);
        }

        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<List<Enrollment>>> GetStudentEnrollments(int studentId)
        {
            var enrollments = await _context.Enrollments
                .Where(e => e.StudentId == studentId)
                .Include(e => e.Student)
                .Include(e => e.Pathway)
                    .ThenInclude(p => p.Modules) // Critical for your dropdown accordion!
                .ToListAsync();

            if (!enrollments.Any())
            {
                // Returning an empty list is better than a 404 for the UI, 
                // so Blazor can cleanly show the "Ainda não estás inscrito" message.
                return Ok(new List<Enrollment>());
            }

            return Ok(enrollments);
        }

        [HttpGet("management")]
        public async Task<ActionResult<List<StudentManagementDto>>> GetAllStudentsForManagement()
        {
            var studentsQuery = await _context.Users
                .Where(u => u.Role == UserRole.Supervisionado) // Ensure we only grab actual students
                .Select(student => new StudentManagementDto
                {
                    StudentId = student.Id,
                    FullName = student.FullName,
                    Email = student.Email,

                    // If you track logins in your DB, map it here. Otherwise, leave null.
                    LastAccess = null,

                    // 1. Grab their active pathways and format them as tags
                    ActivePathways = student.Enrollments.Select(e => new PathwayTagDto
                    {
                        PathwayId = e.Pathway.Id,
                        Title = e.Pathway.Title
                    }).ToList(),

                    // 2. Safely calculate the average progress (prevents divide-by-zero errors)
                    OverallProgress = student.Enrollments.Any()
                        ? (int)student.Enrollments.Average(e => e.ProgressPercentage)
                        : 0
                })
                .ToListAsync();

            return Ok(studentsQuery);
        }
        #endregion

        #region HttpPost
        [HttpPost]
        public async Task<ActionResult<Enrollment>> CreateEnrollment(EnrollmentDto dto)
        {
            // Map the DTO to your real Entity
            var enrollment = new Enrollment
            {
                StudentId = dto.StudentId,
                ProfessorId = dto.ProfessorId,
                PathwayId = dto.PathwayId,
                ProgressPercentage = dto.ProgressPercentage,
                EndDate = dto.EndDate
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return Ok(enrollment);
        }
        #endregion
    }
}
