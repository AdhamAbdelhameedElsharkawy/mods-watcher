using System;
using System.Collections.Generic;
using System.Text;

namespace ModsAutomator.Core.Enums
{
    public enum SyncChangeType
    {
        New,       // Found on web, not in DB
        Modified,  // Found in DB, but web has different Metadata (Size/Link)
        Stale      // In DB, but no longer found on web
    }
}
