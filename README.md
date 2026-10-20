# Safe Eye Windows — Native Desktop Eye-Break Reminder

A lightweight, native Windows desktop application named **Safe Eye**, built with modern C#, WPF, and .NET 9. Safe Eye helps prevent Computer Vision Syndrome (CVS) and RSI by gently reminding you to take regular short and long breaks with eye and posture exercises.

---

## 📥 Download & Installation

You can install **Safe Eye** directly using the pre-built Windows Installer:

| Package | Description | Download Link |
| :--- | :--- | :--- |
| **Safe Eye Installer (.msi)** | Standard Windows Installer package | 💾 **[Download setup.msi](https://raw.githubusercontent.com/MdReyadHossain/safe-eye/main/setup/Debug/setup-safe_eyeb08811a.msi)** |
| **Setup Bootstrapper (.exe)** | Windows Setup Launcher | 🚀 **[Download setup.exe](https://raw.githubusercontent.com/MdReyadHossain/safe-eye/main/setup/Debug/setup-safe_eyeb08811a.exe)** |

> [!TIP]
> Download and run **[setup.msi](https://raw.githubusercontent.com/MdReyadHossain/safe-eye/main/setup/Debug/setup-safe_eyeb08811a.msi)**. The installer registers Safe Eye in Windows, configures Desktop and Start Menu shortcuts, and sets up automatic launch options.

---

## 🚀 Quick Start: Build & Run from Source

### Prerequisites
- Windows 10 (version 1809 or later) or Windows 11 (x64)
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) installed (`dotnet --version` ≥ `9.0.100`)

### 1. Build the Application
```powershell
dotnet build WpfApp1\WpfApp1.csproj
```

### 2. Run the Application
```powershell
dotnet run --project WpfApp1\WpfApp1.csproj
```
Or launch the compiled binary directly:
```powershell
.\WpfApp1\bin\Debug\net9.0-windows\"Safe Eye.exe"
```

### 3. Publish Self-Contained Release
To produce a standalone, single-file Windows x64 release package:
```powershell
dotnet publish WpfApp1\WpfApp1.csproj -c Release -r win-x64 --self-contained true
```
The resulting executable is generated in:
`WpfApp1\bin\Release\net9.0-windows\win-x64\publish\Safe Eye.exe`

---

## ✨ Features

- **Dark Minimalist Break Overlay**: Full-screen dark translucent overlay that gently dims your desktop, featuring centered eye exercises, countdown timer, and optional Skip button.
- **Smart Pause Triggers**:
  - **Idle Detection**: Pauses the work timer when you step away from the computer.
  - **Full-Screen Detection**: Postpones breaks when running full-screen games or presentations.
  - **Lock / Sleep Detection**: Automatically pauses and resumes timers when Windows locks or suspends.
- **System Tray Integration**:
  - Live tooltip timer showing active state and remaining time (`Safe Eye - Working: 18m left`).
  - Context menu to pause, resume, reset, or start a break immediately.
  - Minimizes to notification area to stay out of your way.
- **shadcn/ui-styled Toaster**: Modern dark zinc toast notifications with Lucide vector icons for settings feedback.
- **Strict Break Mode**: Optional mode that hides the Skip button and disables `Escape` to enforce rest.
- **Multi-Monitor Support**: Synchronized overlays span across all connected screens.

---

## ⚙️ Configuration & Storage

Settings are automatically stored in the user's Local Application Data folder:
```
%LocalAppData%\Safe Eye\settings.json
```

### Default Parameters
- **Work Interval**: 20 minutes
- **Short Break**: 20 seconds
- **Long Break**: 5 minutes (every 4 intervals)
- **Overlay Darkness**: 90%
- **Skip Enabled**: Yes (`Escape` or `Skip` button)
