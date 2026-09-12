using GamP_SCPeriop.Shared.Data;

namespace GamP_SCPeriop.Helpers
{
    public static class ModuleDateHelper
    {
        public static DateTime? GetActualStartDate(Module? module)
        {
            if (module?.StageTimelines == null || !module.StageTimelines.Any()) return null;
            var dates = module.StageTimelines.Where(t => t.StartDate.HasValue).Select(t => t.StartDate!.Value).ToList();
            return dates.Any() ? dates.Min() : null;
        }

        public static DateTime? GetActualEndDate(Module? module)
        {
            if (module?.StageTimelines == null || !module.StageTimelines.Any()) return null;
            var dates = module.StageTimelines.Where(t => t.EndDate.HasValue).Select(t => t.EndDate!.Value).ToList();
            return dates.Any() ? dates.Max() : null;
        }
    }
}
