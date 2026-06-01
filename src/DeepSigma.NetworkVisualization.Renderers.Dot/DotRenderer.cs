using System.Globalization;
using System.Text;
using DeepSigma.NetworkVisualization.Rendering;

namespace DeepSigma.NetworkVisualization.Renderers.Dot;

public sealed class DotRenderer : INetworkRenderer<string>
{
    public static RendererMetadata Metadata { get; } = new("dot", "text/plain");
    public string FormatId => Metadata.FormatId;

    public string Render(Network network)
    {
        var sb = new StringBuilder();
        var graphKw = network.Directed ? "digraph" : "graph";
        var edgeOp = network.Directed ? "->" : "--";

        sb.Append(graphKw).Append(' ').AppendLine("\"network\" {");
        sb.Append("  rankdir=").Append(RankDir(network.Layout.Direction)).AppendLine(";");
        sb.Append("  bgcolor=\"").Append(network.Theme.Background.ToHex()).AppendLine("\";");
        sb.Append("  node [style=filled fontname=\"").Append(Escape(network.Theme.DefaultFontFamily)).AppendLine("\"];");
        sb.AppendLine();

        var ungrouped = network.Nodes.Where(n => n.GroupId is null);
        foreach (var n in ungrouped) WriteNode(sb, n, indent: 2);

        var topLevelGroups = network.Groups.Where(g => g.ParentGroupId is null).ToArray();
        var childGroups = network.Groups.Where(g => g.ParentGroupId is not null)
            .GroupBy(g => g.ParentGroupId!).ToDictionary(g => g.Key, g => g.ToArray());
        var groupNodes = network.Nodes.Where(n => n.GroupId is not null)
            .GroupBy(n => n.GroupId!).ToDictionary(g => g.Key, g => g.ToArray());

        foreach (var g in topLevelGroups)
            WriteCluster(sb, g, childGroups, groupNodes, indent: 2);

        sb.AppendLine();
        foreach (var e in network.Edges)
        {
            sb.Append("  ").Append(Quote(e.Source.Value)).Append(' ').Append(edgeOp).Append(' ').Append(Quote(e.Target.Value));
            var attrs = EdgeAttrs(e);
            if (attrs.Length > 0) sb.Append(" [").Append(attrs).Append(']');
            sb.AppendLine(";");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void WriteCluster(StringBuilder sb, Group group,
        IReadOnlyDictionary<string, Group[]> children,
        IReadOnlyDictionary<string, Node[]> nodes,
        int indent)
    {
        var pad = new string(' ', indent);
        sb.Append(pad).Append("subgraph \"cluster_").Append(Escape(group.Id)).AppendLine("\" {");
        if (!string.IsNullOrEmpty(group.Label))
            sb.Append(pad).Append("  label=").Append(Quote(group.Label)).AppendLine(";");
        if (group.Style?.Fill is { } f)
            sb.Append(pad).Append("  bgcolor=\"").Append(f.ToHex()).AppendLine("\";");
        if (group.Style?.Stroke is { } s)
            sb.Append(pad).Append("  color=\"").Append(s.ToHex()).AppendLine("\";");
        sb.Append(pad).AppendLine("  style=\"filled,rounded\";");

        if (nodes.TryGetValue(group.Id, out var ns))
            foreach (var n in ns) WriteNode(sb, n, indent + 2);

        if (children.TryGetValue(group.Id, out var subs))
            foreach (var sg in subs) WriteCluster(sb, sg, children, nodes, indent + 2);

        sb.Append(pad).AppendLine("}");
    }

    private static void WriteNode(StringBuilder sb, Node n, int indent)
    {
        var pad = new string(' ', indent);
        sb.Append(pad).Append(Quote(n.Id.Value));
        var attrs = NodeAttrs(n);
        if (attrs.Length > 0) sb.Append(" [").Append(attrs).Append(']');
        sb.AppendLine(";");
    }

    private static string NodeAttrs(Node n)
    {
        var parts = new List<string> { $"label={Quote(n.ResolvedLabel())}" };
        var s = n.Style;
        if (s is not null)
        {
            parts.Add($"shape={DotShape(s.Shape)}");
            if (s.Fill is { } f) parts.Add($"fillcolor=\"{f.ToHex()}\"");
            if (s.Stroke is { } st) parts.Add($"color=\"{st.ToHex()}\"");
            if (s.LabelColor is { } lc) parts.Add($"fontcolor=\"{lc.ToHex()}\"");
            if (s.FontFamily is { } ff) parts.Add($"fontname=\"{Escape(ff)}\"");
            parts.Add($"fontsize={s.FontSize.ToString(CultureInfo.InvariantCulture)}");
            if (s.Width.HasValue) parts.Add($"width={(s.Width.Value / 72).ToString("0.##", CultureInfo.InvariantCulture)}");
            if (s.Height.HasValue) parts.Add($"height={(s.Height.Value / 72).ToString("0.##", CultureInfo.InvariantCulture)}");
        }
        if (!string.IsNullOrEmpty(n.Tooltip)) parts.Add($"tooltip={Quote(n.Tooltip)}");
        if (!string.IsNullOrEmpty(n.Url)) parts.Add($"URL={Quote(n.Url)}");
        return string.Join(' ', parts);
    }

    private static string EdgeAttrs(Edge e)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(e.Label)) parts.Add($"label={Quote(e.Label)}");
        var s = e.Style;
        if (s is not null)
        {
            if (s.Stroke is { } c) parts.Add($"color=\"{c.ToHex()}\"");
            parts.Add($"penwidth={s.StrokeWidth.ToString(CultureInfo.InvariantCulture)}");
            if (s.LineStyle == LineStyle.Dashed) parts.Add("style=dashed");
            else if (s.LineStyle == LineStyle.Dotted) parts.Add("style=dotted");
            parts.Add($"arrowhead={DotArrow(s.TargetArrow)}");
            if (s.SourceArrow != ArrowStyle.None) parts.Add($"arrowtail={DotArrow(s.SourceArrow)} dir=both");
        }
        if (e.Weight.HasValue) parts.Add($"weight={e.Weight.Value.ToString(CultureInfo.InvariantCulture)}");
        return string.Join(' ', parts);
    }

    private static string DotShape(NodeShape s) => s switch
    {
        NodeShape.Circle => "circle",
        NodeShape.Ellipse => "ellipse",
        NodeShape.Rectangle => "box",
        NodeShape.RoundedRectangle => "box, style=\"rounded,filled\"",
        NodeShape.Diamond => "diamond",
        NodeShape.Hexagon => "hexagon",
        NodeShape.Triangle => "triangle",
        NodeShape.Parallelogram => "parallelogram",
        NodeShape.Cylinder => "cylinder",
        _ => "box"
    };

    private static string DotArrow(ArrowStyle a) => a switch
    {
        ArrowStyle.None => "none",
        ArrowStyle.Triangle => "normal",
        ArrowStyle.Open => "vee",
        ArrowStyle.Diamond => "diamond",
        ArrowStyle.Circle => "dot",
        ArrowStyle.Vee => "vee",
        _ => "normal"
    };

    private static string RankDir(LayoutDirection d) => d switch
    {
        LayoutDirection.TopToBottom => "TB",
        LayoutDirection.BottomToTop => "BT",
        LayoutDirection.LeftToRight => "LR",
        LayoutDirection.RightToLeft => "RL",
        _ => "TB"
    };

    private static string Quote(string s) => "\"" + Escape(s) + "\"";
    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
