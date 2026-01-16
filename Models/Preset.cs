using System.Collections.Generic;

namespace Pie.Models
{
    public class Preset
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> ProcessNames { get; set; } = new();
        public List<PresetAction> Actions { get; set; } = new();
    }

    public class PresetAction
    {
        public string Name { get; set; } = string.Empty;
        public string Shortcut { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}