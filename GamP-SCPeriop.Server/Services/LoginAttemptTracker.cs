using Microsoft.Extensions.Caching.Memory;

namespace GamP_SCPeriop.Server.Services
{
    /// <summary>
    /// Slows down password guessing: after <see cref="MaxFailures"/> wrong passwords for the same email,
    /// logins for that email are refused for <see cref="LockoutDuration"/>.
    /// Counted per email (not per IP), so a whole class logging in from the university network isn't blocked.
    /// Kept in memory: a server restart clears it, which is fine for a single server.
    /// </summary>
    public class LoginAttemptTracker
    {
        public const int MaxFailures = 5;
        public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly IMemoryCache _cache;

        public LoginAttemptTracker(IMemoryCache cache)
        {
            _cache = cache;
        }

        public bool IsLockedOut(string email) =>
            _cache.TryGetValue(Key(email), out int failures) && failures >= MaxFailures;

        public void RecordFailure(string email)
        {
            var failures = _cache.TryGetValue(Key(email), out int current) ? current + 1 : 1;
            _cache.Set(Key(email), failures, LockoutDuration); // the window restarts at every failure
        }

        public void Reset(string email) => _cache.Remove(Key(email));

        private static string Key(string email) => "login-failures:" + email.Trim().ToLowerInvariant();
    }
}
