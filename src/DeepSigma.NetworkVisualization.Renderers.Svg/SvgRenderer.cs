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

    public string FormatId => "svg";

    public string Render(Network network)
    {
        var positioned = EnsureLayout(network);
        var theme = positioned.Theme;
        var sb = new StringBuilder();

        var positions = positioned.Nodes
            .Where(n => n.Position.HasValue)
            .Select(n => (n, p: n.Position!.Value, w: n.Style?.Width ?? _opt.DefaultNodeWidth, h: n.Style?.Height ?? _opt.DefaultNodeHeight))
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

        sb.AppendLine("    <g class=\"nodes\">");
        foreach (var (n, p, w, h) in positions)
            RenderNode(sb, n, p, w, h, theme);
        sb.AppendLine("    </g>");

        sb.AppendLine("  </g>");
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private Network EnsureLayout(Network network)
    {
        if (network.Nodes.All(n => n.Position.HasValue)) return network;
        if (!_opt.AutoLayoutIfMissing)
            throw new InvalidOperationException("SvgRenderer requires node positions. Apply a layout provider first or set SvgRenderOptions.AutoLayoutIfMissing = true.");
        return LayoutProviders.For(network).ApplyLayout(network);
    }

    private static void RenderEdge(StringBuilder sb, Edge e, (Node n, Position p, double w, double h) src, (Node n, Position p, double w, double h) dst, Theme theme, bool directed)
    {
        var stroke = e.Style?.Stroke?.ToHex() ?? theme.DefaultEdgeStroke.ToHex();
        var strokeWidth = (e.Style?.StrokeWidth ?? 1.0).ToString(CultureInfo.InvariantCulture);
        var dashArray = e.Style?.LineStyle switch
        {
            LineStyle.Dashed => " stroke-dasharray=\"6 4\"",
            LineStyle.Dotted => " stroke-dasharray=\"2 3\"",
            _ => string.Empty
        };
        var arrow = directed && (e.Style?.TargetArrow ?? ArrowStyle.Triangle) != ArrowStyle.None
            ? " marker-end=\"url(#arrow)\"" : string.Empty;

        sb.Append("      <line x1=\"").Append(F(src.p.X)).Append("\" y1=\"").Append(F(src.p.Y))
          .Append("\" x2=\"").Append(F(dst.p.X)).Append("\" y2=\"").Append(F(dst.p.Y))
          .Append("\" stroke=\"").Append(stroke).Append("\" stroke-width=\"").Append(strokeWidth).Append('"').Append(dashArray).Append(arrow).AppendLine("/>");

        if (!string.IsNullOrEmpty(e.Label))
        {
            var mx = (src.p.X + dst.p.X) / 2;
            var my = (src.p.Y + dst.p.Y) / 2;
            var color = (e.Style?.LabelColor ?? theme.DefaultLabelColor).ToHex();
            sb.Append("      <text x=\"").Append(F(mx)).Append("\" y=\"").Append(F(my))
              .Append("\" fill=\"").Append(color).Append("\" font-family=\"").Append(XmlEscape(theme.DefaultFontFamily))
              .Append("\" font-size=\"").Append(F(e.Style?.FontSize ?? 10)).Append("\" text-anchor=\"middle\" dominant-baseline=\"central\">")
              .Append(XmlEscape(e.Label)).AppendLine("</text>");
        }
    }

    private static void RenderNode(StringBuilder sb, Node n, Position p, double w, double h, Theme theme)
    {
        var fill = (n.Style?.Fill ?? theme.DefaultNodeFill).ToHex();
        var stroke = (n.Style?.Stroke ?? theme.DefaultNodeStroke).ToHex();
        var strokeWidth = (n.Style?.StrokeWidth ?? 1.0).ToString(CultureInfo.InvariantCulture);
        var labelColor = (n.Style?.LabelColor ?? theme.DefaultLabelColor).ToHex();
        var fontFamily = XmlEscape(n.Style?.FontFamily ?? theme.DefaultFontFamily);
        var fontSize = (n.Style?.FontSize ?? theme.DefaultFontSize).ToString(CultureInfo.InvariantCulture);
        var shape = n.Style?.Shape ?? NodeShape.Ellipse;

        switch (shape)
        {
            case NodeShape.Circle:
            {
                var r = Math.Min(w, h) / 2;
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
            {
                var pts = $"{F(p.X)},{F(p.Y - h / 2)} {F(p.X + w / 2)},{F(p.Y)} {F(p.X)},{F(p.Y + h / 2)} {F(p.X - w / 2)},{F(p.Y)}";
                sb.Append("      <polygon points=\"").Append(pts).Append("\" fill=\"").Append(fill).Append("\" stroke=\"").Append(stroke)
                  .Append("\" stroke-width=\"").Append(strokeWidth).AppendLine("\"/>");
                break;
            }
            case NodeShape.Hexagon:
            {
                var hw = w / 2; var hh = h / 2; var qw = w / 4;
                var pts = $"{F(p.X - hw + qw)},{F(p.Y - hh)} {F(p.X + hw - qw)},{F(p.Y - hh)} {F(p.X + hw)},{F(p.Y)} {F(p.X + hw - qw)},{F(p.Y + hh)} {F(p.X - hw + qw)},{F(p.Y + hh)} {F(p.X - hw)},{F(p.Y)}";
                sb.Append("      <polygon points=\"").Append(pts).Append("\" fill=\"").Append(fill).Append("\" stroke=\"").Append(stroke)
                  .Append("\" stroke-width=\"").Append(strokeWidth).AppendLine("\"/>");
                break;
            }
            case NodeShape.Triangle:
            {
                var pts = $"{F(p.X)},{F(p.Y - h / 2)} {F(p.X + w / 2)},{F(p.Y + h / 2)} {F(p.X - w / 2)},{F(p.Y + h / 2)}";
                sb.Append("      <polygon points=\"").Append(pts).Append("\" fill=\"").Append(fill).Append("\" stroke=\"").Append(stroke)
                  .Append("\" stroke-width=\"").Append(strokeWidth).AppendLine("\"/>");
                break;
            }
            default:
                sb.Append("      <rect x=\"").Append(F(p.X - w / 2)).Append("\" y=\"").Append(F(p.Y - h / 2))
                  .Append("\" width=\"").Append(F(w)).Append("\" height=\"").Append(F(h))
                  .Append("\" fill=\"").Append(fill).Append("\" stroke=\"").Append(stroke)
                  .Append("\" stroke-width=\"").Append(strokeWidth).AppendLine("\"/>");
                break;
        }

        var label = n.Label ?? n.Id.Value;
        sb.Append("      <text x=\"").Append(F(p.X)).Append("\" y=\"").Append(F(p.Y))
          .Append("\" fill=\"").Append(labelColor).Append("\" font-family=\"").Append(fontFamily)
          .Append("\" font-size=\"").Append(fontSize).Append("\" text-anchor=\"middle\" dominant-baseline=\"central\">")
          .Append(XmlEscape(label)).AppendLine("</text>");
    }

    private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    private static string XmlEscape(string s) => WebUtility.HtmlEncode(s);
}
