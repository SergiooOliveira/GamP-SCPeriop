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
        public async Task<ActionResult<Pathway>> CreatePathway(PathwayCreateDto dto)
        {
            // Supervisors always own the pathways they create; only admins may create one for someone else
            if (!User.IsAdmin()) dto.ProfessorId = User.GetUserId();

            // Criação (e cópia do molde, se escolhido) partilhada com o gerador de dados de teste
            var pathway = await _pathways.CreatePathwayAsync(dto);
            return Ok(pathway);
        }

        #endregion

        [HttpGet("{id}")]
        public async Task<ActionResult<Pathway>> GetPathway(int id)
        {
            if (!await _access.CanManagePathwayAsync(User, id)) return Forbid();

            var pathway = await _context.Pathways
                .Include(p => p.Modules)
                    .ThenInclude(m => m.Components)
                .Include(p => p.Modules)
                    .ThenInclude(m => m.StageTimelines)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pathway == null) return NotFound();

            return Ok(pathway);
        }

        [HttpGet("supervisor/{supervisorId}")]
        public async Task<ActionResult<List<PathwayTagDto>>> GetSupervisorPathways(int supervisorId)
        {
            if (!User.IsAdmin() && supervisorId != User.GetUserId()) return Forbid();

            var pathways = await _context.Pathways
                .Where(p => p.ProfessorId == supervisorId && !p.IsArchived)
                .Select(p => new PathwayTagDto
                {
                    PathwayId = p.Id,
                    Title = p.Title
                })
                .ToListAsync();

            if (!pathways.Any()) return Ok(new List<PathwayTagDto>());

            return Ok(pathways);
        }

        [HttpGet("{pathwayId}/enrollments")]
        public async Task<ActionResult<List<Enrollment>>> GetPathwayEnrollments(int pathwayId)
        {
            if (!await _access.CanManagePathwayAsync(User, pathwayId)) return Forbid();

            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.PathwayId == pathwayId)
                .ToListAsync();

            return Ok(enrollments);
        }

        [HttpGet("builder/{id}")]
        public async Task<ActionResult<IEnumerable<EnrollmentModule>>> GetStudentPathwayBuilder(int id)
        {
            if (!await _access.CanManageEnrollmentAsync(User, id)) return Forbid();

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
    }
}
