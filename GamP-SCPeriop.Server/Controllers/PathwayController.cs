using Microsoft.AspNetCore.Mvc;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Server.Data;
using Microsoft.EntityFrameworkCore;
using GamP_SCPeriop.Shared.Entity.Model;

namespace GamP_SCPeriop.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PathwayController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PathwayController(AppDbContext context)
        {
            _context = context;
        }

        #region Subject Management

        [HttpPost]
        public async Task<ActionResult<Pathway>> CreatePathway(PathwayCreateDTO dto)
        {
            var pathway = new Pathway
            {
                Title = dto.Title,
                MinimumPassScore = dto.MinimumPassScore,
                MinimumApprovalScore = dto.MinimumApprovalScore,
                ProfessorId = dto.ProfessorId
                // Modules list starts empty automatically
            };

            _context.Pathways.Add(pathway);
            await _context.SaveChangesAsync();

            // Returning the newly created pathway so the frontend gets the new ID
            return Ok(pathway);
        }

        #endregion

        [HttpGet("{id}")]
        public async Task<ActionResult<Pathway>> GetPathway(int id)
        {
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
            var pathways = await _context.Pathways
                .Where(p => p.ProfessorId == supervisorId)
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
            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.PathwayId == pathwayId)
                .ToListAsync();

            return Ok(enrollments);
        }
    }
}
