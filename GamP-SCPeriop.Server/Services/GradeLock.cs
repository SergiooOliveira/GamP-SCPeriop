using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Enum;

namespace GamP_SCPeriop.Server.Services
{
    /// <summary>
    /// A grade can only be changed during the week it was given (Monday to Sunday, Portuguese time).
    /// From the following Monday on it is final.
    /// </summary>
    public static class GradeLock
    {
        private static readonly TimeZoneInfo PortugalTime = FindPortugalTime();

        /// <summary>
        /// Monday 00:00 of the current week in Portugal, as UTC (evaluations are stored in UTC)
        /// </summary>
        public static DateTime CurrentWeekStartUtc(DateTime utcNow)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(utcNow, PortugalTime);
            int daysSinceMonday = ((int)local.DayOfWeek + 6) % 7;
            var mondayLocal = DateTime.SpecifyKind(local.Date.AddDays(-daysSinceMonday), DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(mondayLocal, PortugalTime);
        }

        public static DateTime TodayInPortugal(DateTime utcNow) =>
            TimeZoneInfo.ConvertTimeFromUtc(utcNow, PortugalTime).Date;

        /// <summary>
        /// Only real grades lock; an empty (pending) evaluation can always be filled in
        /// </summary>
        public static bool IsLocked(ComponentEvaluation evaluation, DateTime weekStartUtc) =>
            evaluation.Status != ComponentStatus.Pending && evaluation.EvaluatedAt < weekStartUtc;

        private static TimeZoneInfo FindPortugalTime()
        {
            // IANA id works on Linux and on Windows with .NET 6+; fall back to the Windows id just in case
            try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon"); }
            catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"); }
        }
    }
}
