using GamP_SCPeriop.Shared;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace GamP_SCPeriop.Server.Data
{
    /// <summary>
    /// Represents the database session and holds the configuration
    /// for all application tables
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the AppDbContext.
        /// </summary>
        /// <param name="options">The configuration options for 
        /// this context instance.</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        #region Database Tables

        public DbSet<User> Users { get; set; }
        public DbSet<Pathway> Pathways { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<ModuleComponent> ModuleComponents { get; set; }

        #endregion

        #region Model Configuration

        /// <summary>
        /// Configures the relationship rules for the database tables
        /// </summary>
        /// <param name="modelBuilder">The builder used to construct 
        /// the model for this context</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Existing Users
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 6,
                    University = "IPCA",
                    Password = "123",
                    Email = "miguel@ipca.com",
                    FullName = "Miguel Teixeira",
                    Role = UserRole.Supervisor
                },
                new User
                {
                    Id = 7,
                    University = "IPCA",
                    Password = "123",
                    Email = "a100@alunos.ipca.pt",
                    FullName = "Rúben Peixoto",
                    Role = UserRole.Supervisionado
                },
                new User
                {
                    Id = 8,
                    University = "IPCA",
                    Password = "123",
                    Email = "professorTeste@ipca.pt",
                    FullName = "Teste de nome",
                    Role = UserRole.Supervisor
                }
            );

            // Links the Enrollment to the Student and
            // prevents auto-deletion of the enrollment
            // if the user is deleted
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Links the Enrollment to the Professor and
            // prevents auto-deletion to avoid SQL multiple
            // cascade path errors
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Professor)
                .WithMany()
                .HasForeignKey(e => e.ProfessorId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- NEW SEED DATA ---

            // Seed Pathways
            modelBuilder.Entity<Pathway>().HasData(
                new Pathway { Id = 1, Title = "Enfermagem Cirúrgica", MinimumPassScore = 50, MinimumApprovalScore = 75 },
                new Pathway { Id = 2, Title = "Anestesia Básica", MinimumPassScore = 50, MinimumApprovalScore = 80 }
            );

            // Seed Modules
            modelBuilder.Entity<Module>().HasData(
                new Module { Id = 1, PathwayId = 1, Title = "Módulo Teórico - Preparação" },
                new Module { Id = 2, PathwayId = 1, Title = "Módulo Prático - Bloco Operatório" },
                new Module { Id = 3, PathwayId = 2, Title = "Módulo Único - Fármacos" }
            );

            // Seed Components
            modelBuilder.Entity<ModuleComponent>().HasData(
                new ModuleComponent { Id = 1, ModuleId = 1, Title = "Guia de Higienização", PdfFilePath = "", Status = 0 },
                new ModuleComponent { Id = 2, ModuleId = 2, Title = "Checklist Cirúrgica", PdfFilePath = "", Status = 0 }
            );

            // Seed Enrollments
            modelBuilder.Entity<Enrollment>().HasData(
                new Enrollment
                {
                    Id = 1,
                    StudentId = 7,       // Rúben Peixoto
                    ProfessorId = 6,     // Miguel Teixeira (Supervisor)
                    PathwayId = 1,       // Enfermagem Cirúrgica
                    ProgressPercentage = 15,
                    // Using a fixed date so EF Core doesn't try to create a new migration every single time you compile
                    EndDate = new DateTime(2026, 6, 30)
                }
            );
        }

        #endregion
    }
}
