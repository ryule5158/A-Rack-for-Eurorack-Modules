using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;

// Raster and vector artwork generator for the Rymovia V0.10 "Phase Halo"
// rear panel. The selected concept A is a family of concentric circular /
// elliptical orbital arcs, interrupted asymmetrically with solid and dotted
// fragments. Millimetre coordinates match the 548 x 420 mm back skin.
internal static class GenerateRymoviaRearPatternV10
{
    private const float Scale = 5.0f;
    // SOLIDWORKS downsamples a 548 mm-wide decal to only a few hundred pixels.
    // Preview strokes are enlarged here; the generated SVGs retain production
    // widths and low contrast.
    private const float PreviewStrokeGain = 2.4f;
    private const float PanelWidthMm = 548.0f;
    private const float PanelHeightMm = 420.0f;

    private sealed class ArcStroke
    {
        internal readonly float RadiusX;
        internal readonly float RadiusY;
        internal readonly float StartDegrees;
        internal readonly float SweepDegrees;
        internal readonly float WidthMm;
        internal readonly int Alpha;
        internal readonly bool Dotted;

        internal ArcStroke(float radiusX, float radiusY, float startDegrees,
            float sweepDegrees, float widthMm, int alpha, bool dotted)
        {
            RadiusX = radiusX;
            RadiusY = radiusY;
            StartDegrees = startDegrees;
            SweepDegrees = sweepDegrees;
            WidthMm = widthMm;
            Alpha = alpha;
            Dotted = dotted;
        }
    }

    private sealed class LineStroke
    {
        internal readonly PointF Start;
        internal readonly PointF End;
        internal readonly float WidthMm;
        internal readonly int Alpha;

        internal LineStroke(float widthMm, int alpha,
            float x1, float y1, float x2, float y2)
        {
            WidthMm = widthMm;
            Alpha = alpha;
            Start = new PointF(x1, y1);
            End = new PointF(x2, y2);
        }
    }

    // Angles use the conventional local CAD frame: 0 degrees is right and
    // positive rotation is counter-clockwise. Staggered breaks reproduce the
    // asymmetry and sparse dotted fragments of the user's selected concept A.
    private static readonly ArcStroke[] Arcs =
    {
        // Orbit 1: closest to the VESA keep-out; its complete ellipse remains
        // outside the central +/-90 mm square.
        Arc(142,124,  20, 45, 0.65f,220,false),
        Arc(142,124, 103, 52, 0.65f,220,false),
        Arc(142,124, 174, 31, 0.56f,204,true),
        Arc(142,124, 250, 65, 0.62f,218,false),

        // Orbit 2.
        Arc(162,141,  43, 48, 0.58f,202,false),
        Arc(162,141, 116, 59, 0.58f,202,false),
        Arc(162,141, 208, 33, 0.50f,188,true),
        Arc(162,141, 278, 62, 0.56f,198,false),

        // Orbit 3.
        Arc(182,158,  18, 39, 0.52f,186,true),
        Arc(182,158,  74, 38, 0.54f,198,false),
        Arc(182,158, 133, 59, 0.54f,198,false),
        Arc(182,158, 229, 57, 0.52f,190,false),

        // Orbit 4.
        Arc(202,175,  28, 39, 0.46f,174,false),
        Arc(202,175,  93, 60, 0.48f,186,false),
        Arc(202,175, 178, 43, 0.42f,166,true),
        Arc(202,175, 250, 55, 0.46f,178,false),

        // Orbit 5: widest, quietest ring. It stays within the 16 mm edge band
        // and well clear of all four R12 rear-foot keep-outs.
        Arc(222,190,   9, 36, 0.40f,154,true),
        Arc(222,190,  65, 38, 0.42f,166,false),
        Arc(222,190, 123, 46, 0.42f,166,false),
        Arc(222,190, 201, 46, 0.40f,154,true),
        Arc(222,190, 280, 56, 0.42f,164,false)
    };

    // Concept A uses one small registration mark at each cardinal direction.
    private static readonly LineStroke[] Calibration =
    {
        Line(0.35f,132, -252,0,-244,0),
        Line(0.35f,132,  244,0, 252,0),
        Line(0.35f,132, 0,182,0,190),
        Line(0.35f,132, 0,-182,0,-190)
    };

    private static ArcStroke Arc(float radiusX, float radiusY,
        float startDegrees, float sweepDegrees, float widthMm, int alpha,
        bool dotted)
    {
        return new ArcStroke(radiusX, radiusY, startDegrees, sweepDegrees,
            widthMm, alpha, dotted);
    }

