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
        public async Task<ActionResult<EnrollmentModule>> GetEnrollmentForEvaluation(int id)
        {
            // Vai buscar a inscrição com o Aluno e todo o "Molde" do Pathway
            var enrollment = await _context.EnrollmentModules
                .Include(e => e.Enrollment.Student)
                .Include(e => e.Enrollment.Pathway)
                    .ThenInclude(p => p.Modules)
                        .ThenInclude(m => m.Components)
                .Include(e => e.Enrollment.Pathway)
                    .ThenInclude(p => p.Modules)
                        .ThenInclude(m => m.StageTimelines)
                .FirstOrDefaultAsync(e => e.EnrollmentId == id);

            if (enrollment == null) return NotFound();

            // Vai buscar as avaliações que o professor já fez a este aluno no passado
            var evaluations = await _context.ComponentEvaluations
                .Where(ce => ce.EnrollmentId == id)
                .ToListAsync();

            // Injeta as notas gravadas na propriedade [NotMapped] para o Front-end ler as cores certas!
            if (enrollment.Enrollment?.Pathway?.Modules != null)
            {
                foreach (var module in enrollment.Enrollment.Pathway.Modules)
                {
                    if (module.Components != null)
                    {
                        foreach (var component in module.Components)
                        {
                            var eval = evaluations.FirstOrDefault(e => e.ModuleComponentId == component.Id);
                            component.Status = eval != null ? eval.Status : ComponentStatus.Pending;
                        }
                    }
                }
            }

            return Ok(enrollment);
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
            var enrollment = await _context.Enrollments
                .Include(e => e.Pathway)
                    .ThenInclude(p => p.Modules)
                        .ThenInclude(m => m.Components)
                .FirstOrDefaultAsync(e => e.Id == request.EnrollmentId);

            if (enrollment != null && enrollment.Pathway != null)
            {
                var allEvaluations = await _context.ComponentEvaluations
                    .Where(ce => ce.EnrollmentId == request.EnrollmentId)
                    .ToListAsync();

                // Conta quantos componentes existem no total deste Pathway
                int totalComponents = enrollment.Pathway.Modules.Sum(m => m.Components?.Count(c => c.Stage != ModuleStage.Teorica) ?? 0);

                if (totalComponents > 0)
                {
                    // 1. Criamos uma lista apenas com os IDs das componentes práticas
                    var praticasIds = enrollment.Pathway.Modules
                        .SelectMany(m => m.Components ?? new List<ModuleComponent>())
                        .Where(c => c.Stage != ModuleStage.Teorica)
                        .Select(c => c.Id)
                        .ToList();

                    // 2. Cruzamos as avaliações com essa lista para garantir que ignoramos as notas "fantasma" das teóricas
                    int completedComponents = allEvaluations.Count(ce =>
                        praticasIds.Contains(ce.ModuleComponentId) &&
                        (ce.Status == ComponentStatus.AcimaDaMedia ||
                         ce.Status == ComponentStatus.Consistente));

                    // Atualiza o objeto Enrollment
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
