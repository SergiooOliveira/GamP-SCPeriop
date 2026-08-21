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

        public EvaluationController(AppDbContext context)
        {
            _context = context;
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
        public async Task<IActionResult> SaveEvaluation([FromBody] EvaluationRequestDto request)
        {
            // 1. Guardar a nota individual na tabela ComponentEvaluations
            var existingEval = await _context.ComponentEvaluations
                .FirstOrDefaultAsync(ce => ce.EnrollmentId == request.EnrollmentId
                                        && ce.ModuleComponentId == request.ModuleComponentId);

            if (existingEval != null)
            {
                existingEval.Status = request.Status;
                existingEval.EvaluatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.ComponentEvaluations.Add(new ComponentEvaluation
                {
                    EnrollmentId = request.EnrollmentId,
                    ModuleComponentId = request.ModuleComponentId,
                    Status = request.Status,
                    EvaluatedAt = DateTime.UtcNow
                });
            }

            // Gravamos logo para que a nova nota conte para a matemática abaixo
            await _context.SaveChangesAsync();

            // 2. Recalcular a percentagem total e guardar na tabela Enrollments
            var enrollmentModules = await _context.EnrollmentModules
                .Include(em => em.Enrollment)
                    .ThenInclude(e => e.Pathway)
                .Include(em => em.Module)
                    .ThenInclude(m => m.Components)
                .Where(em => em.EnrollmentId == request.EnrollmentId)
                .ToListAsync();

            var enrollment = enrollmentModules.FirstOrDefault()?.Enrollment;

            if (enrollment != null)
            {
                var allEvaluations = await _context.ComponentEvaluations
                    .Where(ce => ce.EnrollmentId == request.EnrollmentId)
                    .ToListAsync();

                // Conta os componentes usando a lista de módulos clonados
                int totalComponents = enrollmentModules.Sum(em => em.Module?.Components?.Count(c => c.Stage != ModuleStage.Teorica) ?? 0);

                if (totalComponents > 0)
                {
                    var praticasIds = enrollmentModules
                        .SelectMany(em => em.Module?.Components ?? new List<ModuleComponent>())
                        .Where(c => c.Stage != ModuleStage.Teorica)
                        .Select(c => c.Id)
                        .ToList();

                    int completedComponents = allEvaluations.Count(ce =>
                        praticasIds.Contains(ce.ModuleComponentId) &&
                        (ce.Status == ComponentStatus.AcimaDaMedia ||
                         ce.Status == ComponentStatus.Consistente));

                    enrollment.ProgressPercentage = (int)((double)completedComponents / totalComponents * 100);
                    
                    // --- 🏆 INÍCIO DA ATRIBUIÇÃO DE BADGES (VERSÃO CONGELADA) ---
                    if (enrollment.ProgressPercentage == 100)
                    {
                        // 1. Procuramos a Badge instanciada que pertence EXATAMENTE a este Percurso
                        var pathwayBadge = await _context.Badges
                            .FirstOrDefaultAsync(b => b.PathwayId == enrollment.PathwayId);

                        if (pathwayBadge != null)
                        {
                            // 2. Garantir que não damos a mesma badge duas vezes
                            bool alreadyHasBadge = await _context.UserBadges
                                .AnyAsync(ub => ub.UserId == enrollment.StudentId && ub.BadgeId == pathwayBadge.Id);

                            if (!alreadyHasBadge)
                            {
                                // 3. Guardar a Badge no perfil do Aluno
                                _context.UserBadges.Add(new UserBadge
                                {
                                    UserId = enrollment.StudentId,
                                    BadgeId = pathwayBadge.Id,
                                    EarnedAt = DateTime.UtcNow
                                });

                                // 4. Criar a Notificação de Conquista!
                                var badgeNotification = new Notification
                                {
                                    ReceiverId = enrollment.StudentId,
                                    SenderId = enrollment.Pathway.ProfessorId,
                                    Title = "Nova Conquista! 🏆",
                                    Message = $"Parabéns! Desbloqueaste a badge '{pathwayBadge.Name}' ao concluíres o percurso {enrollment.Pathway.Title}.",
                                    TargetUrl = "/badges",
                                    CreatedAt = DateTime.UtcNow,
                                    IsRead = false
                                };

                                _context.Notifications.Add(badgeNotification);
                            }
                        }
                    }
                    // --- 🏆 FIM DA ATRIBUIÇÃO DE BADGES ---

                    // Grava a nova percentagem E a notificação de forma permanente na mesma transação
                    await _context.SaveChangesAsync();
                }
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
