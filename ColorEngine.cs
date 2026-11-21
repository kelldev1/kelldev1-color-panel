namespace ColorPanel;

public class Settings
{
    public double Brightness { get; set; } = 0;   // -0.5..0.5
    public double Contrast { get; set; } = 1;     // 0.5..2
    public double Saturation { get; set; } = 1;   // 0..3
    public double Hue { get; set; } = 0;          // -180..180
    public double Red { get; set; } = 1;          // 0.5..1.5
    public double Green { get; set; } = 1;
    public double Blue { get; set; } = 1;
}

/// <summary>Applique une matrice de couleur plein ecran via l'API Magnification de Windows (sans injection).</summary>
public sealed class ColorEngine : IDisposable
{
    private bool _ok;
    public bool Available => _ok;

    public ColorEngine() { try { _ok = Native.MagInitialize(); } catch (Exception ex) { Log.Write(ex); _ok = false; } }

    static float[] Identity() => new float[]
    { 1,0,0,0,0,  0,1,0,0,0,  0,0,1,0,0,  0,0,0,1,0,  0,0,0,0,1 };

    // Convention Windows: vecteur ligne [r g b a 1] * M. Mul(A,B) = A appliquee en premier.
    static float[] Mul(float[] a, float[] b)
    {
        var r = new float[25];
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
            {
                float s = 0;
                for (int k = 0; k < 5; k++) s += a[i * 5 + k] * b[k * 5 + j];
                r[i * 5 + j] = s;
            }
        return r;
    }

    public static float[] Build(Settings s)
    {
        const double lr = 0.2126, lg = 0.7152, lb = 0.0722;

        // Saturation
        double sat = s.Saturation;
        var sa = Identity();
        sa[0] = (float)((1 - sat) * lr + sat); sa[1] = (float)((1 - sat) * lr);       sa[2] = (float)((1 - sat) * lr);
        sa[5] = (float)((1 - sat) * lg);       sa[6] = (float)((1 - sat) * lg + sat); sa[7] = (float)((1 - sat) * lg);
        sa[10] = (float)((1 - sat) * lb);      sa[11] = (float)((1 - sat) * lb);      sa[12] = (float)((1 - sat) * lb + sat);

        // Teinte (rotation autour de l'axe gris)
        double a = s.Hue * Math.PI / 180.0, c = Math.Cos(a), sn = Math.Sin(a);
        var h = Identity();
        h[0]  = (float)(lr + c * (1 - lr) + sn * (-lr));
        h[1]  = (float)(lr + c * (-lr) + sn * 0.143);
        h[2]  = (float)(lr + c * (-lr) + sn * (-(1 - lr)));
        h[5]  = (float)(lg + c * (-lg) + sn * (-lg));
        h[6]  = (float)(lg + c * (1 - lg) + sn * 0.140);
        h[7]  = (float)(lg + c * (-lg) + sn * lg);
        h[10] = (float)(lb + c * (-lb) + sn * (1 - lb));
        h[11] = (float)(lb + c * (-lb) + sn * (-0.283));
        h[12] = (float)(lb + c * (1 - lb) + sn * lb);

        // Gains RGB
        var g = Identity();
        g[0] = (float)s.Red; g[6] = (float)s.Green; g[12] = (float)s.Blue;

        // Contraste (autour de 0.5) + luminosite
        var ct = Identity();
        float k = (float)s.Contrast;
        ct[0] = ct[6] = ct[12] = k;
        float off = 0.5f * (1 - k) + (float)s.Brightness;
        ct[20] = ct[21] = ct[22] = off;

        return Mul(Mul(Mul(sa, h), g), ct);
    }

    public bool Apply(Settings s) => Set(Build(s));
    public bool Reset() => Set(Identity());

    bool Set(float[] m)
    {
        if (!_ok) return false;
        try
        {
            var e = new Native.MAGCOLOREFFECT { transform = m };
            return Native.MagSetFullscreenColorEffect(ref e);
        }
        catch (Exception ex) { Log.Write(ex); return false; }
    }

    public void Dispose()
    {
        if (!_ok) return;
        try { Reset(); Native.MagUninitialize(); }
        catch (Exception ex) { Log.Write(ex); }
        _ok = false;
    }
}

