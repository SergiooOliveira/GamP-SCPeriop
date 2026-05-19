using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared;
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
    }
}
