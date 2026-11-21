using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ColorPanel;

// Page "Affichage" : résolution et fréquence de chaque écran (API Windows, indépendant de la marque du GPU)
public partial class MainWindow
{
    record Mode(int W, int H, int Hz);

    string? _dev;
    List<Mode> _modes = new();
    Mode? _selMode, _curMode;
    List<(int W, int H)> _resList = new();
    List<int> _hzList = new();
    bool _syncing;
    Native.DEVMODE _prev;
    Native.DEVMODE _pending;
    bool _confirmPending;
    DispatcherTimer? _confirmTimer;
    int _countdown;

    const int EnumCurrent = -1;
    const uint DM_PELSWIDTH = 0x80000, DM_PELSHEIGHT = 0x100000, DM_DISPLAYFREQUENCY = 0x400000;
    const uint DM_DISPLAYFIXEDOUTPUT = 0x20000000;
    const uint CDS_UPDATEREGISTRY = 1, CDS_TEST = 2;

    void OnScaleSlider(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateScale();

    void UpdateScale()
    {
        if (ScaleValue == null) return;
        ScaleValue.Text = (int)Math.Round(ScaleSlider.Value) switch { 0 => "Par défaut (pilote)", 1 => "Étirer", _ => "Centrer" };
    }

    static Native.DEVMODE NewDevMode()
    {
        var dm = new Native.DEVMODE { dmDeviceName = "", dmFormName = "" };
        dm.dmSize = (ushort)Marshal.SizeOf<Native.DEVMODE>();
        return dm;
    }

    RadioButton Chip(string text, string group, Action onChecked, bool selected)
    {
        var rb = new RadioButton
        {
            Content = text, GroupName = group, Style = (Style)FindResource("Chip"),
            Margin = new Thickness(0, 0, 6, 6)
        };
        rb.Checked += (_, _) => onChecked();
        rb.IsChecked = selected;
        return rb;
    }

    void LoadMonitors()
    {
        MonitorShape.Background = new System.Windows.Media.ImageBrush(_dashAfter) { Stretch = System.Windows.Media.Stretch.UniformToFill };
        UpdateScale();
        MonPanel.Children.Clear();
        var list = new List<(string name, string label, bool primary)>();
        for (uint i = 0; ; i++)
        {
            var dd = new Native.DISPLAY_DEVICE { cb = Marshal.SizeOf<Native.DISPLAY_DEVICE>() };
            if (!Native.EnumDisplayDevices(null, i, ref dd, 0)) break;
            if ((dd.StateFlags & 0x1) == 0) continue; // pas attaché au bureau
            list.Add((dd.DeviceName, dd.DeviceString, (dd.StateFlags & 0x4) != 0));
        }
        if (list.Count == 0) { DispInfo.Text = "Aucun écran détecté."; return; }

        int n = 1;
        string? pick = _dev != null && list.Any(l => l.name == _dev) ? _dev : list.FirstOrDefault(l => l.primary).name ?? list[0].name;
        foreach (var l in list)
        {
            var (name, label, primary) = l;
            string text = $"Écran {n++}" + (primary ? " (principal)" : "") + " · " + label;
            MonPanel.Children.Add(Chip(text, "mon", () => { _dev = name; LoadModes(); }, name == pick));
        }
    }

    void LoadModes()
    {
        if (_dev == null) return;
        var set = new HashSet<Mode>();
        for (int i = 0; ; i++)
        {
            var dm = NewDevMode();
            if (!Native.EnumDisplaySettings(_dev, i, ref dm)) break;
            if (dm.dmBitsPerPel < 32) continue;
            set.Add(new Mode((int)dm.dmPelsWidth, (int)dm.dmPelsHeight, (int)dm.dmDisplayFrequency));
        }
        _modes = set.OrderByDescending(m => (long)m.W * m.H).ThenByDescending(m => m.Hz).ToList();

        var cur = NewDevMode();
        Native.EnumDisplaySettings(_dev, EnumCurrent, ref cur);
        var current = new Mode((int)cur.dmPelsWidth, (int)cur.dmPelsHeight, (int)cur.dmDisplayFrequency);
        DispInfo.Text = $"Actuel : {current.W} × {current.H} @ {current.Hz} Hz   ·   {_modes.Count} modes disponibles";

        _curMode = current;
        _selMode = null;
        _resList = _modes.Select(m => (m.W, m.H)).Distinct().OrderBy(r => (long)r.W * r.H).ToList();
        if (_resList.Count == 0) return;

        BuildQuick();

        _syncing = true;
        ResSlider.Minimum = 0;
        ResSlider.Maximum = Math.Max(1, _resList.Count - 1);
        ResSlider.IsEnabled = _resList.Count > 1;
        int idx = _resList.FindIndex(r => r.W == current.W && r.H == current.H);
        ResSlider.Value = idx >= 0 ? idx : _resList.Count - 1;
        _syncing = false;
        UpdateRes();
    }

    void UpdateRes()
    {
        if (_resList.Count == 0) return;
        int i = Math.Clamp((int)Math.Round(ResSlider.Value), 0, _resList.Count - 1);
        var r = _resList[i];
        ResValue.Text = $"{r.W} × {r.H}";

        _hzList = _modes.Where(m => m.W == r.W && m.H == r.H).Select(m => m.Hz).Distinct().OrderBy(x => x).ToList();
        int wanted = _selMode?.Hz ?? _curMode?.Hz ?? _hzList.Last();
        int pick = _hzList.Contains(wanted) ? wanted : _hzList.Last();

        _syncing = true;
        HzSlider.Minimum = 0;
        HzSlider.Maximum = Math.Max(1, _hzList.Count - 1);
        HzSlider.IsEnabled = _hzList.Count > 1;
        HzSlider.Value = _hzList.IndexOf(pick);
        _syncing = false;
        UpdateHz();
    }

    void UpdateHz()
    {
        if (_resList.Count == 0 || _hzList.Count == 0) return;
        int i = Math.Clamp((int)Math.Round(ResSlider.Value), 0, _resList.Count - 1);
        int j = Math.Clamp((int)Math.Round(HzSlider.Value), 0, _hzList.Count - 1);
        var r = _resList[i];
        HzValue.Text = $"{_hzList[j]} Hz";
        _selMode = new Mode(r.W, r.H, _hzList[j]);
        UpdateHero();
    }

    void BuildQuick()
    {
        QuickPanel.Children.Clear();
        if (_resList.Count == 0) return;
        var seen = new HashSet<(int, int)>();
        void Add(string label, (int W, int H) r)
        {
            int idx = _resList.FindIndex(x => x.W == r.W && x.H == r.H);
            if (idx < 0 || !seen.Add(r)) return;
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold });
            sp.Children.Add(new TextBlock { Text = $"{r.W} × {r.H}", FontSize = 11, Foreground = (System.Windows.Media.Brush)FindResource("Sub"), Margin = new Thickness(0, 3, 0, 0) });
            var b = new Button { Style = (Style)FindResource("PresetCard"), Width = 128, Content = sp };
            b.Click += (_, _) => ResSlider.Value = idx;
            QuickPanel.Children.Add(b);
        }
        Add("Native", _resList[^1]);
        Add("2K", (2560, 1440));
        Add("Full HD", (1920, 1080));
        Add("HD", (1280, 720));
        Add("4:3", (1024, 768));
    }

    static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);

    static string Ratio(int w, int h)
    {
        double a = (double)w / h;
        (string, double)[] known = { ("16:9", 16.0 / 9), ("16:10", 1.6), ("4:3", 4.0 / 3), ("5:4", 1.25),
                                     ("21:9", 21.0 / 9), ("21:9", 64.0 / 27), ("32:9", 32.0 / 9), ("3:2", 1.5) };
        foreach (var (name, v) in known) if (Math.Abs(a - v) < 0.02) return name;
        int g = Gcd(w, h);
        return $"{w / g}:{h / g}";
    }

    void UpdateHero()
    {
        if (_selMode == null || BigRes == null) return;
        var m = _selMode;
        BigRes.Text = $"{m.W} × {m.H}";
        BigHz.Text = $"{m.Hz} Hz";
        MonitorText.Text = $"{m.W}×{m.H}";

        double aspect = (double)m.W / m.H;
        double w = 220, h = w / aspect;
        if (h > 124) { h = 124; w = h * aspect; }
        MonitorShape.Width = w;
        MonitorShape.Height = h;

        ResClassText.Text = m.H >= 2160 ? "4K UHD" : m.H >= 1440 ? "QHD" : m.H >= 1080 ? "Full HD" : m.H >= 720 ? "HD" : "SD";
        HzClassText.Text = m.Hz >= 240 ? "Compétitif" : m.Hz >= 144 ? "Très fluide" : m.Hz >= 100 ? "Fluide" : "Standard";
        RatioText.Text = Ratio(m.W, m.H);
        MpText.Text = $"{m.W * (double)m.H / 1e6:0.0} MP";
        long max = _resList.Count > 0 ? (long)_resList[^1].W * _resList[^1].H : (long)m.W * m.H;
        PctText.Text = $"{100.0 * m.W * m.H / max:0} % du max";

        bool same = _curMode != null && _curMode.W == m.W && _curMode.H == m.H && _curMode.Hz == m.Hz;
        StateText2.Text = same ? "Actuel" : "Nouveau";
        StateText2.Foreground = same ? System.Windows.Media.Brushes.White : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0B, 0x1A, 0x26));
        StateBadge.Background = same ? (System.Windows.Media.Brush)FindResource("CardBg") : (System.Windows.Media.Brush)FindResource("Accent");
    }

    void OnResSlider(object sender, RoutedPropertyChangedEventArgs<double> e) { if (!_syncing) UpdateRes(); }
    void OnHzSlider(object sender, RoutedPropertyChangedEventArgs<double> e) { if (!_syncing) UpdateHz(); }

    void OnRefreshModes(object sender, RoutedEventArgs e) { LoadMonitors(); }

    void OnApplyMode(object sender, RoutedEventArgs e)
    {
        if (_dev == null || _selMode == null || _confirmPending) return;

        var cur = NewDevMode();
        Native.EnumDisplaySettings(_dev, EnumCurrent, ref cur);
        _prev = cur;

        var dm = cur;
        dm.dmPelsWidth = (uint)_selMode.W;
        dm.dmPelsHeight = (uint)_selMode.H;
        dm.dmDisplayFrequency = (uint)_selMode.Hz;
        dm.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT | DM_DISPLAYFREQUENCY | DM_DISPLAYFIXEDOUTPUT;
        dm.dmDisplayFixedOutput = (uint)Math.Clamp((int)Math.Round(ScaleSlider.Value), 0, 2); // 0 défaut, 1 étirer, 2 centrer

        int test = Native.ChangeDisplaySettingsEx(_dev, ref dm, IntPtr.Zero, CDS_TEST, IntPtr.Zero);
        string note = "";
        if (test != 0)
        {
            // Le pilote refuse peut-être l'option de mise à l'échelle : on réessaie sans
            dm.dmFields &= ~DM_DISPLAYFIXEDOUTPUT;
            dm.dmDisplayFixedOutput = _prev.dmDisplayFixedOutput;
            int test2 = Native.ChangeDisplaySettingsEx(_dev, ref dm, IntPtr.Zero, CDS_TEST, IntPtr.Zero);
            if (test2 != 0)
            {
                Status.Text = $"Mode refusé par l'écran ou le pilote (code {test2}).";
                return;
            }
            note = " (mise à l'échelle ignorée par le pilote)";
        }
        int res = Native.ChangeDisplaySettingsEx(_dev, ref dm, IntPtr.Zero, 0, IntPtr.Zero);
        if (res != 0) { Status.Text = "Échec du changement de mode (code " + res + ")."; return; }

        Status.Text = "Mode appliqué" + note + ".";
        _pending = dm;
        _confirmPending = true;
        _countdown = 15;
        ConfirmCard.Visibility = Visibility.Visible;
        UpdateConfirmText();
        _confirmTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _confirmTimer.Tick -= OnConfirmTick;
        _confirmTimer.Tick += OnConfirmTick;
        _confirmTimer.Start();
    }

    void UpdateConfirmText() =>
        ConfirmText.Text = $"Garder {_selMode?.W} × {_selMode?.H} @ {_selMode?.Hz} Hz ?  Retour automatique dans {_countdown} s.";

    void OnConfirmTick(object? s, EventArgs e)
    {
        if (--_countdown <= 0) { RevertMode(); return; }
        UpdateConfirmText();
    }

    void OnKeepMode(object sender, RoutedEventArgs e)
    {
        if (!_confirmPending || _dev == null) return;
        _confirmTimer?.Stop();
        var dm = _pending;
        Native.ChangeDisplaySettingsEx(_dev, ref dm, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero); // rend le mode permanent
        _confirmPending = false;
        ConfirmCard.Visibility = Visibility.Collapsed;
        Status.Text = "Résolution conservée.";
        LoadModes();
    }

    void OnRevertMode(object sender, RoutedEventArgs e) => RevertMode();

    void RevertMode()
    {
        _confirmTimer?.Stop();
        if (_confirmPending && _dev != null)
        {
            var dm = _prev;
            Native.ChangeDisplaySettingsEx(_dev, ref dm, IntPtr.Zero, 0, IntPtr.Zero);
        }
        _confirmPending = false;
        ConfirmCard.Visibility = Visibility.Collapsed;
        Status.Text = "Ancienne résolution rétablie.";
        LoadModes();
    }
}


