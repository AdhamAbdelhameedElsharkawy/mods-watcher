using ModsAutomator.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModsAutomator.Core.DTO
{
    public class AppSummaryDto
    {
        public ModdedApp App { get; set; }
        public int ActiveCount { get; set; }
        public int PotentialUpdatesCount { get; set; }
    }
}
