using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Server.Services;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GamP_SCPeriop.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;

        public AuthController(AppDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        // Só os Administradores criam contas (qualquer pessoa podia criar uma conta de Admin)
        [HttpPost("register")]
        [Authorize(Roles = nameof(UserRole.Admin))]
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

            // Cria o Token assinado (com validade de 7 dias)
            var tokenString = _tokenService.CreateToken(user);

            // Limpamos a password por segurança e devolvemos o Token + Dados do Utilizador
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
        [Authorize(Roles = nameof(UserRole.Admin))]
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
