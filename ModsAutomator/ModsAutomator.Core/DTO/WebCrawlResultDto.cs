using ModsAutomator.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModsAutomator.Core.DTO
{
    public class WebCrawlResultDto
    {
        public Guid ModId { get; set; }
        public List<AvailableMod> Versions { get; set; } = new();
    }
}
