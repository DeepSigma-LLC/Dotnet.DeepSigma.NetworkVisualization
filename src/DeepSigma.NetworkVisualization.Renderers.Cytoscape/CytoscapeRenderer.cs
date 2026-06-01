using System.Text.Json;
using DeepSigma.NetworkVisualization.Json;
using DeepSigma.NetworkVisualization.Rendering;

namespace DeepSigma.NetworkVisualization.Renderers.Cytoscape;

public sealed class CytoscapeRenderer : IJsonNetworkRenderer
{
    public static RendererMetadata Metadata { get; } = new("cytoscape", "application/json");
    public string FormatId => Metadata.FormatId;
    public string FormatVersion => "1.0";

    public string Render(Network network)
    {
        var theme = network.Theme;
        var groupNodes = network.Groups.Select(g =>
        {
            object? perGroupStyle = (g.Style?.Fill is { } f || g.Style?.Stroke is { } s)
                ? new
                {
                    backgroundColor = (g.Style?.Fill ?? theme.DefaultNodeFill).ToHex(),
                    borderColor = (g.Style?.Stroke ?? theme.DefaultNodeStroke).ToHex(),
                }
                : null;
            return (object)new
            {
                data = new { id = g.Id, label = g.Label ?? g.Id, parent = g.ParentGroupId, isGroup = true },
                classes = "group",
                style = perGroupStyle,
            };
        });

        var dataNodes = network.Nodes.Select(n =>
        {
            object? position = n.Position is { } p ? new { x = p.X, y = p.Y } : null;
            return (object)new
            {
                data = new
                {
                    id = n.Id.Value,
                    label = n.ResolvedLabel(),
                    parent = n.GroupId,
                    tooltip = n.Tooltip,
                    custom = n.Data,
                },
                position,
                classes = n.Style?.CssClass,
            };
        });

        var edges = network.Edges.Select(e => new
        {
            data = new
            {
                id = e.Id.Value,
                source = e.Source.Value,
                target = e.Target.Value,
                label = e.Label,
                weight = e.Weight,
                custom = e.Data,
            },
            classes = e.Style?.CssClass,
        });

        var style = new object[]
        {
            new { selector = "node", style = new {
                label = "data(label)",
                width = 80,
                height = 40,
                shape = "round-rectangle",
                backgroundColor = theme.DefaultNodeFill.ToHex(),
                borderColor = theme.DefaultNodeStroke.ToHex(),
                borderWidth = 1,
                color = theme.DefaultLabelColor.ToHex(),
                fontFamily = theme.DefaultFontFamily,
                fontSize = theme.DefaultFontSize,
                textValign = "center",
                textHalign = "center",
                // Fade labels as the user zooms out so a dense graph stays readable.
                // mapData(zoom, in0, in1, out0, out1) interpolates linearly; below 0.5x labels are gone.
                textOpacity = "mapData(zoom, 0.5, 1.5, 0, 1)",
                minZoomedFontSize = 8,
            } },
            new { selector = "edge", style = new {
                width = 1.5,
                lineColor = theme.DefaultEdgeStroke.ToHex(),
                targetArrowColor = theme.DefaultEdgeStroke.ToHex(),
                targetArrowShape = network.Directed ? "triangle" : "none",
                curveStyle = "bezier",
                controlPointStepSize = 40,
                label = "data(label)",
                fontSize = 10,
                color = theme.DefaultLabelColor.ToHex(),
                textBackgroundColor = theme.Background.ToHex(),
                textBackgroundOpacity = 0.85,
                textBackgroundPadding = "2px",
                textBackgroundShape = "roundrectangle",
                textOpacity = "mapData(zoom, 0.7, 1.5, 0, 1)",
            } },
            new { selector = ".group", style = new {
                shape = "round-rectangle",
                backgroundOpacity = 0.12,
                backgroundColor = theme.DefaultNodeFill.ToHex(),
                borderColor = theme.DefaultNodeStroke.ToHex(),
                borderStyle = "dashed",
                color = theme.DefaultLabelColor.ToHex(),
                label = "data(label)",
            } },
        };

        var payload = new
        {
            format = FormatId,
            version = FormatVersion,
            directed = network.Directed,
            elements = new
            {
                nodes = groupNodes.Concat(dataNodes).ToArray(),
                edges = edges.ToArray(),
            },
            style,
            layout = new
            {
                name = MapLayout(network.Layout.Algorithm),
                rankDir = MapDirection(network.Layout.Direction),
                nodeSep = network.Layout.NodeSpacing,
                rankSep = network.Layout.RankSpacing,
                padding = network.Layout.Padding,
                fit = network.Interaction.FitOnLoad,
            },
            interaction = network.Interaction,
        };

        return JsonSerializer.Serialize(payload, NetworkJsonSerializer.Options);
    }

    private static string MapLayout(LayoutAlgorithm a) => a switch
    {
        LayoutAlgorithm.None => "preset",
        LayoutAlgorithm.Grid => "grid",
        LayoutAlgorithm.Circular => "circle",
        LayoutAlgorithm.Tree or LayoutAlgorithm.Hierarchical or LayoutAlgorithm.Sugiyama => "dagre",
        LayoutAlgorithm.ForceDirected => "cose",
        LayoutAlgorithm.Radial => "concentric",
        LayoutAlgorithm.Mds => "cose",
        _ => "cose"
    };

    private static string MapDirection(LayoutDirection d) => d switch
    {
        LayoutDirection.TopToBottom => "TB",
        LayoutDirection.BottomToTop => "BT",
        LayoutDirection.LeftToRight => "LR",
        LayoutDirection.RightToLeft => "RL",
        _ => "TB"
    };
}
