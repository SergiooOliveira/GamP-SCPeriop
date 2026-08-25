using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Entity.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PathwayController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PathwayController(AppDbContext context)
        {
            _context = context;
        }

        #region Subject Management

        [HttpPost]
        public async Task<ActionResult<Pathway>> CreatePathway(PathwayCreateDto dto)
        {
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
                    .Include(p => p.ModuleTemplates)
                        .ThenInclude(m => m.ComponentTemplates)
                    .FirstOrDefaultAsync(p => p.Id == dto.TemplateId.Value);

                if (template != null && template.ModuleTemplates != null)
                {
                    foreach (var modTpl in template.ModuleTemplates)
                    {
                        // Clona o Módulo (sem Description, de acordo com o teu modelo)
                        var newModule = new Module
                        {
                            Title = modTpl.Title,
                            Weight = 1,
                            Components = new List<ModuleComponent>(),
                            IsFromTemplate = true
                        };

                        // Dicionário para mapear quem é pai de quem
                        var parentMap = new Dictionary<int, ModuleComponent>();

                        // 2A. Copiar os PAIS primeiro
                        var parentTemplates = modTpl.ComponentTemplates.Where(c => c.ParentComponentTemplateId == null);
                        foreach (var parentTpl in parentTemplates)
                        {
                            var newParent = new ModuleComponent
                            {
                                Title = parentTpl.Title,
                                Description = parentTpl.Description,
                                Stage = parentTpl.Stage,
                                Weight = parentTpl.Weight,
                                PdfFilePath = parentTpl.PdfFilePath,
                                IsFromTemplate = true
                            };

                            newModule.Components.Add(newParent);
                            parentMap[parentTpl.Id] = newParent;
                        }

                        // 2B. Copiar os FILHOS a seguir
                        var childTemplates = modTpl.ComponentTemplates.Where(c => c.ParentComponentTemplateId != null);
                        foreach (var childTpl in childTemplates)
                        {
                            var newChild = new ModuleComponent
                            {
                                Title = childTpl.Title,
                                Description = childTpl.Description,
                                Stage = childTpl.Stage,
                                Weight = childTpl.Weight,
                                PdfFilePath = childTpl.PdfFilePath,
                                IsFromTemplate = true
                            };

                            // Liga o filho ao PAI NOVO
                            if (parentMap.ContainsKey(childTpl.ParentComponentTemplateId.Value))
                            {
                                newChild.ParentComponent = parentMap[childTpl.ParentComponentTemplateId.Value];
                            }

                            newModule.Components.Add(newChild);
                        }

                        pathway.Modules.Add(newModule);
                    }
                }
            }

            _context.Pathways.Add(pathway);
            await _context.SaveChangesAsync();

            // --- 🏆 INÍCIO DA CÓPIA DAS BADGES (DEEP COPY) ---
            if (dto.TemplateId.HasValue && dto.TemplateId.Value > 0)
            {
                // Vamos buscar todas as badges associadas a este Template
                var badgeTemplates = await _context.BadgeTemplates
                    .Where(b => b.PathwayTemplateId == dto.TemplateId.Value)
                    .ToListAsync();

                if (badgeTemplates.Any())
                {
                    // Para cada template de badge, criamos uma cópia congelada para este aluno/turma
                    foreach (var badgeTpl in badgeTemplates)
                    {
                        var newBadge = new Badge
                        {
                            PathwayId = pathway.Id, // Liga ao percurso instanciado!
                            Name = badgeTpl.Name,
                            Description = badgeTpl.Description,
                            Icon = badgeTpl.Icon,
                            Tier = badgeTpl.Tier,
                            TriggerType = badgeTpl.TriggerType,
                            TriggerValue = badgeTpl.TriggerValue
                        };
                        _context.Badges.Add(newBadge); // Assume que adicionaste public DbSet<Badge> Badges ao teu DbContext
                    }

                    // Gravamos as novas badges na base de dados
                    await _context.SaveChangesAsync();
                }
            }
            // --- FIM DA CÓPIA DAS BADGES ---

            return Ok(pathway);
        }

        #endregion

        [HttpGet("{id}")]
        public async Task<ActionResult<Pathway>> GetPathway(int id)
        {
            var pathway = await _context.Pathways
                .Include(p => p.Modules)
                    .ThenInclude(m => m.Components)
                .Include(p => p.Modules)
                    .ThenInclude(m => m.StageTimelines)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pathway == null) return NotFound();

            return Ok(pathway);
        }

        [HttpGet("supervisor/{supervisorId}")]
        public async Task<ActionResult<List<PathwayTagDto>>> GetSupervisorPathways(int supervisorId)
        {
            var pathways = await _context.Pathways
                .Where(p => p.ProfessorId == supervisorId && !p.IsArchived)
                .Select(p => new PathwayTagDto
                {
                    PathwayId = p.Id,
                    Title = p.Title
                })
                .ToListAsync();

            if (!pathways.Any()) return Ok(new List<PathwayTagDto>());

            return Ok(pathways);
        }

        [HttpGet("{pathwayId}/enrollments")]
        public async Task<ActionResult<List<Enrollment>>> GetPathwayEnrollments(int pathwayId)
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.PathwayId == pathwayId)
                .ToListAsync();

            return Ok(enrollments);
        }

        [HttpGet("builder/{id}")]
        public async Task<ActionResult<IEnumerable<EnrollmentModule>>> GetStudentPathwayBuilder(int id)
        {
            var studentModules = await _context.EnrollmentModules
                .Include(em => em.Enrollment)
                    .ThenInclude(e => e.Student)
                .Include(em => em.Enrollment)
                    .ThenInclude(e => e.Pathway)
                .Include(em => em.Module)
                    .ThenInclude(m => m.Components)
                .Include(em => em.Module)
                    .ThenInclude(m => m.StageTimelines)
                .Where(em => em.EnrollmentId == id)
                .ToListAsync();

            if (!studentModules.Any())
            {
                return NotFound();
            }

            return Ok(studentModules);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePathway(int id)
        {
            var pathway = await _context.Pathways.FindAsync(id);
            if (pathway == null) return NotFound();

            // Em vez de apagar, arquivamos! 
            // Assim o aluno continua com os dados intactos, mas podes filtrar isto nas listas do professor.
            pathway.IsArchived = true;

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
