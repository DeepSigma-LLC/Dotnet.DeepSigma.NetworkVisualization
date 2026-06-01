using System.Text.Json;
using DeepSigma.NetworkVisualization.Json;
using DeepSigma.NetworkVisualization.Layouts;
using DeepSigma.NetworkVisualization.Rendering;

namespace DeepSigma.NetworkVisualization.Renderers.ReactFlow;

public sealed class ReactFlowRenderer : IJsonNetworkRenderer
{
    private const double GroupPaddingX = 16;
    private const double GroupPaddingTop = 32;
    private const double GroupPaddingBottom = 16;
    private const double DefaultNodeWidth = 100;
    private const double DefaultNodeHeight = 50;

    public static RendererMetadata Metadata { get; } = new("reactflow", "application/json", RequiresLayout: true);
    public string FormatId => Metadata.FormatId;
    public string FormatVersion => "1.0";
    public bool AutoLayoutIfMissing { get; init; } = true;

    public string Render(Network network)
    {
        var positioned = network.EnsureLayout(AutoLayoutIfMissing);

        var groupNodes = positioned.Groups.Select(g =>
        {
            var box = ComputeGroupBox(positioned, g.Id);
            return (object)new
            {
                id = g.Id,
                position = new { x = box.X, y = box.Y },
                data = new { label = g.Label ?? g.Id },
                type = "group",
                parentNode = g.ParentGroupId,
                style = new
                {
                    width = box.Width,
                    height = box.Height,
                    backgroundColor = "rgba(148,163,184,0.08)",
                    border = "1px dashed #94a3b8",
                    borderRadius = 8,
                    padding = 4,
                },
            };
        });

        var dataNodes = positioned.Nodes.Select(n =>
        {
            var pos = n.Position ?? Position.Origin;
            var (offX, offY) = n.GroupId is { } groupId
                ? (ComputeGroupBox(positioned, groupId).X, ComputeGroupBox(positioned, groupId).Y)
                : (0d, 0d);
            return (object)new
            {
                id = n.Id.Value,
                position = new { x = pos.X - offX, y = pos.Y - offY },
                data = new { label = n.ResolvedLabel(), tooltip = n.Tooltip, custom = n.Data },
                type = MapNodeType(n.ResolvedShape()),
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
                type = e.ResolvedLineStyle() == LineStyle.Solid ? "default" : "smoothstep",
                animated = false,
                markerEnd = e.HasArrowHead(positioned.Directed) ? new { type = "arrowclosed" } : null,
                style = EdgeStyleDto(e, positioned.Theme),
                data = e.Data,
            }),
            groups = positioned.Groups.Select(g => new { id = g.Id, label = g.Label, parent = g.ParentGroupId }),
        };

        return JsonSerializer.Serialize(payload, NetworkJsonSerializer.Options);
    }

    private static (double X, double Y, double Width, double Height) ComputeGroupBox(Network net, string groupId)
    {
        var children = net.Nodes
            .Where(n => n.GroupId == groupId && n.Position.HasValue)
            .Select(n =>
            {
                var (w, h) = n.ResolvedSize(DefaultNodeWidth, DefaultNodeHeight);
                return (Pos: n.Position!.Value, W: w, H: h);
            })
            .ToArray();

        if (children.Length == 0) return (0, 0, 200, 100);

        var minX = children.Min(c => c.Pos.X - c.W / 2) - GroupPaddingX;
        var minY = children.Min(c => c.Pos.Y - c.H / 2) - GroupPaddingTop;
        var maxX = children.Max(c => c.Pos.X + c.W / 2) + GroupPaddingX;
        var maxY = children.Max(c => c.Pos.Y + c.H / 2) + GroupPaddingBottom;
        return (minX, minY, maxX - minX, maxY - minY);
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

    private static object NodeStyleDto(Node n, Theme theme) => new
    {
        backgroundColor = n.ResolvedFill(theme).ToHex(),
        borderColor = n.ResolvedStroke(theme).ToHex(),
        borderWidth = n.ResolvedStrokeWidth(),
        color = n.ResolvedLabelColor(theme).ToHex(),
        width = n.Style?.Width,
        height = n.Style?.Height,
        fontFamily = n.ResolvedFontFamily(theme),
        fontSize = n.ResolvedFontSize(theme),
    };

    private static object EdgeStyleDto(Edge e, Theme theme) => new
    {
        stroke = e.ResolvedStroke(theme).ToHex(),
        strokeWidth = e.ResolvedStrokeWidth(),
        strokeDasharray = e.ResolvedLineStyle() switch
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
