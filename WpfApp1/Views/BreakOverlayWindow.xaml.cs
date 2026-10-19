using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using SafeEyes.ViewModels;

namespace SafeEyes.Views;

public partial class BreakOverlayWindow : Window
{
    public BreakOverlayWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        KeyDown += OnKeyDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Fade in animation if enabled
        if (DataContext is BreakOverlayViewModel)
        {
            var fadeIn = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(200)));
            BeginAnimation(OpacityProperty, fadeIn);
        }

        Activate();
        Focus();
    }

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (DataContext is BreakOverlayViewModel vm && vm.CanSkip)
            {
                vm.SkipCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    public void PositionOnScreen(int screenLeft, int screenTop, int screenWidth, int screenHeight)
    {
        var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(this);
        double dpiX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
        double dpiY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

        Left = screenLeft / dpiX;
        Top = screenTop / dpiY;
        Width = screenWidth / dpiX;
        Height = screenHeight / dpiY;
    }
}
