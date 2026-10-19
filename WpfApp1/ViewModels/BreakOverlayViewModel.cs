using System.Windows.Input;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

namespace SafeEyes.ViewModels;

public class BreakOverlayViewModel : ViewModelBase
{
    private string _currentInstruction = "Tightly close your eyes";
    private TimeSpan _remainingTime = TimeSpan.FromSeconds(20);
    private bool _isCountdownVisible = true;
    private bool _canSkip = true;
    private bool _strictMode = false;
    private double _instructionFontSize = 32.0;
    private double _overlayDarkness = 0.90;
    private SolidColorBrush _overlayBrush = new(MediaColor.FromArgb(230, 14, 15, 18));

    public event Action? SkipRequested;

    public string CurrentInstruction
    {
        get => _currentInstruction;
        set => SetProperty(ref _currentInstruction, value);
    }

    public TimeSpan RemainingTime
    {
        get => _remainingTime;
        set
        {
            if (SetProperty(ref _remainingTime, value))
            {
                OnPropertyChanged(nameof(RemainingTimeFormatted));
            }
        }
    }

    public string RemainingTimeFormatted
    {
        get
        {
            var totalSeconds = (int)Math.Max(0, Math.Ceiling(RemainingTime.TotalSeconds));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }
    }

    public bool IsCountdownVisible
    {
        get => _isCountdownVisible;
        set => SetProperty(ref _isCountdownVisible, value);
    }

    public bool CanSkip
    {
        get => _canSkip && !_strictMode;
        set => SetProperty(ref _canSkip, value);
    }

    public bool StrictMode
    {
        get => _strictMode;
        set
        {
            if (SetProperty(ref _strictMode, value))
            {
                OnPropertyChanged(nameof(CanSkip));
            }
        }
    }

    public double InstructionFontSize
    {
        get => _instructionFontSize;
        set => SetProperty(ref _instructionFontSize, value);
    }

    public double OverlayDarkness
    {
        get => _overlayDarkness;
        set
        {
            if (SetProperty(ref _overlayDarkness, value))
            {
                UpdateOverlayBrush();
            }
        }
    }

    public SolidColorBrush OverlayBrush
    {
        get => _overlayBrush;
        private set => SetProperty(ref _overlayBrush, value);
    }

    public ICommand SkipCommand { get; }

    public BreakOverlayViewModel()
    {
        SkipCommand = new RelayCommand(OnSkip);
        UpdateOverlayBrush();
    }

    private void OnSkip()
    {
        if (CanSkip)
        {
            SkipRequested?.Invoke();
        }
    }

    public void UpdateRemainingTime(TimeSpan remaining)
    {
        RemainingTime = remaining;
    }

    private void UpdateOverlayBrush()
    {
        byte alpha = (byte)Math.Clamp((int)(_overlayDarkness * 255), 100, 252);
        // Dark translucent tint: #0E0F12 with alpha
        var brush = new SolidColorBrush(MediaColor.FromArgb(alpha, 14, 15, 18));
        brush.Freeze();
        OverlayBrush = brush;
    }
}
