using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ColorPanel;

/// <summary>Bouton rotatif (gauge circulaire). Glisser verticalement, molette, double-clic = reset.</summary>
public sealed class Dial : FrameworkElement
{
    static readonly Color AccentColor = Color.FromRgb(0x7F, 0xDB, 0xFF);
    static readonly Brush AccentBrush = Make(AccentColor);
    static readonly Brush TrackBrush = Make(Color.FromArgb(0x33, 255, 255, 255));
    static readonly Brush SubBrush = Make(Color.FromRgb(0xA3, 0xB8, 0xC8));
    static readonly Pen TrackPen = MakePen(TrackBrush, 7);
    static readonly Pen ValuePen = MakePen(AccentBrush, 7);
    static readonly Pen ThumbPen = MakePen(AccentBrush, 3);
    static readonly Typeface Face = new(new FontFamily("Segoe UI Variable Text, Segoe UI"),
        FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

    static Brush Make(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
    static Pen MakePen(Brush b, double t)
    {
        var p = new Pen(b, t) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        p.Freeze();
        return p;
    }

    public double Min { get; init; }
    public double Max { get; init; } = 1;
    public double Def { get; init; }
    public string Label { get; init; } = "";
    public string Fmt { get; init; } = "0.00";
    public string Unit { get; init; } = "";

    double _v;
    public double Value => _v;
    public event Action<double>? ValueChanged;

    double _dragStartY, _dragStartV;

    public Dial()
    {
        Width = 112;
        Height = 128;
        Cursor = Cursors.Hand;
        ToolTip = "Glisser, molette, ou double-clic pour réinitialiser";
    }

    public void SetValue(double v, bool notify)
    {
        v = Math.Clamp(v, Min, Max);
        if (Math.Abs(v - _v) < 1e-9) return;
        _v = v;
        InvalidateVisual();
        if (notify) ValueChanged?.Invoke(_v);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) { SetValue(Def, true); return; }
        _dragStartY = e.GetPosition(this).Y;
        _dragStartV = _v;
        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!IsMouseCaptured) return;
        double dy = _dragStartY - e.GetPosition(this).Y;
        double fine = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 0.2 : 1.0;
        SetValue(_dragStartV + dy / 150.0 * (Max - Min) * fine, true);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) => ReleaseMouseCapture();

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        SetValue(_v + Math.Sign(e.Delta) * (Max - Min) / 100.0, true);
        e.Handled = true;
    }

    static readonly Point C = new(56, 54);
    const double R = 40;

    static Point Pt(double deg)
    {
        double a = deg * Math.PI / 180.0;
        return new Point(C.X + R * Math.Cos(a), C.Y + R * Math.Sin(a));
    }

    double Ang(double v) => 135 + 270 * (v - Min) / (Max - Min);

    static void Arc(DrawingContext dc, Pen pen, double a0, double a1)
    {
        if (Math.Abs(a1 - a0) < 0.1) return;
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            ctx.BeginFigure(Pt(a0), false, false);
            ctx.ArcTo(Pt(a1), new Size(R, R), 0, Math.Abs(a1 - a0) > 180,
                a1 > a0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, true, false);
        }
        g.Freeze();
        dc.DrawGeometry(null, pen, g);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, Width, Height));

        Arc(dc, TrackPen, 135, 405);
        Arc(dc, ValuePen, Ang(Def), Ang(_v));
        dc.DrawEllipse(Brushes.White, ThumbPen, Pt(Ang(_v)), 6, 6);

        var val = new FormattedText(_v.ToString(Fmt, CultureInfo.CurrentCulture) + Unit,
            CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, Face, 16, Brushes.White, dpi);
        dc.DrawText(val, new Point(C.X - val.Width / 2, C.Y - val.Height / 2));

        var lab = new FormattedText(Label, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            Face, 12, SubBrush, dpi);
        dc.DrawText(lab, new Point(C.X - lab.Width / 2, 104));
    }
}

