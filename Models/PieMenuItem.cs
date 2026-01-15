using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Pie.Models
{
    public enum PieMenuItemType
    {
        Application,
        Folder,
        File,
        Action,
        MediaControl,
        Group
    }

    public class GroupAppItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }

    public class PieMenuItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public PieMenuItemType Type { get; set; } = PieMenuItemType.Application;
        public ImageSource? Icon { get; set; }
        public string? CustomIconPath { get; set; }
        public string? KeyboardShortcut { get; set; }
        public int Order { get; set; }
        public bool IsRunning { get; set; }
        public IntPtr WindowHandle { get; set; }
        public string? ProcessName { get; set; }
        public List<GroupAppItem> GroupItems { get; set; } = new();

        public PieMenuItem Clone()
        {
            return new PieMenuItem
            {
                Id = Id,
                Name = Name,
                Path = Path,
                Type = Type,
                Icon = Icon,
                CustomIconPath = CustomIconPath,
                KeyboardShortcut = KeyboardShortcut,
                Order = Order,
                IsRunning = IsRunning,
                WindowHandle = WindowHandle,
                ProcessName = ProcessName,
                GroupItems = new List<GroupAppItem>(GroupItems)
            };
        }
    }
}
