# Pie

A radial pie menu launcher for Windows. Quick access to your apps, window switching, media controls, and custom shortcuts - all from a slick circular menu.


https://github.com/user-attachments/assets/06160b3e-37ac-4fdd-931e-28865a740a72


   

![.NET 6](https://img.shields.io/badge/.NET-6.0-purple)
![Windows](https://img.shields.io/badge/Platform-Windows-blue)

UNDER DEVELOPMENT — FEATURES MAY BE UNSTABLE. REPORT BUGS IN ISSUES.

## What is this?

Pie is a productivity tool that gives you a circular menu (like in video games) to:

- **Switch between open windows** - No more Alt+Tab hunting
- **Launch your favorite apps** - Pin the apps you use most
- **Control media playback** - Play/pause, next/previous, volume
- **Trigger keyboard shortcuts** - Set up custom actions per app

Activate it with a hotkey (default: `Ctrl+Space`) or a 3-finger tap on your touchpad( in windows settings switch the 3 finger tap -> middle mouse click).

## What's New in v0.2 Alpha 🚀

- **Group Management:** Create app groups (folders) to organize your launcher.
- **Performance Overhaul:**  async loading and icon caching.
- **Double-Tap Trigger:** Configure double-tap actions 
- **Enhanced Settings:**
  - Drag-and-drop to reorder items.
  - Multi-select when adding apps.
  - New "Running Apps" exclusion picker.
- **Smoother & Faster:** Optimized logging and resource handling.


## Features

- Smooth animations
- DPI aware (works great on high-res displays)
- Runs in system tray
- Customizable hotkeys
- Multiple modes: Switcher, Launcher, Controller, Music Remote

## Requirements

- Windows 10/11
- .NET 6.0 Runtime (or build with self-contained option)

## Installation

### Option 1: Build from source

1. Make sure you have [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) installed

2. Clone this repo
   ```
   git clone https://github.com/yourusername/Pie.git
   cd Pie
   ```

3. Build it
   ```
   dotnet build
   ```

4. Run it
   ```
   dotnet run
   ```

### Option 2: Create a standalone executable

For a portable .exe you can run anywhere:

```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
```

This creates a single `Pie.exe` in the `publish` folder. Copy it wherever you want and run it.

### Option 3: Framework-dependent (smaller size)

If you already have .NET 6 runtime installed:

```bash
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish
```

## Usage

1. Run `Pie.exe`
2. Look for the pie icon in your system tray
3. Press `Ctrl+Space` (or use 3-finger tap) to open the menu
4. Move your mouse to select, click to confirm
5. Right-click the tray icon for settings

### Modes

- **Switcher** - Shows all your open windows. Click to switch to that app.
- **Launcher** - Your pinned apps and folders. Add them in Settings.
- **Controller** - Custom keyboard shortcuts for specific apps (like media keys for Spotify).
- **Music Remote** - Play/pause, skip tracks, volume controls.

### Settings

Right-click the tray icon and hit "Settings" to:

- Change the activation hotkey
- Add/remove launcher items
- Set up app-specific shortcuts
- Adjust menu size and appearance
- Enable/disable sounds

## Building for Development

```bash
# Clone
git clone https://github.com/yourusername/Pie.git
cd Pie

# Restore packages
dotnet restore

# Build debug
dotnet build

# Run
dotnet run
```

## Folder Structure

```
Pie/
├── Controls/          # Custom WPF controls (the pie menu itself)
├── Helpers/           # Utility classes
├── Models/            # Data models
├── Services/          # Core services (hotkey, window management, etc.)
├── Views/             # Windows (settings, pie menu window)
├── App.xaml           # App entry point
└── Pie.csproj         # Project file
```

## Tips

- Double-click the tray icon to open settings
- Press `Escape` to close the menu without selecting
- Click in the center or outside the ring to cancel
- The menu follows your cursor - it opens right where your mouse is

## Troubleshooting

**Menu doesn't appear?**
- Check if the app is running (look for tray icon)
- Try a different hotkey in settings
- Make sure no other app is using the same hotkey

**Tray icon not visible?**
- Click the ^ arrow in your taskbar to see hidden icons
- Drag the Pie icon to your visible tray area

**App crashes on startup?**
- Make sure you have .NET 6 runtime installed
- Try running from command line to see error messages

---
UNDER DEVELOPMENT — FEATURES MAY BE UNSTABLE. REPORT BUGS IN ISSUES.
