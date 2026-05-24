using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared.Entity.Model
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public string TargetUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
