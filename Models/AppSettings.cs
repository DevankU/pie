using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace Pie.Models
{
    public enum PieMenuMode
    {
        Switcher,
        Launcher,
        Controller,
        MusicRemote
    }

    public enum SoundTheme
    {
        Default,
        Playful,
        Minimal,
        Silent
    }

    public class AppControllerConfig
    {
        public string ProcessName { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public List<ControllerAction> Actions { get; set; } = new();
    }

    public class ControllerAction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string KeyboardShortcut { get; set; } = string.Empty;
        public string? IconPath { get; set; }
    }

    public class AppSettings
    {
        public string ActivationHotkey { get; set; } = "Ctrl+Space";
        public Key ActivationKey { get; set; } = Key.Space;
        public ModifierKeys ActivationModifiers { get; set; } = ModifierKeys.Control;

        public PieMenuMode DefaultMode { get; set; } = PieMenuMode.Switcher;

        public SoundTheme SoundTheme { get; set; } = SoundTheme.Default;
        public double SoundVolume { get; set; } = 0.5;
        public bool SoundsEnabled { get; set; } = true;
        public bool HapticsEnabled { get; set; } = true;

        public double AnimationSpeed { get; set; } = 1.0;
        public double MenuRadius { get; set; } = 180;
        public double IconSize { get; set; } = 48;

        public bool LaunchAtStartup { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;
        public bool ShowInTaskbar { get; set; } = false;

        public List<string> ExcludedApps { get; set; } = new() { "explorer", "SystemSettings", "TextInputHost" };
        public List<PieMenuItemData> LauncherItems { get; set; } = new();
        public List<AppControllerConfig> ControllerConfigs { get; set; } = new();

        public bool DarkMode { get; set; } = true;

        // Double-tap settings (for 3-finger tap / middle mouse)
        public bool DoubleTapEnabled { get; set; } = true;
        public int DoubleTapTimeoutMs { get; set; } = 700;
        public PieMenuMode DoubleTapMode { get; set; } = PieMenuMode.Launcher;

        // Visual Flow Configuration (Right-Click Cycle)
        // Key = Current Mode, Value = Next Mode on Right-Click
        public Dictionary<PieMenuMode, PieMenuMode> RightClickFlow { get; set; } = new()
        {
            { PieMenuMode.Switcher, PieMenuMode.Launcher },
            { PieMenuMode.Launcher, PieMenuMode.Controller },
            { PieMenuMode.Controller, PieMenuMode.MusicRemote },
            { PieMenuMode.MusicRemote, PieMenuMode.Switcher }
        };
    }

    public class PieMenuItemData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public PieMenuItemType Type { get; set; } = PieMenuItemType.Application;
        public string? CustomIconPath { get; set; }
        public int Order { get; set; }
        public List<GroupAppItem> GroupItems { get; set; } = new();
    }
}
