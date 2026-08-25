using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Entity.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BadgesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BadgesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<List<StudentBadgeDto>>> GetStudentBadges(int studentId)
        {
            // 1. Descobrir todos os Percursos (PathwayId) em que o aluno está inscrito
            var studentPathwayIds = await _context.Enrollments
                .Where(e => e.StudentId == studentId)
                .Select(e => e.PathwayId)
                .ToListAsync();

            if (!studentPathwayIds.Any())
                return Ok(new List<StudentBadgeDto>()); // Se não tiver percursos, devolve lista vazia

            // 2. Ir buscar as Badges "Clonadas" que pertencem a esses Percursos
            var pathwayBadges = await _context.Badges
                .Include(b => b.Pathway)
                .Where(b => studentPathwayIds.Contains(b.PathwayId))
                .ToListAsync();

            // 3. Ir buscar a tabela de conquistas (UserBadges) para saber quais é que ele já desbloqueou
            var unlockedUserBadges = await _context.UserBadges
                .Where(ub => ub.UserId == studentId)
                .ToListAsync();

            // 4. Transformar os dados da Base de Dados no DTO que o teu Frontend está a pedir
            var badgeDtos = pathwayBadges.Select(b =>
            {
                var unlockData = unlockedUserBadges.FirstOrDefault(ub => ub.BadgeId == b.Id);
                bool isUnlocked = unlockData != null;

                return new StudentBadgeDto
                {
                    Id = b.Id,
                    PathwayTitle = b.Pathway?.Title ?? "Percurso Desconhecido",
                    Name = b.Name,
                    Description = b.Description,
                    Icon = b.Icon,
                    Tier = b.Tier,
                    IsUnlocked = isUnlocked,
                    UnlockedAt = isUnlocked ? unlockData!.EarnedAt : null
                };
            }).ToList();

            // Vamos ordenar para as "Desbloqueadas" aparecerem primeiro na montra
            badgeDtos = badgeDtos
                .OrderByDescending(b => b.IsUnlocked)
                .ThenBy(b => b.Tier)
                .ToList();

            return Ok(badgeDtos);
        }
    }
}