    private static LineStroke Line(float widthMm, int alpha,
        float x1, float y1, float x2, float y2)
    {
        return new LineStroke(widthMm, alpha, x1, y1, x2, y2);
    }

    private static int Main(string[] args)
    {
        if (args == null || args.Length != 1)
            throw new ArgumentException(
                "Usage: GenerateRymoviaRearPatternV10.exe <output.png>");

        string output = Path.GetFullPath(args[0]);
        string directory = Path.GetDirectoryName(output);
        Directory.CreateDirectory(directory);
        int width = (int)Math.Round(PanelWidthMm * Scale);
        int height = (int)Math.Round(PanelHeightMm * Scale);

        using (Bitmap bitmap = new Bitmap(width, height,
            PixelFormat.Format32bppArgb))
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            foreach (ArcStroke arc in Arcs) DrawArc(graphics, arc);
            foreach (LineStroke line in Calibration) DrawLine(graphics, line);

            // Fail-safe raster erasure. The vector geometry is already
            // analytically outside these zones, but future edits cannot leave
            // visible marks over the VESA contact square or rear feet.
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

        string reviewSvg = Path.Combine(directory,
            "rymovia-phase-halo-rear-v10.svg");
        string productionSvg = Path.Combine(directory,
            "rymovia-phase-halo-rear-v10-production-lowcontrast.svg");
        WriteSvg(reviewSvg, false);
        WriteSvg(productionSvg, true);

        Console.WriteLine("RYMOVIA_REAR_PATTERN=" + output);
        Console.WriteLine("RYMOVIA_REAR_PATTERN_MM=548x420");
        Console.WriteLine("RYMOVIA_REAR_VESA_KEEPOUT_MM=180x180");
        Console.WriteLine("RYMOVIA_REAR_FOOT_KEEPOUTS=4xR12");
        Console.WriteLine("RYMOVIA_REAR_PHASE_ORBITS=5");
        Console.WriteLine("RYMOVIA_REAR_ARC_SEGMENTS=" +
            Arcs.Length.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("RYMOVIA_REAR_REFERENCE=A_CONCENTRIC_ORBITAL_ARCS");
        return 0;
    }

    private static void DrawArc(Graphics graphics, ArcStroke arc)
    {
        if (arc.Dotted)
        {
            float averageRadius = (arc.RadiusX + arc.RadiusY) / 2.0f;
            float pitchMm = 3.1f;
            float step = 180.0f * pitchMm /
                ((float)Math.PI * averageRadius);
            float diameter = Math.Max(0.52f, arc.WidthMm * 1.45f) *
                Scale * PreviewStrokeGain;
            using (Brush brush = new SolidBrush(
                Color.FromArgb(arc.Alpha, 220, 218, 211)))
            {
                int count = Math.Max(2,
                    (int)Math.Floor(Math.Abs(arc.SweepDegrees) / step));
                for (int index = 0; index <= count; index++)
                {
                    float angle = arc.StartDegrees +
                        arc.SweepDegrees * index / count;
                    PointF point = ToPixel(ArcPoint(arc, angle));
                    graphics.FillEllipse(brush, point.X - diameter / 2.0f,
                        point.Y - diameter / 2.0f, diameter, diameter);
                }
            }
            return;
        }

        int samples = Math.Max(2,
            (int)Math.Ceiling(Math.Abs(arc.SweepDegrees) * 2.0f));
        PointF[] points = new PointF[samples + 1];
        for (int index = 0; index <= samples; index++)
        {
            float angle = arc.StartDegrees + arc.SweepDegrees * index / samples;
            points[index] = ToPixel(ArcPoint(arc, angle));
        }

        using (Pen pen = new Pen(Color.FromArgb(arc.Alpha, 220, 218, 211),
            arc.WidthMm * Scale * PreviewStrokeGain))
        {
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;
            graphics.DrawLines(pen, points);
        }
    }

    private static void DrawLine(Graphics graphics, LineStroke line)
    {
        PointF start = ToPixel(line.Start);
        PointF end = ToPixel(line.End);
        using (Pen pen = new Pen(Color.FromArgb(line.Alpha, 220, 218, 211),
            line.WidthMm * Scale * PreviewStrokeGain))
        {
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            graphics.DrawLine(pen, start, end);
        }
    }

    private static PointF ArcPoint(ArcStroke arc, float angleDegrees)
    {
        double radians = angleDegrees * Math.PI / 180.0;
        return new PointF(
            arc.RadiusX * (float)Math.Cos(radians),
            arc.RadiusY * (float)Math.Sin(radians));
    }

    private static PointF ToPixel(PointF local)
    {
        return new PointF(
            (local.X + PanelWidthMm / 2.0f) * Scale,
            (PanelHeightMm / 2.0f - local.Y) * Scale);
    }

    private static void WriteSvg(string path, bool production)
    {
        StringBuilder svg = new StringBuilder(16384);
        svg.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        svg.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"-274 -210 548 420\" fill=\"none\">");
        svg.AppendLine(production
            ? "  <title>Rymovia Phase Halo rear-panel production intent V0.10</title>"
            : "  <title>Rymovia Phase Halo rear-panel artwork V0.10</title>");
        svg.AppendLine("  <desc>Selected concept A: five concentric circular and elliptical orbital bands, split into asymmetric solid and dotted arcs. The 180 mm square VESA zone, four R12 foot zones and 16 mm edge band remain clear.</desc>");
        svg.AppendLine(production
            ? "  <!-- Finished size 548 x 420 mm; do not scale. Supplier must qualify low-energy laser marking or one-colour screen printing on a finish coupon. -->"
            : "  <!-- Coordinates are finished millimetres. This editable review master and the production file are generated from the same arc definitions. -->");
        svg.AppendLine("  <g id=\"phase-halo-orbital-arcs\" transform=\"scale(1,-1)\" stroke=\"#DCDAD3\" stroke-linecap=\"round\" stroke-linejoin=\"round\">");
        for (int index = 0; index < Arcs.Length; index++)
        {
            ArcStroke arc = Arcs[index];
            PointF start = ArcPoint(arc, arc.StartDegrees);
            PointF end = ArcPoint(arc,
                arc.StartDegrees + arc.SweepDegrees);
            double reviewOpacity = Math.Min(0.62,
                arc.Alpha / 255.0 * 0.67);
            double opacity = production ? reviewOpacity * 0.48 : reviewOpacity;
            string dotted = arc.Dotted
                ? " stroke-dasharray=\"0.01 3.1\""
                : string.Empty;
            int largeArc = Math.Abs(arc.SweepDegrees) > 180.0f ? 1 : 0;
            int sweep = arc.SweepDegrees >= 0.0f ? 1 : 0;
            svg.Append("    <path d=\"M")
                .Append(F(start.X)).Append(' ').Append(F(start.Y))
                .Append(" A").Append(F(arc.RadiusX)).Append(' ')
                .Append(F(arc.RadiusY)).Append(" 0 ")
                .Append(largeArc.ToString(CultureInfo.InvariantCulture))
                .Append(' ').Append(sweep.ToString(CultureInfo.InvariantCulture))
                .Append(' ').Append(F(end.X)).Append(' ').Append(F(end.Y))
                .Append("\" stroke-width=\"").Append(F(arc.WidthMm))
                .Append("\" opacity=\"").Append(F(opacity)).Append('"')
                .Append(dotted).AppendLine("/>");
        }
        svg.AppendLine("    <g id=\"cardinal-registration-marks\" stroke-width=\"0.35\" opacity=\"0.20\">");
        foreach (LineStroke line in Calibration)
        {
            svg.Append("      <path d=\"M").Append(F(line.Start.X))
                .Append(' ').Append(F(line.Start.Y)).Append(" L")
                .Append(F(line.End.X)).Append(' ').Append(F(line.End.Y))
                .AppendLine("\"/>");
        }
        svg.AppendLine("    </g>");
        svg.AppendLine("  </g>");
        svg.AppendLine("  <g id=\"non-printing-keepouts\" display=\"none\" fill=\"none\" stroke=\"#FF0000\" stroke-width=\"0.2\">");
        svg.AppendLine("    <rect x=\"-258\" y=\"-194\" width=\"516\" height=\"388\"/>");
        svg.AppendLine("    <rect x=\"-90\" y=\"-90\" width=\"180\" height=\"180\"/>");
        svg.AppendLine("    <circle cx=\"-245\" cy=\"-185\" r=\"12\"/><circle cx=\"245\" cy=\"-185\" r=\"12\"/>");
        svg.AppendLine("    <circle cx=\"-245\" cy=\"185\" r=\"12\"/><circle cx=\"245\" cy=\"185\" r=\"12\"/>");
        svg.AppendLine("  </g>");
        svg.AppendLine("</svg>");
        File.WriteAllText(path, svg.ToString(), new UTF8Encoding(false));
    }

    private static string F(double value)
    {
        if (Math.Abs(value) < 0.0005) value = 0.0;
        return value.ToString("0.###", CultureInfo.InvariantCulture);
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
