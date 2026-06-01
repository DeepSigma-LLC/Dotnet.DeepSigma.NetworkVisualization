using DeepSigma.NetworkVisualization.Rendering;

namespace DeepSigma.NetworkVisualization.Layouts;

public sealed class NoLayoutProvider : ILayoutProvider
{
    public Network ApplyLayout(Network network) => network;
}

public sealed class GridLayoutProvider : ILayoutProvider
{
    public int? Columns { get; init; }
    public double CellWidth { get; init; } = 140;
    public double CellHeight { get; init; } = 80;

    public Network ApplyLayout(Network network)
    {
        var nodes = network.Nodes.ToArray();
        if (nodes.Length == 0) return network;

        var cols = Columns ?? (int)Math.Ceiling(Math.Sqrt(nodes.Length));
        var positioned = new Node[nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
        {
            int row = i / cols, col = i % cols;
            positioned[i] = nodes[i] with { Position = new Position(col * CellWidth, row * CellHeight) };
        }
        return network with { Nodes = positioned };
    }
}

public sealed class CircularLayoutProvider : ILayoutProvider
{
    public double Radius { get; init; } = 200;
    public double CenterX { get; init; }
    public double CenterY { get; init; }

    public Network ApplyLayout(Network network)
    {
        var nodes = network.Nodes.ToArray();
        if (nodes.Length == 0) return network;

        var step = 2 * Math.PI / nodes.Length;
        var positioned = new Node[nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
        {
            var angle = i * step - Math.PI / 2;
            positioned[i] = nodes[i] with { Position = new Position(CenterX + Radius * Math.Cos(angle), CenterY + Radius * Math.Sin(angle)) };
        }
        return network with { Nodes = positioned };
    }
}

public sealed class TreeLayoutProvider : ILayoutProvider
{
    public double LevelGap { get; init; } = 100;
    public double SiblingGap { get; init; } = 120;

    public Network ApplyLayout(Network network)
    {
        if (network.Nodes.Count == 0) return network;

        var children = new Dictionary<string, List<string>>();
        var indeg = network.Nodes.ToDictionary(n => n.Id.Value, _ => 0);
        foreach (var e in network.Edges)
        {
            if (!children.TryGetValue(e.Source.Value, out var list))
                children[e.Source.Value] = list = [];
            list.Add(e.Target.Value);
            if (indeg.TryGetValue(e.Target.Value, out var cur)) indeg[e.Target.Value] = cur + 1;
        }

        var roots = indeg.Where(kv => kv.Value == 0).Select(kv => kv.Key).ToList();
        if (roots.Count == 0) roots.Add(network.Nodes[0].Id.Value);

        var positions = new Dictionary<string, Position>();
        var visited = new HashSet<string>();
        double cursorX = 0;

        foreach (var root in roots)
            cursorX = Layout(root, 0, cursorX);

        var positioned = network.Nodes.Select(n => positions.TryGetValue(n.Id.Value, out var p) ? n with { Position = p } : n).ToArray();
        return network with { Nodes = positioned };

        double Layout(string nodeId, int depth, double xCursor)
        {
            if (!visited.Add(nodeId))
            {
                positions[nodeId] = new Position(xCursor, depth * LevelGap);
                return xCursor + SiblingGap;
            }
            if (!children.TryGetValue(nodeId, out var kids) || kids.Count == 0)
            {
                positions[nodeId] = new Position(xCursor, depth * LevelGap);
                return xCursor + SiblingGap;
            }

            double childStart = xCursor;
            double childEnd = xCursor;
            foreach (var c in kids)
                childEnd = Layout(c, depth + 1, childEnd);
            var center = (childStart + childEnd - SiblingGap) / 2;
            positions[nodeId] = new Position(center, depth * LevelGap);
            return childEnd;
        }
    }
}

public sealed class SimpleForceDirectedLayoutProvider : ILayoutProvider
{
    public int Iterations { get; init; } = 200;
    public double Width { get; init; } = 800;
    public double Height { get; init; } = 600;
    public int Seed { get; init; } = 42;

    public Network ApplyLayout(Network network)
    {
        var nodes = network.Nodes.ToArray();
        var n = nodes.Length;
        if (n == 0) return network;

        var rng = new Random(Seed);
        var px = new double[n];
        var py = new double[n];
        var index = new Dictionary<string, int>(n);
        for (int i = 0; i < n; i++)
        {
            index[nodes[i].Id.Value] = i;
            px[i] = nodes[i].Position?.X ?? rng.NextDouble() * Width;
            py[i] = nodes[i].Position?.Y ?? rng.NextDouble() * Height;
        }

        var area = Width * Height;
        var k = Math.Sqrt(area / Math.Max(1, n));
        var t = Width / 10.0;

        var edges = network.Edges
            .Where(e => index.ContainsKey(e.Source.Value) && index.ContainsKey(e.Target.Value))
            .Select(e => (index[e.Source.Value], index[e.Target.Value]))
            .ToArray();

        var dispX = new double[n];
        var dispY = new double[n];

        for (int it = 0; it < Iterations; it++)
        {
            Array.Clear(dispX); Array.Clear(dispY);

            for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                var dx = px[i] - px[j];
                var dy = py[i] - py[j];
                var dist = Math.Sqrt(dx * dx + dy * dy) + 1e-9;
                var force = (k * k) / dist;
                var ux = dx / dist; var uy = dy / dist;
                dispX[i] += ux * force; dispY[i] += uy * force;
                dispX[j] -= ux * force; dispY[j] -= uy * force;
            }

            foreach (var (a, b) in edges)
            {
                var dx = px[a] - px[b];
                var dy = py[a] - py[b];
                var dist = Math.Sqrt(dx * dx + dy * dy) + 1e-9;
                var force = (dist * dist) / k;
                var ux = dx / dist; var uy = dy / dist;
                dispX[a] -= ux * force; dispY[a] -= uy * force;
                dispX[b] += ux * force; dispY[b] += uy * force;
            }

            for (int i = 0; i < n; i++)
            {
                var d = Math.Sqrt(dispX[i] * dispX[i] + dispY[i] * dispY[i]) + 1e-9;
                px[i] += (dispX[i] / d) * Math.Min(d, t);
                py[i] += (dispY[i] / d) * Math.Min(d, t);
                px[i] = Math.Clamp(px[i], 0, Width);
                py[i] = Math.Clamp(py[i], 0, Height);
            }

            t = Math.Max(t * 0.95, 1.0);
        }

        var positioned = new Node[n];
        for (int i = 0; i < n; i++)
            positioned[i] = nodes[i] with { Position = new Position(px[i], py[i]) };
        return network with { Nodes = positioned };
    }
}

public static class LayoutProviders
{
    public static ILayoutProvider For(Network network) => For(network.Layout);

    public static ILayoutProvider For(LayoutSettings settings) => settings.Algorithm switch
    {
        LayoutAlgorithm.None => new NoLayoutProvider(),
        LayoutAlgorithm.Grid => new GridLayoutProvider(),
        LayoutAlgorithm.Circular => new CircularLayoutProvider(),
        LayoutAlgorithm.Tree or LayoutAlgorithm.Hierarchical => new TreeLayoutProvider { LevelGap = settings.RankSpacing, SiblingGap = settings.NodeSpacing },
        LayoutAlgorithm.ForceDirected => new SimpleForceDirectedLayoutProvider { Seed = settings.RandomSeed ?? 42 },
        _ => new SimpleForceDirectedLayoutProvider { Seed = settings.RandomSeed ?? 42 },
    };
}
