using GamP_SCPeriop.Helpers;
using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Services
{
    /// <summary>
    /// Creating a pathway (optionally from a template) and enrolling a student in it.
    /// Used by the API and by the test data seeder, so test data is built exactly like real data.
    /// Callers check permissions first.
    /// </summary>
    public class PathwayService
    {
        private readonly AppDbContext _context;

        public PathwayService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// New pathway; with a template, a deep copy of its modules, parameters and badges.
        /// Stage dates start empty: the supervisor sets them in the builder.
        /// </summary>
        public async Task<Pathway> CreatePathwayAsync(PathwayCreateDto dto)
        {
            Dictionary<int, Module> moduleMap = new();

            // 1. Cria a base do novo Percurso
            var pathway = new Pathway
            {
                Title = dto.Title,
                MinimumPassScore = dto.MinimumPassScore,
                MinimumApprovalScore = dto.MinimumApprovalScore,
                ProfessorId = dto.ProfessorId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Modules = new List<Module>()
            };

            // 2. Clonagem Profunda se o utilizador escolheu um Molde
            if (dto.TemplateId.HasValue && dto.TemplateId.Value > 0)
            {
                var template = await _context.PathwayTemplates
                    .AsNoTracking()
                    .Include(p => p.ModuleTemplates)
                        .ThenInclude(m => m.ComponentTemplates)
                    .FirstOrDefaultAsync(p => p.Id == dto.TemplateId.Value);

                if (template != null)
                {
                    foreach (var modTpl in template.ModuleTemplates.OrderBy(m => m.OrderIndex))
                    {
                        // Clona o Módulo (a ordem também: antes perdia-se)
                        var newModule = new Module
                        {
                            Title = modTpl.Title,
                            Weight = modTpl.Weight,
                            OrderIndex = modTpl.OrderIndex,
                            Components = new List<ModuleComponent>(),
                            IsFromTemplate = true,
                            StageTimelines = new List<ModuleStageTimelineDto>()
                        };

                        // As fases práticas começam sem datas
                        foreach (var stage in ModuleStageHelper.GetTimelineStages())
                        {
                            newModule.StageTimelines.Add(new ModuleStageTimelineDto { Stage = stage, StartDate = null, EndDate = null });
                        }

                        // Pais primeiro, para os filhos saberem a quem pertencem
                        var parentMap = new Dictionary<int, ModuleComponent>();
                        foreach (var componentTpl in modTpl.ComponentTemplates.OrderBy(c => c.ParentComponentTemplateId.HasValue ? 1 : 0))
                        {
                            var newComponent = new ModuleComponent
                            {
                                Title = componentTpl.Title,
                                Description = componentTpl.Description,
                                Stage = componentTpl.Stage,
                                Weight = componentTpl.Weight,
                                OrderIndex = componentTpl.OrderIndex,
                                PdfFilePath = componentTpl.PdfFilePath,
                                IsFromTemplate = true
                            };

                            if (componentTpl.ParentComponentTemplateId.HasValue
                                && parentMap.TryGetValue(componentTpl.ParentComponentTemplateId.Value, out var newParent))
                            {
                                newComponent.ParentComponent = newParent;
                            }
                            else if (!componentTpl.ParentComponentTemplateId.HasValue)
                            {
                                parentMap[componentTpl.Id] = newComponent;
                            }

                            newModule.Components.Add(newComponent);
                        }

                        pathway.Modules.Add(newModule);
                        moduleMap[modTpl.Id] = newModule;
                    }
                }
            }

            _context.Pathways.Add(pathway);
            await _context.SaveChangesAsync();

            // 3. Badges: cópia congelada das badges do molde (as de módulo passam a apontar para o módulo novo)
            if (dto.TemplateId.HasValue && dto.TemplateId.Value > 0)
            {
                var badgeTemplates = await _context.BadgeTemplates
                    .AsNoTracking()
                    .Where(b => b.PathwayTemplateId == dto.TemplateId.Value)
                    .ToListAsync();

                foreach (var badgeTpl in badgeTemplates)
                {
                    string newTriggerValue = badgeTpl.TriggerValue;

                    if (badgeTpl.TriggerType == BadgeTriggerType.ModuleCompletion &&
                        int.TryParse(badgeTpl.TriggerValue, out int oldModuleId) &&
                        moduleMap.TryGetValue(oldModuleId, out var mappedModule))
                    {
                        newTriggerValue = mappedModule.Id.ToString();
                    }

                    _context.Badges.Add(new Badge
                    {
                        PathwayId = pathway.Id,
                        Name = badgeTpl.Name,
                        Description = badgeTpl.Description,
                        Icon = badgeTpl.Icon,
                        Tier = badgeTpl.Tier,
                        TriggerType = badgeTpl.TriggerType,
                        TriggerValue = newTriggerValue
                    });
                }

                if (badgeTemplates.Any()) await _context.SaveChangesAsync();
            }

            return pathway;
        }

        /// <summary>
        /// Enrols a student: every student gets an independent copy of the pathway's modules
        /// (dates, parameters and sub-parameters), built in memory and saved in one database trip.
        /// </summary>
        public async Task<Enrollment> EnrollStudentAsync(int studentId, int pathwayId)
        {
            var enrollment = new Enrollment
            {
                StudentId = studentId,
                PathwayId = pathwayId,
                ProgressPercentage = 0,
            };
            _context.Enrollments.Add(enrollment);

            var pathwayModules = await _context.Modules
                .AsNoTracking()
                .Include(m => m.StageTimelines)
                .Include(m => m.Components)
                .Where(m => m.PathwayId == pathwayId)
                .ToListAsync();

            foreach (var baseModule in pathwayModules)
            {
                var clonedModule = new Module
                {
                    Title = baseModule.Title,
                    Weight = baseModule.Weight,
                    OrderIndex = baseModule.OrderIndex,
                    PathwayId = null,
                    IsFromTemplate = true,
                    OriginalModuleId = baseModule.Id,
                    StageTimelines = (baseModule.StageTimelines ?? new List<ModuleStageTimelineDto>())
                        .Select(t => new ModuleStageTimelineDto { Stage = t.Stage, StartDate = t.StartDate, EndDate = t.EndDate })
                        .ToList()
                };

                // Parents first, so each sub-parameter can point to its parent's copy
                var copies = new Dictionary<int, ModuleComponent>();
                foreach (var component in baseModule.Components.OrderBy(c => c.ParentComponentId.HasValue ? 1 : 0))
                {
                    ModuleComponent? parentCopy = null;
                    if (component.ParentComponentId.HasValue && !copies.TryGetValue(component.ParentComponentId.Value, out parentCopy))
                        continue; // orphaned sub-parameter: skip

                    var copy = new ModuleComponent
                    {
                        Stage = component.Stage,
                        Title = component.Title,
                        Description = component.Description,
                        Weight = component.Weight,
                        OrderIndex = component.OrderIndex,
                        PdfFilePath = component.PdfFilePath,
                        IsFromTemplate = true,
                        ParentComponent = parentCopy
                    };
                    copies[component.Id] = copy;
                    clonedModule.Components.Add(copy);
                }

                // Link the copy to the enrollment, with the module's overall dates
                _context.EnrollmentModules.Add(new EnrollmentModule
                {
                    Enrollment = enrollment,
                    Module = clonedModule,
                    StartDate = clonedModule.StageTimelines.Where(t => t.StartDate.HasValue).Select(t => t.StartDate).Min(),
                    EndDate = clonedModule.StageTimelines.Where(t => t.EndDate.HasValue).Select(t => t.EndDate).Max()
                });
            }

            await _context.SaveChangesAsync();
            return enrollment;
        }
    }
}
