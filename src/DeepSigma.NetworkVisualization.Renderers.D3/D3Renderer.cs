using System.Text.Json;
using DeepSigma.NetworkVisualization.Json;
using DeepSigma.NetworkVisualization.Rendering;

namespace DeepSigma.NetworkVisualization.Renderers.D3;

public sealed class D3Renderer : IJsonNetworkRenderer
{
    public string FormatId => "d3-force";
    public string FormatVersion => "1.0";

    public string Render(Network network)
    {
        var theme = network.Theme;

        var nodes = network.Nodes.Select(n => new
        {
            id = n.Id.Value,
            label = n.Label ?? n.Id.Value,
            group = n.GroupId,
            tooltip = n.Tooltip,
            fx = n.Position?.X,
            fy = n.Position?.Y,
            shape = (n.Style?.Shape ?? NodeShape.Ellipse).ToString().ToLowerInvariant(),
            fill = (n.Style?.Fill ?? theme.DefaultNodeFill).ToHex(),
            stroke = (n.Style?.Stroke ?? theme.DefaultNodeStroke).ToHex(),
            radius = ComputeRadius(n),
            data = n.Data,
        });

        var links = network.Edges.Select(e => new
        {
            id = e.Id.Value,
            source = e.Source.Value,
            target = e.Target.Value,
            label = e.Label,
            value = e.Weight ?? 1.0,
            stroke = (e.Style?.Stroke ?? theme.DefaultEdgeStroke).ToHex(),
            strokeWidth = e.Style?.StrokeWidth ?? 1.0,
            lineStyle = (e.Style?.LineStyle ?? LineStyle.Solid).ToString().ToLowerInvariant(),
            data = e.Data,
        });

        var groups = network.Groups.Select(g => new { id = g.Id, label = g.Label, parent = g.ParentGroupId });

        var payload = new
        {
            format = FormatId,
            version = FormatVersion,
            directed = network.Directed,
            theme = new
            {
                background = theme.Background.ToHex(),
                fontFamily = theme.DefaultFontFamily,
                fontSize = theme.DefaultFontSize,
                labelColor = theme.DefaultLabelColor.ToHex(),
            },
            simulation = new
            {
                charge = -300,
                linkDistance = network.Layout.NodeSpacing,
                alpha = 1.0,
                alphaDecay = 0.02,
            },
            interaction = network.Interaction,
            nodes,
            links,
            groups,
        };

        return JsonSerializer.Serialize(payload, NetworkJsonSerializer.Options);
    }

    private static double ComputeRadius(Node n)
    {
        var w = n.Style?.Width ?? 60;
        var h = n.Style?.Height ?? 40;
        return Math.Max(8, Math.Min(w, h) / 4);
    }
}
