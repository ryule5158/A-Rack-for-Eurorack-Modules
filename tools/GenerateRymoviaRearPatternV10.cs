using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;

// Raster preview generator for the Rymovia V0.10 "Phase Halo / Structural Echo"
// rear-panel artwork.  Millimetre coordinates match the 548 x 420 mm back skin.
// The production master is the sibling SVG; this PNG is only the SOLIDWORKS decal.
internal static class GenerateRymoviaRearPatternV10
{
    private const float Scale = 5.0f;
    // The CAD viewport downsamples a 548 mm-wide texture to only a few hundred
    // pixels.  Enlarge preview strokes without changing the production SVG so
    // the intended low-contrast A-direction remains legible in SOLIDWORKS.
    private const float PreviewStrokeGain = 2.4f;
    private const float PanelWidthMm = 548.0f;
    private const float PanelHeightMm = 420.0f;

    private sealed class Stroke
    {
        internal readonly PointF[] Points;
        internal readonly float WidthMm;
        internal readonly int Alpha;

        internal Stroke(float widthMm, int alpha, params PointF[] points)
        {
            Points = points;
            WidthMm = widthMm;
            Alpha = alpha;
        }
    }

    private static readonly Stroke[] Curves =
    {
        // Inner halo.  Every segment remains outside the central x/y +/-90 mm
        // VESA contact square; the deliberate gaps avoid a generic target motif.
        Bezier(0.65f, 220, P(-30,120), P(-104,120), P(-138,120), P(-138,86), P(-138,28)),
        Bezier(0.65f, 220, P(22,120), P(104,120), P(138,120), P(138,86), P(138,42)),
        Bezier(0.65f, 220, P(138,-18), P(138,-86), P(138,-120), P(104,-120), P(36,-120)),
        Bezier(0.65f, 220, P(-20,-120), P(-104,-120), P(-138,-120), P(-138,-86), P(-138,-34)),

        // Middle halo.
        Bezier(0.55f, 190, P(-58,150), P(-130,150), P(-176,150), P(-176,104), P(-176,48)),
        Bezier(0.55f, 190, P(38,150), P(130,150), P(176,150), P(176,104), P(176,28)),
        Bezier(0.55f, 190, P(176,-46), P(176,-104), P(176,-150), P(130,-150), P(52,-150)),
        Bezier(0.55f, 190, P(-40,-150), P(-130,-150), P(-176,-150), P(-176,-104), P(-176,-54)),

        // Outer halo; its horizontal shoulders echo the real y=+/-155 rear
        // crossbeam load paths while clearing all four back feet.
        Bezier(0.45f, 160, P(-92,178), P(-162,178), P(-218,178), P(-218,122), P(-218,70)),
        Bezier(0.45f, 160, P(74,178), P(162,178), P(218,178), P(218,122), P(218,64)),
        Bezier(0.45f, 160, P(218,-68), P(218,-122), P(218,-178), P(162,-178), P(92,-178)),
        Bezier(0.45f, 160, P(-76,-178), P(-162,-178), P(-218,-178), P(-218,-122), P(-218,-60))
    };

    private static readonly Stroke[] Calibration =
    {
        Line(0.35f, 132, -252,155,-228,155), Line(0.35f,132,228,155,252,155),
        Line(0.35f, 132, -252,-155,-228,-155), Line(0.35f,132,228,-155,252,-155),
        Line(0.35f, 122, -115,184,-115,190), Line(0.35f,122,115,184,115,190),
        Line(0.35f, 122, -115,-184,-115,-190), Line(0.35f,122,115,-184,115,-190),
        Line(0.35f, 122, -12,184,-12,190), Line(0.35f,122,0,182,0,190),
        Line(0.35f, 122, 12,184,12,190), Line(0.35f,122,-12,-184,-12,-190),
        Line(0.35f, 122, 0,-182,0,-190), Line(0.35f,122,12,-184,12,-190),
        Line(0.35f, 122, -252,-12,-246,-12), Line(0.35f,122,-252,0,-244,0),
        Line(0.35f, 122, -252,12,-246,12), Line(0.35f,122,246,-12,252,-12),
        Line(0.35f, 122, 244,0,252,0), Line(0.35f,122,246,12,252,12)
    };

    private static Stroke Bezier(float widthMm, int alpha, params PointF[] points)
    {
        return new Stroke(widthMm, alpha, points);
    }

