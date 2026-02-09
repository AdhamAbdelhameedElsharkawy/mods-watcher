using System;
using System.Collections.Generic;
using System.Text;

namespace ModsAutomator.Core.Entities
{
    public class UnusedModHistory
    {
        public int Id { get; init; }

        public Guid ModId { get; init; }

        public int ModdedAppId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Version { get; init; } = "";

        public string AppVersion { get; init; } = "";

        public DateOnly? RemovedAt { get; set; }

        public string? Reason { get; set; } = "";

        public string? Description { get; set; } = "";

        public  string RootSourceUrl { get; set; }
    }
}
