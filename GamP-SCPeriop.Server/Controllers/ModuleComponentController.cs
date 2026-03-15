using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared;
using Microsoft.AspNetCore.Mvc;

namespace GamP_SCPeriop.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModuleComponentController : Controller
    {
        private readonly AppDbContext _context;

        public ModuleComponentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<ModuleComponent>> CreateComponent(ModuleComponent newModuleComponent)
        {
            _context.ModuleComponents.Add(newModuleComponent);

            await _context.SaveChangesAsync();

            return Ok(newModuleComponent);
        }
    }
}
