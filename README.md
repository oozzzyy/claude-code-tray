# Claude Code Tray

A drop-down Claude Code window for Windows. Press **Ctrl+Alt+C** anywhere and the terminal slides up from the taskbar. Press it again and it slides away, with the session still running.

[Русская версия](README.ru.md)

<!-- Add a demo GIF here: docs/demo.gif -->

## Features

- **Toggle window:** Ctrl+Alt+C or a click on the tray icon.
- **Voice typing:** Ctrl+Alt+V opens the window and starts Windows voice typing (Win+H). It listens in the language of your current keyboard layout.
- **Smooth animation:** a slide and a star, in step with your screen's refresh rate.
- **Reminders:** repeats a sound while Claude is waiting and the window is hidden (optional, see below).
- **Small footprint:** one C# file, no dependencies.

## Requirements

- Windows 11 (the only version tested)
- [Windows Terminal](https://aka.ms/terminal)
- Claude Code installed at `%USERPROFILE%\.local\bin\claude.exe` (the native installer's location)

## Build and run

```powershell
.\build.ps1
.\ClaudeTray.exe
```

The script uses the C# compiler included in Windows, so nothing else needs installing. To start the tray at logon, right-click its icon and choose **Start with Windows**.

## Reminder sounds

1. Merge `hooks.example.json` into `~/.claude/settings.json` and set the path to `ClaudeTray.exe`.
2. Put `coin-quiet.wav` (response finished) and `notify-quiet.wav` (Claude needs you) in `~/.claude/sounds`.

The tray repeats the sound once a minute, up to 30 times, until you open the window.

## Good to know

- New sessions start in your user folder.
- The voice panel is closed by clicking its X, because Windows offers no safe API for it. The mouse moves there for a moment.

## License

[MIT](LICENSE)
