using GamP_SCPeriop.Helpers;
using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Server.Services;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = Roles.Staff)]
    public class PathwayController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AccessService _access;
        private readonly PathwayService _pathways;

        public PathwayController(AppDbContext context, AccessService access, PathwayService pathways)
        {
            _context = context;
            _access = access;
            _pathways = pathways;
        }

        #region Subject Management

        [HttpPost]
        [Authorize(Roles = Roles.TeacherOrSupport)]
        public async Task<ActionResult<Pathway>> CreatePathway(PathwayCreateDto dto)
        {
            // Teachers always own the pathways they create; Support creates one for a chosen Teacher
            if (!User.IsSupport()) dto.ProfessorId = User.GetUserId();
            else if (!await IsTeacherAsync(dto.ProfessorId))
                return BadRequest("Escolha o professor responsável pelo percurso.");

            // Criação (e cópia do molde, se escolhido) partilhada com o gerador de dados de teste
            var pathway = await _pathways.CreatePathwayAsync(dto);
            return Ok(pathway);
        }

        #endregion

        /// <summary>
        /// Teachers/Support who manage it, or a Supervisor with a student in it (read-only for them)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Pathway>> GetPathway(int id)
        {
            if (!await _access.CanViewPathwayAsync(User, id)) return Forbid();

            var pathway = await _context.Pathways
                .Include(p => p.Modules)
                    .ThenInclude(m => m.Components)
                .Include(p => p.Modules)
                    .ThenInclude(m => m.StageTimelines)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pathway == null) return NotFound();

            return Ok(pathway);
        }

        /// <summary>
        /// Active pathways the user owns (Teacher), or every active pathway (Support)
        /// </summary>
        [HttpGet("owned")]
        [Authorize(Roles = Roles.TeacherOrSupport)]
        public async Task<ActionResult<List<PathwayTagDto>>> GetOwnedPathways()
        {
            var userId = User.GetUserId();
            var isSupport = User.IsSupport();

            var pathways = await _context.Pathways
                .Where(p => (isSupport || p.ProfessorId == userId) && !p.IsArchived)
                .Select(p => new PathwayTagDto
                {
                    PathwayId = p.Id,
                    Title = p.Title
                })
                .ToListAsync();

            return Ok(pathways);
        }

        [HttpGet("{pathwayId}/enrollments")]
        [Authorize(Roles = Roles.TeacherOrSupport)]
        public async Task<ActionResult<List<Enrollment>>> GetPathwayEnrollments(int pathwayId)
        {
            if (!await _access.CanManagePathwayAsync(User, pathwayId)) return Forbid();

            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Supervisor)
                .Where(e => e.PathwayId == pathwayId)
                .ToListAsync();

            return Ok(enrollments);
        }

        /// <summary>
        /// One student's copy of the pathway: editable by its Teacher, read-only for its Supervisor
        /// </summary>
        [HttpGet("builder/{id}")]
        public async Task<ActionResult<IEnumerable<EnrollmentModule>>> GetStudentPathwayBuilder(int id)
        {
            if (!await _access.CanViewEnrollmentAsync(User, id)) return Forbid();

            var studentModules = await _context.EnrollmentModules
                .Include(em => em.Enrollment)
                    .ThenInclude(e => e.Student)
                .Include(em => em.Enrollment)
                    .ThenInclude(e => e.Pathway)
                .Include(em => em.Module)
                    .ThenInclude(m => m.Components)
                .Include(em => em.Module)
                    .ThenInclude(m => m.StageTimelines)
                .Where(em => em.EnrollmentId == id)
                .ToListAsync();

            if (!studentModules.Any())
            {
                return NotFound();
            }

            return Ok(studentModules);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.TeacherOrSupport)]
        public async Task<IActionResult> DeletePathway(int id)
        {
            if (!await _access.CanManagePathwayAsync(User, id)) return Forbid();

            var pathway = await _context.Pathways.FindAsync(id);
            if (pathway == null) return NotFound();

            // Em vez de apagar, arquivamos!
            // Assim o aluno continua com os dados intactos, mas podes filtrar isto nas listas do professor.
            pathway.IsArchived = true;

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("{id}/title")]
        [Authorize(Roles = Roles.TeacherOrSupport)]
        public async Task<IActionResult> UpdatePathwayTitle(int id, [FromBody] string newTitle)
        {
            if (!await _access.CanManagePathwayAsync(User, id)) return Forbid();

            if (string.IsNullOrWhiteSpace(newTitle)) return BadRequest("O título não pode estar vazio.");

            var pathway = await _context.Pathways.FindAsync(id);
            if (pathway == null) return NotFound();

            pathway.Title = newTitle.Trim();
            await _context.SaveChangesAsync();

            return Ok();
        }

        /// <summary>
        /// Support only: hand a pathway to another Teacher (e.g. pathways that a supervisor created before the Teacher role existed)
        /// </summary>
        [HttpPut("{id}/teacher")]
        [Authorize(Roles = Roles.Support)]
        public async Task<IActionResult> ChangePathwayTeacher(int id, [FromBody] int teacherId)
        {
            var pathway = await _context.Pathways.FindAsync(id);
            if (pathway == null) return NotFound();
            if (!await IsTeacherAsync(teacherId)) return BadRequest("O utilizador escolhido não é um professor.");

            pathway.ProfessorId = teacherId;
            await _context.SaveChangesAsync();
            return Ok();
        }

        private Task<bool> IsTeacherAsync(int userId) =>
            _context.Users.AnyAsync(u => u.Id == userId && u.Role == UserRole.Teacher);
    }
}
