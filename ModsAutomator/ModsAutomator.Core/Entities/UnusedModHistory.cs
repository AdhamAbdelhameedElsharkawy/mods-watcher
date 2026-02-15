using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace ModsAutomator.Core.Entities
{
    public class UnusedModHistory
    {
        public int Id { get; set; }

        public Guid ModId { get; init; }

        public int ModdedAppId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string AppName { get; init; } = "";

        public string AppVersion { get; init; } = "";

        public DateOnly? RemovedAt { get; set; }

        //TODO:Prompet for adding a reason
        public string? Reason { get; set; } = "";

        public string? Description { get; set; } = "";

        public  string RootSourceUrl { get; set; } = "";
    }
}
