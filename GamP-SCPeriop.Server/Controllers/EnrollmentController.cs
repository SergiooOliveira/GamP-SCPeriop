using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Server.Services;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GamP_SCPeriop.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EnrollmentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AccessService _access;
        private readonly PathwayService _pathways;

        public EnrollmentController(AppDbContext context, AccessService access, PathwayService pathways)
        {
            _context = context;
            _access = access;
            _pathways = pathways;
        }

        #region HttpGet
        /// <summary>
        /// Enrollments in the Teacher's own pathways (Support: all)
        /// </summary>
        [HttpGet("owned")]
        [Authorize(Roles = Roles.TeacherOrSupport)]
        public async Task<ActionResult<List<Enrollment>>> GetOwnedEnrollments()
        {
            var userId = User.GetUserId();
            var isSupport = User.IsSupport();

            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Pathway)
                .Include(e => e.Supervisor)
                .Where(e => isSupport || e.Pathway!.ProfessorId == userId)
                .ToListAsync();

            return Ok(enrollments);
        }

        /// <summary>
        /// What the logged-in user may do on this enrollment (screens use it to show or hide controls)
        /// </summary>
        [HttpGet("{id}/access")]
        public async Task<ActionResult<EnrollmentAccessDto>> GetAccess(int id)
        {
            if (!await _access.CanViewEnrollmentAsync(User, id)) return Forbid();

            var supervisor = await _context.Enrollments
                .Where(e => e.Id == id)
                .Select(e => e.Supervisor == null ? null : e.Supervisor.FullName)
                .FirstOrDefaultAsync();
            var canGrade = await _access.CanGradeEnrollmentAsync(User, id);

            return Ok(new EnrollmentAccessDto
            {
                CanEdit = await _access.CanManageEnrollmentAsync(User, id),
                CanGrade = canGrade,
                CanRequestExtension = canGrade,
                HasSupervisor = supervisor != null,
                SupervisorName = supervisor
            });
        }

        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<List<StudentDashboardCardDto>>> GetStudentEnrollments(int studentId)
        {
            // O painel do aluno: só o próprio (ou um admin)
            if (!User.IsSupport() && studentId != User.GetUserId()) return Forbid();

            // Start from the enrollments themselves, so a pathway with no modules yet still shows on the dashboard
            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Pathway)
                    .ThenInclude(p => p!.Professor)
                .Where(e => e.StudentId == studentId)
                .ToListAsync();

            if (!enrollments.Any()) return Ok(new List<StudentDashboardCardDto>());

            var enrollmentIds = enrollments.Select(e => e.Id).ToList();
            var modulesByEnrollment = (await _context.EnrollmentModules
                    .AsNoTracking()
                    .Include(em => em.Module)
                        .ThenInclude(m => m!.StageTimelines)
                    .Where(em => enrollmentIds.Contains(em.EnrollmentId))
                    .ToListAsync())
                .ToLookup(em => em.EnrollmentId);

            var dashboardCards = enrollments.Select(enrollment =>
                {
                    var group = modulesByEnrollment[enrollment.Id].ToList();

                    // Extrai as datas tanto do EnrollmentModule como das StageTimelines associadas
                    var datasInicio = group.Where(em => em.StartDate.HasValue).Select(em => em.StartDate!.Value)
                        .Concat(group.Where(em => em.Module?.StageTimelines != null)
                                     .SelectMany(em => em.Module!.StageTimelines!)
                                     .Where(t => t.StartDate.HasValue)
                                     .Select(t => t.StartDate!.Value))
                        .ToList();

                    var datasFim = group.Where(em => em.EndDate.HasValue).Select(em => em.EndDate!.Value)
                        .Concat(group.Where(em => em.Module?.StageTimelines != null)
                                     .SelectMany(em => em.Module!.StageTimelines!)
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
                        EnrollmentId = enrollment.Id,
                        PathwayId = enrollment.PathwayId,
                        PathwayTitle = enrollment.Pathway?.Title ?? "Sem título",
                        ProfessorName = enrollment.Pathway?.Professor?.DisplayShortName ?? "Sem supervisor",
                        StartDate = datasInicio.Any() ? datasInicio.Min() : (DateTime?)null,
                        LimitDate = datasFim.Any() ? datasFim.Max() : (DateTime?)null,
                        AllModulesScheduled = allModulesScheduled,
                        ProgressPercentage = enrollment.ProgressPercentage,
                        MinimumApprovalScore = enrollment.Pathway?.MinimumApprovalScore ?? 65,
                        IsStarred = enrollment.IsStarred,
                        IsHidden = enrollment.IsHidden,
                        IsArchived = enrollment.Pathway?.IsArchived ?? false,
                        IsFullyEvaluated = enrollment.ProgressPercentage == 100
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

        /// <summary>
        /// Students list for Teachers (students in their own pathways, and only those enrollments) and Support (everyone)
        /// </summary>
        [Authorize(Roles = Roles.TeacherOrSupport)]
        [HttpGet("management")]
        public async Task<ActionResult<List<StudentManagementDto>>> GetAllStudentsForManagement()
        {
            // To add new students, the pathway page lists every student through api/User/students
            var userId = User.GetUserId();
            Expression<Func<Enrollment, bool>> visible = User.IsSupport()
                ? e => true
                : e => e.Pathway!.ProfessorId == userId;

            return Ok(await BuildStudentListAsync(visible));
        }

        /// <summary>
        /// The Supervisor's students: only the enrollments assigned to them
        /// </summary>
        [Authorize(Roles = Roles.Supervisor)]
        [HttpGet("assigned")]
        public async Task<ActionResult<List<StudentManagementDto>>> GetAssignedStudents()
        {
            var userId = User.GetUserId();
            return Ok(await BuildStudentListAsync(e => e.SupervisorId == userId));
        }

        private async Task<List<StudentManagementDto>> BuildStudentListAsync(Expression<Func<Enrollment, bool>> visible)
        {
            // 1. Extração SQL: Inclui sub-consultas para as datas e para as avaliações reais
            var rawData = await _context.Users
                .Where(u => u.Role == UserRole.Student)
                .Where(u => u.Enrollments.AsQueryable().Any(visible))
                .Select(student => new
                {
                    student.Id,
                    student.FullName,
                    student.Email,
                    Enrollments = student.Enrollments
                        .AsQueryable()
                        .Where(visible)
                        .Select(e => new
                    {
                        EnrollmentId = e.Id,
                        PathwayId = e.Pathway!.Id,
                        PathwayTitle = e.Pathway.Title,
                        e.ProgressPercentage,
                        e.Pathway.IsArchived,
                        e.Pathway.MinimumApprovalScore,
                        e.SupervisorId,
                        SupervisorName = e.Supervisor == null ? null : e.Supervisor.FullName,

                        // FIX 1: count from this enrollment's actual modules, not the live template
                        TotalModules = _context.EnrollmentModules.Count(em => em.EnrollmentId == e.Id),
                        ScheduledModules = _context.EnrollmentModules.Count(em =>
                            em.EnrollmentId == e.Id &&
                            em.StartDate != null && em.EndDate != null &&
                            em.Module!.StageTimelines!.Any() &&
                            em.Module.StageTimelines!.All(t => t.StartDate != null && t.EndDate != null)),
                        MinStartDate = _context.EnrollmentModules.Where(em => em.EnrollmentId == e.Id).Min(em => em.StartDate),
                        MaxEndDate = _context.EnrollmentModules.Where(em => em.EnrollmentId == e.Id).Max(em => em.EndDate),

                        // Total = the items that can actually be graded (no theory, no weightless items, no group headers)
                        TotalEvaluations = _context.EnrollmentModules
                            .Where(em => em.EnrollmentId == e.Id)
                            .SelectMany(em => em.Module!.Components)
                            .Count(c => c.Stage != ModuleStage.Teorica && c.Weight > 0 && !c.SubComponents.Any()),
                        CompletedEvaluations = _context.ComponentEvaluations
                            .Count(ce => ce.EnrollmentId == e.Id && ce.Status != ComponentStatus.Pending)
                    }).ToList()
                })
                .ToListAsync();

            var now = DateTime.UtcNow;

            // 2. Processamento em Memória: Árvore de Decisão rigorosa com prioridades
            return rawData.Select(student => new StudentManagementDto
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
                    SupervisorId = e.SupervisorId,
                    SupervisorName = e.SupervisorName,
                    Status = EnrollmentStatusHelper.GetStatus(
                        e.ProgressPercentage, e.IsArchived, e.MinimumApprovalScore,
                        e.TotalModules, e.ScheduledModules,
                        e.TotalEvaluations, e.CompletedEvaluations,
                        e.MinStartDate, e.MaxEndDate, e.SupervisorId != null, now)
                })
                .OrderBy(p => EnrollmentStatusHelper.GetSortOrder(p.Status))
                .ThenBy(p => p.Title)
                .ToList()
            }).ToList();
        }

        #endregion

        #region HttpPost
        [HttpPost]
        [Authorize(Roles = Roles.TeacherOrSupport)]
        public async Task<ActionResult<Enrollment>> CreateEnrollment(EnrollmentDto dto)
        {
            if (!await _access.CanManagePathwayAsync(User, dto.PathwayId)) return Forbid();

            if (!await _context.Users.AnyAsync(u => u.Id == dto.StudentId && u.Role == UserRole.Student))
                return BadRequest("Só é possível inscrever alunos.");

            if (dto.SupervisorId.HasValue && !await _context.Users.AnyAsync(u => u.Id == dto.SupervisorId && u.Role == UserRole.Supervisor))
                return BadRequest("O utilizador escolhido não é um supervisor.");

            var pathway = await _context.Pathways.FindAsync(dto.PathwayId);

            if (pathway == null)
                return NotFound("Percurso não encontrado.");

            var alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.StudentId == dto.StudentId && e.PathwayId == dto.PathwayId);

            if (alreadyEnrolled)
                return BadRequest("O aluno já se encontra inscrito neste percurso."); // 400 Bad Request

            // Copies the pathway's modules for this student (shared with the test data seeder)
            var enrollment = await _pathways.EnrollStudentAsync(dto.StudentId, dto.PathwayId);

            if (dto.SupervisorId.HasValue)
            {
                var error = await _pathways.AssignSupervisorAsync(enrollment.Id, dto.SupervisorId, User.GetUserId());
                if (error != null) return BadRequest(error);
            }

            return Ok(enrollment);
        }
        #endregion

        #region HttpPut
        /// <summary>
        /// Teacher (or Support) chooses who grades this student; null removes the supervisor
        /// </summary>
        [HttpPut("{id}/supervisor")]
        [Authorize(Roles = Roles.TeacherOrSupport)]
        public async Task<IActionResult> SetSupervisor(int id, [FromBody] int? supervisorId)
        {
            if (!await _access.CanManageEnrollmentAsync(User, id)) return Forbid();

            var error = await _pathways.AssignSupervisorAsync(id, supervisorId, User.GetUserId());
            return error == null ? NoContent() : BadRequest(error);
        }

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
