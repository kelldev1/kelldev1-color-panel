using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace ColorPanel;

public partial class MainWindow : Window
{
    record Row(string Label, double Min, double Max, Func<Settings, double> Get,
               Action<Settings, double> Set, string Fmt, string Unit = "");

    readonly ColorEngine _engine = new();
    readonly Settings _s = new();
    readonly List<(Row r, Slider sl)> _rows = new();
    readonly List<Border> _after = new();
    static readonly Color[] Swatches = BuildSwatches();
    const int DashW = 240, DashH = 135, ActW = 520, ActH = 200, ThumbW = 168, ThumbH = 92;
    readonly WriteableBitmap _dashAfter = Scene.Create(DashW, DashH);
    readonly WriteableBitmap _activeBmp = Scene.Create(ActW, ActH);
    readonly WriteableBitmap _dashBefore = Scene.Create(DashW, DashH);
    readonly List<string> _photos = LoadPhotoList();
    int _photoIdx;
    TextBlock? _activeName;
    string _presetName = "Personnalisé";
    bool _loading;
    const int HotkeyId = 1;

    public MainWindow()
    {
        InitializeComponent();

        Safe(BuildPreviewCard);
        BuildSection("Lumière",
            new Row("Luminosité", -0.5, 0.5, s => s.Brightness, (s, v) => s.Brightness = v, "+0.00;-0.00;0.00"),
            new Row("Contraste", 0.5, 2, s => s.Contrast, (s, v) => s.Contrast = v, "0.00"));
        BuildSection("Couleur",
            new Row("Saturation", 0, 3, s => s.Saturation, (s, v) => s.Saturation = v, "0.00"),
            new Row("Teinte", -180, 180, s => s.Hue, (s, v) => s.Hue = v, "+0;-0;0", "°"));
        BuildSection("Balance RGB",
            new Row("Rouge", 0.5, 1.5, s => s.Red, (s, v) => s.Red = v, "0.00"),
            new Row("Vert", 0.5, 1.5, s => s.Green, (s, v) => s.Green = v, "0.00"),
            new Row("Bleu", 0.5, 1.5, s => s.Blue, (s, v) => s.Blue = v, "0.00"));

        Safe(BuildActiveCard);
        int presetIndex = 0;

        foreach (var group in PresetLibrary.All.GroupBy(p => p.Category))
        {
            PresetsPanel.Children.Add(new TextBlock
            {
                Text = group.Key.ToUpper() + "  (" + group.Count() + ")",
                Style = (Style)FindResource("Section")
            });
            var wrap = new WrapPanel();
            PresetsPanel.Children.Add(wrap);
            foreach (var p in group)
            {
                int pi = presetIndex++;
                var btn = new Button { Style = (Style)FindResource("PresetCard"), Tag = p };
                var sp = new StackPanel();
                sp.Children.Add(new Border
                {
                    CornerRadius = new CornerRadius(6), ClipToBounds = true, Margin = new Thickness(0, 0, 0, 8),
                    Child = new Image { Source = SafeThumb(p, pi), Width = ThumbW, Height = ThumbH, Stretch = Stretch.Fill }
                });
                sp.Children.Add(new TextBlock { Text = p.Name, FontWeight = FontWeights.SemiBold });
                sp.Children.Add(new TextBlock
                {
                    Text = p.Desc, FontSize = 11, Margin = new Thickness(0, 4, 0, 0),
                    Foreground = (Brush)FindResource("Sub"), TextWrapping = TextWrapping.Wrap
                });
                btn.Content = sp;
                btn.Click += (_, _) =>
                {
                    CopyFrom(p.S); LoadToUi();
                    _presetName = p.Name;
                    if (_activeName != null) _activeName.Text = p.Name;
                    Status.Text = "Preset appliqué : " + p.Name;
                };
                wrap.Children.Add(btn);
            }
        }

        ChkOn.Click += (_, _) => Refresh();
        Status.Text = _engine.Available
            ? "Prêt. Fonctionne mieux en plein écran fenêtré (borderless)."
            : "API Magnification indisponible sur ce système.";
        NavAdjust.IsChecked = true;
        RbDash.IsChecked = true;
        Safe(UpdatePreview);
        Safe(LoadMonitors);

        Closing += (_, _) =>
        {
            if (_confirmPending) RevertMode();
            Native.UnregisterHotKey(new WindowInteropHelper(this).Handle, HotkeyId);
            Safe(_engine.Dispose);
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Safe(InitWindowEffects);
    }

    void InitWindowEffects()
    {
        var h = new WindowInteropHelper(this).Handle;
        var src = HwndSource.FromHwnd(h);
        src.CompositionTarget.BackgroundColor = Colors.Transparent;

        int dark = 1, corner = 2, backdrop = 3; // 3 = acrylique
        Native.DwmSetWindowAttribute(h, 20, ref dark, 4);
        Native.DwmSetWindowAttribute(h, 33, ref corner, 4);
        int res = Native.DwmSetWindowAttribute(h, 38, ref backdrop, 4);
        if (res != 0) Root.Background = new SolidColorBrush(Color.FromRgb(0x10, 0x1C, 0x28)); // pas d'acrylique : fond opaque

        Native.RegisterHotKey(h, HotkeyId, 0x2 | 0x1, 0x43); // Ctrl+Alt+C
        src.AddHook(WndProc);
    }

    IntPtr WndProc(IntPtr hwnd, int msg, IntPtr w, IntPtr l, ref bool handled)
    {
        if (msg == 0x0086) // WM_NCACTIVATE : garde l'aspect "actif" (acrylique) même sans le focus
        {
            handled = true;
            return Native.DefWindowProc(hwnd, msg, (IntPtr)1, new IntPtr(-1));
        }
        if (msg == 0x0312 && w.ToInt32() == HotkeyId)
        {
            ChkOn.IsChecked = !(ChkOn.IsChecked ?? false);
            Refresh();
            handled = true;
        }
        return IntPtr.Zero;
    }

    static void Safe(Action a)
    {
        try { a(); } catch (Exception ex) { Log.Write(ex); }
    }

    ImageSource? SafeThumb(Preset p, int pi)
    {
        try { return Scene.Thumb(ThumbW, ThumbH, p.S, BaseFor(ThumbW, ThumbH, PhotoIndexFor(p.Name, pi))); }
        catch (Exception ex) { Log.Write(ex); return null; }
    }

    // ---------- Photos ----------
    static List<string> LoadPhotoList()
    {
        string[] dirs =
        {
            Path.Combine(AppContext.BaseDirectory, "photos"),
            Path.Combine(AppContext.BaseDirectory, "..", "photos")
        };
        foreach (var d in dirs)
        {
            if (!Directory.Exists(d)) continue;
            var files = Directory.GetFiles(d)
                .Where(f => new[] { ".jpg", ".jpeg", ".png", ".bmp" }.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f).ToList();
            if (files.Count > 0) return files;
        }
        return new List<string>();
    }

    // Nom de preset -> nom de fichier photo (sans extension) quand un thème correspond
    static readonly Dictionary<string, string> PhotoMap = new()
    {
        ["Coucher de soleil"] = "coucher-soleil", ["Aube dorée"] = "coucher-soleil", ["Teal & Orange"] = "coucher-soleil",
        ["Forêt"] = "foret", ["Printemps"] = "fleurs", ["Fantasy"] = "fleurs", ["Pastel"] = "fleurs",
        ["Océan"] = "ocean", ["Tropical"] = "plage-tropicale", ["Désert"] = "desert", ["Western poussiéreux"] = "desert",
        ["Neige"] = "neige-montagne", ["Froid"] = "neige-montagne", ["Orage"] = "orage", ["Thriller froid"] = "orage",
        ["Automne"] = "automne", ["Rétro"] = "automne", ["Vintage 70s"] = "automne",
        ["Cyberpunk"] = "ville-nuit", ["Blade Runner"] = "ville-nuit", ["Course nocturne"] = "ville-nuit",
        ["Néon"] = "ville-nuit", ["Film noir"] = "ville-nuit", ["Noir & Blanc"] = "ville-nuit",
        ["Survie"] = "foret", ["Monde ouvert"] = "foret", ["Médiéval"] = "automne", ["Post-apocalypse"] = "desert",
    };

    int PhotoIndexFor(string presetName, int fallback)
    {
        if (PhotoMap.TryGetValue(presetName, out var stem))
        {
            int i = _photos.FindIndex(f => Path.GetFileNameWithoutExtension(f) == stem);
            if (i >= 0) return i;
        }
        return fallback;
    }

    float[] BaseFor(int w, int h, int idx)
    {
        if (_photos.Count == 0) return Scene.Base(w, h);
        try { return Scene.BaseFromPhoto(_photos[idx % _photos.Count], w, h); }
        catch { return Scene.Base(w, h); }
    }

    // ---------- Aperçu avant / après ----------
    static Color[] BuildSwatches()
    {
        var list = new List<Color>();
        for (int i = 0; i < 12; i++) list.Add(FromHsv(i * 30, 1, 1));
        for (int i = 0; i < 4; i++) list.Add(FromHsv(i * 90 + 20, 0.35, 1));
        list.Add(Color.FromRgb(0xC6, 0x8E, 0x6E)); // ton chair
        list.Add(Color.FromRgb(0x8A, 0x5A, 0x3C));
        for (int i = 0; i < 6; i++) { byte g = (byte)(i * 51); list.Add(Color.FromRgb(g, g, g)); }
        return list.ToArray();
    }

    static Color FromHsv(double h, double s, double v)
    {
        double c = v * s, x = c * (1 - Math.Abs(h / 60 % 2 - 1)), m = v - c;
        (double r, double g, double b) = (int)(h / 60) switch
        {
            0 => (c, x, 0.0), 1 => (x, c, 0.0), 2 => (0.0, c, x),
            3 => (0.0, x, c), 4 => (x, 0.0, c), _ => (c, 0.0, x)
        };
        return Color.FromRgb((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
    }

    void BuildPreviewCard()
    {
        DashView.Children.Add(new TextBlock { Text = "APERÇU EN DIRECT", Style = (Style)FindResource("Section") });
        var stack = new StackPanel();

        // Images avant / après
        var before = _dashBefore;
        var pair = new Grid();
        pair.ColumnDefinitions.Add(new ColumnDefinition());
        pair.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        pair.ColumnDefinitions.Add(new ColumnDefinition());
        pair.Children.Add(ImageWithCaption("Avant", before, 0));
        pair.Children.Add(ImageWithCaption("Après", _dashAfter, 2));
        stack.Children.Add(pair);

        stack.Children.Add(new TextBlock { Text = "Palette", Foreground = (Brush)FindResource("Sub"), FontSize = 11, Margin = new Thickness(0, 12, 0, 0) });
        stack.Children.Add(MakeStrip(false));
        stack.Children.Add(MakeStrip(true));
        DashView.Children.Add(new Border
        {
            Style = (Style)FindResource("Card"), Padding = new Thickness(16, 12, 16, 16), Child = stack
        });
    }

    UIElement ImageWithCaption(string caption, ImageSource src, int col)
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = caption, Foreground = (Brush)FindResource("Sub"), FontSize = 11, Margin = new Thickness(0, 0, 0, 6) });
        sp.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(8), ClipToBounds = true,
            Child = new Image { Source = src, Stretch = Stretch.Fill, Height = 130 }
        });
        Grid.SetColumn(sp, col);
        return sp;
    }

    void BuildActiveCard()
    {
        PagePresets.Children.Insert(0, new TextBlock { Text = "PRESET ACTIF", Style = (Style)FindResource("Section") });
        _activeName = new TextBlock { Text = _presetName, FontWeight = FontWeights.SemiBold, FontSize = 15, Margin = new Thickness(0, 10, 0, 0) };
        var sp = new StackPanel();
        var activeImg = new Image
        {
            Source = _activeBmp, Stretch = Stretch.Fill, Height = 190,
            Cursor = System.Windows.Input.Cursors.Hand, ToolTip = "Clique pour changer d'image"
        };
        activeImg.MouseLeftButtonDown += (_, _) => { _photoIdx++; UpdatePreview(); };
        sp.Children.Add(new Border { CornerRadius = new CornerRadius(8), ClipToBounds = true, Child = activeImg });
        sp.Children.Add(_activeName);
        sp.Children.Add(new TextBlock
        {
            Text = "Aperçu du rendu sur une scène de référence. Il suit tes réglages en direct.",
            Foreground = (Brush)FindResource("Sub"), FontSize = 11, Margin = new Thickness(0, 4, 0, 0)
        });
        PagePresets.Children.Insert(1, new Border
        {
            Style = (Style)FindResource("Card"), Padding = new Thickness(14, 14, 14, 14), Child = sp
        });
    }

    UIElement MakeStrip(bool after)
    {
        var grid = new UniformGrid { Rows = 1, Height = 30, Margin = new Thickness(0, 6, 0, 0) };
        foreach (var c in Swatches)
        {
            var b = new Border { Background = new SolidColorBrush(c), Margin = new Thickness(1, 0, 1, 0) };
            if (after) _after.Add(b);
            grid.Children.Add(b);
        }
        return new Border { CornerRadius = new CornerRadius(6), ClipToBounds = true, Child = grid };
    }

    void UpdatePreview()
    {
        if (_after.Count == 0) return;
        bool on = ChkOn.IsChecked == true;
        var m = ColorEngine.Build(on ? _s : new Settings());
        Scene.Render(_dashBefore, BaseFor(DashW, DashH, _photoIdx), ColorEngine.Build(new Settings()));
        Scene.Render(_dashAfter, BaseFor(DashW, DashH, _photoIdx), m);
        Scene.Render(_activeBmp, BaseFor(ActW, ActH, _photoIdx), m);
        for (int i = 0; i < _after.Count; i++)
        {
            var c = Swatches[i];
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            byte Out(int j) => (byte)(Math.Clamp(r * m[j] + g * m[5 + j] + b * m[10 + j] + m[20 + j], 0, 1) * 255);
            ((SolidColorBrush)_after[i].Background).Color = Color.FromRgb(Out(0), Out(1), Out(2));
        }
    }

    // ---------- Construction des sections (curseurs + dials) ----------
    void BuildSection(string title, params Row[] rows)
    {
        var sectionHeader = () => new TextBlock { Text = title.ToUpper(), Style = (Style)FindResource("Section") };
        SlidersView.Children.Add(sectionHeader());
        DashView.Children.Add(sectionHeader());

        var stack = new StackPanel();
        var dials = new WrapPanel();

        foreach (var row in rows)
        {
            double def = row.Get(new Settings());
            var head = new Grid();
            head.ColumnDefinitions.Add(new ColumnDefinition());
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            head.Children.Add(new TextBlock { Text = row.Label });
            var val = new TextBlock { Foreground = (Brush)FindResource("Accent"), FontWeight = FontWeights.SemiBold };
            Grid.SetColumn(val, 1);
            head.Children.Add(val);

            var sl = new Slider
            {
                Minimum = row.Min, Maximum = row.Max, Value = def,
                Style = (Style)FindResource("GlassSlider"),
                Margin = new Thickness(0, 4, 0, 12),
                ToolTip = "Double-clic : réinitialiser"
            };
            val.Text = def.ToString(row.Fmt) + row.Unit;

            var dial = new Dial
            {
                Min = row.Min, Max = row.Max, Def = def, Label = row.Label,
                Fmt = row.Fmt, Unit = row.Unit, Margin = new Thickness(4, 4, 8, 0)
            };
            dial.SetValue(def, false);
            dial.ValueChanged += v => sl.Value = v;

            sl.ValueChanged += (_, e) =>
            {
                val.Text = e.NewValue.ToString(row.Fmt) + row.Unit;
                dial.SetValue(e.NewValue, false);
                if (_loading) return;
                _presetName = "Personnalisé";
                if (_activeName != null) _activeName.Text = _presetName;
                row.Set(_s, e.NewValue);
                Refresh();
            };
            sl.MouseDoubleClick += (_, _) => sl.Value = def;

            stack.Children.Add(head);
            stack.Children.Add(sl);
            dials.Children.Add(dial);
            _rows.Add((row, sl));
        }
        SlidersView.Children.Add(new Border { Style = (Style)FindResource("Card"), Child = stack });
        DashView.Children.Add(new Border
        {
            Style = (Style)FindResource("Card"), Padding = new Thickness(12, 8, 12, 8), Child = dials
        });
    }

    void OnView(object sender, RoutedEventArgs e)
    {
        if (DashView == null || SlidersView == null) return;
        DashView.Visibility = RbDash.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        SlidersView.Visibility = RbSliders.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    void OnNav(object sender, RoutedEventArgs e)
    {
        if (PageAdjust == null || PagePresets == null || PageAbout == null || PageDisplay == null) return;
        PageAdjust.Visibility = NavAdjust.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PagePresets.Visibility = NavPresets.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PageAbout.Visibility = NavAbout.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PageDisplay.Visibility = NavDisplay.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PageTitle.Text = NavAdjust.IsChecked == true ? "Réglages" : NavPresets.IsChecked == true ? "Presets" : NavDisplay.IsChecked == true ? "Affichage" : "Infos";
    }

    void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    void OnMaximize(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    void OnClose(object sender, RoutedEventArgs e) => Close();

    void OnReset(object sender, RoutedEventArgs e) { CopyFrom(new Settings()); LoadToUi(); }

    void Refresh()
    {
        bool on = ChkOn.IsChecked == true;
        StateText.Text = on ? "Effet actif" : "Effet coupé";
        if (on) _engine.Apply(_s); else _engine.Reset();
        UpdatePreview();
    }

    void LoadToUi()
    {
        _loading = true;
        foreach (var (r, sl) in _rows) sl.Value = r.Get(_s);
        _loading = false;
        Refresh();
    }

    void CopyFrom(Settings o)
    {
        _s.Brightness = o.Brightness; _s.Contrast = o.Contrast; _s.Saturation = o.Saturation;
        _s.Hue = o.Hue; _s.Red = o.Red; _s.Green = o.Green; _s.Blue = o.Blue;
    }

    void OnSave(object sender, RoutedEventArgs e)
    {
        var d = new SaveFileDialog { Filter = "Preset (*.json)|*.json", FileName = "preset.json" };
        if (d.ShowDialog() == true) File.WriteAllText(d.FileName, JsonSerializer.Serialize(_s));
    }

    void OnLoad(object sender, RoutedEventArgs e)
    {
        var d = new OpenFileDialog { Filter = "Preset (*.json)|*.json" };
        if (d.ShowDialog() != true) return;
        try
        {
            var o = JsonSerializer.Deserialize<Settings>(File.ReadAllText(d.FileName));
            if (o != null) { CopyFrom(o); LoadToUi(); Status.Text = "Preset chargé."; }
        }
        catch { Status.Text = "Preset invalide."; }
    }
}






