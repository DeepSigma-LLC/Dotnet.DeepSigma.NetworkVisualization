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
            label = n.ResolvedLabel(),
            group = n.GroupId,
            tooltip = n.Tooltip,
            fx = n.Position?.X,
            fy = n.Position?.Y,
            shape = n.ResolvedShape().ToString().ToLowerInvariant(),
            fill = n.ResolvedFill(theme).ToHex(),
            stroke = n.ResolvedStroke(theme).ToHex(),
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
            stroke = e.ResolvedStroke(theme).ToHex(),
            strokeWidth = e.ResolvedStrokeWidth(),
            lineStyle = e.ResolvedLineStyle().ToString().ToLowerInvariant(),
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
        var (w, h) = n.ResolvedSize(60, 40);
        return Math.Max(8, Math.Min(w, h) / 4);
    }
}
