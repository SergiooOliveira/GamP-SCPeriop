using GamP_SCPeriop.Shared.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GamP_SCPeriop.Shared
{
    public class User
    {
        public int Id { get; set; }

        [Required] // Added this to enforce the rule in the database!
        public string FullName { get; set; } = string.Empty;

        // The [Required] attribute is what actually tells the SQL database "This cannot be empty!"
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        // CHANGED: Replaced plain 'Password' with 'PasswordHash'
        public string Password { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Supervisionado;

        public string University { get; set; } = "Universidade do Minho";

        public List<Enrollment> Enrollments { get; set; } = new();

        /// <summary>
        /// Call this to get the First and Last name
        /// </summary>
        public string DisplayShortName
        {
            get
            {
                if (string.IsNullOrEmpty(FullName)) return "Unknown";

                var names = FullName.Trim().Split(' ');
                if (names.Length == 1) return names[0];

                return $"{names[0]} {names[^1]}";
            }
        }
    }
}