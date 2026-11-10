using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Services
{
    /// <summary>
    /// Deadline extensions: the supervisor asks for a later end date on one stage of a student's module,
    /// the pathway's teacher approves it (possibly with another date) or rejects it.
    /// Permission checks are done by the controller (AccessService); this class enforces the data rules.
    /// </summary>
    public class ExtensionRequestService
    {
        public const string DateNotLater = "A nova data tem de ser posterior à data de fim atual.";

        private readonly AppDbContext _context;

        public ExtensionRequestService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(ExtensionRequestDto? Result, string? Error)> CreateAsync(ExtensionRequestCreateDto dto, int requestedById)
        {
            var belongsToEnrollment = await _context.EnrollmentModules
                .AnyAsync(em => em.EnrollmentId == dto.EnrollmentId && em.ModuleId == dto.ModuleId);
            if (!belongsToEnrollment) return (null, "Este módulo não pertence a esta inscrição.");

            var timeline = await FindTimelineAsync(dto.ModuleId, dto.Stage);
            if (timeline?.EndDate == null) return (null, "Esta fase não tem data de fim definida.");
            if (dto.RequestedEndDate.Date <= timeline.EndDate.Value.Date) return (null, DateNotLater);

            var alreadyPending = await _context.DeadlineExtensionRequests.AnyAsync(r =>
                r.EnrollmentId == dto.EnrollmentId && r.ModuleId == dto.ModuleId &&
                r.Stage == dto.Stage && r.Status == ExtensionRequestStatus.Pending);
            if (alreadyPending) return (null, "Já existe um pedido pendente para esta fase.");

            var request = new DeadlineExtensionRequest
            {
                EnrollmentId = dto.EnrollmentId,
                ModuleId = dto.ModuleId,
                Stage = dto.Stage,
                RequestedById = requestedById,
                CurrentEndDate = timeline.EndDate.Value.Date,
                RequestedEndDate = dto.RequestedEndDate.Date,
                Reason = string.IsNullOrWhiteSpace(dto.Reason) ? null : dto.Reason.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _context.DeadlineExtensionRequests.Add(request);

            var info = await LoadInfoAsync(dto.EnrollmentId, dto.ModuleId, requestedById);
            _context.Notifications.Add(new Notification
            {
                ReceiverId = info.TeacherId,
                SenderId = requestedById,
                Title = "Pedido de prolongamento",
                Message = $"{info.RequestedByName} pede prolongamento de \"{dto.Stage.DisplayName()}\" ({info.ModuleTitle}) de {info.StudentName} até {request.RequestedEndDate:dd/MM/yyyy}.",
                TargetUrl = "/extension-requests",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return (await ToDtoAsync(request.Id), null);
        }

        public async Task<(ExtensionRequestDto? Result, string? Error)> DecideAsync(int requestId, ExtensionDecisionDto dto, int decidedById)
        {
            var request = await _context.DeadlineExtensionRequests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null) return (null, "Pedido não encontrado.");
            if (request.Status != ExtensionRequestStatus.Pending) return (null, "Este pedido já foi decidido.");

            var timeline = await FindTimelineAsync(request.ModuleId, request.Stage);
            if (timeline?.EndDate == null) return (null, "Esta fase não tem data de fim definida.");

            string message;
            if (dto.Approve)
            {
                // Checked against today's end date: the teacher may have changed it after the request was made
                var newEnd = (dto.EndDate ?? request.RequestedEndDate).Date;
                if (newEnd <= timeline.EndDate.Value.Date) return (null, DateNotLater);

                timeline.EndDate = newEnd;

                // The module's overall end date (shown on the student's dashboard) follows its latest stage
                var link = await _context.EnrollmentModules
                    .FirstOrDefaultAsync(em => em.EnrollmentId == request.EnrollmentId && em.ModuleId == request.ModuleId);
                if (link != null && (link.EndDate == null || link.EndDate < newEnd)) link.EndDate = newEnd;

                request.Status = ExtensionRequestStatus.Approved;
                request.DecidedEndDate = newEnd;
                message = $"A fase \"{request.Stage.DisplayName()}\" pode ser avaliada até {newEnd:dd/MM/yyyy}.";
            }
            else
            {
                request.Status = ExtensionRequestStatus.Rejected;
                message = $"O pedido para \"{request.Stage.DisplayName()}\" foi recusado." +
                          (string.IsNullOrWhiteSpace(dto.Note) ? "" : $" {dto.Note.Trim()}");
            }

            request.DecidedById = decidedById;
            request.DecidedAt = DateTime.UtcNow;
            request.DecisionNote = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();

            _context.Notifications.Add(new Notification
            {
                ReceiverId = request.RequestedById,
                SenderId = decidedById,
                Title = dto.Approve ? "Prolongamento aprovado" : "Prolongamento recusado",
                Message = message,
                TargetUrl = $"/evaluate/{request.EnrollmentId}",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return (await ToDtoAsync(request.Id), null);
        }

        /// <summary>
        /// Requests as shown on screen, newest first
        /// </summary>
        public async Task<List<ExtensionRequestDto>> ListAsync(IQueryable<DeadlineExtensionRequest> requests)
        {
            var rows = await requests
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    Request = r,
                    StudentName = r.Enrollment!.Student!.FullName,
                    PathwayTitle = r.Enrollment.Pathway!.Title,
                    ModuleTitle = _context.Modules.Where(m => m.Id == r.ModuleId).Select(m => m.Title).FirstOrDefault(),
                    RequestedByName = _context.Users.Where(u => u.Id == r.RequestedById).Select(u => u.FullName).FirstOrDefault()
                })
                .ToListAsync();

            return rows.Select(x => new ExtensionRequestDto
            {
                Id = x.Request.Id,
                EnrollmentId = x.Request.EnrollmentId,
                ModuleId = x.Request.ModuleId,
                StudentName = x.StudentName,
                PathwayTitle = x.PathwayTitle,
                ModuleTitle = x.ModuleTitle ?? string.Empty,
                Stage = x.Request.Stage,
                RequestedByName = x.RequestedByName ?? string.Empty,
                CurrentEndDate = x.Request.CurrentEndDate,
                RequestedEndDate = x.Request.RequestedEndDate,
                Reason = x.Request.Reason,
                Status = x.Request.Status,
                DecidedEndDate = x.Request.DecidedEndDate,
                DecisionNote = x.Request.DecisionNote,
                CreatedAt = x.Request.CreatedAt
            }).ToList();
        }

        private async Task<ExtensionRequestDto> ToDtoAsync(int requestId) =>
            (await ListAsync(_context.DeadlineExtensionRequests.Where(r => r.Id == requestId))).Single();

        private Task<ModuleStageTimelineDto?> FindTimelineAsync(int moduleId, ModuleStage stage) =>
            _context.ModuleStageTimelines.FirstOrDefaultAsync(t => t.ModuleId == moduleId && t.Stage == stage);

        private async Task<(int TeacherId, string StudentName, string ModuleTitle, string RequestedByName)> LoadInfoAsync(
            int enrollmentId, int moduleId, int requestedById)
        {
            var enrollment = await _context.Enrollments
                .Where(e => e.Id == enrollmentId)
                .Select(e => new { e.Pathway!.ProfessorId, StudentName = e.Student!.FullName })
                .SingleAsync();
            var moduleTitle = await _context.Modules.Where(m => m.Id == moduleId).Select(m => m.Title).SingleAsync();
            var requestedBy = await _context.Users.Where(u => u.Id == requestedById).Select(u => u.FullName).SingleAsync();

            return (enrollment.ProfessorId, enrollment.StudentName, moduleTitle, requestedBy);
        }
    }
}
