using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ColorPanel;

/// <summary>Scène de référence générée en code (paysage, cabane, fleurs, dégradé de gris)
/// sur laquelle on applique la matrice de couleur d'un preset pour en montrer le rendu.</summary>
public static class Scene
{
    static readonly Dictionary<(int, int), float[]> Cache = new();

    public static float[] Base(int w, int h)
    {
        if (Cache.TryGetValue((w, h), out var cached)) return cached;
        var a = new float[w * h * 3];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var (r, g, b) = Pixel((x + 0.5) / w, (y + 0.5) / h, w, h);
                int i = (y * w + x) * 3;
                a[i] = (float)Math.Clamp(r, 0, 1);
                a[i + 1] = (float)Math.Clamp(g, 0, 1);
                a[i + 2] = (float)Math.Clamp(b, 0, 1);
            }
        Cache[(w, h)] = a;
        return a;
    }

    static readonly Dictionary<(string, int, int), float[]> PhotoCache = new();

    /// <summary>Charge une vraie photo, recadrée en remplissage (UniformToFill) à w x h.</summary>
    public static float[] BaseFromPhoto(string path, int w, int h)
    {
        if (PhotoCache.TryGetValue((path, w, h), out var cached)) return cached;
        var bi = new BitmapImage();
        bi.BeginInit();
        bi.CacheOption = BitmapCacheOption.OnLoad;
        bi.UriSource = new Uri(path);
        bi.EndInit();

        double s = Math.Max((double)w / bi.PixelWidth, (double)h / bi.PixelHeight);
        var scaled = new TransformedBitmap(bi, new ScaleTransform(s, s));
        int sw = scaled.PixelWidth, sh = scaled.PixelHeight;
        var fc = new FormatConvertedBitmap(scaled, PixelFormats.Bgr32, null, 0);
        var px = new byte[sw * sh * 4];
        fc.CopyPixels(px, sw * 4, 0);

        int ox = Math.Max(0, (sw - w) / 2), oy = Math.Max(0, (sh - h) / 2);
        var a = new float[w * h * 3];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int sx = Math.Min(sw - 1, x + ox), sy = Math.Min(sh - 1, y + oy);
                int si = (sy * sw + sx) * 4, di = (y * w + x) * 3;
                a[di] = px[si + 2] / 255f;
                a[di + 1] = px[si + 1] / 255f;
                a[di + 2] = px[si] / 255f;
            }
        PhotoCache[(path, w, h)] = a;
        return a;
    }

    public static ImageSource Thumb(int w, int h, Settings s, float[] baseRgb)
    {
        var bmp = Create(w, h);
        Render(bmp, baseRgb, ColorEngine.Build(s));
        bmp.Freeze();
        return bmp;
    }

    public static WriteableBitmap Create(int w, int h) => new(w, h, 96, 96, PixelFormats.Bgr32, null);

    public static void Render(WriteableBitmap bmp, float[] src, float[] m)
    {
        int w = bmp.PixelWidth, h = bmp.PixelHeight;
        var buf = new byte[w * h * 4];
        for (int p = 0; p < w * h; p++)
        {
            float r = src[p * 3], g = src[p * 3 + 1], b = src[p * 3 + 2];
            buf[p * 4 + 2] = Out(r * m[0] + g * m[5] + b * m[10] + m[20]);
            buf[p * 4 + 1] = Out(r * m[1] + g * m[6] + b * m[11] + m[21]);
            buf[p * 4 + 0] = Out(r * m[2] + g * m[7] + b * m[12] + m[22]);
        }
        bmp.WritePixels(new Int32Rect(0, 0, w, h), buf, w * 4, 0);
    }

    static byte Out(float v) => (byte)(Math.Clamp(v, 0f, 1f) * 255f + 0.5f);

    public static ImageSource Thumb(int w, int h, Settings s)
    {
        var bmp = Create(w, h);
        Render(bmp, Base(w, h), ColorEngine.Build(s));
        bmp.Freeze();
        return bmp;
    }

    static (double, double, double) Lerp((double r, double g, double b) a, (double r, double g, double b) b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return (a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t);
    }

    static (double, double, double) Pixel(double u, double v, int w, int h)
    {
        double aspect = (double)w / h;

        // Dégradé de gris en bas (pour juger contraste/luminosité)
        if (v > 0.95) return (u, u, u);

        double ridge1 = 0.48 - 0.09 * Math.Sin(u * 6 + 1) - 0.03 * Math.Sin(u * 17);
        double ridge2 = 0.58 - 0.06 * Math.Sin(u * 9 + 2);
        const double horizon = 0.66;

        (double r, double g, double b) col;

        if (v < horizon)
        {
            // Ciel
            double t = Math.Clamp(v / 0.62, 0, 1);
            col = Lerp((0.20, 0.45, 0.88), (1.0, 0.76, 0.50), t * t);

            // Soleil + halo
            double dx = (u - 0.72) * aspect, dy = v - 0.28;
            double d = Math.Sqrt(dx * dx + dy * dy);
            if (d < 0.10) col = (1.0, 0.96, 0.76);
            else
            {
                double glow = 0.45 * Math.Exp(-(d - 0.10) * 6);
                col = (col.r + glow, col.g + glow * 0.8, col.b + glow * 0.45);
            }

            // Montagnes lointaines
            if (v > ridge1) col = Lerp((0.36, 0.30, 0.58), (0.85, 0.62, 0.62), Math.Clamp((v - ridge1) * 3, 0, 1) * 0.5);
            // Collines proches
            if (v > ridge2) col = Lerp((0.12, 0.38, 0.30), (0.20, 0.50, 0.24), (v - ridge2) * 3);

            // Cabane sur la colline de gauche
            if (u > 0.10 && u < 0.22 && v > 0.52 && v < 0.62)
                col = (0.86, 0.72, 0.54);
            double roofT = (v - 0.45) / 0.07;
            if (v > 0.45 && v <= 0.52 && Math.Abs(u - 0.16) < 0.075 * roofT)
                col = (0.72, 0.20, 0.15);
            if (u > 0.145 && u < 0.175 && v > 0.55 && v < 0.595)
                col = (1.0, 0.86, 0.40);
        }
        else if (v < 0.80)
        {
            // Lac
            double t = (v - horizon) / (0.80 - horizon);
            col = Lerp((0.88, 0.68, 0.52), (0.14, 0.42, 0.66), Math.Pow(t, 0.6));
            col = (col.r + 0.03 * Math.Sin(v * 140 + u * 12), col.g + 0.03 * Math.Sin(v * 140 + u * 12), col.b);
            double refl = Math.Abs(u - 0.72);
            if (refl < 0.035 * (1 + t * 1.5))
            {
                double k = (1 - refl / (0.035 * (1 + t * 1.5))) * 0.35 * (1 - t * 0.5);
                col = (col.r + k, col.g + k * 0.85, col.b + k * 0.5);
            }
        }
        else
        {
            // Herbe + fleurs
            double wave = 0.88 + 0.12 * Math.Sin(u * 55 + v * 25);
            col = (0.22 * wave, 0.64 * wave, 0.18 * wave);
            int cx = (int)(u * w / 5.0), cy = (int)(v * h / 5.0);
            int n = (int)(((uint)(cx * 73856093) ^ (uint)(cy * 19349663)) % 100);
            if (n < 5) col = (0.92, 0.14, 0.14);
            else if (n < 9) col = (0.96, 0.84, 0.18);
            else if (n < 11) col = (0.95, 0.95, 0.95);
        }
        return col;
    }
}
