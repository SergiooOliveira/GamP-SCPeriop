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

    # Remove the last applied migration
    dotnet ef migrations remove --force
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
        public DbSet<EnrollmentModule> EnrollmentModules { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ComponentEvaluation> ComponentEvaluations { get; set; }
        public DbSet<ModuleStageTimelineDto> ModuleStageTimelines { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        
        // Templates
        public DbSet<PathwayTemplate> PathwayTemplates { get; set; }
        public DbSet<ModuleTemplate> ModuleTemplates { get; set; }
        public DbSet<ComponentTemplate> ComponentTemplates { get; set; }
        public DbSet<BadgeTemplate> BadgeTemplates { get; set; }
        #endregion

        #region Model Configuration

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // RELATIONS
            // ==========================================
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne<User>().WithMany()
                .HasForeignKey(n => n.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne<User>().WithMany()
                .HasForeignKey(n => n.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ComponentEvaluation>()
                .HasOne(ce => ce.ModuleComponent)
                .WithMany()
                .HasForeignKey(ce => ce.ModuleComponentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EnrollmentModule>()
                .HasOne(em => em.Module)
                .WithMany()
                .HasForeignKey(em => em.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================================
            // DUMMY DATA: MOLDES (TEMPLATES)
            // ==========================================

            // 1. Criar os Moldes Base
            modelBuilder.Entity<PathwayTemplate>().HasData(
                new PathwayTemplate { Id = 1, Title = "Molde Standard - Bloco Operatório", Description = "Molde base que inclui preparação teórica e observação prática." },
                new PathwayTemplate { Id = 2, Title = "Molde Avançado - Anestesia", Description = "Focado exclusivamente em procedimentos de anestesiologia." }
            );

            // 2. Módulos do Molde
            modelBuilder.Entity<ModuleTemplate>().HasData(
                new ModuleTemplate { Id = 1, PathwayTemplateId = 1, Title = "Módulo Teórico - Preparação" },
                new ModuleTemplate { Id = 2, PathwayTemplateId = 1, Title = "Módulo Prático - Bloco Operatório" },
                new ModuleTemplate { Id = 3, PathwayTemplateId = 2, Title = "Módulo Único - Fármacos" },
                new ModuleTemplate { Id = 4, PathwayTemplateId = 2, Title = "UT1 - Introdução à Anestesia" }
            );

            // 3. Componentes do Molde (Adaptados da tua lista)
            modelBuilder.Entity<ComponentTemplate>().HasData(
                new ComponentTemplate { Id = 1, ModuleTemplateId = 1, Title = "Guia de Higienização", Stage = ModuleStage.Teorica, Weight = 50 },
                new ComponentTemplate { Id = 3, ModuleTemplateId = 1, Title = "Manual de Acolhimento", Stage = ModuleStage.Teorica, Weight = 50 },
                new ComponentTemplate { Id = 2, ModuleTemplateId = 2, Title = "Checklist Cirúrgica", Stage = ModuleStage.ObservacaoPassiva, Weight = 100 },

                new ComponentTemplate { Id = 7, ModuleTemplateId = 3, Title = "Tabela de Fármacos de Emergência", Stage = ModuleStage.Teorica, Weight = 100 },

                // Módulo 4 - Anestesia (Os teus grupos)
                new ComponentTemplate { Id = 10, ModuleTemplateId = 4, Title = "Demonstra conhecimento das Normas de prevenção", Stage = ModuleStage.ObservacaoPassiva, Weight = 20 },
                new ComponentTemplate { Id = 11, ModuleTemplateId = 4, Title = "Procede aos devidos registos clínicos", Stage = ModuleStage.ObservacaoPassiva, Weight = 20 },

                // GRUPO: Sclínico (PAI = Id 12)
                new ComponentTemplate { Id = 12, ModuleTemplateId = 4, Title = "Sclínico", Stage = ModuleStage.ObservacaoPassiva, Weight = 30 },
                new ComponentTemplate { Id = 13, ModuleTemplateId = 4, ParentComponentTemplateId = 12, Title = "Regista Diagnósticos de Enfermagem", Stage = ModuleStage.ObservacaoPassiva, Weight = 34 },
                new ComponentTemplate { Id = 14, ModuleTemplateId = 4, ParentComponentTemplateId = 12, Title = "Regista Atitudes terapêuticas", Stage = ModuleStage.ObservacaoPassiva, Weight = 33 },
                new ComponentTemplate { Id = 15, ModuleTemplateId = 4, ParentComponentTemplateId = 12, Title = "Regista SV e Glicemia Capilar", Stage = ModuleStage.ObservacaoPassiva, Weight = 33 },

                // GRUPO: Ghaf (PAI = Id 17)
                new ComponentTemplate { Id = 17, ModuleTemplateId = 4, Title = "Ghaf", Stage = ModuleStage.ObservacaoPassiva, Weight = 30 },
                new ComponentTemplate { Id = 18, ModuleTemplateId = 4, ParentComponentTemplateId = 17, Title = "Administração de Antibioterapia", Stage = ModuleStage.ObservacaoPassiva, Weight = 25 },
                new ComponentTemplate { Id = 19, ModuleTemplateId = 4, ParentComponentTemplateId = 17, Title = "Efetua débitos ao armazém", Stage = ModuleStage.ObservacaoPassiva, Weight = 25 },
                new ComponentTemplate { Id = 20, ModuleTemplateId = 4, ParentComponentTemplateId = 17, Title = "Efetua devoluções ao armazém", Stage = ModuleStage.ObservacaoPassiva, Weight = 25 },
                new ComponentTemplate { Id = 21, ModuleTemplateId = 4, ParentComponentTemplateId = 17, Title = "Efetua pedidos de dietas", Stage = ModuleStage.ObservacaoPassiva, Weight = 25 }
            );
        }
        #endregion
    }
}