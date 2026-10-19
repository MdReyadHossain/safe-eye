using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SafeEyes.Models;
using SafeEyes.ViewModels;
using SafeEyes.Views;

namespace SafeEyes.Services;

public class BreakOverlayService : IBreakOverlayService
{
    private readonly ISettingsService _settingsService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BreakOverlayService> _logger;

    private readonly List<BreakOverlayWindow> _activeWindows = new();
    private BreakOverlayViewModel? _currentViewModel;
    private bool _isPreview = false;
    private System.Threading.Timer? _previewTimer;

    public bool IsOverlayVisible => _activeWindows.Count > 0;

    public BreakOverlayService(
        ISettingsService settingsService,
        IServiceProvider serviceProvider,
        ILogger<BreakOverlayService> logger)
    {
        _settingsService = settingsService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void ShowBreak(BreakInfo breakInfo)
    {
        var app = System.Windows.Application.Current;
        if (app == null) return;

        app.Dispatcher.Invoke(() =>
        {
            try
            {
                CloseExistingWindows();
                _isPreview = false;

                var settings = _settingsService.Settings;
                _currentViewModel = new BreakOverlayViewModel
                {
                    CurrentInstruction = breakInfo.Instruction,
                    RemainingTime = breakInfo.RemainingTime,
                    IsCountdownVisible = settings.ShowCountdown,
                    CanSkip = breakInfo.CanSkip,
                    StrictMode = breakInfo.StrictMode,
                    OverlayDarkness = settings.OverlayDarkness,
                    InstructionFontSize = settings.InstructionFontSize
                };

                _currentViewModel.SkipRequested += OnSkipRequested;

                CreateAndShowWindows(_currentViewModel);
                _logger.LogInformation("Break overlay displayed on {Count} screen(s).", _activeWindows.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to display break overlay.");
            }
        });
    }

    public void UpdateCountdown(TimeSpan remainingTime)
    {
        var app = System.Windows.Application.Current;
        if (app == null || _currentViewModel == null) return;

        app.Dispatcher.Invoke(() =>
        {
            _currentViewModel.UpdateRemainingTime(remainingTime);
        });
    }

    public void CloseBreak()
    {
        var app = System.Windows.Application.Current;
        if (app == null) return;

        app.Dispatcher.Invoke(() =>
        {
            try
            {
                _previewTimer?.Dispose();
                _previewTimer = null;
                CloseExistingWindows();
                _logger.LogInformation("Break overlay closed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to close break overlay.");
            }
        });
    }

    public void PreviewBreak(string instruction, int durationSeconds)
    {
        var app = System.Windows.Application.Current;
        if (app == null) return;

        app.Dispatcher.Invoke(() =>
        {
            try
            {
                CloseExistingWindows();
                _isPreview = true;

                var settings = _settingsService.Settings;
                var totalDuration = TimeSpan.FromSeconds(durationSeconds);

                _currentViewModel = new BreakOverlayViewModel
                {
                    CurrentInstruction = string.IsNullOrWhiteSpace(instruction) ? "Tightly close your eyes" : instruction,
                    RemainingTime = totalDuration,
                    IsCountdownVisible = settings.ShowCountdown,
                    CanSkip = true,
                    StrictMode = false,
                    OverlayDarkness = settings.OverlayDarkness,
                    InstructionFontSize = settings.InstructionFontSize
                };

                _currentViewModel.SkipRequested += () => CloseBreak();

                CreateAndShowWindows(_currentViewModel);

                // Run preview countdown timer
                var endTime = DateTimeOffset.UtcNow + totalDuration;
                _previewTimer?.Dispose();
                _previewTimer = new System.Threading.Timer(_ =>
                {
                    var rem = endTime - DateTimeOffset.UtcNow;
                    if (rem <= TimeSpan.Zero)
                    {
                        CloseBreak();
                    }
                    else
                    {
                        UpdateCountdown(rem);
                    }
                }, null, 1000, 1000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preview break overlay.");
            }
        });
    }

    private void CreateAndShowWindows(BreakOverlayViewModel viewModel)
    {
        var screens = _settingsService.Settings.AllMonitors
            ? Screen.AllScreens
            : new[] { Screen.PrimaryScreen ?? Screen.AllScreens.FirstOrDefault() };

        foreach (var screen in screens)
        {
            if (screen == null) continue;

            var window = new BreakOverlayWindow
            {
                DataContext = viewModel
            };

            window.PositionOnScreen(screen.Bounds.Left, screen.Bounds.Top, screen.Bounds.Width, screen.Bounds.Height);
            window.Show();
            _activeWindows.Add(window);
        }
    }

    private void CloseExistingWindows()
    {
        foreach (var win in _activeWindows)
        {
            try
            {
                win.Close();
            }
            catch { }
        }
        _activeWindows.Clear();

        if (_currentViewModel != null)
        {
            _currentViewModel.SkipRequested -= OnSkipRequested;
            _currentViewModel = null;
        }
    }

    private void OnSkipRequested()
    {
        if (_isPreview)
        {
            CloseBreak();
            return;
        }

        try
        {
            var scheduler = _serviceProvider.GetRequiredService<IBreakScheduler>();
            scheduler.SkipCurrentBreak();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute skip on scheduler.");
        }
    }

    public void Dispose()
    {
        _previewTimer?.Dispose();
        _previewTimer = null;
        CloseBreak();
    }
}
