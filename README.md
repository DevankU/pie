# Pie

A radial pie menu launcher for Windows. Quick access to your apps, window switching, media controls, and custom shortcuts - all from a slick circular menu.


https://github.com/user-attachments/assets/06160b3e-37ac-4fdd-931e-28865a740a72


   

![.NET 6](https://img.shields.io/badge/.NET-6.0-purple)
![Windows](https://img.shields.io/badge/Platform-Windows-blue)

UNDER DEVELOPMENT — FEATURES MAY BE UNSTABLE. REPORT BUGS IN ISSUES.

## screenshots
<img width="1202" height="740" alt="Launcher" src="https://github.com/user-attachments/assets/5f28ef29-686b-43f4-8a3b-84f531db8f74" />
<img width="1199" height="742" alt="settings" src="https://github.com/user-attachments/assets/2e1b72b1-4a9c-40a8-9845-4fb094c5c721" />
<img width="595" height="611" alt="Switcher" src="https://github.com/user-attachments/assets/4274da8d-1ccb-410c-bd58-0db31c6ca430" />
<img width="1225" height="859" alt="image" src="https://github.com/user-attachments/assets/f84f5b4b-1494-4864-bbde-f767395554e3" />



## What is this?

Pie is a productivity tool that gives you a circular menu (like in video games) to:

- **Switch between open windows** - No more Alt+Tab hunting
- **Launch your favorite apps** - Pin the apps you use most
- **Control media playback** - Play/pause, next/previous, volume
- **Trigger keyboard shortcuts** - Set up custom actions per app

Activate it with a hotkey (default: `Ctrl+Space`) or a 3-finger tap on your touchpad( in windows settings switch the 3 finger tap -> middle mouse click).

## What's New in v0.5 Alpha 🚀

- **Smart Controller:** Automatically detects your active app (Chrome, VS Code, etc.) and switches to the correct shortcut preset.
- **Built-in Presets:** 50+ pre-configured app profiles for Browsers, IDEs, and Creative tools.
- **Group Launcher:** Organize apps into folders/groups with "stacked" icons.
- **Improved Settings:**
  - Real-time preview wheel.
  - Drag-and-drop reordering.
  - Import custom JSON presets.
  - "Browse Running Apps" to quickly add shortcuts.

## Features

- **Switcher Mode**: Alt-Tab replacement. Visual and fast.
- **Launcher Mode**: Launch apps, folders, and groups.
- **Controller Mode**: Context-aware shortcuts for your active application.
- **Music Remote**: Universal media controls.
- **Fluid Animations**: High-performance rendering with physics-based motion.
- **DPI Aware**: Crisp visuals on any display scale.
- **System Tray**: Runs quietly in the background (~150-250 MB RAM).

## Requirements

- Windows 10/11
- .NET 6.0 Runtime (or build self-contained)

## Installation

### Option 1: Build from source

1. Make sure you have [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) installed

2. Clone this repo
   ```
   git clone https://github.com/DevankU/Pie.git
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

### Option 2: Portable Release

```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
```

This creates a single `Pie.exe` you can run anywhere.

## Usage

1. Run `Pie.exe`
2. Press `Ctrl+Space` to open the menu
3. Move mouse to select, click to confirm
4. Right-click tray icon for **Settings**

### Modes

- **Switcher**: Switch active windows.
- **Launcher**: Launch pinned apps/groups.
- **Controller**: Triggers keyboard shortcuts for the foreground app.
- **Music Remote**: Media playback controls.

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

## Building from Scratch

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

## License

Copyright (c) 2026 Devank.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, subject to the following conditions:

**The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.**

**CREDIT REQUIREMENT:** Any use, modification, or distribution of this software must clearly attribute the original author (Devank) in the user interface and documentation.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

---
UNDER DEVELOPMENT — FEATURES MAY BE UNSTABLE. REPORT BUGS IN ISSUES.
