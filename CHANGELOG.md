# Changelog - Pie Launcher

## [0.6-alpha] - 2026-01-17

### New Features
- **Visual Gesture Editor**: New settings tab to customize your workflow using a drag-and-drop interface.
  - Configure **Single Tap** and **Double Tap** entry points.
  - Define custom **Right-Click Cycle** flows (e.g., Switcher -> Controller -> Launcher).
- **Drag-and-Drop Launcher**: Reorder your pinned apps and groups by simply dragging them in the settings list.
- **Dynamic Icons**: Smart icon generation for controller actions.
  - Added specific icons for VS Code (Command Palette, Zen Mode, Split) and Browsers (Incognito, Bookmarks).
- **Smart Context switching**: Double-tapping to switch modes now preserves the active window context, ensuring Controller shortcuts work on the correct app.

### Interaction Improvements
- **Click Outside to Close**: Clicking anywhere on the screen (outside the menu) now instantly closes the wheel.
- **Right-Click Cycle**: Right-clicking the center ring now reliably cycles to the next mode in your custom flow.
- **Controller Configuration**: Shows a helpful "Configure [App]" button if no shortcuts are defined for the current program, instead of failing silently.

### Performance
- **Instant Open**: Completely eliminated the "first-launch freeze". The menu window appears immediately.
- **GPU Acceleration**: Applied `BitmapCache` to shadows and shapes for silky smooth 60fps animations.
- **Memory Optimization**: Reduced RAM usage (~150-250MB) with LRU icon caching.

## [0.6-alpha] - 2026-01-17
