using Microsoft.AspNetCore.Mvc;
using GamP_SCPeriop.Shared;
using GamP_SCPeriop.Server.Data;
using Microsoft.EntityFrameworkCore;

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
                MinimumApprovalScore = dto.MinimumApprovalScore
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
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pathway == null) return NotFound();

            return Ok(pathway);
        }
    }
}
