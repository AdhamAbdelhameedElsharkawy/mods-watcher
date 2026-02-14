using System;
using System.Collections.Generic;
using System.Text;

namespace ModsAutomator.Core.Enums
{
    public enum WatcherStatusType : byte
    {
        Idle = 0,        // No changes detected / Initial state
        Checking = 1,    // Script is currently running
        UpdateFound = 2, // Hash mismatch detected (Trigger Glow/Badge)
        Error = 3        // XPath failed or Site is down (Trigger Alert icon)
    }
}
