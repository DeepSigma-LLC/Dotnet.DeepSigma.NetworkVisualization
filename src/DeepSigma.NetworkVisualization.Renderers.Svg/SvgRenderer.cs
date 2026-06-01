using System.Globalization;
using System.Net;
using System.Text;
using DeepSigma.NetworkVisualization.Layouts;
using DeepSigma.NetworkVisualization.Rendering;

namespace DeepSigma.NetworkVisualization.Renderers.Svg;

public sealed record SvgRenderOptions
{
    public double Padding { get; init; } = 32;
    public double DefaultNodeWidth { get; init; } = 100;
    public double DefaultNodeHeight { get; init; } = 50;
    public bool AutoLayoutIfMissing { get; init; } = true;
}

public sealed class SvgRenderer(SvgRenderOptions? options = null) : INetworkRenderer<string>
{
    private readonly SvgRenderOptions _opt = options ?? new SvgRenderOptions();

    public static RendererMetadata Metadata { get; } = new("svg", "image/svg+xml", RequiresLayout: true);
    public string FormatId => Metadata.FormatId;

    public string Render(Network network)
    {
        var positioned = network.EnsureLayout(_opt.AutoLayoutIfMissing);
        var theme = positioned.Theme;
        var sb = new StringBuilder();

        var positions = positioned.Nodes
            .Where(n => n.Position.HasValue)
            .Select(n =>
            {
                var (w, h) = n.ResolvedSize(_opt.DefaultNodeWidth, _opt.DefaultNodeHeight);
                return (n, p: n.Position!.Value, w, h);
            })
            .ToArray();

        var minX = positions.Min(t => t.p.X - t.w / 2);
        var minY = positions.Min(t => t.p.Y - t.h / 2);
        var maxX = positions.Max(t => t.p.X + t.w / 2);
        var maxY = positions.Max(t => t.p.Y + t.h / 2);

        var pad = _opt.Padding;
        var width = (maxX - minX) + 2 * pad;
        var height = (maxY - minY) + 2 * pad;
        var tx = pad - minX;
        var ty = pad - minY;

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ")
          .Append(F(width)).Append(' ').Append(F(height)).Append("\" width=\"").Append(F(width)).Append("\" height=\"").Append(F(height)).AppendLine("\">");
        sb.Append("  <rect x=\"0\" y=\"0\" width=\"").Append(F(width)).Append("\" height=\"").Append(F(height))
          .Append("\" fill=\"").Append(theme.Background.ToHex()).AppendLine("\"/>");

        sb.AppendLine("  <defs>");
        sb.AppendLine("    <marker id=\"arrow\" viewBox=\"0 0 10 10\" refX=\"9\" refY=\"5\" markerWidth=\"6\" markerHeight=\"6\" orient=\"auto-start-reverse\">");
        sb.Append("      <path d=\"M0,0 L10,5 L0,10 z\" fill=\"").Append(theme.DefaultEdgeStroke.ToHex()).AppendLine("\"/>");
        sb.AppendLine("    </marker>");
        sb.AppendLine("  </defs>");

        sb.Append("  <g transform=\"translate(").Append(F(tx)).Append(' ').Append(F(ty)).AppendLine(")\">");

        var byId = positions.ToDictionary(t => t.n.Id.Value, t => t);

        sb.AppendLine("    <g class=\"edges\">");
        foreach (var e in positioned.Edges)
        {
            if (!byId.TryGetValue(e.Source.Value, out var src) || !byId.TryGetValue(e.Target.Value, out var dst)) continue;
            RenderEdge(sb, e, src, dst, theme, positioned.Directed);
        }
        sb.AppendLine("    </g>");

        // Compute which node labels to show. When two nodes' label boxes overlap, hide the lower-degree one
        // (the one with fewer edges) — keeping the more important node legible.
        var visibleLabels = ResolveLabelCollisions(positions, positioned, theme);

        sb.AppendLine("    <g class=\"nodes\">");
        foreach (var (n, p, w, h) in positions)
            RenderNode(sb, n, p, w, h, theme, showLabel: visibleLabels.Contains(n.Id.Value));
        sb.AppendLine("    </g>");

        sb.AppendLine("  </g>");
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static void RenderEdge(StringBuilder sb, Edge e, (Node n, Position p, double w, double h) src, (Node n, Position p, double w, double h) dst, Theme theme, bool directed)
    {
        var stroke = e.ResolvedStroke(theme).ToHex();
        var strokeWidth = e.ResolvedStrokeWidth().ToString(CultureInfo.InvariantCulture);
        var dashArray = e.ResolvedLineStyle() switch
        {
            LineStyle.Dashed => " stroke-dasharray=\"6 4\"",
            LineStyle.Dotted => " stroke-dasharray=\"2 3\"",
            _ => string.Empty
        };
        var arrow = e.HasArrowHead(directed) ? " marker-end=\"url(#arrow)\"" : string.Empty;

        sb.Append("      <line x1=\"").Append(F(src.p.X)).Append("\" y1=\"").Append(F(src.p.Y))
          .Append("\" x2=\"").Append(F(dst.p.X)).Append("\" y2=\"").Append(F(dst.p.Y))
          .Append("\" stroke=\"").Append(stroke).Append("\" stroke-width=\"").Append(strokeWidth).Append('"').Append(dashArray).Append(arrow).AppendLine("/>");

        if (!string.IsNullOrEmpty(e.Label))
        {
            var mx = (src.p.X + dst.p.X) / 2;
            var my = (src.p.Y + dst.p.Y) / 2;
            var color = e.ResolvedLabelColor(theme).ToHex();
            // Small white background rect makes the edge label legible when it crosses other lines.
            var fontSize = e.ResolvedFontSize();
            var bgWidth = e.Label.Length * fontSize * 0.55;
            var bgHeight = fontSize + 4;
            sb.Append("      <rect x=\"").Append(F(mx - bgWidth / 2)).Append("\" y=\"").Append(F(my - bgHeight / 2))
              .Append("\" width=\"").Append(F(bgWidth)).Append("\" height=\"").Append(F(bgHeight))
              .Append("\" fill=\"").Append(theme.Background.ToHex()).AppendLine("\" fill-opacity=\"0.85\" rx=\"3\"/>");
            sb.Append("      <text x=\"").Append(F(mx)).Append("\" y=\"").Append(F(my))
              .Append("\" fill=\"").Append(color).Append("\" font-family=\"").Append(XmlEscape(theme.DefaultFontFamily))
              .Append("\" font-size=\"").Append(F(e.Style?.FontSize ?? 10)).Append("\" text-anchor=\"middle\" dominant-baseline=\"central\">")
              .Append(XmlEscape(e.Label)).AppendLine("</text>");
        }
    }

    private static void RenderNode(StringBuilder sb, Node n, Position p, double w, double h, Theme theme, bool showLabel = true)
    {
        var fill = n.ResolvedFill(theme).ToHex();
        var stroke = n.ResolvedStroke(theme).ToHex();
        var strokeWidth = n.ResolvedStrokeWidth().ToString(CultureInfo.InvariantCulture);
        var labelColor = n.ResolvedLabelColor(theme).ToHex();
        var fontFamily = XmlEscape(n.ResolvedFontFamily(theme));
        var fontSize = n.ResolvedFontSize(theme).ToString(CultureInfo.InvariantCulture);
        var shape = n.ResolvedShape();

        switch (shape)
        {
            case NodeShape.Circle:
            {
                var r = ShapeGeometry.CircleRadius(w, h);
                sb.Append("      <circle cx=\"").Append(F(p.X)).Append("\" cy=\"").Append(F(p.Y))
                  .Append("\" r=\"").Append(F(r)).Append("\" fill=\"").Append(fill).Append("\" stroke=\"").Append(stroke)
                  .Append("\" stroke-width=\"").Append(strokeWidth).AppendLine("\"/>");
                break;
            }
            case NodeShape.Ellipse:
                sb.Append("      <ellipse cx=\"").Append(F(p.X)).Append("\" cy=\"").Append(F(p.Y))
                  .Append("\" rx=\"").Append(F(w / 2)).Append("\" ry=\"").Append(F(h / 2))
                  .Append("\" fill=\"").Append(fill).Append("\" stroke=\"").Append(stroke)
                  .Append("\" stroke-width=\"").Append(strokeWidth).AppendLine("\"/>");
                break;
            case NodeShape.RoundedRectangle:
                sb.Append("      <rect x=\"").Append(F(p.X - w / 2)).Append("\" y=\"").Append(F(p.Y - h / 2))
                  .Append("\" width=\"").Append(F(w)).Append("\" height=\"").Append(F(h))
                  .Append("\" rx=\"8\" ry=\"8\" fill=\"").Append(fill).Append("\" stroke=\"").Append(stroke)
                  .Append("\" stroke-width=\"").Append(strokeWidth).AppendLine("\"/>");
                break;
            case NodeShape.Diamond:
            case NodeShape.Hexagon:
            case NodeShape.Triangle:
            {
                var pts = ShapeGeometry.PolygonFor(shape, p.X, p.Y, w, h)!;
                WritePolygon(sb, pts, fill, stroke, strokeWidth);
                break;
            }
            default:
                sb.Append("      <rect x=\"").Append(F(p.X - w / 2)).Append("\" y=\"").Append(F(p.Y - h / 2))
                  .Append("\" width=\"").Append(F(w)).Append("\" height=\"").Append(F(h))
                  .Append("\" fill=\"").Append(fill).Append("\" stroke=\"").Append(stroke)
                  .Append("\" stroke-width=\"").Append(strokeWidth).AppendLine("\"/>");
                break;
        }

        if (showLabel)
        {
            var label = n.ResolvedLabel();
            sb.Append("      <text x=\"").Append(F(p.X)).Append("\" y=\"").Append(F(p.Y))
              .Append("\" fill=\"").Append(labelColor).Append("\" font-family=\"").Append(fontFamily)
              .Append("\" font-size=\"").Append(fontSize).Append("\" text-anchor=\"middle\" dominant-baseline=\"central\">")
              .Append(XmlEscape(label)).AppendLine("</text>");
        }
    }

    /// <summary>
    /// Greedy label-collision resolver. Approximates each label's bounding rect from its text length
    /// and font size, then for every overlapping pair hides the lower-degree node's label.
    /// Returns the set of node ids that should still show their label.
    /// </summary>
    private static HashSet<string> ResolveLabelCollisions(
        (Node n, Position p, double w, double h)[] positions,
        Network network,
        Theme theme)
    {
        var degree = new Dictionary<string, int>(positions.Length);
        foreach (var (n, _, _, _) in positions) degree[n.Id.Value] = 0;
        foreach (var e in network.Edges)
        {
            if (degree.TryGetValue(e.Source.Value, out var ds)) degree[e.Source.Value] = ds + 1;
            if (degree.TryGetValue(e.Target.Value, out var dt)) degree[e.Target.Value] = dt + 1;
        }

        // Approximate label rect for each node.
        var rects = positions.Select(t =>
        {
            var label = t.n.ResolvedLabel();
            var fontSize = t.n.ResolvedFontSize(theme);
            var lw = Math.Max(20, label.Length * fontSize * 0.55);
            var lh = fontSize + 4;
            return (id: t.n.Id.Value,
                    minX: t.p.X - lw / 2, minY: t.p.Y - lh / 2,
                    maxX: t.p.X + lw / 2, maxY: t.p.Y + lh / 2,
                    deg: degree[t.n.Id.Value]);
        }).ToArray();

        var hidden = new HashSet<string>();
        for (int i = 0; i < rects.Length; i++)
        {
            if (hidden.Contains(rects[i].id)) continue;
            for (int j = i + 1; j < rects.Length; j++)
            {
                if (hidden.Contains(rects[j].id)) continue;
                var a = rects[i]; var b = rects[j];
                var overlaps = a.minX < b.maxX && a.maxX > b.minX && a.minY < b.maxY && a.maxY > b.minY;
                if (!overlaps) continue;
                // Hide the lower-degree label; ties broken by later-declared.
                if (a.deg >= b.deg) hidden.Add(b.id);
                else hidden.Add(a.id);
            }
        }

        return positions.Select(t => t.n.Id.Value).Where(id => !hidden.Contains(id)).ToHashSet(StringComparer.Ordinal);
    }

    private static void WritePolygon(StringBuilder sb, Position[] points, string fill, string stroke, string strokeWidth)
    {
        sb.Append("      <polygon points=\"");
        for (int i = 0; i < points.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(F(points[i].X)).Append(',').Append(F(points[i].Y));
        }
        sb.Append("\" fill=\"").Append(fill).Append("\" stroke=\"").Append(stroke)
          .Append("\" stroke-width=\"").Append(strokeWidth).AppendLine("\"/>");
    }

    private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    private static string XmlEscape(string s) => WebUtility.HtmlEncode(s);
}
