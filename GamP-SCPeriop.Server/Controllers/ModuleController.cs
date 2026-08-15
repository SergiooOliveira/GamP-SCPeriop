using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Entity.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModuleController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ModuleController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<Module>> CreateModule(ModuleCreateDTO dto)
        {
            var module = new Module
            {
                Title = dto.Title,
                PathwayId = dto.PathwayId
                // Components list starts empty
            };

            _context.Modules.Add(module);
            await _context.SaveChangesAsync();

            return Ok(module);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Module>> GetModule(int id)
        {
            var module = await _context.Modules
                // 1. Grab the components inside the module
                .Include(m => m.Components)
                .Include(m => m.StageTimelines)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (module == null)
            {
                return NotFound();
            }

            return Ok(module);
        }

        [HttpGet("{moduleId}/student/{studentId}")]
        public async Task<ActionResult<Module>> GetModuleForStudent(int moduleId, int studentId)
        {
            // 1. Vamos buscar o Módulo e os seus Componentes (o Molde)
            var module = await _context.Modules
                .Include(m => m.Components)
                .FirstOrDefaultAsync(m => m.Id == moduleId);

            if (module == null) return NotFound();

            // 2. Vamos buscar a Inscrição deste aluno neste Pathway
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == studentId && e.PathwayId == module.PathwayId);

            if (enrollment != null)
            {
                // 3. Vamos buscar as notas específicas deste aluno
                var evaluations = await _context.ComponentEvaluations
                    .Where(ce => ce.EnrollmentId == enrollment.Id)
                    .ToListAsync();

                // 4. Injetamos as notas nos componentes antes de enviar para o Frontend!
                foreach (var component in module.Components)
                {
                    var eval = evaluations.FirstOrDefault(e => e.ModuleComponentId == component.Id);
                    if (eval != null)
                    {
                        component.Status = eval.Status; // Usa a propriedade [NotMapped]
                    }
                }
            }

            return Ok(module);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateModule(int id, Module updatedModule)
        {
            if (id != updatedModule.Id) return BadRequest();

            // 1. Vai buscar o módulo e as datas atuais
            var existingModule = await _context.Modules
                .Include(m => m.StageTimelines)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (existingModule == null) return NotFound();

            // 2. Atualiza os dados do Módulo
            existingModule.Title = updatedModule.Title;
            existingModule.Weight = updatedModule.Weight;

            if (updatedModule.StageTimelines != null)
            {
                foreach (var updatedTimeline in updatedModule.StageTimelines)
                {
                    var existingTimeline = existingModule.StageTimelines?.FirstOrDefault(t => t.Stage == updatedTimeline.Stage);

                    if (existingTimeline != null)
                    {
                        existingTimeline.StartDate = updatedTimeline.StartDate;
                        existingTimeline.EndDate = updatedTimeline.EndDate;
                    }
                    else
                    {
                        existingModule.StageTimelines ??= new List<ModuleStageTimeline>();
                        existingModule.StageTimelines.Add(new ModuleStageTimeline
                        {
                            Stage = updatedTimeline.Stage,
                            StartDate = updatedTimeline.StartDate,
                            EndDate = updatedTimeline.EndDate,
                            ModuleId = id
                        });
                    }
                }
            }

            // Grava as alterações do Módulo primeiro
            await _context.SaveChangesAsync();

            // --- 🕒 INÍCIO DA PROPAGAÇÃO (PATHWAY E ENROLLMENTS) ---

            var allTimelinesInPathway = await _context.Modules
                .Where(m => m.PathwayId == existingModule.PathwayId)
                .SelectMany(m => m.StageTimelines)
                .ToListAsync();

            if (allTimelinesInPathway.Any())
            {
                var realStartDate = allTimelinesInPathway.Min(t => t.StartDate);
                var realEndDate = allTimelinesInPathway.Max(t => t.EndDate);

                var pathway = await _context.Pathways.FindAsync(existingModule.PathwayId);

                // Verifica se houve realmente uma mudança nos limites
                if (pathway != null && (pathway.StartDate != realStartDate || pathway.EndDate != realEndDate))
                {
                    // A. Atualiza a Pathway
                    pathway.StartDate = realStartDate;
                    pathway.EndDate = realEndDate;

                    // B. Atualiza TODOS os alunos inscritos
                    var enrollments = await _context.EnrollmentModules
                        .Where(e => e.Enrollment.PathwayId == existingModule.PathwayId)
                        .ToListAsync();

                    foreach (var enrollment in enrollments)
                    {
                        enrollment.StartDate = realStartDate;
                        enrollment.EndDate = realEndDate;
                    }

                    // Grava o efeito cascata de uma só vez
                    await _context.SaveChangesAsync();
                }
            }
            // --- FIM DA PROPAGAÇÃO ---

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteModule(int id)
        {
            // Vai buscar o módulo e inclui os componentes associados
            var module = await _context.Modules
                .Include(m => m.Components)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (module == null) return NotFound();

            // Remove primeiro os componentes (limpa os filhos)
            if (module.Components != null && module.Components.Any())
            {
                _context.ModuleComponents.RemoveRange(module.Components);
            }

            // Agora sim, pode apagar o módulo em segurança (apaga o pai)
            _context.Modules.Remove(module);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
