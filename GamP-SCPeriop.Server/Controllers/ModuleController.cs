using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared;
using Microsoft.AspNetCore.Mvc;

namespace GamP_SCPeriop.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModuleController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ModuleController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<Module>> CreateModule(Module newModule)
        {
            _context.Modules.Add(newModule);
            await _context.SaveChangesAsync();

            return Ok(newModule);
        }
    }
}
