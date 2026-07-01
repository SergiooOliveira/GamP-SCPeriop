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

            // Vai buscar o módulo existente e as suas datas (timelines)
            var existingModule = await _context.Modules
                .Include(m => m.StageTimelines)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (existingModule == null) return NotFound();

            // Atualiza os dados principais do módulo (como o Peso)
            existingModule.Title = updatedModule.Title;
            existingModule.Weight = updatedModule.Weight;

            // Atualiza as datas das Fases (Timelines)
            if (updatedModule.StageTimelines != null)
            {
                foreach (var updatedTimeline in updatedModule.StageTimelines)
                {
                    var existingTimeline = existingModule.StageTimelines?.FirstOrDefault(t => t.Stage == updatedTimeline.Stage);

                    if (existingTimeline != null)
                    {
                        // Se a fase já existe, apenas atualiza as datas
                        existingTimeline.StartDate = updatedTimeline.StartDate;
                        existingTimeline.EndDate = updatedTimeline.EndDate;
                    }
                    else
                    {
                        // Se a fase for nova, adiciona à base de dados
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

            await _context.SaveChangesAsync();
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
