using GamP_SCPeriop.Shared.Enum;

namespace GamP_SCPeriop.Helpers
{
    public static class ModuleStageHelper
    {
        public static IEnumerable<ModuleStage> GetTimelineStages()
        {
            return System.Enum.GetValues<ModuleStage>()
                         .Where(stage => stage != ModuleStage.Teorica);
        }
    }
}
