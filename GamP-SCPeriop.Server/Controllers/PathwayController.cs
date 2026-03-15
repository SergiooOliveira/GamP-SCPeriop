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

        /// <summary>
        /// Call this method to create a new Pathway
        /// </summary>
        /// <param name="newPathway">New Pathway to add</param>
        [HttpPost]
        public async Task<ActionResult<Pathway>> CreatePathway(Pathway newPathway)
        {
            // Stage the new pathway in Entity Framework
            _context.Pathways.Add(newPathway);

            // Commit  the staged changes to the SQL database
            await _context.SaveChangesAsync();

            return Ok(newPathway);
        }

        #endregion

        [HttpGet]
        public async Task<ActionResult<List<Pathway>>> GetFullPathways()
        {
            var pathways = await _context.Pathways
                .Include(p => p.Modules)
                    .ThenInclude(m => m.Components)
                .ToListAsync();

            return Ok(pathways);
        }
    }
}
