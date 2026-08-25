using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Entity.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GamP_SCPeriop.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserRegisterDto request)
        {
            var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
            {
                return BadRequest("Email já está em uso.");
            }

            var newUser = new User
            {
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName,
                Role = request.Role,
                University = request.University
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(newUser);
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login(UserLoginDto request)
        {
            // Verifica na base de dados se as credenciais estão corretas
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                return BadRequest("User not found or password incorrect");
            }

            // 1. Criar a lista de informações do crachá (Claims)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()) // Guarda se é Admin, Supervisor ou Supervisionado
            };

            // 2. Assinar o crachá com a tua chave secreta
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            // 3. Montar o Token (com validade de 7 dias)
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(securityToken);

            // 4. Limpamos a password por segurança e devolvemos o Token + Dados do Utilizador
            user.Password = string.Empty;

            return Ok(new LoginResponseDto
            {
                Token = tokenString,
                User = user
            });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            // 1. Ler o ID do utilizador logado diretamente do Token (segurança máxima)
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
                return Unauthorized("Sessão inválida.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("Utilizador não encontrado.");

            // 2. Verificar se a password atual bate certo
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
                return BadRequest("A password atual está incorreta.");

            // 3. Encriptar a nova e guardar
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok("Password alterada com sucesso.");
        }

        [HttpPost("reset-password")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return NotFound("Utilizador alvo não encontrado.");

            // O Administrador não precisa de saber a antiga. Esmagamos diretamente.
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok("Password resetada com sucesso.");
        }
    }
}
