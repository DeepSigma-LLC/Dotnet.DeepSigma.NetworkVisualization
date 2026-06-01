using DeepSigma.NetworkVisualization.Layouts;
using DeepSigma.NetworkVisualization.Rendering;
using SkiaSharp;

namespace DeepSigma.NetworkVisualization.Renderers.SkiaSharp;

public sealed record SkiaRenderOptions
{
    public double Padding { get; init; } = 32;
    public double DefaultNodeWidth { get; init; } = 100;
    public double DefaultNodeHeight { get; init; } = 50;
    public float PixelDensity { get; init; } = 2.0f;
    public SKEncodedImageFormat Format { get; init; } = SKEncodedImageFormat.Png;
    public int Quality { get; init; } = 95;
    public bool AutoLayoutIfMissing { get; init; } = true;
}

public sealed class SkiaRenderer(SkiaRenderOptions? options = null) : INetworkRenderer<byte[]>
{
    private readonly SkiaRenderOptions _opt = options ?? new SkiaRenderOptions();

    public string FormatId => "skia.raster";

    public byte[] Render(Network network)
    {
        var positioned = EnsureLayout(network);
        var theme = positioned.Theme;

        var laidOut = positioned.Nodes
            .Where(n => n.Position.HasValue)
            .Select(n =>
            {
                var (w, h) = n.ResolvedSize(_opt.DefaultNodeWidth, _opt.DefaultNodeHeight);
                return (n, p: n.Position!.Value, w, h);
            })
            .ToArray();

        if (laidOut.Length == 0) return EmptyImage();

        var minX = laidOut.Min(t => t.p.X - t.w / 2);
        var minY = laidOut.Min(t => t.p.Y - t.h / 2);
        var maxX = laidOut.Max(t => t.p.X + t.w / 2);
        var maxY = laidOut.Max(t => t.p.Y + t.h / 2);
        var width = (float)((maxX - minX) + 2 * _opt.Padding);
        var height = (float)((maxY - minY) + 2 * _opt.Padding);
        var pixelW = (int)Math.Ceiling(width * _opt.PixelDensity);
        var pixelH = (int)Math.Ceiling(height * _opt.PixelDensity);

        using var bitmap = new SKBitmap(pixelW, pixelH);
        using var canvas = new SKCanvas(bitmap);
        canvas.Scale(_opt.PixelDensity);
        canvas.Clear(ToSk(theme.Background));
        canvas.Translate((float)(_opt.Padding - minX), (float)(_opt.Padding - minY));

        var byId = laidOut.ToDictionary(t => t.n.Id.Value, t => t);

        using var edgePaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke };
        foreach (var e in positioned.Edges)
        {
            if (!byId.TryGetValue(e.Source.Value, out var src) || !byId.TryGetValue(e.Target.Value, out var dst)) continue;
            edgePaint.Color = ToSk(e.ResolvedStroke(theme));
            edgePaint.StrokeWidth = (float)e.ResolvedStrokeWidth();
            edgePaint.PathEffect = e.ResolvedLineStyle() switch
            {
                LineStyle.Dashed => SKPathEffect.CreateDash([6f, 4f], 0),
                LineStyle.Dotted => SKPathEffect.CreateDash([2f, 3f], 0),
                _ => null
            };
            canvas.DrawLine((float)src.p.X, (float)src.p.Y, (float)dst.p.X, (float)dst.p.Y, edgePaint);

            if (e.HasArrowHead(positioned.Directed))
                DrawArrowHead(canvas, src.p, dst.p, edgePaint.Color);

            if (!string.IsNullOrEmpty(e.Label))
            {
                var color = e.ResolvedLabelColor(theme);
                DrawText(canvas, e.Label, (float)((src.p.X + dst.p.X) / 2), (float)((src.p.Y + dst.p.Y) / 2),
                    (float)e.ResolvedFontSize(), theme.DefaultFontFamily, color);
            }
        }

