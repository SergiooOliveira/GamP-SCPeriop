using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Entity.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

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
        public async Task<ActionResult<Pathway>> CreatePathway(PathwayCreateDTO dto)
        {
            // 1. Cria a base do novo Percurso
            var pathway = new Pathway
            {
                Title = dto.Title,
                MinimumPassScore = dto.MinimumPassScore,
                MinimumApprovalScore = dto.MinimumApprovalScore,
                ProfessorId = dto.ProfessorId,
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
                            Weight = 1, // Valor por defeito que tens na tua classe
                            Components = new List<ModuleComponent>()
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
                                Weight = 0
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
                                Weight = 0
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
                .Where(p => p.ProfessorId == supervisorId)
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePathway(int id)
        {
            // Carrega o percurso completo com todas as suas ramificações
            var pathway = await _context.Pathways
                .Include(p => p.Modules)
                    .ThenInclude(m => m.Components)
                .Include(p => p.Modules)
                    .ThenInclude(m => m.StageTimelines)
                .FirstOrDefaultAsync(p => p.Id == id);

            // Se já não existir, devolvemos 404 (pode ter sido apagado por outro separador)
            if (pathway == null)
            {
                return NotFound();
            }

            // Marca a árvore toda para eliminação e guarda as alterações
            _context.Pathways.Remove(pathway);
            await _context.SaveChangesAsync();

            // 204 No Content é o standard HTTP para um Delete com sucesso
            return NoContent();
        }
    }
}
