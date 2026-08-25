using GamP_SCPeriop.Shared.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared.Entity.Model
{
    public class LoginResponseDto
    {
        public string Token { get; set; }
        public User User { get; set; }
    }
}
