using System.Globalization;
using System.Text;
using DeepSigma.NetworkVisualization.Rendering;

namespace DeepSigma.NetworkVisualization.Renderers.Mermaid;

public sealed class MermaidRenderer : INetworkRenderer<string>
{
    public static RendererMetadata Metadata { get; } = new("mermaid", "text/plain");
    public string FormatId => Metadata.FormatId;

    public string Render(Network network)
    {
        var sb = new StringBuilder();
        var direction = DirectionCode(network.Layout.Direction);
        sb.Append("flowchart ").AppendLine(direction);

        var topLevelGroups = network.Groups.Where(g => g.ParentGroupId is null).ToArray();
        var nestedGroups = network.Groups.Where(g => g.ParentGroupId is not null)
            .GroupBy(g => g.ParentGroupId!)
            .ToDictionary(g => g.Key, g => g.ToArray());
        var nodesByGroup = network.Nodes.Where(n => n.GroupId is not null)
            .GroupBy(n => n.GroupId!)
            .ToDictionary(g => g.Key, g => g.ToArray());
        var ungroupedNodes = network.Nodes.Where(n => n.GroupId is null).ToArray();

        foreach (var n in ungroupedNodes)
            sb.Append("    ").AppendLine(NodeDecl(n));

        foreach (var g in topLevelGroups)
            EmitGroup(sb, g, nestedGroups, nodesByGroup, indent: 1);

        for (int i = 0; i < network.Edges.Count; i++)
        {
            var e = network.Edges[i];
            sb.Append("    ").AppendLine(EdgeDecl(e, network.Directed));
        }

        for (int i = 0; i < network.Edges.Count; i++)
        {
            var s = network.Edges[i].Style;
            if (s?.Stroke is null && s?.StrokeWidth is null) continue;
            var parts = new List<string>();
            if (s.Stroke is { } c) parts.Add($"stroke:{c.ToHex()}");
            parts.Add($"stroke-width:{s.StrokeWidth.ToString(CultureInfo.InvariantCulture)}px");
            if (s.LineStyle == LineStyle.Dashed) parts.Add("stroke-dasharray:6 4");
            else if (s.LineStyle == LineStyle.Dotted) parts.Add("stroke-dasharray:2 3");
            sb.Append("    linkStyle ").Append(i).Append(' ').AppendLine(string.Join(',', parts));
        }

        foreach (var n in network.Nodes)
        {
            if (n.Style is null) continue;
            var parts = new List<string>();
            if (n.Style.Fill is { } f) parts.Add($"fill:{f.ToHex()}");
            if (n.Style.Stroke is { } s) parts.Add($"stroke:{s.ToHex()}");
            parts.Add($"stroke-width:{n.Style.StrokeWidth.ToString(CultureInfo.InvariantCulture)}px");
            if (n.Style.LabelColor is { } lc) parts.Add($"color:{lc.ToHex()}");
            sb.Append("    style ").Append(EscapeId(n.Id.Value)).Append(' ').AppendLine(string.Join(',', parts));
        }

        return sb.ToString();
    }

    private static void EmitGroup(
        StringBuilder sb,
        Group group,
        IReadOnlyDictionary<string, Group[]> nestedGroups,
        IReadOnlyDictionary<string, Node[]> nodesByGroup,
        int indent)
    {
        var pad = new string(' ', indent * 4);
        var title = string.IsNullOrEmpty(group.Label) ? group.Id : group.Label;
        sb.Append(pad).Append("subgraph ").Append(EscapeId(group.Id)).Append("[\"").Append(EscapeLabel(title)).AppendLine("\"]");

        if (nodesByGroup.TryGetValue(group.Id, out var ns))
            foreach (var n in ns)
                sb.Append(pad).Append("    ").AppendLine(NodeDecl(n));

        if (nestedGroups.TryGetValue(group.Id, out var children))
            foreach (var c in children)
                EmitGroup(sb, c, nestedGroups, nodesByGroup, indent + 1);

        sb.Append(pad).AppendLine("end");
    }

    private static string NodeDecl(Node n)
    {
        var id = EscapeId(n.Id.Value);
        var label = EscapeLabel(n.ResolvedLabel());
        var shape = n.ResolvedShape();
        return shape switch
        {
            NodeShape.Rectangle => $"{id}[\"{label}\"]",
            NodeShape.RoundedRectangle => $"{id}(\"{label}\")",
            NodeShape.Circle or NodeShape.Ellipse => $"{id}((\"{label}\"))",
            NodeShape.Diamond => $"{id}{{\"{label}\"}}",
            NodeShape.Hexagon => $"{id}{{{{\"{label}\"}}}}",
            NodeShape.Parallelogram => $"{id}[/\"{label}\"/]",
            NodeShape.Cylinder => $"{id}[(\"{label}\")]",
            NodeShape.Triangle => $"{id}>\"{label}\"]",
            _ => $"{id}[\"{label}\"]"
        };
    }

    private static string EdgeDecl(Edge e, bool directed)
    {
        var source = EscapeId(e.Source.Value);
        var target = EscapeId(e.Target.Value);
        var style = e.ResolvedLineStyle();
        var hasArrow = e.HasArrowHead(directed);
        var connector = (style, hasArrow) switch
        {
            (LineStyle.Dashed, true) => "-.->",
            (LineStyle.Dashed, false) => "-.-",
            (LineStyle.Dotted, true) => "-.->",
            (LineStyle.Dotted, false) => "-.-",
            (_, true) => "-->",
            (_, false) => "---",
        };
        return string.IsNullOrEmpty(e.Label)
            ? $"{source} {connector} {target}"
            : $"{source} {connector}|\"{EscapeLabel(e.Label)}\"| {target}";
    }

    private static string DirectionCode(LayoutDirection d) => d switch
    {
        LayoutDirection.TopToBottom => "TD",
        LayoutDirection.BottomToTop => "BT",
        LayoutDirection.LeftToRight => "LR",
        LayoutDirection.RightToLeft => "RL",
        _ => "TD"
    };

    private static string EscapeId(string id)
    {
        var sb = new StringBuilder(id.Length);
        foreach (var ch in id)
            sb.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
        return sb.ToString();
    }

    private static string EscapeLabel(string label)
        => label.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
