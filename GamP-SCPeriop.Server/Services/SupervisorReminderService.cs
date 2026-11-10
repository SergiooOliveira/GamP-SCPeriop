using GamP_SCPeriop.Server.Data;

namespace GamP_SCPeriop.Server.Services
{
    /// <summary>
    /// Runs the missing-supervisor warnings when the server starts and then once a day.
    /// A failure is logged and never stops the site.
    /// </summary>
    public class SupervisorReminderService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<SupervisorReminderService> _logger;

        public SupervisorReminderService(IServiceScopeFactory scopes, ILogger<SupervisorReminderService> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);
            do
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var sent = await SupervisorReminderJob.RunAsync(db, EvaluationWindow.TodayInPortugal(DateTime.UtcNow));
                    if (sent > 0) _logger.LogInformation("Sent {Count} missing-supervisor warnings.", sent);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Missing-supervisor warnings failed.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
