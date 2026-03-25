using System.ComponentModel;
using BlancoMonitor.UI.Theme;

namespace BlancoMonitor.UI.Controls;

/// <summary>
/// Custom-drawn "B" logo in neon-green retro terminal style.
/// Renders a geometric "B" shape with glow effect on black background.
/// Sizes: Icon (32×32), Small (48×48), Medium (80×80), Large (128×128).
///
/// Design concept: Bold geometric "B" formed by two rounded bumps on the right,
/// a solid vertical stroke on the left, with neon green outlines and subtle
/// glow — evoking an old CRT monitor aesthetic.
/// </summary>
public sealed class BlancoLogo : Control
{
    private LogoSize _logoSize = LogoSize.Medium;
    private bool _showGlow = true;

    public enum LogoSize
    {
        Icon = 32,
        Small = 48,
        Medium = 80,
        Large = 128,
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public LogoSize CurrentSize
    {
        get => _logoSize;
        set
        {
            _logoSize = value;
            Size = new Size((int)value, (int)value);
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowGlow
    {
        get => _showGlow;
        set { _showGlow = value; Invalidate(); }
    }

    public BlancoLogo()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Size = new Size((int)_logoSize, (int)_logoSize);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var sz = Math.Min(Width, Height);
        float pad = sz * 0.08f;
        var area = new RectangleF(pad, pad, sz - pad * 2, sz - pad * 2);

        // Background circle (optional dark backing)
        using var bgBrush = new SolidBrush(Color.FromArgb(200, 5, 5, 5));
        g.FillEllipse(bgBrush, area);

        // Border circle
        float borderWidth = Math.Max(1.5f, sz * 0.02f);
        using var borderPen = new Pen(NeonTheme.Border, borderWidth);
        g.DrawEllipse(borderPen, area);

        // Draw the "B" letter
        DrawLetterB(g, area);

        // Glow effect — outer ring
        if (_showGlow && sz >= 48)
        {
            var glowColor = Color.FromArgb(30, 0, 255, 65);
            using var glowPen = new Pen(glowColor, sz * 0.04f);
            var glowRect = new RectangleF(area.X - 2, area.Y - 2, area.Width + 4, area.Height + 4);
            g.DrawEllipse(glowPen, glowRect);
        }
    }

    private void DrawLetterB(Graphics g, RectangleF area)
    {
        float cx = area.X + area.Width * 0.5f;
        float cy = area.Y + area.Height * 0.5f;

        // Scale factors
        float w = area.Width;
        float h = area.Height;

        // "B" proportions within the circle
        float left = area.X + w * 0.25f;
        float right = area.X + w * 0.72f;
        float top = area.Y + h * 0.18f;
        float mid = cy;
        float bottom = area.Y + h * 0.82f;

        float strokeWidth = Math.Max(2f, w * 0.06f);

        // Neon green pen
        using var mainPen = new Pen(NeonTheme.TextPrimary, strokeWidth)
        {
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round,
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
        };

        // Accent pen (brighter for bump curves)
        using var accentPen = new Pen(NeonTheme.TextAccent, strokeWidth * 0.85f)
        {
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round,
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
        };

        // Vertical stroke (left side of B)
        g.DrawLine(mainPen, left, top, left, bottom);

        // Top horizontal
        g.DrawLine(mainPen, left, top, right - w * 0.08f, top);

        // Upper bump (arc from top-right down to middle)
        float bumpW1 = right - left - w * 0.05f;
        float bumpH1 = mid - top;
        var upperBump = new RectangleF(left + w * 0.05f, top, bumpW1, bumpH1 * 2);
        g.DrawArc(accentPen, upperBump, -90, 180);

        // Middle horizontal
        g.DrawLine(mainPen, left, mid, right - w * 0.05f, mid);

        // Lower bump (slightly wider)
        float bumpW2 = right - left;
        float bumpH2 = bottom - mid;
        var lowerBump = new RectangleF(left + w * 0.02f, mid, bumpW2, bumpH2 * 2);
        g.DrawArc(accentPen, lowerBump, -90, 180);

        // Bottom horizontal
        g.DrawLine(mainPen, left, bottom, right - w * 0.03f, bottom);

        // Inner glow: subtle fill in the bumps
        if (_showGlow && w >= 48)
        {
            var fillColor = Color.FromArgb(15, 0, 255, 65);
            using var fillBrush = new SolidBrush(fillColor);

            // Upper fill
            var upperFill = new RectangleF(left + w * 0.08f, top + strokeWidth, bumpW1 * 0.7f, bumpH1 - strokeWidth);
            g.FillEllipse(fillBrush, upperFill);

            // Lower fill
            var lowerFill = new RectangleF(left + w * 0.08f, mid + strokeWidth, bumpW2 * 0.7f, bumpH2 - strokeWidth);
            g.FillEllipse(fillBrush, lowerFill);
        }
    }

    /// <summary>
    /// Generate a Bitmap of the logo at the specified pixel size.
    /// Useful for creating icons or embedding in reports.
    /// </summary>
    public static Bitmap RenderToBitmap(int pixelSize)
    {
        var bmp = new Bitmap(pixelSize, pixelSize);
        using var ctrl = new BlancoLogo { CurrentSize = (LogoSize)pixelSize, ShowGlow = pixelSize >= 48 };
        ctrl.Size = new Size(pixelSize, pixelSize);
        ctrl.DrawToBitmap(bmp, new Rectangle(0, 0, pixelSize, pixelSize));
        return bmp;
    }

    /// <summary>
    /// Create ASCII art representation of the "B" logo for console/text contexts.
    /// </summary>
    public static string AsciiLogo => """
      ╔══════════════╗
      ║  ██████╗     ║
      ║  ██╔══██╗    ║
      ║  ██████╔╝    ║
      ║  ██╔══██╗    ║
      ║  ██████╔╝    ║
      ║  ╚═════╝     ║
      ╚══════════════╝
      """;
}
