using GamP_SCPeriop.Server.Data;
using GamP_SCPeriop.Server.Services;
using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace GamP_SCPeriop.Server.Controllers
{
    /// <summary>
    /// Test panel endpoints: overview of every user, "log in as" and test data reset.
    /// Anonymous on purpose, so every action answers 404 outside the Development environment.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [DevelopmentOnly]
    public class DevController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;
        private readonly DbSeeder _seeder;

        public DevController(AppDbContext context, TokenService tokenService, DbSeeder seeder)
        {
            _context = context;
            _tokenService = tokenService;
            _seeder = seeder;
        }

        [HttpGet("overview")]
        public async Task<ActionResult<DevOverviewDto>> GetOverview()
        {
            var users = await _context.Users.AsNoTracking().OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync();

            var pathways = await _context.Pathways.AsNoTracking().ToListAsync();

            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Pathway)
                    .ThenInclude(p => p!.Professor)
                .ToListAsync();

            var enrollmentModules = await _context.EnrollmentModules
                .AsNoTracking()
                .Include(em => em.Module)
                    .ThenInclude(m => m!.Components)
                .Include(em => em.Module)
                    .ThenInclude(m => m!.StageTimelines)
                .ToListAsync();

            var completedEvaluations = await _context.ComponentEvaluations
                .Where(ce => ce.Status != ComponentStatus.Pending)
                .GroupBy(ce => ce.EnrollmentId)
                .Select(g => new { EnrollmentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EnrollmentId, x => x.Count);

            var badgesEarned = await _context.UserBadges
                .GroupBy(ub => ub.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var unreadNotifications = await _context.Notifications
                .Where(n => !n.IsRead)
                .GroupBy(n => n.ReceiverId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var now = DateTime.UtcNow;
            var modulesByEnrollment = enrollmentModules.ToLookup(em => em.EnrollmentId);

            var enrollmentDtos = enrollments.Select(e =>
            {
                var modules = modulesByEnrollment[e.Id].ToList();
                var totalEvaluations = modules.Sum(em => em.Module?.Components.Count ?? 0);
                var completed = completedEvaluations.GetValueOrDefault(e.Id);
                var scheduled = modules.Count(em =>
                    em.StartDate != null && em.EndDate != null &&
                    em.Module?.StageTimelines != null && em.Module.StageTimelines.Any() &&
                    em.Module.StageTimelines.All(t => t.StartDate != null && t.EndDate != null));

                return new
                {
                    e.StudentId,
                    Dto = new DevEnrollmentDto
                    {
                        EnrollmentId = e.Id,
                        PathwayId = e.PathwayId,
                        PathwayTitle = e.Pathway?.Title ?? "Sem título",
                        SupervisorName = e.Pathway?.Professor?.DisplayShortName ?? "Sem supervisor",
                        ProgressPercentage = e.ProgressPercentage,
                        MinimumApprovalScore = e.Pathway?.MinimumApprovalScore ?? 0,
                        CompletedEvaluations = completed,
                        TotalEvaluations = totalEvaluations,
                        IsArchived = e.Pathway?.IsArchived ?? false,
                        Status = EnrollmentStatusHelper.GetStatus(
                            e.ProgressPercentage, e.Pathway?.IsArchived ?? false, e.Pathway?.MinimumApprovalScore ?? 0,
                            modules.Count, scheduled, totalEvaluations, completed,
                            modules.Min(em => em.StartDate), modules.Max(em => em.EndDate), now)
                    }
                };
            }).ToList();

            var enrollmentsByStudent = enrollmentDtos.ToLookup(x => x.StudentId, x => x.Dto);
            var enrollmentsByPathway = enrollments.ToLookup(e => e.PathwayId);

            var overview = new DevOverviewDto
            {
                TestPassword = DbSeeder.TestPassword,
                Users = users.Select(u => new DevUserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    BadgesEarned = badgesEarned.GetValueOrDefault(u.Id),
                    UnreadNotifications = unreadNotifications.GetValueOrDefault(u.Id),
                    Enrollments = enrollmentsByStudent[u.Id]
                        .OrderBy(en => EnrollmentStatusHelper.GetSortOrder(en.Status))
                        .ThenBy(en => en.PathwayTitle)
                        .ToList(),
                    Pathways = pathways
                        .Where(p => p.ProfessorId == u.Id)
                        .Select(p => new DevPathwaySummaryDto
                        {
                            PathwayId = p.Id,
                            Title = p.Title,
                            IsArchived = p.IsArchived,
                            StudentCount = enrollmentsByPathway[p.Id].Count(),
                            AverageProgress = enrollmentsByPathway[p.Id].Any()
                                ? (int)enrollmentsByPathway[p.Id].Average(e => e.ProgressPercentage)
                                : 0
                        })
                        .OrderBy(p => p.IsArchived)
                        .ThenBy(p => p.Title)
                        .ToList()
                }).ToList()
            };

            return Ok(overview);
        }

        /// <summary>
        /// Returns a real login token for any user, so the panel can switch accounts without passwords
        /// </summary>
        [HttpPost("impersonate/{userId}")]
        public async Task<ActionResult<LoginResponseDto>> Impersonate(int userId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound("Utilizador não encontrado.");

            var token = _tokenService.CreateToken(user);
            user.Password = string.Empty;

            return Ok(new LoginResponseDto { Token = token, User = user });
        }

        /// <summary>
        /// Wipes the database and recreates the test data
        /// </summary>
        [HttpPost("reseed")]
        public async Task<ActionResult<SeedSummary>> Reseed()
        {
            return Ok(await _seeder.ResetAndSeedAsync());
        }
    }

    /// <summary>
    /// Hides the decorated controller/action (404) unless the app runs in the Development environment
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class DevelopmentOnlyAttribute : Attribute, IResourceFilter
    {
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var environment = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            if (!environment.IsDevelopment())
            {
                context.Result = new NotFoundResult();
            }
        }

        public void OnResourceExecuted(ResourceExecutedContext context) { }
    }
}
