using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Shared.Enum;
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
                .Where(e => e.Pathway.ProfessorId == supervisorId)
                .ToListAsync();

            return Ok(enrollments);
        }

        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<List<StudentDashboardCardDTO>>> GetStudentEnrollments(int studentId)
        {
            var enrollmentsModules = await _context.EnrollmentModules
                .Include(en => en.Enrollment)
                    .ThenInclude(e => e.Pathway)
                        .ThenInclude(p => p.Professor)                            
                .Where(en => en.Enrollment != null && en.Enrollment.StudentId == studentId)
                .ToListAsync();

            if (!enrollmentsModules.Any()) return Ok(new List<StudentDashboardCardDTO>());

            var dashboardCards = enrollmentsModules
                .GroupBy(em => em.EnrollmentId)
                .Select(group =>
                {
                    var line = group.First();
                    return new StudentDashboardCardDTO
                    {
                        EnrollmentId = line.EnrollmentId,
                        PathwayId = line.Enrollment?.PathwayId ?? 0,
                        PathwayTitle = line.Enrollment?.Pathway?.Title ?? "Sem título",
                        ProfessorName = line.Enrollment?.Pathway?.Professor?.DisplayShortName ?? "Sem supervisor",
                        StartDate = group.Min(em => em.StartDate),
                        LimitDate = group.Max(em => em.EndDate),
                        ProgressPercentage = line.Enrollment?.ProgressPercentage ?? 0,
                        MinimumApprovalScore = line.Enrollment?.Pathway?.MinimumApprovalScore ?? 65,
                        IsStarred = line.Enrollment?.IsStarred ?? false,
                        IsHidden = line.Enrollment?.IsHidden ?? false,
                        IsArchived = line.Enrollment?.Pathway?.IsArchived ?? false,
                        IsFullyEvaluated = (line.Enrollment?.ProgressPercentage ?? 0) == 100
                    };                    
                })
                .ToList();

            return Ok(dashboardCards);
        }

        [HttpGet("student/{studentId}/pathway/{pathwayId}")]
        public async Task<ActionResult<IEnumerable<EnrollmentModule>>> GetEnrollmentDetails(int studentId, int pathwayId)
        {
            var enrollmentDetails = await _context.EnrollmentModules
                .Include(em => em.Enrollment)
                    .ThenInclude(e => e.Pathway)
                        .ThenInclude(p => p.Professor)
                .Include(em => em.Module)
                    .ThenInclude(m => m.Components)
                .Where (em => em.Enrollment != null
                        && em.Enrollment.StudentId == studentId
                        && em.Enrollment.PathwayId == pathwayId)
                .ToListAsync();

            if (!enrollmentDetails.Any()) return NotFound();

            return Ok(enrollmentDetails);
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
                        Title = e.Pathway.Title,
                        Status = e.ProgressPercentage >= 100 ? "Concluido" : "Em curso"
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
            var pathway = await _context.Pathways.FindAsync(dto.PathwayId);

            if (pathway == null)
            {
                return NotFound("Percurso não encontrado.");
            }

            if (pathway.StartDate == null || pathway.EndDate == null)
            {
                return BadRequest("Não é possível inscrever alunos. O percurso precisa de ter datas de início e fim definidas.");
            }

            var alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.StudentId == dto.StudentId && e.PathwayId == dto.PathwayId);

            if (alreadyEnrolled)
            {
                return BadRequest("O aluno já se encontra inscrito neste percurso."); // 400 Bad Request
            }

            // Map the DTO to your real Entity
            var enrollment = new Enrollment
            {
                StudentId = dto.StudentId,
                PathwayId = dto.PathwayId,
                ProgressPercentage = 0,
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            var pathwayModules = await _context.Modules
                .Where(m => m.PathwayId == dto.PathwayId)
                .ToListAsync();

            foreach (var module in pathwayModules)
            {
                var enrollmentModule = new EnrollmentModule
                {
                    EnrollmentId = enrollment.Id,
                    ModuleId = module.Id,
                    StartDate = null,
                    EndDate = null,
                };
                _context.EnrollmentModules.Add(enrollmentModule);
            }

            await _context.SaveChangesAsync();

            return Ok(enrollment);
        }
        #endregion

        #region HttpPut
        [HttpPut("{id}/star")]
        public async Task<IActionResult> ToggleStar(int id, [FromBody] bool isStarred)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null)
            {
                return NotFound("Inscrição não encontrada.");
            }

            enrollment.IsStarred = isStarred;
            await _context.SaveChangesAsync();

            return NoContent(); // 204 Sucesso sem devolver conteúdo
        }

        [HttpPut("{id}/hidden")]
        public async Task<IActionResult> ToggleHidden(int id, [FromBody] bool isHidden)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null)
            {
                return NotFound("Inscrição não encontrada.");
            }

            enrollment.IsHidden = isHidden;
            await _context.SaveChangesAsync();

            return NoContent(); // 204 Sucesso sem devolver conteúdo
        }
        #endregion
    }
}
