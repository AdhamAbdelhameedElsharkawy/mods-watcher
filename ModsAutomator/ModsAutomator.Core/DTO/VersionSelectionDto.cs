using ModsWatcher.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModsWatcher.Core.DTO
{
    public class VersionSelectionDto
    {
        public AvailableMod Mod { get; init; }
        public bool IsSelected { get; set; }

        public bool IsPrimary { get; set; }

        public VersionSelectionDto(AvailableMod mod) => Mod = mod;
    }
}
