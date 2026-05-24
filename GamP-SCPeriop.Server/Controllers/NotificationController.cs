using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Server.Data;

namespace GamP_SCPeriop.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly AppDbContext _context; // Substitui pelo nome real do teu DbContext

        public NotificationController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Listar notificações do utilizador (Ordenadas pelas mais recentes)
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<NotificationDto>>> GetUserNotifications(int userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.ReceiverId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    TargetUrl = n.TargetUrl,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return Ok(notifications);
        }

        // 2. Marcar uma notificação como lida
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
