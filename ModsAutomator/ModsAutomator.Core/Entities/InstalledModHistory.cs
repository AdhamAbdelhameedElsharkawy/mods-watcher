using System;
using System.Collections.Generic;
using System.Text;

namespace ModsAutomator.Core.Entities
{
    public class InstalledModHistory
    {
        public int InternalId { get; init; }

        public Guid ModId { get; init; }

        public string Version { get; init; } = "";

        public string AppVersion { get; init; } = "";

        public DateOnly InstalledAt { get; set; }

        public DateOnly? RemovedAt { get; set; }

        public string? LocalFilePath { get; set; } = "";

        public bool IsRollbackTarget { get; set; }
    }
}
