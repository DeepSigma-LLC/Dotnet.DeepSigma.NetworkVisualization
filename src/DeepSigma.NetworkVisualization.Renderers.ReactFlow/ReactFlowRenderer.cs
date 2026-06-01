using System.Text.Json;
using DeepSigma.NetworkVisualization.Json;
using DeepSigma.NetworkVisualization.Layouts;
using DeepSigma.NetworkVisualization.Rendering;

namespace DeepSigma.NetworkVisualization.Renderers.ReactFlow;

public sealed class ReactFlowRenderer : IJsonNetworkRenderer
{
    public string FormatId => "reactflow";
    public string FormatVersion => "1.0";
    public bool AutoLayoutIfMissing { get; init; } = true;

    public string Render(Network network)
    {
        var positioned = network.Nodes.All(n => n.Position.HasValue) || !AutoLayoutIfMissing
            ? network
            : LayoutProviders.For(network).ApplyLayout(network);

        var groupNodes = positioned.Groups.Select(g =>
        {
            var children = positioned.Nodes
                .Where(n => n.GroupId == g.Id && n.Position.HasValue)
                .Select(n => (n.Position!.Value, w: n.Style?.Width ?? 100, h: n.Style?.Height ?? 50))
                .ToArray();
            var hasChildren = children.Length > 0;
            var minX = hasChildren ? children.Min(c => c.Value.X - c.w / 2) - 16 : 0;
            var minY = hasChildren ? children.Min(c => c.Value.Y - c.h / 2) - 32 : 0;
            var maxX = hasChildren ? children.Max(c => c.Value.X + c.w / 2) + 16 : 200;
            var maxY = hasChildren ? children.Max(c => c.Value.Y + c.h / 2) + 16 : 100;
            return (object)new
            {
                id = g.Id,
                position = new { x = minX, y = minY },
                data = new { label = g.Label ?? g.Id },
                type = "group",
                parentNode = g.ParentGroupId,
                style = new
                {
                    width = maxX - minX,
                    height = maxY - minY,
                    backgroundColor = "rgba(148,163,184,0.08)",
                    border = "1px dashed #94a3b8",
                    borderRadius = 8,
                    padding = 4,
                },
            };
        });

        var dataNodes = positioned.Nodes.Select(n =>
        {
            var pos = n.Position ?? new Position(0, 0);
            var parentPos = n.GroupId is { } gid ? positioned.Nodes
                .Where(other => other.GroupId == gid && other.Position.HasValue)
                .Select(other => (Position?)other.Position).FirstOrDefault() : null;
            (double offX, double offY) = n.GroupId is { } groupId
                ? GroupOffset(positioned, groupId)
                : (0, 0);
            return (object)new
            {
                id = n.Id.Value,
                position = new { x = pos.X - offX, y = pos.Y - offY },
                data = new { label = n.Label ?? n.Id.Value, tooltip = n.Tooltip, custom = n.Data },
                type = MapNodeType(n.Style?.Shape ?? NodeShape.Ellipse),
                parentNode = n.GroupId,
                extent = n.GroupId is null ? null : "parent",
                style = NodeStyleDto(n, positioned.Theme),
            };
        });

        var payload = new
        {
            format = FormatId,
            version = FormatVersion,
            directed = positioned.Directed,
            theme = ThemeDto(positioned.Theme),
            interaction = positioned.Interaction,
            nodes = groupNodes.Concat(dataNodes),
            edges = positioned.Edges.Select(e => new
            {
                id = e.Id.Value,
                source = e.Source.Value,
                target = e.Target.Value,
                label = e.Label,
                type = (e.Style?.LineStyle ?? LineStyle.Solid) == LineStyle.Solid ? "default" : "smoothstep",
                animated = false,
                markerEnd = positioned.Directed && (e.Style?.TargetArrow ?? ArrowStyle.Triangle) != ArrowStyle.None
                    ? new { type = "arrowclosed" } : null,
                style = EdgeStyleDto(e, positioned.Theme),
                data = e.Data,
            }),
            groups = positioned.Groups.Select(g => new { id = g.Id, label = g.Label, parent = g.ParentGroupId }),
        };

        return JsonSerializer.Serialize(payload, NetworkJsonSerializer.Options);
    }

    private static (double X, double Y) GroupOffset(Network net, string groupId)
    {
        var children = net.Nodes
            .Where(n => n.GroupId == groupId && n.Position.HasValue)
            .Select(n => (n.Position!.Value, w: n.Style?.Width ?? 100, h: n.Style?.Height ?? 50))
            .ToArray();
        if (children.Length == 0) return (0, 0);
        var minX = children.Min(c => c.Value.X - c.w / 2) - 16;
        var minY = children.Min(c => c.Value.Y - c.h / 2) - 32;
        return (minX, minY);
    }

    private static string MapNodeType(NodeShape shape) => shape switch
    {
        NodeShape.Circle => "circle",
        NodeShape.Ellipse => "ellipse",
        NodeShape.Diamond => "diamond",
        NodeShape.Hexagon => "hexagon",
        NodeShape.Triangle => "triangle",
        NodeShape.RoundedRectangle => "rounded",
        _ => "default"
    };

    private static object NodeStyleDto(Node n, Theme theme)
    {
        var fill = n.Style?.Fill ?? theme.DefaultNodeFill;
        var stroke = n.Style?.Stroke ?? theme.DefaultNodeStroke;
        var label = n.Style?.LabelColor ?? theme.DefaultLabelColor;
        return new
        {
            backgroundColor = fill.ToHex(),
            borderColor = stroke.ToHex(),
            borderWidth = n.Style?.StrokeWidth ?? 1.0,
            color = label.ToHex(),
            width = n.Style?.Width,
            height = n.Style?.Height,
            fontFamily = n.Style?.FontFamily ?? theme.DefaultFontFamily,
            fontSize = n.Style?.FontSize ?? theme.DefaultFontSize,
        };
    }

    private static object EdgeStyleDto(Edge e, Theme theme) => new
    {
        stroke = (e.Style?.Stroke ?? theme.DefaultEdgeStroke).ToHex(),
        strokeWidth = e.Style?.StrokeWidth ?? 1.0,
        strokeDasharray = e.Style?.LineStyle switch
        {
            LineStyle.Dashed => "6 4",
            LineStyle.Dotted => "2 3",
            _ => (string?)null,
        },
    };

    private static object ThemeDto(Theme t) => new
    {
        background = t.Background.ToHex(),
        defaultNodeFill = t.DefaultNodeFill.ToHex(),
        defaultNodeStroke = t.DefaultNodeStroke.ToHex(),
        defaultEdgeStroke = t.DefaultEdgeStroke.ToHex(),
        defaultLabelColor = t.DefaultLabelColor.ToHex(),
        fontFamily = t.DefaultFontFamily,
        fontSize = t.DefaultFontSize,
    };
}
