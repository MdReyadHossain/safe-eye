using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace SafeEyes.Services;

public class SystemTrayService : ISystemTrayService
{
    private readonly ILogger<SystemTrayService> _logger;
    private NotifyIcon? _notifyIcon;
    private ToolStripMenuItem? _statusMenuItem;
    private ToolStripMenuItem? _pauseResumeMenuItem;
    private bool _isPaused = false;
    private bool _isDisposed = false;

    public event Action? OpenSettingsRequested;
    public event Action? StartBreakRequested;
    public event Action? TogglePauseRequested;
    public event Action? ResetTimerRequested;
    public event Action? ExitRequested;

    public SystemTrayService(ILogger<SystemTrayService> logger)
    {
        _logger = logger;
    }

    public void Initialize()
    {
        if (_notifyIcon != null) return;

        try
        {
            var contextMenu = new ContextMenuStrip();

            _statusMenuItem = new ToolStripMenuItem("Safe Eye - Starting...")
            {
                Enabled = false,
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold)
            };
            contextMenu.Items.Add(_statusMenuItem);
            contextMenu.Items.Add(new ToolStripSeparator());

            var settingsItem = new ToolStripMenuItem("Open Settings", null, (_, _) => OpenSettingsRequested?.Invoke());
            var startBreakItem = new ToolStripMenuItem("Start Break Now", null, (_, _) => StartBreakRequested?.Invoke());
            _pauseResumeMenuItem = new ToolStripMenuItem("Pause Reminders", null, (_, _) => TogglePauseRequested?.Invoke());
            var resetItem = new ToolStripMenuItem("Reset Work Timer", null, (_, _) => ResetTimerRequested?.Invoke());
            var exitItem = new ToolStripMenuItem("Exit", null, (_, _) => ExitRequested?.Invoke());

            contextMenu.Items.Add(settingsItem);
            contextMenu.Items.Add(startBreakItem);
            contextMenu.Items.Add(_pauseResumeMenuItem);
            contextMenu.Items.Add(resetItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            _notifyIcon = new NotifyIcon
            {
                Text = "Safe Eye - Eye-Break Reminder",
                ContextMenuStrip = contextMenu,
                Icon = CreateAppIcon(),
                Visible = true
            };

            _notifyIcon.DoubleClick += (_, _) => OpenSettingsRequested?.Invoke();
            _notifyIcon.Click += (_, e) =>
            {
                if (e is MouseEventArgs mouseArgs && mouseArgs.Button == MouseButtons.Left)
                {
                    OpenSettingsRequested?.Invoke();
                }
            };

            _logger.LogInformation("System tray icon initialized.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize system tray icon.");
        }
    }

    public void UpdateStatus(string statusText)
    {
        if (_notifyIcon == null) return;

        try
        {
            if (_statusMenuItem != null)
            {
                _statusMenuItem.Text = statusText;
            }

            // Keep tooltip text within 63 characters (NotifyIcon limitation)
            var tooltip = statusText.Length > 63 ? statusText.Substring(0, 60) + "..." : statusText;
            _notifyIcon.Text = tooltip;

            if (_pauseResumeMenuItem != null)
            {
                _isPaused = statusText.Contains("Paused", StringComparison.OrdinalIgnoreCase);
                _pauseResumeMenuItem.Text = _isPaused ? "Resume Reminders" : "Pause Reminders";
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error updating tray status.");
        }
    }

    public void ShowNotification(string title, string message)
    {
        if (_notifyIcon == null) return;

        try
        {
            _notifyIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to show tray balloon notification.");
        }
    }

    private static Icon CreateAppIcon()
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var iconPath = System.IO.Path.Combine(baseDir, "Resources", "app.ico");
            if (System.IO.File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }

            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
            {
                var extracted = Icon.ExtractAssociatedIcon(exePath);
                if (extracted != null) return extracted;
            }
        }
        catch { }

        // Fallback dynamically created icon
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // Dark rounded container
            using var bgBrush = new SolidBrush(Color.FromArgb(24, 26, 32));
            g.FillEllipse(bgBrush, 1, 1, 30, 30);

            // Outline
            using var borderPen = new Pen(Color.FromArgb(70, 160, 245), 1.5f);
            g.DrawEllipse(borderPen, 1, 1, 30, 30);

            // Eye outline
            using var eyePen = new Pen(Color.White, 2.0f);
            // Upper and lower eye lid curves
            var p1 = new PointF(5, 16);
            var p2 = new PointF(16, 8);
            var p3 = new PointF(27, 16);
            var p4 = new PointF(16, 24);

            var path = new GraphicsPath();
            path.AddBezier(p1, p2, p2, p3);
            path.AddBezier(p3, p4, p4, p1);
            g.DrawPath(eyePen, path);

            // Pupil
            using var pupilBrush = new SolidBrush(Color.FromArgb(70, 190, 255));
            g.FillEllipse(pupilBrush, 13, 13, 6, 6);

            // Specular highlight
            using var highlightBrush = new SolidBrush(Color.White);
            g.FillEllipse(highlightBrush, 16, 14, 2, 2);
        }

        return Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }
}
