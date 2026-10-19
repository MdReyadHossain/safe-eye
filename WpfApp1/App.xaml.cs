using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SafeEyes.Models;
using SafeEyes.Services;
using SafeEyes.ViewModels;
using SafeEyes.Views;

namespace SafeEyes;

public partial class App : System.Windows.Application
{
    private IServiceProvider? _serviceProvider;
    private ILogger<App>? _logger;
    private IBreakScheduler? _scheduler;
    private IBreakOverlayService? _overlayService;
    private ISystemTrayService? _trayService;
    private ISettingsService? _settingsService;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // Keep app running in the background when windows are closed
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Configure Services
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        _logger.LogInformation("Safe Eye Windows starting up...");

        _settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        _scheduler = _serviceProvider.GetRequiredService<IBreakScheduler>();
        _overlayService = _serviceProvider.GetRequiredService<IBreakOverlayService>();
        _trayService = _serviceProvider.GetRequiredService<ISystemTrayService>();

        // Setup System Tray
        SetupTrayIntegration();

        // Setup Scheduler Events
        SetupSchedulerEvents();

        // Start scheduler if configured
        if (_settingsService.Settings.StartSchedulerOnLaunch)
        {
            _scheduler.Start();
        }

        // Check launch args or preferences
        bool startMinimized = e.Args.Any(a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase)) ||
                             _settingsService.Settings.LaunchMinimizedToTray;

        if (!startMinimized)
        {
            ShowSettingsWindow();
        }
        else
        {
            _logger.LogInformation("Safe Eye launched minimized to system tray.");
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Singletons
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IStartupService, StartupService>();
        services.AddSingleton<IIdleDetectionService, IdleDetectionService>();
        services.AddSingleton<IFullScreenDetectionService, FullScreenDetectionService>();
        services.AddSingleton<IBreakScheduler, BreakScheduler>();
        services.AddSingleton<IBreakOverlayService, BreakOverlayService>();
        services.AddSingleton<ISystemTrayService, SystemTrayService>();

        // ViewModels
        services.AddSingleton<SettingsViewModel>();
        services.AddTransient<BreakOverlayViewModel>();

        // Views
        services.AddSingleton<SettingsWindow>();
    }

    private void SetupTrayIntegration()
    {
        if (_trayService == null) return;

        _trayService.Initialize();

        _trayService.OpenSettingsRequested += () =>
        {
            Dispatcher.Invoke(ShowSettingsWindow);
        };

        _trayService.StartBreakRequested += () =>
        {
            _scheduler?.StartBreakNow();
        };

        _trayService.TogglePauseRequested += () =>
        {
            if (_scheduler == null) return;
            if (_scheduler.State == SchedulerState.Paused)
            {
                _scheduler.Resume();
            }
            else
            {
                _scheduler.Pause();
            }
        };

        _trayService.ResetTimerRequested += () =>
        {
            _scheduler?.ResetWorkTimer();
        };

        _trayService.ExitRequested += () =>
        {
            Dispatcher.Invoke(ShutdownApp);
        };
    }

    private void SetupSchedulerEvents()
    {
        if (_scheduler == null) return;

        _scheduler.Tick += remaining =>
        {
            Dispatcher.Invoke(() =>
            {
                if (_scheduler.State == SchedulerState.InBreak)
                {
                    _overlayService?.UpdateCountdown(remaining);
                    var remSec = (int)Math.Ceiling(remaining.TotalSeconds);
                    _trayService?.UpdateStatus($"Safe Eye - Break: {remSec / 60:D2}:{remSec % 60:D2}");
                }
                else if (_scheduler.State == SchedulerState.Working)
                {
                    var remMinutes = (int)Math.Ceiling(remaining.TotalMinutes);
                    _trayService?.UpdateStatus($"Safe Eye - Working: {remMinutes}m left");
                }
                else if (_scheduler.State == SchedulerState.Paused)
                {
                    _trayService?.UpdateStatus("Safe Eye - Paused");
                }
            });
        };

        _scheduler.BreakStarting += breakInfo =>
        {
            Dispatcher.Invoke(() =>
            {
                _overlayService?.ShowBreak(breakInfo);
                _trayService?.UpdateStatus("Safe Eye - Break in progress");

                if (_settingsService?.Settings.NotificationsEnabled == true)
                {
                    _trayService?.ShowNotification("Time for a break!", breakInfo.Instruction);
                }
            });
        };

        _scheduler.BreakEnded += breakInfo =>
        {
            Dispatcher.Invoke(() =>
            {
                _overlayService?.CloseBreak();
                _trayService?.UpdateStatus("Safe Eye - Break ended");

                if (_settingsService?.Settings.NotificationsEnabled == true)
                {
                    _trayService?.ShowNotification("Break finished", "Ready to get back to work!");
                }
            });
        };

        _scheduler.StateChanged += state =>
        {
            Dispatcher.Invoke(() =>
            {
                if (state == SchedulerState.Paused)
                {
                    _trayService?.UpdateStatus("Safe Eye - Paused");
                }
            });
        };
    }

    private void ShowSettingsWindow()
    {
        if (_settingsWindow == null)
        {
            _settingsWindow = _serviceProvider?.GetRequiredService<SettingsWindow>();
        }

        if (_settingsWindow != null)
        {
            if (_settingsWindow.WindowState == WindowState.Minimized)
            {
                _settingsWindow.WindowState = WindowState.Normal;
            }

            _settingsWindow.Show();
            _settingsWindow.Activate();
            _settingsWindow.Focus();
        }
    }

    private void ShutdownApp()
    {
        _logger?.LogInformation("Shutting down Safe Eye Windows cleanly...");

        if (_settingsWindow != null)
        {
            _settingsWindow.AllowExplicitClose();
            _settingsWindow.Close();
        }

        _overlayService?.Dispose();
        _scheduler?.Dispose();
        _trayService?.Dispose();

        Shutdown();
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        if (e.Exception is ArgumentException argEx && argEx.Message.Contains("VisualTarget", StringComparison.OrdinalIgnoreCase))
        {
            _logger?.LogWarning(argEx, "Handled transient WPF VisualTarget parent conflict.");
            e.Handled = true;
            return;
        }

        _logger?.LogError(e.Exception, "Unhandled UI exception caught in Dispatcher.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _overlayService?.Dispose();
        _scheduler?.Dispose();
        _trayService?.Dispose();
        base.OnExit(e);
    }
}
