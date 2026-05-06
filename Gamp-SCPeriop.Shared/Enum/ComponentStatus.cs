using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared.Enum
{
    public enum ComponentStatus
    {
        Pending,        // Gray
        InProgress,     // Blue
        Passed,         // Yellow (>= 50%)
        Approved        // Green (>= 80%)
    }
}
