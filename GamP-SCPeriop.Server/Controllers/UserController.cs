using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Enum;

namespace GamP_SCPeriop.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("students")]
        public async Task<ActionResult<List<User>>> GetStudents()
        {
            var students = await _context.Users
                .Where(u => u.Role == UserRole.Supervisionado)
                .ToListAsync();

            return Ok(students);
        }
    }
}
