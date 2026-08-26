using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<ActionResult<Module>> CreateModule(ModuleCreateDto dto)
        {
            var module = new Module
            {
                Title = dto.Title,
                PathwayId = dto.EnrollmentId.HasValue ? null : dto.PathwayId,
                Weight = dto.Weight,
                // Se vier de um Enrollment, NÃO é do template
                IsFromTemplate = false
            };

            _context.Modules.Add(module);
            await _context.SaveChangesAsync();

            if (dto.EnrollmentId.HasValue)
            {
                var enrollmentModule = new EnrollmentModule
                {
                    EnrollmentId = dto.EnrollmentId.Value,
                    ModuleId = module.Id
                };

                _context.EnrollmentModules.Add(enrollmentModule);
                await _context.SaveChangesAsync();
            }

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
            // 1. Procurar na tabela ponte (EnrollmentModule) para encontrar o clone exato e a inscrição
            var enrollmentModule = await _context.EnrollmentModules
                .Include(em => em.Enrollment)
                    .ThenInclude(e => e.Pathway)
                .Include(em => em.Module)
                    .ThenInclude(m => m.Components)
                .FirstOrDefaultAsync(em => em.ModuleId == moduleId && em.Enrollment.StudentId == studentId);

            if (enrollmentModule == null || enrollmentModule.Module == null) return NotFound();

            var module = enrollmentModule.Module;

            // [TRUQUE MAGICO]: Preenchemos o PathwayId em memória para o Frontend não quebrar!
            module.PathwayId = enrollmentModule.Enrollment.PathwayId;

            // 2. Vamos buscar as notas específicas deste aluno
            var evaluations = await _context.ComponentEvaluations
                .Where(ce => ce.EnrollmentId == enrollmentModule.EnrollmentId)
                .ToListAsync();

            // 3. Injetamos as notas nos componentes antes de enviar para o Frontend
            if (module.Components != null)
            {
                foreach (var component in module.Components)
                {
                    var eval = evaluations.FirstOrDefault(e => e.ModuleComponentId == component.Id);
                    component.Status = eval != null ? eval.Status : ComponentStatus.Pending;
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
                        existingModule.StageTimelines ??= new List<ModuleStageTimelineDto>();
                        existingModule.StageTimelines.Add(new ModuleStageTimelineDto
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

        [HttpPut("{id}/timeline")]
        public async Task<IActionResult> UpdateStudentTimeline(int id, [FromBody] Module updatedModule)
        {
            if (id != updatedModule.Id) return BadRequest();

            var existingModule = await _context.Modules
                .Include(m => m.StageTimelines)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (existingModule == null) return NotFound();

            // Atualiza APENAS as datas na tabela ModuleStageTimelines
            if (updatedModule.StageTimelines != null)
            {
                foreach (var updatedTimeline in updatedModule.StageTimelines)
                {
                    var existingTimeline = existingModule.StageTimelines?.FirstOrDefault(t => t.Stage == updatedTimeline.Stage);

                    DateTime? safeStartDate = updatedTimeline.StartDate == default(DateTime) ? null : updatedTimeline.StartDate;
                    DateTime? safeEndDate = updatedTimeline.EndDate == default(DateTime) ? null : updatedTimeline.EndDate;

                    if (existingTimeline != null)
                    {
                        existingTimeline.StartDate = updatedTimeline.StartDate;
                        existingTimeline.EndDate = updatedTimeline.EndDate;
                    }
                    else
                    {
                        existingModule.StageTimelines = new List<ModuleStageTimelineDto>();
                        existingModule.StageTimelines.Add(new ModuleStageTimelineDto
                        {
                            Stage = updatedTimeline.Stage,
                            StartDate = updatedTimeline.StartDate,
                            EndDate = updatedTimeline.EndDate,
                            ModuleId = id
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteModule(int id)
        {
            var module = await _context.Modules.FindAsync(id);
            if (module == null) return NotFound();

            // 1. Limpar Componentes do Módulo
            var components = await _context.ModuleComponents.Where(c => c.ModuleId == id).ToListAsync();
            if (components.Any()) _context.ModuleComponents.RemoveRange(components);

            // 2. Limpar Datas (Timelines)
            var timelines = await _context.ModuleStageTimelines.Where(t => t.ModuleId == id).ToListAsync();
            if (timelines.Any()) _context.ModuleStageTimelines.RemoveRange(timelines);

            // 3. LIMPAR A LIGAÇÃO AO ALUNO (O que estava a causar o Erro 500)
            var enrollments = await _context.EnrollmentModules.Where(e => e.ModuleId == id).ToListAsync();
            if (enrollments.Any()) _context.EnrollmentModules.RemoveRange(enrollments);

            // 4. Agora sim, podemos apagar o Módulo com segurança
            _context.Modules.Remove(module);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}