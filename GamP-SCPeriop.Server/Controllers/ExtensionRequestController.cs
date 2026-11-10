using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Server.Services;
using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Controllers
{
    /// <summary>
    /// Deadline extensions: the assigned supervisor asks, the pathway's teacher decides
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = Roles.Staff)]
    public class ExtensionRequestController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AccessService _access;
        private readonly ExtensionRequestService _requests;

        public ExtensionRequestController(AppDbContext context, AccessService access, ExtensionRequestService requests)
        {
            _context = context;
            _access = access;
            _requests = requests;
        }

        [HttpPost]
        public async Task<ActionResult<ExtensionRequestDto>> Create(ExtensionRequestCreateDto dto)
        {
            if (!await _access.CanGradeEnrollmentAsync(User, dto.EnrollmentId)) return Forbid();

            var (result, error) = await _requests.CreateAsync(dto, User.GetUserId());
            return error == null ? Ok(result) : BadRequest(error);
        }

        /// <summary>
        /// Pending requests on the Teacher's own pathways (Support: all)
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Roles = Roles.TeacherOrSupport)]
        public async Task<ActionResult<List<ExtensionRequestDto>>> GetPending()
        {
            var userId = User.GetUserId();
            var isSupport = User.IsSupport();

            var pending = _context.DeadlineExtensionRequests
                .Where(r => r.Status == ExtensionRequestStatus.Pending)
                .Where(r => isSupport || r.Enrollment!.Pathway!.ProfessorId == userId);

            return Ok(await _requests.ListAsync(pending));
        }

        /// <summary>
        /// Every request (any status) for one student's enrollment
        /// </summary>
        [HttpGet("enrollment/{enrollmentId}")]
        public async Task<ActionResult<List<ExtensionRequestDto>>> GetForEnrollment(int enrollmentId)
        {
            if (!await _access.CanViewEnrollmentAsync(User, enrollmentId)) return Forbid();

            return Ok(await _requests.ListAsync(_context.DeadlineExtensionRequests.Where(r => r.EnrollmentId == enrollmentId)));
        }

        [HttpPut("{id}/decision")]
        [Authorize(Roles = Roles.TeacherOrSupport)]
        public async Task<ActionResult<ExtensionRequestDto>> Decide(int id, ExtensionDecisionDto dto)
        {
            var enrollmentId = await _context.DeadlineExtensionRequests
                .Where(r => r.Id == id)
                .Select(r => (int?)r.EnrollmentId)
                .FirstOrDefaultAsync();
            if (enrollmentId == null) return NotFound();
            if (!await _access.CanManageEnrollmentAsync(User, enrollmentId.Value)) return Forbid();

            var (result, error) = await _requests.DecideAsync(id, dto, User.GetUserId());
            return error == null ? Ok(result) : BadRequest(error);
        }
    }
}
