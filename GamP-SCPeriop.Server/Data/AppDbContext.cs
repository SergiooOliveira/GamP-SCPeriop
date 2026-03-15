using Microsoft.EntityFrameworkCore;
using GamP_SCPeriop.Shared;

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
        }

        #endregion
    }
}
