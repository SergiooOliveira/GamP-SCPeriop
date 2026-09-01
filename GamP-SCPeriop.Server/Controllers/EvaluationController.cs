using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Enum;
using GamP_SCPeriop.Server.Services;
using GamP_SCPeriop.Shared.Entity.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EvaluationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BadgeService _badgeService;

        public EvaluationController(AppDbContext context, BadgeService badgeService)
        {
            _context = context;
            _badgeService = badgeService;
        }

        // 1. CARREGAR A PÁGINA: Devolve o aluno, o percurso e as notas
        [HttpGet("enrollment/{id}")]
        public async Task<ActionResult<List<EnrollmentModule>>> GetEnrollmentForEvaluation(int id)
        {
            // Vai buscar TODOS os módulos clonados que pertencem a esta inscrição
            var enrollmentModules = await _context.EnrollmentModules
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

            if (enrollmentModules == null || !enrollmentModules.Any()) return NotFound();

            var evaluations = await _context.ComponentEvaluations
                .Where(ce => ce.EnrollmentId == id)
                .ToListAsync();

            // Injeta as notas nas componentes
            foreach (var em in enrollmentModules)
            {
                if (em.Module?.Components != null)
                {
                    foreach (var component in em.Module.Components)
                    {
                        var eval = evaluations.FirstOrDefault(e => e.ModuleComponentId == component.Id);
                        component.Status = eval != null ? eval.Status : ComponentStatus.Pending;
                    }
                }
            }

            return Ok(enrollmentModules);
        }

        // 2. GRAVAR AVALIAÇÃO: Quando o professor clica num botão colorido
        [HttpPost]
        public async Task<IActionResult> SaveEvaluations([FromBody] List<EvaluationRequestDto> requests)
        {
            if (requests == null || !requests.Any()) return BadRequest("Invalid evaluation requests.");

            int enrollmentId = requests.First().EnrollmentId;

            // 1. Load existing evaluations
            var existingEval = await _context.ComponentEvaluations
                .Where(ce => ce.EnrollmentId == enrollmentId)
                .ToDictionaryAsync(ce => ce.ModuleComponentId);

            // 2. Update or add evaluations in memory
            foreach (var req in requests)
            {
                if (existingEval.TryGetValue(req.ModuleComponentId, out var eval))
                {
                    eval.Status = req.Status;
                    eval.EvaluatedAt = DateTime.UtcNow;
                }
                else
                {
                    _context.ComponentEvaluations.Add(new ComponentEvaluation
                    {
                        EnrollmentId = req.EnrollmentId,
                        ModuleComponentId = req.ModuleComponentId,
                        Status = req.Status,
                        EvaluatedAt = DateTime.UtcNow
                    });
                }
            }

            // Save all grades in one single database trip
            await _context.SaveChangesAsync();

            // 3. Load Pathway and Enrollment structure
            var enrollmentModules = await _context.EnrollmentModules
                .Include(em => em.Enrollment)
                    .ThenInclude(e => e.Pathway)
                .Include(em => em.Module)
                    .ThenInclude(m => m.Components)
                .Where(em => em.EnrollmentId == enrollmentId)
                .ToListAsync();

            var enrollment = enrollmentModules.FirstOrDefault()?.Enrollment;

            if (enrollment != null)
            {
                // Reload updated evaluations for accurate calculations
                var allEvaluations = await _context.ComponentEvaluations
                    .Where(ce => ce.EnrollmentId == enrollmentId)
                    .ToListAsync();

                // 4. Calculate Global Progress (Pathway) with Strict Filtering
                var allComponents = enrollmentModules
                    .SelectMany(em => em.Module?.Components ?? new List<ModuleComponent>())
                    .ToList();

                var parentsIds = allComponents
                    .Where(c => c.ParentComponentId.HasValue)
                    .Select(c => c.ParentComponentId.Value)
                    .ToHashSet();

                var assessableIds = allComponents
                    .Where(c => c.Stage == ModuleStage.PraticaSupervisionada || c.Stage == ModuleStage.PraticaAssistida)
                    .Where(c => !parentsIds.Contains(c.Id))
                    .Where(c => c.Weight > 0)
                    .Select(c => c.Id)
                    .ToList();

                if (assessableIds.Any())
                {
                    int completedGlobal = allEvaluations.Count(ce =>
                        assessableIds.Contains(ce.ModuleComponentId) &&
                        (ce.Status == ComponentStatus.AcimaDaMedia || ce.Status == ComponentStatus.Consistente));

                    enrollment.ProgressPercentage = (int)((double)completedGlobal / assessableIds.Count * 100);
                }

                // 5. Discover changed modules
                var changedComponentsIds = requests.Select(r => r.ModuleComponentId).ToHashSet();
                var affectedModdules = enrollmentModules
                    .Select(em => em.Module)
                    .Where(m => m.Components != null && m.Components.Any(c => changedComponentsIds.Contains(c.Id)))
                    .ToList();

                // 6. Calculate progress and evaluate badges ONLY for affected modules
                foreach (var currentModule in affectedModdules)
                {
                    var moduleParentesIds = currentModule.Components
                        .Where(c => c.ParentComponentId.HasValue)
                        .Select(c => c.ParentComponentId.Value)
                        .ToHashSet();

                    var moduleAssessableIds = currentModule.Components
                        .Where(c => c.Stage == ModuleStage.PraticaSupervisionada || c.Stage == ModuleStage.PraticaAssistida)
                        .Where(c => !moduleParentesIds.Contains(c.Id))
                        .Where(c => c.Weight > 0)
                        .Select(c => c.Id)
                        .ToList();

                    if (moduleAssessableIds.Any())
                    {
                        int completedModule = allEvaluations.Count(ce =>
                            moduleAssessableIds.Contains(ce.ModuleComponentId) &&
                            (ce.Status == ComponentStatus.AcimaDaMedia || ce.Status == ComponentStatus.Consistente));

                        int modulePercentage = (int)((double)completedModule / moduleAssessableIds.Count * 100);
                        int badgeTargetModuleId = currentModule.OriginalModuleId ?? currentModule.Id;

                        await _badgeService.EvaluateModuleBadgeAsync(
                            enrollment.StudentId,
                            enrollment.Pathway.ProfessorId,
                            badgeTargetModuleId,
                            enrollment.PathwayId,
                            modulePercentage);
                    }
                }

                await _badgeService.EvaluatePathwayBadgeAsync(
                    enrollment.StudentId,
                    enrollment.Pathway.ProfessorId,
                    enrollment.PathwayId,
                    enrollment.ProgressPercentage);

                _context.Notifications.Add(new Notification
                {
                    ReceiverId = enrollment.StudentId,
                    SenderId = enrollment.Pathway.ProfessorId,
                    Title = "Atualização de Avaliação 📊",
                    Message = $"O professor atualizou as tuas avaliações no percurso '{enrollment.Pathway.Title}'.",
                    TargetUrl = $"/pathway/{enrollment.PathwayId}",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });

                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpGet("student/{studentId}/pdf")]
        public async Task<IActionResult> DownloadGlobalStudentPdf(int studentId)
        {
            // 1. Vai buscar a estrutura base (Grelha Limpa) e as Inscrições
            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Pathway)
                    .ThenInclude(p => p.Modules)
                        .ThenInclude(m => m.Components)
                .Where(e => e.StudentId == studentId)
                .ToListAsync();

            if (enrollments == null || !enrollments.Any())
                return NotFound("Nenhuma inscrição encontrada para este aluno.");

            // 2. Vai buscar TODAS as avaliações reais deste aluno
            // NOTA: Se a tua tabela se chamar algo diferente no AppDbContext (ex: StudentEvaluations), altera aqui
            var studentEvaluations = await _context.ComponentEvaluations
                .Where(ev => ev.Enrollment != null && ev.Enrollment.StudentId == studentId)
                .ToListAsync();

            // 3. MAGIA: Cruzar a Grelha Limpa com as notas do aluno
            foreach (var enrollment in enrollments)
            {
                var evalsForThisEnrollment = studentEvaluations.Where(ev => ev.EnrollmentId == enrollment.Id).ToList();

                if (enrollment.Pathway?.Modules != null)
                {
                    foreach (var module in enrollment.Pathway.Modules)
                    {
                        if (module.Components != null)
                        {
                            foreach (var comp in module.Components)
                            {
                                // Procura se o professor deu alguma nota a este parâmetro
                                var eval = evalsForThisEnrollment.FirstOrDefault(ev => ev.ModuleComponentId == comp.Id);

                                if (eval != null)
                                {
                                    comp.Status = eval.Status; // Aplica a nota real
                                }
                                else
                                {
                                    comp.Status = ComponentStatus.Pending; // Se não tiver nota, marca como Pendente
                                }
                            }
                        }
                    }
                }
            }

            // 4. Agora sim, gerar o PDF com os dados preenchidos!
            var generator = new EvaluationPdfGenerator(enrollments);
            var pdfBytes = generator.GeneratePdf();

            var studentName = enrollments.First().Student?.DisplayShortName?.Replace(" ", " ") ?? "Aluno";
            var fileName = $"Relatorio_Avaliacao_{studentName}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}
