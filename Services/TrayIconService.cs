using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BROZILLA.Models;

namespace BROZILLA.Services;

/// <summary>
/// Lightweight WinForms NotifyIcon wrapper with dynamic active/inactive status badge.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly ToolStripMenuItem _statusItem;
    private readonly Icon _inactiveIcon;
    private readonly Icon _activeIcon;

    public event Action? ToggleRequested;
    public event Action? ShowWindowRequested;
    public event Action? ExitRequested;

    public TrayIconService()
    {
        _inactiveIcon = CreateBadgedIcon(isActive: false);
        _activeIcon = CreateBadgedIcon(isActive: true);

        _statusItem = new ToolStripMenuItem("Durum: Kapalı") { Enabled = false };
        _toggleItem = new ToolStripMenuItem("Korumayı Başlat", null, (_, _) => ToggleRequested?.Invoke());
        var showItem = new ToolStripMenuItem("BROZILLA'yı Göster", null, (_, _) => ShowWindowRequested?.Invoke())
        {
            Font = new Font(SystemFonts.MenuFont ?? SystemFonts.DefaultFont, FontStyle.Bold)
        };
        var exitItem = new ToolStripMenuItem("Tamamen Çık", null, (_, _) => ExitRequested?.Invoke());

        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_toggleItem);
        menu.Items.Add(showItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = _inactiveIcon,
            Text = "BROZILLA — Koruma Kapalı",
            Visible = true,
            ContextMenuStrip = menu
        };

        _notifyIcon.DoubleClick += (_, _) => ShowWindowRequested?.Invoke();
    }

    public void UpdateState(bool isActive, DpiProfile? profile)
    {
        _notifyIcon.Icon = isActive ? _activeIcon : _inactiveIcon;
        _notifyIcon.Text = isActive
            ? $"BROZILLA — Aktif ({profile?.Name ?? "Standart"})"[..Math.Min(63, $"BROZILLA — Aktif ({profile?.Name ?? "Standart"})".Length)]
            : "BROZILLA — Koruma Kapalı";

        _statusItem.Text = isActive ? "Durum: Koruma Aktif" : "Durum: Kapalı";
        _toggleItem.Text = isActive ? "Korumayı Durdur" : "Korumayı Başlat";
    }

    public void ShowBalloon(string title, string message)
    {
        _notifyIcon.ShowBalloonTip(2000, title, message, ToolTipIcon.Info);
    }

    private static Icon CreateBadgedIcon(bool isActive)
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            var uri = new Uri("pack://application:,,,/Assets/brozilla_logo.png");
            var streamInfo = System.Windows.Application.GetResourceStream(uri);
            if (streamInfo != null)
            {
                using var stream = streamInfo.Stream;
                using var logo = new Bitmap(stream);
                g.DrawImage(logo, 1, 1, 30, 30);
            }

            // Small status dot at bottom-right
            var dotColor = isActive ? Color.FromArgb(16, 185, 129) : Color.FromArgb(148, 163, 184);
            using var borderBrush = new SolidBrush(Color.White);
            using var dotBrush = new SolidBrush(dotColor);
            g.FillEllipse(borderBrush, 19, 19, 12, 12);
            g.FillEllipse(dotBrush, 21, 21, 8, 8);
        }

        return Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _inactiveIcon.Dispose();
        _activeIcon.Dispose();
    }
}
