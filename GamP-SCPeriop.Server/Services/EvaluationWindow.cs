namespace GamP_SCPeriop.Server.Services
{
    /// <summary>
    /// Each stage has an evaluation period set by the supervisor (its start and end dates).
    /// Grades can be given and changed from the start date until the end date (inclusive);
    /// from the next day on, the stage is closed and its grades are final.
    /// </summary>
    public static class EvaluationWindow
    {
        private static readonly TimeZoneInfo PortugalTime = FindPortugalTime();

        public static DateTime TodayInPortugal(DateTime utcNow) =>
            TimeZoneInfo.ConvertTimeFromUtc(utcNow, PortugalTime).Date;

        public static bool HasNotStarted(DateTime? startDate, DateTime today) =>
            startDate.HasValue && today < startDate.Value.Date;

        public static bool HasEnded(DateTime? endDate, DateTime today) =>
            endDate.HasValue && today > endDate.Value.Date;

        private static TimeZoneInfo FindPortugalTime()
        {
            // IANA id works on Linux and on Windows with .NET 6+; fall back to the Windows id just in case
            try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon"); }
            catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"); }
        }
    }
}
