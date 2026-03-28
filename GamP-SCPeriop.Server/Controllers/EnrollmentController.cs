using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared;
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
    }
}
