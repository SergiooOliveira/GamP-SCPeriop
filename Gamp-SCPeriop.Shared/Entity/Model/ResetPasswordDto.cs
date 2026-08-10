using System.ComponentModel.DataAnnotations;

namespace GamP_SCPeriop.Shared.Entity.Model
{
    public class ResetPasswordDto
    {
        [Required]
        public int UserId { get; set; } // Ajusta para 'string' se os teus IDs forem GUIDs

        [Required(ErrorMessage = "A nova password é obrigatória.")]
        [MinLength(6, ErrorMessage = "A password deve ter pelo menos 6 caracteres.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
