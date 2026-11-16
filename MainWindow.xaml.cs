using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BROZILLA.Models;
using BROZILLA.Services;

namespace BROZILLA;

public partial class MainWindow : Window
{
    private readonly GoodbyeDpiManager _dpiManager;
    private readonly TrayIconService _trayService;
    private readonly DispatcherTimer _uptimeTimer;
    private readonly Storyboard _activeLoopStoryboard;

    private AppSettings _settings;
    private bool _isInitializing = true;
    private bool _isExiting;
    private bool _isPressed;

    private static readonly System.Windows.Media.Color EmeraldColor = System.Windows.Media.Color.FromRgb(16, 185, 129);
    private static readonly System.Windows.Media.Color SlateColor = System.Windows.Media.Color.FromRgb(148, 163, 184);
    private static readonly System.Windows.Media.Color ActiveBorderColor = System.Windows.Media.Color.FromRgb(167, 243, 208);

    public MainWindow(bool startMinimized = false)
    {
        InitializeComponent();

        _settings = SettingsService.Load();
        _dpiManager = new GoodbyeDpiManager();
        _trayService = new TrayIconService();

        _activeLoopStoryboard = (Storyboard)FindResource("ActiveLoopStoryboard");

        _uptimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _uptimeTimer.Tick += UptimeTimer_Tick;

        // Populate ISP profiles
        ProfileCombo.ItemsSource = DpiProfile.All;
        var selected = DpiProfile.GetById(_settings.SelectedProfileId);
        ProfileCombo.SelectedItem = selected;
        StartupToggle.IsChecked = _settings.StartWithWindows;

        _dpiManager.StateChanged += active => Dispatcher.BeginInvoke(() => ApplyVisualState(active));
        _trayService.ToggleRequested += () => Dispatcher.BeginInvoke(ToggleDpi);
        _trayService.ShowWindowRequested += () => Dispatcher.BeginInvoke(RestoreFromTray);
        _trayService.ExitRequested += () => Dispatcher.BeginInvoke(ExitApplication);

        _isInitializing = false;

        if (startMinimized || _settings.AutoConnectOnLaunch)
        {
            var (ok, err) = _dpiManager.Start(selected);
            if (!ok && err != null)
            {
                FeedbackText.Text = err;
            }
        }

        if (startMinimized)
        {
            Hide();
        }
    }

