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
                .Include(e => e.Professor)
                .Include(e => e.Pathway)
                    .ThenInclude(p => p.Modules)
                        .ThenInclude(m => m.Components)
                .Where(e => e.StudentId == studentId)
                .ToListAsync();

            foreach (var enrollment in enrollments)
            {
                var evaluations = await _context.ComponentEvaluations
                    .Where(ce => ce.EnrollmentId == enrollment.Id)
                    .ToListAsync();

                if (enrollment.Pathway?.Modules != null)
                {
                    int totalPraticasPathway = 0;
                    int concluidasPathway = 0;

                    foreach (var module in enrollment.Pathway.Modules)
                    {
                        if (module.Components != null)
                        {
                            foreach (var comp in module.Components)
                            {
                                // 1. FORÇAR RESET: Se não houver nota, passa a Pendente explicitamente
                                var eval = evaluations.FirstOrDefault(e => e.ModuleComponentId == comp.Id);
                                comp.Status = eval?.Status ?? ComponentStatus.Pending;

                                // 2. Contabilização global (Todos os módulos, todos os componentes práticos)
                                if (comp.Stage != ModuleStage.Teorica)
                                {
                                    totalPraticasPathway++;
                                    if (comp.Status == ComponentStatus.AcimaDaMedia || comp.Status == ComponentStatus.Consistente)
                                    {
                                        concluidasPathway++;
                                    }
                                }
                            }
                        }
                    }

                    // 3. Sobrescreve o valor antigo com a matemática real antes de enviar para o ecrã
                    enrollment.ProgressPercentage = totalPraticasPathway > 0
                        ? (int)((double)concluidasPathway / totalPraticasPathway * 100)
                        : 0;
                }
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