        foreach (var (n, p, w, h) in laidOut)
            DrawNode(canvas, n, p, w, h, theme);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(_opt.Format, _opt.Quality);
        return data.ToArray();
    }

    private Network EnsureLayout(Network network) => network.EnsureLayout(_opt.AutoLayoutIfMissing);

    private static void DrawNode(SKCanvas canvas, Node n, Position p, double w, double h, Theme theme)
    {
        var fill = n.ResolvedFill(theme);
        var stroke = n.ResolvedStroke(theme);
        var strokeWidth = (float)n.ResolvedStrokeWidth();
        var shape = n.ResolvedShape();

        using var fillPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = ToSk(fill) };
        using var strokePaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, Color = ToSk(stroke), StrokeWidth = strokeWidth };

        var cx = (float)p.X; var cy = (float)p.Y;
        var fw = (float)w; var fh = (float)h;

        switch (shape)
        {
            case NodeShape.Circle:
            {
                var r = Math.Min(fw, fh) / 2;
                canvas.DrawCircle(cx, cy, r, fillPaint);
                canvas.DrawCircle(cx, cy, r, strokePaint);
                break;
            }
            case NodeShape.Ellipse:
            {
                var rect = new SKRect(cx - fw / 2, cy - fh / 2, cx + fw / 2, cy + fh / 2);
                canvas.DrawOval(rect, fillPaint);
                canvas.DrawOval(rect, strokePaint);
                break;
            }
            case NodeShape.RoundedRectangle:
            {
                var rect = new SKRect(cx - fw / 2, cy - fh / 2, cx + fw / 2, cy + fh / 2);
                canvas.DrawRoundRect(rect, 8, 8, fillPaint);
                canvas.DrawRoundRect(rect, 8, 8, strokePaint);
                break;
            }
            case NodeShape.Diamond:
            {
                using var path = new SKPath();
                path.MoveTo(cx, cy - fh / 2);
                path.LineTo(cx + fw / 2, cy);
                path.LineTo(cx, cy + fh / 2);
                path.LineTo(cx - fw / 2, cy);
                path.Close();
                canvas.DrawPath(path, fillPaint);
                canvas.DrawPath(path, strokePaint);
                break;
            }
            case NodeShape.Hexagon:
            {
                using var path = new SKPath();
                var qw = fw / 4;
                path.MoveTo(cx - fw / 2 + qw, cy - fh / 2);
                path.LineTo(cx + fw / 2 - qw, cy - fh / 2);
                path.LineTo(cx + fw / 2, cy);
                path.LineTo(cx + fw / 2 - qw, cy + fh / 2);
                path.LineTo(cx - fw / 2 + qw, cy + fh / 2);
                path.LineTo(cx - fw / 2, cy);
                path.Close();
                canvas.DrawPath(path, fillPaint);
                canvas.DrawPath(path, strokePaint);
                break;
            }
            case NodeShape.Triangle:
            {
                using var path = new SKPath();
                path.MoveTo(cx, cy - fh / 2);
                path.LineTo(cx + fw / 2, cy + fh / 2);
                path.LineTo(cx - fw / 2, cy + fh / 2);
                path.Close();
                canvas.DrawPath(path, fillPaint);
                canvas.DrawPath(path, strokePaint);
                break;
            }
            default:
            {
                var rect = new SKRect(cx - fw / 2, cy - fh / 2, cx + fw / 2, cy + fh / 2);
                canvas.DrawRect(rect, fillPaint);
                canvas.DrawRect(rect, strokePaint);
                break;
            }
        }

        DrawText(canvas, n.ResolvedLabel(), cx, cy, (float)n.ResolvedFontSize(theme),
            n.ResolvedFontFamily(theme), n.ResolvedLabelColor(theme));
    }

    private static void DrawArrowHead(SKCanvas canvas, Position from, Position to, SKColor color)
    {
        const double headLen = 10;
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-6) return;
        var ux = dx / len; var uy = dy / len;
        var px = -uy; var py = ux;
        var bx = to.X - ux * headLen;
        var by = to.Y - uy * headLen;
        var lx = bx + px * (headLen * 0.4); var ly = by + py * (headLen * 0.4);
        var rx = bx - px * (headLen * 0.4); var ry = by - py * (headLen * 0.4);

        using var path = new SKPath();
        path.MoveTo((float)to.X, (float)to.Y);
        path.LineTo((float)lx, (float)ly);
        path.LineTo((float)rx, (float)ry);
        path.Close();
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = color };
        canvas.DrawPath(path, paint);
    }

    private static void DrawText(SKCanvas canvas, string text, float x, float y, float size, string fontFamily, Color color)
    {
        using var typeface = SKTypeface.FromFamilyName(fontFamily.Split(',')[0].Trim());
        using var font = new SKFont(typeface ?? SKTypeface.Default, size);
        using var paint = new SKPaint { IsAntialias = true, Color = ToSk(color) };
        var metrics = font.Metrics;
        var bounds = new SKRect();
        font.MeasureText(text, out bounds, paint);
        var baselineY = y - (metrics.Ascent + metrics.Descent) / 2;
        canvas.DrawText(text, x - bounds.MidX, baselineY, SKTextAlign.Left, font, paint);
    }

    private static SKColor ToSk(Color c) => new(c.R, c.G, c.B, c.A);

    private byte[] EmptyImage()
    {
        using var bitmap = new SKBitmap(8, 8);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(_opt.Format, _opt.Quality);
        return data.ToArray();
    }
}
