using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

internal static class GenerateRymoviaPatternV09
{
    private static readonly double[][] Segments =
    {
        new[] { 165.0, 90.0, 285.0, 90.0 }, new[] { 330.0, 90.0, 515.0, 90.0 },
        new[] { 145.0, 105.0, 250.0, 105.0 }, new[] { 285.0, 105.0, 465.0, 105.0 },
        new[] { 175.0, 120.0, 360.0, 120.0 }, new[] { 400.0, 120.0, 535.0, 120.0 },
        new[] { 55.0, 205.0, 230.0, 205.0 }, new[] { 260.0, 205.0, 430.0, 205.0 },
        new[] { 455.0, 205.0, 535.0, 205.0 }, new[] { 80.0, 220.0, 300.0, 220.0 },
        new[] { 335.0, 220.0, 510.0, 220.0 }, new[] { 50.0, 235.0, 190.0, 235.0 },
        new[] { 225.0, 235.0, 370.0, 235.0 }, new[] { 400.0, 235.0, 530.0, 235.0 },
        new[] { 70.0, 315.0, 210.0, 315.0 }, new[] { 250.0, 315.0, 425.0, 315.0 },
        new[] { 465.0, 315.0, 520.0, 315.0 }, new[] { 90.0, 330.0, 270.0, 330.0 },
        new[] { 305.0, 330.0, 535.0, 330.0 }, new[] { 50.0, 345.0, 180.0, 345.0 },
        new[] { 220.0, 345.0, 350.0, 345.0 }, new[] { 385.0, 345.0, 510.0, 345.0 }
    };

    private static int Main(string[] args)
    {
        if (args == null || args.Length != 1)
            throw new ArgumentException("Usage: GenerateRymoviaPatternV09.exe <output.png>");
        string output = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        const int width = 2316;
        const int height = 1692;
        const float scale = 4.0f;
        using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        using (Graphics graphics = Graphics.FromImage(bitmap))
        using (Pen pen = new Pen(Color.FromArgb(155, 216, 216, 216), 3.2f))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            foreach (double[] segment in Segments)
                graphics.DrawLine(pen,
                    (float)segment[0] * scale, (float)segment[1] * scale,
                    (float)segment[2] * scale, (float)segment[3] * scale);
            bitmap.Save(output, ImageFormat.Png);
        }
        Console.WriteLine("RYMOVIA_TIMEGRID_PATTERN=" + output);
        Console.WriteLine("RYMOVIA_TIMEGRID_SEGMENTS=" + Segments.Length);
        return 0;
    }
}
