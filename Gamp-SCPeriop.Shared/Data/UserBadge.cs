using GamP_SCPeriop.Shared.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared.Data
{
    public class UserBadge
    {
        public int Id { get; set; }

        // O ID do utilizador (Nota: se usares o Identity default do ASP.NET, costuma ser string. Se usares int, altera aqui!)
        public int UserId { get; set; }

        // A badge que ele ganhou
        public int BadgeId { get; set; }
        public Badge? Badge { get; set; }

        // Quando é que ganhou (para podermos ordenar no perfil do aluno)
        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    }
}
