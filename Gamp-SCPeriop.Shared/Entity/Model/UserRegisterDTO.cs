using System.ComponentModel.DataAnnotations;

namespace GamP_SCPeriop.Shared.Entity.Model
{
    public class UserRegisterDTO
    {
        #region Registration Fields

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Supervisionado;

        public string University { get; set; } = "IPCA";

        #endregion
    }
}