    private static Stroke Line(float widthMm, int alpha, float x1, float y1, float x2, float y2)
    {
        return new Stroke(widthMm, alpha, P(x1,y1), P(x2,y2));
    }

    private static PointF P(float x, float y) { return new PointF(x, y); }

    private static int Main(string[] args)
    {
        if (args == null || args.Length != 1)
            throw new ArgumentException("Usage: GenerateRymoviaRearPatternV10.exe <output.png>");

        string output = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        int width = (int)Math.Round(PanelWidthMm * Scale);
        int height = (int)Math.Round(PanelHeightMm * Scale);

        using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            foreach (Stroke stroke in Curves) Draw(graphics, stroke, true);
            foreach (Stroke stroke in Calibration) Draw(graphics, stroke, false);

            // The artwork is already analytically outside the keep-outs.  Clear
            // them again at raster time so future edits fail safe visually.
            using (Brush erase = new SolidBrush(Color.Transparent))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                FillLocalRect(graphics, erase, -90, -90, 180, 180);
                foreach (float x in new[] { -245.0f, 245.0f })
                    foreach (float y in new[] { -185.0f, 185.0f })
                        FillLocalCircle(graphics, erase, x, y, 12.0f);
                graphics.CompositingMode = CompositingMode.SourceOver;
            }

            bitmap.Save(output, ImageFormat.Png);
        }

        Console.WriteLine("RYMOVIA_REAR_PATTERN=" + output);
        Console.WriteLine("RYMOVIA_REAR_PATTERN_MM=548x420");
        Console.WriteLine("RYMOVIA_REAR_VESA_KEEPOUT_MM=180x180");
        Console.WriteLine("RYMOVIA_REAR_FOOT_KEEPOUTS=4xR12");
        Console.WriteLine("RYMOVIA_REAR_PHASE_HALOS=3");
        return 0;
    }

    private static void Draw(Graphics graphics, Stroke stroke, bool roundedBezier)
    {
        using (Pen pen = new Pen(Color.FromArgb(stroke.Alpha, 220, 218, 211),
            stroke.WidthMm * Scale * PreviewStrokeGain))
        using (GraphicsPath path = new GraphicsPath())
        {
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;
            PointF[] p = Transform(stroke.Points);
            if (!roundedBezier || p.Length == 2)
            {
                path.AddLines(p);
            }
            else
            {
                // Five control points describe: straight shoulder, quadratic
                // rounded corner, straight return.  Convert the quadratic to a
                // cubic Bezier so the PNG and SVG share the same construction.
                path.StartFigure();
                path.AddLine(p[0], p[1]);
                PointF c1 = new PointF(
                    p[1].X + (2.0f / 3.0f) * (p[2].X - p[1].X),
                    p[1].Y + (2.0f / 3.0f) * (p[2].Y - p[1].Y));
                PointF c2 = new PointF(
                    p[3].X + (2.0f / 3.0f) * (p[2].X - p[3].X),
                    p[3].Y + (2.0f / 3.0f) * (p[2].Y - p[3].Y));
                path.AddBezier(p[1], c1, c2, p[3]);
                path.AddLine(p[3], p[4]);
            }
            graphics.DrawPath(pen, path);
        }
    }

    private static PointF[] Transform(PointF[] source)
    {
        PointF[] result = new PointF[source.Length];
        for (int i = 0; i < source.Length; i++)
            result[i] = new PointF(
                (source[i].X + PanelWidthMm / 2.0f) * Scale,
                (PanelHeightMm / 2.0f - source[i].Y) * Scale);
        return result;
    }

    private static void FillLocalRect(Graphics graphics, Brush brush,
        float x, float y, float width, float height)
    {
        graphics.FillRectangle(brush,
            (x + PanelWidthMm / 2.0f) * Scale,
            (PanelHeightMm / 2.0f - (y + height)) * Scale,
            width * Scale, height * Scale);
    }

    private static void FillLocalCircle(Graphics graphics, Brush brush,
        float x, float y, float radius)
    {
        graphics.FillEllipse(brush,
            (x - radius + PanelWidthMm / 2.0f) * Scale,
            (PanelHeightMm / 2.0f - (y + radius)) * Scale,
            2.0f * radius * Scale, 2.0f * radius * Scale);
    }
}
