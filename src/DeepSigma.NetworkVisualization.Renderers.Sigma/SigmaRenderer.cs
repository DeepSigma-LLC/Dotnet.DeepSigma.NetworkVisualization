using System.Text.Json;
using DeepSigma.NetworkVisualization.Json;
using DeepSigma.NetworkVisualization.Layouts;
using DeepSigma.NetworkVisualization.Rendering;

namespace DeepSigma.NetworkVisualization.Renderers.Sigma;

public sealed class SigmaRenderer : IJsonNetworkRenderer
{
    public static RendererMetadata Metadata { get; } = new("sigma", "application/json", RequiresLayout: true);
    public string FormatId => Metadata.FormatId;
    public string FormatVersion => "1.0";
    public bool AutoLayoutIfMissing { get; init; } = true;

    public string Render(Network network)
    {
        var positioned = network.EnsureLayout(AutoLayoutIfMissing);
        var theme = positioned.Theme;

        var graphologyPayload = new
        {
            attributes = new { name = positioned.Title ?? "network" },
            options = new
            {
                type = positioned.Directed ? "directed" : "undirected",
                multi = false,
                allowSelfLoops = true,
            },
            nodes = positioned.Nodes.Select(n =>
            {
                var pos = n.Position ?? Position.Origin;
                var (w, h) = n.ResolvedSize(60, 40);
                return new
                {
                    key = n.Id.Value,
                    attributes = new
                    {
                        label = n.ResolvedLabel(),
                        x = pos.X,
                        y = -pos.Y, // sigma's Y axis points up
                        size = Math.Max(6, Math.Min(w, h) / 6),
                        color = n.ResolvedFill(theme).ToHex(),
                        borderColor = n.ResolvedStroke(theme).ToHex(),
                        shape = MapShape(n.ResolvedShape()),
                        tooltip = n.Tooltip,
                    },
                };
            }),
            edges = positioned.Edges.Select(e => new
            {
                key = e.Id.Value,
                source = e.Source.Value,
                target = e.Target.Value,
                attributes = new
                {
                    label = e.Label,
                    weight = e.Weight ?? 1.0,
                    size = e.ResolvedStrokeWidth(),
                    color = e.ResolvedStroke(theme).ToHex(),
                    type = e.ResolvedLineStyle() == LineStyle.Solid ? "line" : "arrow",
                },
            }),
        };

        var envelope = new
        {
            format = FormatId,
            version = FormatVersion,
            theme = new
            {
                background = theme.Background.ToHex(),
                fontFamily = theme.DefaultFontFamily,
                fontSize = theme.DefaultFontSize,
                labelColor = theme.DefaultLabelColor.ToHex(),
            },
            interaction = positioned.Interaction,
            graph = graphologyPayload,
        };

        return JsonSerializer.Serialize(envelope, NetworkJsonSerializer.Options);
    }

    private static string MapShape(NodeShape s) => s switch
    {
        NodeShape.Circle or NodeShape.Ellipse => "circle",
        NodeShape.Rectangle or NodeShape.RoundedRectangle => "square",
        NodeShape.Diamond => "diamond",
        NodeShape.Triangle => "triangle",
        _ => "circle"
    };
}
