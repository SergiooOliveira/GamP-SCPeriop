using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared
{
    public enum UserRole
    {
        Supervisor = 0,
        Supervisionado = 1,
        Admin = 2
    }

    public enum ComponentStatus
    {
        Pending,        // Gray
        InProgress,     // Blue
        Passed,         // Yellow (>= 50%)
        Approved        // Green (>= 80%)
    }
}