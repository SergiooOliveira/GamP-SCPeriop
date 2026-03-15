using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared;

namespace GamP_SCPeriop.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserRegisterDTO request)
        {
            var newUser = new User
            {
                Email = request.Email,
                Password = request.Password,
                FullName = request.FullName,
                Role = request.Role,
                University = request.University
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(newUser);
        }

        [HttpPost("login")]
        public async Task<ActionResult<User>> Login(UserLoginDTO request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Password == request.Password);

            if (user == null)
            {
                return BadRequest("User not found or password incorrect");
            }

            user.Password = string.Empty;
            return Ok(user);
        }
    }
}
