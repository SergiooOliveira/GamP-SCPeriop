using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.AspNetCore.Authorization;
using GamP_SCPeriop.Server.Services;

namespace GamP_SCPeriop.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : Controller
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("students")]
        [Authorize(Roles = Roles.Staff)]
        public async Task<ActionResult<List<User>>> GetStudents()
        {
            // Used to pick students to enrol: only what's needed to identify them
            var students = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == UserRole.Supervisionado)
                .OrderBy(u => u.FullName)
                .Select(u => new User { Id = u.Id, FullName = u.FullName, Email = u.Email, Role = u.Role, University = u.University })
                .ToListAsync();

            return Ok(students);
        }

        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<List<User>>> GetAllUsers()
        {
            var users = await _context.Users
                .Where(u => u.Role != UserRole.Admin)
                .OrderByDescending(u => u.Id) // Coloca os registos mais recentes no topo
                .ToListAsync();

            return Ok(users);
        }
    }
}
