using DeepSigma.NetworkVisualization.Rendering;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Prototype.Ranking;
using MsaglNode = Microsoft.Msagl.Core.Layout.Node;
using MsaglEdge = Microsoft.Msagl.Core.Layout.Edge;

namespace DeepSigma.NetworkVisualization.Layout.Msagl;

internal static class MsaglConverter
{
    public static (GeometryGraph Graph, Dictionary<string, MsaglNode> Index) ToGeometryGraph(Network network, double defaultWidth = 100, double defaultHeight = 50)
    {
        var graph = new GeometryGraph();
        var index = new Dictionary<string, MsaglNode>(network.Nodes.Count);
        foreach (var n in network.Nodes)
        {
            var w = n.Style?.Width ?? defaultWidth;
            var h = n.Style?.Height ?? defaultHeight;
            var curve = CurveFactory.CreateRectangle(w, h, new Point(0, 0));
            var node = new MsaglNode(curve, n.Id.Value);
            graph.Nodes.Add(node);
            index[n.Id.Value] = node;
        }
        foreach (var e in network.Edges)
        {
            if (!index.TryGetValue(e.Source.Value, out var src)) continue;
            if (!index.TryGetValue(e.Target.Value, out var dst)) continue;
            graph.Edges.Add(new MsaglEdge(src, dst));
        }
        return (graph, index);
    }

    public static Network ApplyPositions(Network network, Dictionary<string, MsaglNode> index)
    {
        var updated = new Node[network.Nodes.Count];
        for (int i = 0; i < network.Nodes.Count; i++)
        {
            var n = network.Nodes[i];
            updated[i] = index.TryGetValue(n.Id.Value, out var m)
                ? n with { Position = new Position(m.Center.X, m.Center.Y) }
                : n;
        }
        return network with { Nodes = updated };
    }
}

public sealed class MsaglSugiyamaLayoutProvider : ILayoutProvider
{
    public double LayerSeparation { get; init; } = 60;
    public double NodeSeparation { get; init; } = 30;

    public Network ApplyLayout(Network network)
    {
        if (network.Nodes.Count == 0) return network;
        var (graph, index) = MsaglConverter.ToGeometryGraph(network);

        var settings = new SugiyamaLayoutSettings
        {
            LayerSeparation = LayerSeparation,
            NodeSeparation = NodeSeparation,
            Transformation = network.Layout.Direction switch
            {
                LayoutDirection.LeftToRight => PlaneTransformation.Rotation(Math.PI / 2),
                LayoutDirection.RightToLeft => PlaneTransformation.Rotation(-Math.PI / 2),
                LayoutDirection.BottomToTop => PlaneTransformation.Rotation(Math.PI),
                _ => PlaneTransformation.UnitTransformation,
            },
        };

        var layout = new LayeredLayout(graph, settings);
        layout.Run();
        return MsaglConverter.ApplyPositions(network, index);
    }
}

public sealed class MsaglMdsLayoutProvider : ILayoutProvider
{
    public double PivotNumber { get; init; } = 50;

    public Network ApplyLayout(Network network)
    {
        if (network.Nodes.Count == 0) return network;
        var (graph, index) = MsaglConverter.ToGeometryGraph(network);
        var settings = new MdsLayoutSettings();
        var layout = new MdsGraphLayout(settings, graph);
        layout.Run();
        return MsaglConverter.ApplyPositions(network, index);
    }
}

public sealed class MsaglRankingLayoutProvider : ILayoutProvider
{
    public Network ApplyLayout(Network network)
    {
        if (network.Nodes.Count == 0) return network;
        var (graph, index) = MsaglConverter.ToGeometryGraph(network);
        var settings = new RankingLayoutSettings();
        var layout = new RankingLayout(settings, graph);
        layout.Run();
        return MsaglConverter.ApplyPositions(network, index);
    }
}
