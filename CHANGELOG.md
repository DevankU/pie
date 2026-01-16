# Changelog - Pie Launcher

## [0.5-alpha] - 2026-01-16

### New Features
- **Smart Controller Mode**: Automatically detects the active application (Chrome, VS Code, Terminal, etc.) and switches context instantly.
- **Controller Presets**: Built-in support for 50+ popular applications including Browsers, IDEs, Creative Tools, and Office apps.
- **Preset Management**:
  - Import presets from the built-in library.
  - Import custom presets from JSON files.
  - "From Running Apps" scanner to quickly add configurations for currently open programs.
- **Group Launcher**: Create folders/groups in the launcher to organize multiple apps under one slice.
  - Includes auto-generated "stacked" icons for groups.
  - Launches all apps in a group simultaneously.
- **Enhanced Settings UI**:
  - Real-time **Preview Wheel** in Launcher and Controller settings.
  - **Drag-and-Drop** reordering for Launcher items.
  -  Up/Down buttons for manual reordering.
  - "Browse Installed" and "From Running" pickers for easier app selection.

### Performance & Polish
- **Instant Response**: Refactored window spawning to eliminate "first-launch freeze" and UI lag.
- **Zero Lag Rendering**: Applied GPU-accelerated `BitmapCache` for shadows and complex geometry.
- **Memory Optimization**: Implemented LRU (Least Recently Used) caching for icons to keep memory usage low.
- **Async Loading**: Switcher mode now loads process data asynchronously on a background thread.

### Fixes
- Fixed "invisible arrows" in Launcher settings reorder buttons.
- Fixed Controller keyboard shortcuts not firing due to focus stealing (added focus restoration logic).
- Fixed issue where Controller icons appeared as generic dots (now contextually mapped to action names like "Save", "Find", "New").
- Fixed "flash" artifact when opening the menu.
- Fixed drag-and-drop file locking issues.

### Technical
- Migrated icon handling to `WindowService` with thread-safe caching.
- Added DPI-aware mouse tracking caching for smoother cursor interaction.
- Refactored `PieMenuControl` animation logic for better frame rates.
