using GamP_SCPeriop.Shared;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Data.Template;
using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

/*
 * # Create a new migration
    dotnet ef migrations add <MigrationName> --output-dir Data/Migrations

    # Apply migrations to the database
    dotnet ef database update

    # Remove the last migration (if not yet applied)
    dotnet ef migrations remove
 */


namespace GamP_SCPeriop.Server.Data
{
    /// <summary>
    /// Represents the database session and holds the configuration
    /// for all application tables
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        #region Database Tables

        public DbSet<User> Users { get; set; }
        public DbSet<Pathway> Pathways { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<ModuleComponent> ModuleComponents { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ComponentEvaluation> ComponentEvaluations { get; set; }
        public DbSet<ModuleStageTimeline> ModuleStageTimelines { get; set; }

        // Templates
        public DbSet<PathwayTemplate> PathwayTemplates { get; set; }
        public DbSet<ModuleTemplate> ModuleTemplates { get; set; }
        public DbSet<ComponentTemplate> ComponentTemplates { get; set; }
        #endregion

        #region Model Configuration

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // 1. USERS
            // ==========================================
            modelBuilder.Entity<User>().HasData(
                // Your existing users
                new User { Id = 6, University = "IPCA", Password = "123", Email = "miguel@ipca.com", FullName = "Miguel Teixeira", Role = UserRole.Supervisor },
                new User { Id = 7, University = "IPCA", Password = "123", Email = "a100@alunos.ipca.pt", FullName = "Rúben Peixoto", Role = UserRole.Supervisionado },
                new User { Id = 8, University = "IPCA", Password = "123", Email = "professorTeste@ipca.pt", FullName = "Teste de nome", Role = UserRole.Supervisor },

                // New Dummy Users (Password is also 123 for easy testing)
                new User { Id = 9, University = "Hospital Central", Password = "123", Email = "armando.costa@hospital.pt", FullName = "Dr. Armando Costa", Role = UserRole.Supervisor },
                new User { Id = 10, University = "Hospital Central", Password = "123", Email = "beatriz.sousa@hospital.pt", FullName = "Enf. Beatriz Sousa", Role = UserRole.Supervisor },
                new User { Id = 11, University = "Universidade do Minho", Password = "123", Email = "a101@alunos.ipca.pt", FullName = "Ana Silva", Role = UserRole.Supervisionado },
                new User { Id = 12, University = "Universidade do Porto", Password = "123", Email = "a102@alunos.ipca.pt", FullName = "Carlos Martins", Role = UserRole.Supervisionado }
            );

            // Relationships
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Professor)
                .WithMany()
                .HasForeignKey(e => e.ProfessorId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================================
            // 2. PATHWAYS
            // ==========================================
            modelBuilder.Entity<Pathway>().HasData(
                new Pathway { Id = 1, Title = "Enfermagem Cirúrgica", MinimumPassScore = 50, MinimumApprovalScore = 75, ProfessorId = 9 },
                new Pathway { Id = 2, Title = "Anestesia Básica", MinimumPassScore = 50, MinimumApprovalScore = 80, ProfessorId = 10 }
            );

            // ==========================================
            // 3. MODULES
            // ==========================================
            modelBuilder.Entity<Module>().HasData(
                new Module { Id = 1, PathwayId = 1, Title = "Módulo Teórico - Preparação" },
                new Module { Id = 2, PathwayId = 1, Title = "Módulo Prático - Bloco Operatório" },
                new Module { Id = 3, PathwayId = 2, Title = "Módulo Único - Fármacos" },
                new Module { Id = 4, PathwayId = 2, Title = "UT1 - Introdução à Anestesia" }
            );

            // ==========================================
            // 4. COMPONENTS
            // ==========================================
            modelBuilder.Entity<ModuleComponent>().HasData(
                // Your existing
                new ModuleComponent { Id = 1, ModuleId = 1, Title = "Guia de Higienização", PdfFilePath = "", Stage = ModuleStage.Teorica },
                new ModuleComponent { Id = 2, ModuleId = 2, Title = "Checklist Cirúrgica", PdfFilePath = "", Stage = ModuleStage.ObservacaoPassiva },

                // New Dummy Components
                new ModuleComponent { Id = 3, ModuleId = 1, Title = "Manual de Acolhimento", Stage = ModuleStage.Teorica, PdfFilePath = "https://example.com/manual.pdf" },
                new ModuleComponent { Id = 4, ModuleId = 1, Title = "Checklist de Segurança (OMS)", Stage = ModuleStage.ObservacaoPassiva, PdfFilePath = "" },
                new ModuleComponent { Id = 5, ModuleId = 2, Title = "Preparação da Sala Operatória", Stage = ModuleStage.PraticaAssistida, PdfFilePath = "" },
                new ModuleComponent { Id = 6, ModuleId = 2, Title = "Circulação na Sala", Stage = ModuleStage.PraticaSupervisionada, PdfFilePath = "" },
                new ModuleComponent { Id = 7, ModuleId = 3, Title = "Tabela de Fármacos de Emergência", Stage = ModuleStage.Teorica, PdfFilePath = "https://example.com/farmacos.pdf" },
                new ModuleComponent { Id = 8, ModuleId = 3, Title = "Preparação do Ventilação", Stage = ModuleStage.ObservacaoParticipada, PdfFilePath = "" },
                new ModuleComponent { Id = 9, ModuleId = 3, Title = "Entubação Endotraqueal", Stage = ModuleStage.PraticaSupervisionada, PdfFilePath = "" },

                // Parâmetro Solto
                new ModuleComponent { Id = 10, ModuleId = 4, Title = "Demonstra conhecimento das Normas de prevenção da Infeção do Local Cirúrgico", Stage = ModuleStage.ObservacaoPassiva },
                new ModuleComponent { Id = 11, ModuleId = 4, Title = "Procede aos devidos registos clínicos informáticos no intraoperatório", Stage = ModuleStage.ObservacaoPassiva },

                // GRUPO: Sclínico (PAI)
                new ModuleComponent { Id = 12, ModuleId = 4, Title = "Sclínico", Stage = ModuleStage.ObservacaoPassiva, ParentComponentId = null },
                // Filhos do Sclínico (Apontam para o ParentComponentId = 12)
                new ModuleComponent { Id = 13, ModuleId = 4, ParentComponentId = 12, Title = "Regista Diagnósticos de Enfermagem adequadamente", Stage = ModuleStage.ObservacaoPassiva },
                new ModuleComponent { Id = 14, ModuleId = 4, ParentComponentId = 12, Title = "Regista Atitudes terapêuticas adequadamente", Stage = ModuleStage.ObservacaoPassiva },
                new ModuleComponent { Id = 15, ModuleId = 4, ParentComponentId = 12, Title = "Regista SV (incluindo temperatura corporal) e Glicemia Capilar de acordo com as normas em vigor", Stage = ModuleStage.ObservacaoPassiva },

                // Parâmetro Solto
                new ModuleComponent { Id = 16, ModuleId = 4, Title = "Valida adequadamente a administração de medicação no sistema Ghaf;", Stage = ModuleStage.ObservacaoPassiva },

                // GRUPO: Ghaf (PAI)
                new ModuleComponent { Id = 17, ModuleId = 4, Title = "Ghaf", Stage = ModuleStage.ObservacaoPassiva, ParentComponentId = null },
                // Filhos do Ghaf (Apontam para o ParentComponentId = 17)
                new ModuleComponent { Id = 18, ModuleId = 4, ParentComponentId = 17, Title = "Administração de Antibioterapia, de acordo com a norma em vigor", Stage = ModuleStage.ObservacaoPassiva },
                new ModuleComponent { Id = 19, ModuleId = 4, ParentComponentId = 17, Title = "Efetua débitos ao armazém", Stage = ModuleStage.ObservacaoPassiva },
                new ModuleComponent { Id = 20, ModuleId = 4, ParentComponentId = 17, Title = "Efetua devoluções ao armazém", Stage = ModuleStage.ObservacaoPassiva },
                new ModuleComponent { Id = 21, ModuleId = 4, ParentComponentId = 17, Title = "Efetua pedidos de dietas para o utente e acompanhante (quando aplicável)", Stage = ModuleStage.ObservacaoPassiva },

                // Parâmetro Solto
                new ModuleComponent { Id = 22, ModuleId = 4, Title = "Regista adequadamente a administração de estupefacientes em folha própria (Mod.3)", Stage = ModuleStage.ObservacaoPassiva }
            );

            // ==========================================
            // 5. ENROLLMENTS
            // ==========================================
            modelBuilder.Entity<Enrollment>().HasData(
                // New Dummy Enrollments
                new Enrollment { Id = 3, StudentId = 11, ProfessorId = 10, PathwayId = 2, ProgressPercentage = 0, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 5, 20) },
    new Enrollment { Id = 4, StudentId = 12, ProfessorId = 9, PathwayId = 1, ProgressPercentage = 0, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 10) }
            );

            // ==========================================
            // 6. NOTIFICATIONS
            // ==========================================
            modelBuilder.Entity<Notification>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(n => n.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict); // Impede que apagar um utilizador apague a notificação em cascata

            modelBuilder.Entity<Notification>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(n => n.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ComponentEvaluation>()
                .HasOne(ce => ce.ModuleComponent)
                .WithMany()
                .HasForeignKey(ce => ce.ModuleComponentId)
                .OnDelete(DeleteBehavior.Restrict); // Corta o segundo caminho de cascata!
        }
        #endregion
    }
}