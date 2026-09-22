using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Server.Services;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EnrollmentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AccessService _access;

        public EnrollmentController(AppDbContext context, AccessService access)
        {
            _context = context;
            _access = access;
        }

        #region HttpGet
        [HttpGet("supervisor/{supervisorId}")]
        [Authorize(Roles = Roles.Staff)]
        public async Task<ActionResult<List<Enrollment>>> GetSupervisorEnrollments(int supervisorId)
        {
            if (!User.IsAdmin() && supervisorId != User.GetUserId()) return Forbid();

            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Pathway)
                .Where(e => e.Pathway.ProfessorId == supervisorId)
                .ToListAsync();

            return Ok(enrollments);
        }

        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<List<StudentDashboardCardDto>>> GetStudentEnrollments(int studentId)
        {
            // O painel do aluno: só o próprio (ou um admin)
            if (!User.IsAdmin() && studentId != User.GetUserId()) return Forbid();

            var enrollmentsModules = await _context.EnrollmentModules
                .Include(en => en.Enrollment)
                    .ThenInclude(e => e.Pathway)
                        .ThenInclude(p => p.Professor)
                .Include(en => en.Module)
                    .ThenInclude(m => m.StageTimelines)
                .Where(en => en.Enrollment != null && en.Enrollment.StudentId == studentId)
                .ToListAsync();

            if (!enrollmentsModules.Any()) return Ok(new List<StudentDashboardCardDto>());

            var dashboardCards = enrollmentsModules
                .GroupBy(em => em.EnrollmentId)
                .Select(group =>
                {
                    var line = group.First();

                    // Extrai as datas tanto do EnrollmentModule como das StageTimelines associadas
                    var datasInicio = group.Where(em => em.StartDate.HasValue).Select(em => em.StartDate!.Value)
                        .Concat(group.Where(em => em.Module?.StageTimelines != null)
                                     .SelectMany(em => em.Module!.StageTimelines)
                                     .Where(t => t.StartDate.HasValue)
                                     .Select(t => t.StartDate!.Value))
                        .ToList();

                    var datasFim = group.Where(em => em.EndDate.HasValue).Select(em => em.EndDate!.Value)
                        .Concat(group.Where(em => em.Module?.StageTimelines != null)
                                     .SelectMany(em => em.Module!.StageTimelines)
                                     .Where(t => t.EndDate.HasValue)
                                     .Select(t => t.EndDate!.Value))
                        .ToList();

                    // Scheduled = EnrollmentModule has both dates AND its Module's StageTimelines are fully filled
                    bool allModulesScheduled = group.Any() && group.All(em =>
                        em.StartDate.HasValue && em.EndDate.HasValue &&
                        em.Module?.StageTimelines != null && em.Module.StageTimelines.Any() &&
                        em.Module.StageTimelines.All(t => t.StartDate.HasValue && t.EndDate.HasValue));

                    return new StudentDashboardCardDto
                    {
                        EnrollmentId = line.EnrollmentId,
                        PathwayId = line.Enrollment?.PathwayId ?? 0,
                        PathwayTitle = line.Enrollment?.Pathway?.Title ?? "Sem título",
                        ProfessorName = line.Enrollment?.Pathway?.Professor?.DisplayShortName ?? "Sem supervisor",
                        StartDate = datasInicio.Any() ? datasInicio.Min() : (DateTime?)null,
                        LimitDate = datasFim.Any() ? datasFim.Max() : (DateTime?)null,
                        AllModulesScheduled = allModulesScheduled,
                        ProgressPercentage = line.Enrollment?.ProgressPercentage ?? 0,
                        MinimumApprovalScore = line.Enrollment?.Pathway?.MinimumApprovalScore ?? 65,
                        IsStarred = line.Enrollment?.IsStarred ?? false,
                        IsHidden = line.Enrollment?.IsHidden ?? false,
                        IsArchived = line.Enrollment?.Pathway?.IsArchived ?? false,
                        IsFullyEvaluated = (line.Enrollment?.ProgressPercentage ?? 0) == 100
                    };
                }).ToList();

            return Ok(dashboardCards);
        }

        [HttpGet("student/{studentId}/pathway/{pathwayId}")]
        public async Task<ActionResult<IEnumerable<EnrollmentModule>>> GetEnrollmentDetails(int studentId, int pathwayId)
        {
            var enrollmentDetails = await _context.EnrollmentModules
                .Include(em => em.Enrollment)
                    .ThenInclude(e => e.Pathway)
                        .ThenInclude(p => p.Professor)
                .Include(em => em.Module)
                    .ThenInclude(m => m.Components)
                .Include(em => em.Module)                                // <-- 1. INCLUIR AS DATAS AQUI
                    .ThenInclude(m => m.StageTimelines)
                .Where(em => em.Enrollment != null
                        && em.Enrollment.StudentId == studentId
                        && em.Enrollment.PathwayId == pathwayId)
                .ToListAsync();

            if (!enrollmentDetails.Any()) return NotFound();

            var enrollmentId = enrollmentDetails.First().EnrollmentId;
            if (!await _access.CanViewEnrollmentAsync(User, enrollmentId)) return Forbid();

            var evaluations = await _context.ComponentEvaluations
                .Where(ce => ce.EnrollmentId == enrollmentId)
                .ToListAsync();

            foreach (var em in enrollmentDetails)
            {
                // 2. HIGIENE DE SEGURANÇA: Apagar dados sensíveis antes de enviar
                if (em.Enrollment?.Pathway?.Professor != null)
                {
                    var prof = em.Enrollment.Pathway.Professor;
                    prof.Password = string.Empty; // Impede que a hash vá para a net
                    prof.Email = string.Empty;
                }

                // 3. Injetar notas (O teu código original)
                if (em.Module?.Components != null)
                {
                    foreach (var comp in em.Module.Components)
                    {
                        var eval = evaluations.FirstOrDefault(e => e.ModuleComponentId == comp.Id);
                        comp.Status = eval != null ? eval.Status : ComponentStatus.Pending;
                    }
                }
            }

            return Ok(enrollmentDetails);
        }

        [Authorize(Roles = Roles.Staff)]
        [HttpGet("management")]
        public async Task<ActionResult<List<StudentManagementDto>>> GetAllStudentsForManagement()
        {
            // Supervisors only see the students they have enrolled in their pathways, and only that progress.
            // (To add new students, the pathway page lists every student through api/User/students.)
            var isAdmin = User.IsAdmin();
            var supervisorId = User.GetUserId();

            // 1. Extração SQL: Inclui sub-consultas para as datas e para as avaliações reais
            var rawData = await _context.Users
                .Where(u => u.Role == UserRole.Supervisionado)
                .Where(u => isAdmin || u.Enrollments.Any(e => e.Pathway!.ProfessorId == supervisorId))
                .Select(student => new
                {
                    student.Id,
                    student.FullName,
                    student.Email,
                    Enrollments = student.Enrollments
                        .Where(e => isAdmin || e.Pathway.ProfessorId == supervisorId)
                        .Select(e => new
                    {
                        EnrollmentId = e.Id,
                        PathwayId = e.Pathway.Id,
                        PathwayTitle = e.Pathway.Title,
                        e.ProgressPercentage,
                        e.Pathway.IsArchived,
                        e.Pathway.MinimumApprovalScore,

                        // FIX 1: count from this enrollment's actual modules, not the live template
                        TotalModules = _context.EnrollmentModules.Count(em => em.EnrollmentId == e.Id),
                        ScheduledModules = _context.EnrollmentModules.Count(em =>
                            em.EnrollmentId == e.Id &&
                            em.StartDate != null && em.EndDate != null &&
                            em.Module.StageTimelines.Any() &&
                            em.Module.StageTimelines.All(t => t.StartDate != null && t.EndDate != null)),
                        MinStartDate = _context.EnrollmentModules.Where(em => em.EnrollmentId == e.Id).Min(em => em.StartDate),
                        MaxEndDate = _context.EnrollmentModules.Where(em => em.EnrollmentId == e.Id).Max(em => em.EndDate),

                        // FIX 2: total = every component that belongs to this enrollment's actual modules, not just the ones with a row already
                        TotalEvaluations = _context.EnrollmentModules
                            .Where(em => em.EnrollmentId == e.Id)
                            .SelectMany(em => em.Module.Components)
                            .Count(),
                        CompletedEvaluations = _context.ComponentEvaluations
                            .Count(ce => ce.EnrollmentId == e.Id && ce.Status != ComponentStatus.Pending)
                    }).ToList()
                })
                .ToListAsync();

            var now = DateTime.UtcNow;

            // 2. Processamento em Memória: Árvore de Decisão rigorosa com prioridades
            var studentsResult = rawData.Select(student => new StudentManagementDto
            {
                StudentId = student.Id,
                FullName = student.FullName,
                Email = student.Email,
                LastAccess = null,

                OverallProgress = student.Enrollments.Any()
                    ? (int)student.Enrollments.Average(e => e.ProgressPercentage)
                    : 0,

                ActivePathways = student.Enrollments.Select(e => new PathwayTagDto
                {
                    PathwayId = e.PathwayId,
                    Title = e.PathwayTitle,
                    EnrollmentId = e.EnrollmentId,
                    Status = EnrollmentStatusHelper.GetStatus(
                        e.ProgressPercentage, e.IsArchived, e.MinimumApprovalScore,
                        e.TotalModules, e.ScheduledModules,
                        e.TotalEvaluations, e.CompletedEvaluations,
                        e.MinStartDate, e.MaxEndDate, now)
                })
                .OrderBy(p => EnrollmentStatusHelper.GetSortOrder(p.Status))
                .ThenBy(p => p.Title)
                .ToList()
            }).ToList();

            return Ok(studentsResult);
        }

        #endregion

        #region HttpPost
        [HttpPost]
        [Authorize(Roles = Roles.Staff)]
        public async Task<ActionResult<Enrollment>> CreateEnrollment(EnrollmentDto dto)
        {
            if (!await _access.CanManagePathwayAsync(User, dto.PathwayId)) return Forbid();

            if (!await _context.Users.AnyAsync(u => u.Id == dto.StudentId && u.Role == UserRole.Supervisionado))
                return BadRequest("Só é possível inscrever alunos.");

            var pathway = await _context.Pathways.FindAsync(dto.PathwayId);

            if (pathway == null)
                return NotFound("Percurso não encontrado.");

            var alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.StudentId == dto.StudentId && e.PathwayId == dto.PathwayId);

            if (alreadyEnrolled)
                return BadRequest("O aluno já se encontra inscrito neste percurso."); // 400 Bad Request

            // Map the DTO to your real Entity
            var enrollment = new Enrollment
            {
                StudentId = dto.StudentId,
                PathwayId = dto.PathwayId,
                ProgressPercentage = 0,
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            var pathwayModules = await _context.Modules
                .Include(m => m.StageTimelines)
                .Include(m => m.Components)
                .Where(m => m.PathwayId == dto.PathwayId)
                .ToListAsync();

            foreach (var baseModule in pathwayModules)
            {
                var clonedModule = new Module
                {
                    Title = baseModule.Title,
                    Weight = baseModule.Weight,
                    PathwayId = null,
                    IsFromTemplate = true,
                    OriginalModuleId = baseModule.Id
                };

                _context.Modules.Add(clonedModule);
                await _context.SaveChangesAsync();

                var clonedTimelines = new List<ModuleStageTimelineDto>();

                if (baseModule.StageTimelines != null)
                {
                    foreach (var timeline in baseModule.StageTimelines)
                    {
                        var newTimeline = new ModuleStageTimelineDto
                        {
                            ModuleId = clonedModule.Id,
                            Stage = timeline.Stage,
                            StartDate = timeline.StartDate,
                            EndDate = timeline.EndDate
                        };

                        _context.ModuleStageTimelines.Add(newTimeline);
                        clonedTimelines.Add(newTimeline);
                    }
                }

                // 5. CLONAR COMPONENTES (Com respeito pela Hierarquia Pai -> Filho)
                if (baseModule.Components != null)
                {
                    // Dicionário para guardar a correspondência entre o ID Antigo do Pai e o ID Novo do Clone
                    var parentIdMap = new Dictionary<int, int>();

                    // 5.1. Clonar apenas as tarefas Principais (Pais)
                    var parents = baseModule.Components.Where(c => c.ParentComponentId == null).ToList();
                    foreach (var parent in parents)
                    {
                        var clonedParent = new ModuleComponent
                        {
                            ModuleId = clonedModule.Id,
                            Stage = parent.Stage,
                            Title = parent.Title,
                            Description = parent.Description,
                            Weight = parent.Weight,
                            PdfFilePath = parent.PdfFilePath,
                            ParentComponentId = null,
                            IsFromTemplate = true
                        };

                        _context.ModuleComponents.Add(clonedParent);
                        await _context.SaveChangesAsync(); // Gerar o novo ID deste Pai
                        parentIdMap[parent.Id] = clonedParent.Id; // Guardar no mapa para os filhos saberem a quem pertencer
                    }

                    // 5.2. Clonar as Sub-tarefas (Filhos)
                    var children = baseModule.Components.Where(c => c.ParentComponentId != null).ToList();
                    foreach (var child in children)
                    {
                        // Verifica quem era o pai antigo e vai buscar o ID do pai clonado
                        if (child.ParentComponentId.HasValue && parentIdMap.TryGetValue(child.ParentComponentId.Value, out int newParentId))
                        {
                            var clonedChild = new ModuleComponent
                            {
                                ModuleId = clonedModule.Id,
                                Stage = child.Stage,
                                Title = child.Title,
                                Description = child.Description,
                                Weight = child.Weight,
                                PdfFilePath = child.PdfFilePath,
                                ParentComponentId = newParentId,
                                IsFromTemplate = true
                            };
                            _context.ModuleComponents.Add(clonedChild);
                        }
                    }
                }

                // 6. LIGAR O NOVO CLONE À INSCRIÇÃO
                var inheritedStart = clonedTimelines.Where(t => t.StartDate.HasValue).Select(t => t.StartDate).Min();
                var inheritedEnd = clonedTimelines.Where(t => t.EndDate.HasValue).Select(t => t.EndDate).Max();

                var enrollmentModule = new EnrollmentModule
                {
                    EnrollmentId = enrollment.Id,
                    ModuleId = clonedModule.Id, // Agora sim, atrelamos o clone independente!
                    StartDate = inheritedStart,
                    EndDate = inheritedEnd,
                };
                _context.EnrollmentModules.Add(enrollmentModule);
            }

            await _context.SaveChangesAsync();

            return Ok(enrollment);
        }
        #endregion

        #region HttpPut
        [HttpPut("{id}/star")]
        public async Task<IActionResult> ToggleStar(int id, [FromBody] bool isStarred)
        {
            // Preferências do painel: só o próprio aluno
            var studentId = User.GetUserId();
            var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.Id == id && e.StudentId == studentId);
            if (enrollment == null)
            {
                return NotFound("Inscrição não encontrada.");
            }

            enrollment.IsStarred = isStarred;
            await _context.SaveChangesAsync();

            return NoContent(); // 204 Sucesso sem devolver conteúdo
        }

        [HttpPut("{id}/hidden")]
        public async Task<IActionResult> ToggleHidden(int id, [FromBody] bool isHidden)
        {
            var studentId = User.GetUserId();
            var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.Id == id && e.StudentId == studentId);
            if (enrollment == null)
            {
                return NotFound("Inscrição não encontrada.");
            }

            enrollment.IsHidden = isHidden;
            await _context.SaveChangesAsync();

            return NoContent(); // 204 Sucesso sem devolver conteúdo
        }
        #endregion
    }
}