    public void RestoreFromTray()
    {
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    private void ToggleDpi()
    {
        if (_dpiManager.IsRunning)
        {
            _dpiManager.Stop();
            FeedbackText.Text = "Koruma durduruldu. Tekrar açmak için kalkana dokun.";
        }
        else
        {
            var profile = ProfileCombo.SelectedItem as DpiProfile ?? DpiProfile.All[0];
            FeedbackText.Text = $"Başlatılıyor ({profile.Name})...";
            var (ok, err) = _dpiManager.Start(profile);
            if (ok)
            {
                FeedbackText.Text = $"Aktif: {profile.Name}";
            }
            else
            {
                FeedbackText.Text = err ?? "Başlatılamadı.";
            }
        }
    }

    private void ApplyVisualState(bool isActive)
    {
        var profile = _dpiManager.ActiveProfile ?? (ProfileCombo.SelectedItem as DpiProfile);
        _trayService.UpdateState(isActive, profile);

        if (isActive)
        {
            StatusDot.Fill = new SolidColorBrush(EmeraldColor);
            StatusText.Text = "KORUMA AKTİF";
            StatusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(5, 150, 105));
            StatusPill.BorderBrush = new SolidColorBrush(ActiveBorderColor);
            UptimeText.Visibility = Visibility.Visible;
            UpdateUptimeDisplay();

            AnimateOpacity(OrbitRing, 0.85, 350);
            PowerButton.BorderBrush = new SolidColorBrush(ActiveBorderColor);

            if (IsVisible)
            {
                _activeLoopStoryboard.Begin(this, true);
                _uptimeTimer.Start();
            }
        }
        else
        {
            _activeLoopStoryboard.Stop(this);
            _uptimeTimer.Stop();

            StatusDot.Fill = new SolidColorBrush(SlateColor);
            StatusText.Text = "KORUMA KAPALI";
            StatusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105));
            StatusPill.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240));
            UptimeText.Visibility = Visibility.Collapsed;
            UptimeText.Text = "";

            AnimateOpacity(OrbitRing, 0, 250);
            AnimateOpacity(ActiveGlow, 0, 250);
            PulseRing1.Opacity = 0;
            PulseRing2.Opacity = 0;
            PowerButton.BorderBrush = System.Windows.Media.Brushes.White;
        }
    }

    private void UptimeTimer_Tick(object? sender, EventArgs e) => UpdateUptimeDisplay();

    private void UpdateUptimeDisplay()
    {
        if (_dpiManager.StartedAtUtc is { } started)
        {
            var elapsed = DateTime.UtcNow - started;
            UptimeText.Text = $"• {elapsed:hh\\:mm\\:ss}";
        }
    }

    private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // Pause all animations & timers when hidden in system tray -> 0% CPU
        if (IsVisible)
        {
            if (_dpiManager.IsRunning)
            {
                _activeLoopStoryboard.Begin(this, true);
                _uptimeTimer.Start();
                UpdateUptimeDisplay();
            }
        }
        else
        {
            _activeLoopStoryboard.Stop(this);
            _uptimeTimer.Stop();
        }
    }

    #region 3D Tactile Shield Button Animations

    private void PowerButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isPressed)
        {
            AnimateScale(1.03, 180, new CubicEase { EasingMode = EasingMode.EaseOut });
        }
    }

    private void PowerButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _isPressed = false;
        AnimateScale(1.0, 200, new CubicEase { EasingMode = EasingMode.EaseOut });
        PowerShadow.ShadowDepth = 10;
    }

    private void PowerButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isPressed = true;
        AnimateScale(0.92, 100, new CubicEase { EasingMode = EasingMode.EaseOut });
        PowerShadow.ShadowDepth = 2;
    }

    private void PowerButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isPressed) return;
        _isPressed = false;

        AnimateScale(1.03, 360, new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.55 });
        PowerShadow.ShadowDepth = 10;
        ToggleDpi();
    }

    private void AnimateScale(double target, int durationMs, IEasingFunction easing)
    {
        var dur = TimeSpan.FromMilliseconds(durationMs);
        var animX = new DoubleAnimation(target, dur) { EasingFunction = easing };
        var animY = new DoubleAnimation(target, dur) { EasingFunction = easing };
        PowerScale.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
        PowerScale.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
    }

    private static void AnimateOpacity(UIElement element, double target, int durationMs)
    {
        var anim = new DoubleAnimation(target, TimeSpan.FromMilliseconds(durationMs))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        element.BeginAnimation(OpacityProperty, anim);
    }

    #endregion

    #region Controls & Events

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseToTrayButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        _trayService.ShowBalloon("BROZILLA", "Uygulama arka planda çalışmaya devam ediyor.");
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!_isExiting)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void ProfileCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (ProfileCombo.SelectedItem is not DpiProfile profile) return;

        _settings.SelectedProfileId = profile.Id;
        SettingsService.Save(_settings);

        if (_dpiManager.IsRunning)
        {
            FeedbackText.Text = $"Profil değiştiriliyor: {profile.Name}...";
            var (ok, err) = _dpiManager.Start(profile);
            FeedbackText.Text = ok ? $"Aktif: {profile.Name}" : (err ?? "Profil başlatılamadı.");
        }
        else
        {
            FeedbackText.Text = $"Seçili profil: {profile.Name}";
        }
    }

    private async void TestButton_Click(object sender, RoutedEventArgs e)
    {
        TestButton.IsEnabled = false;
        TestButtonText.Text = "Test ediliyor...";
        TestButtonText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));

        var (ok, _, message) = await GoodbyeDpiManager.TestConnectionAsync();

        TestButtonText.Text = ok ? "Erişim Açık ✓" : "Engelli ✕";
        TestButtonText.Foreground = new SolidColorBrush(ok
            ? System.Windows.Media.Color.FromRgb(5, 150, 105)
            : System.Windows.Media.Color.FromRgb(220, 38, 38));
        FeedbackText.Text = $"Test sonucu: {message}";

        await Task.Delay(2500);
        TestButtonText.Text = "Bağlantıyı Test Et";
        TestButtonText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105));
        TestButton.IsEnabled = true;
    }

    private void StartupToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        bool enable = StartupToggle.IsChecked == true;
        _settings.StartWithWindows = enable;
        _settings.AutoConnectOnLaunch = enable;
        SettingsService.Save(_settings);

        bool ok = GoodbyeDpiManager.SetStartWithWindows(enable);
        FeedbackText.Text = enable
            ? (ok ? "Windows açılışında otomatik başlayacak." : "Başlangıç görevi eklenemedi.")
            : "Windows açılışında başlatma kapatıldı.";
    }

    private void ExitApplication()
    {
        _isExiting = true;
        _uptimeTimer.Stop();
        _dpiManager.Dispose();
        _trayService.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    #endregion
}
