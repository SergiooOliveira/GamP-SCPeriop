using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared;
using Gamp_SCPeriop.Shared;

namespace GamP_SCPeriop.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DBContext _context;

        public AuthController(DBContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(User request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("User already Exists");
            }

            _context.Users.Add(request);
            await _context.SaveChangesAsync();

            return Ok(request);
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserLoginDTO request)
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
