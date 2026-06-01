using DeepSigma.NetworkVisualization.Rendering;

namespace DeepSigma.NetworkVisualization.Layouts;

public sealed class RadialLayoutProvider : ILayoutProvider
{
    public double CenterX { get; init; }
    public double CenterY { get; init; }
    public double RingSpacing { get; init; } = 120;
    public double InnerRadius { get; init; }
    public NodeId? Root { get; init; }

    public Network ApplyLayout(Network network)
    {
        if (network.Nodes.Count == 0) return network;

        var rootId = ChooseRoot(network);

        // BFS depths (treat the graph as undirected for tree-laying purposes).
        var adjacency = BuildAdjacency(network);
        var depth = new Dictionary<string, int>();
        var order = new List<string>();
        var queue = new Queue<string>();
        depth[rootId] = 0;
        queue.Enqueue(rootId);
        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            order.Add(cur);
            if (!adjacency.TryGetValue(cur, out var neighbors)) continue;
            foreach (var n in neighbors)
            {
                if (depth.ContainsKey(n)) continue;
                depth[n] = depth[cur] + 1;
                queue.Enqueue(n);
            }
        }

        // Place any unreachable nodes (disconnected components) at successive depths after the main tree.
        var maxDepth = depth.Count == 0 ? 0 : depth.Values.Max();
        foreach (var n in network.Nodes)
        {
            if (depth.ContainsKey(n.Id.Value)) continue;
            maxDepth++;
            depth[n.Id.Value] = maxDepth;
            order.Add(n.Id.Value);
        }

        // Group by depth, then evenly distribute each ring around the circle.
        var byDepth = depth.GroupBy(kv => kv.Value)
            .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToArray());

        var positions = new Dictionary<string, Position>(network.Nodes.Count);
        foreach (var (d, ids) in byDepth)
        {
            if (d == 0)
            {
                positions[ids[0]] = new Position(CenterX, CenterY);
                continue;
            }

            var radius = InnerRadius + d * RingSpacing;
            var step = 2 * Math.PI / ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                var angle = i * step - Math.PI / 2;
                positions[ids[i]] = new Position(
                    CenterX + radius * Math.Cos(angle),
                    CenterY + radius * Math.Sin(angle));
            }
        }

        var updated = network.Nodes
            .Select(n => positions.TryGetValue(n.Id.Value, out var p) ? n with { Position = p } : n)
            .ToArray();
        return network with { Nodes = updated };
    }

    private string ChooseRoot(Network network)
    {
        if (Root is { } r && network.FindNode(r.Value) is not null) return r.Value;

        // Lowest in-degree wins; ties broken by first declared.
        var inDegree = network.Nodes.ToDictionary(n => n.Id.Value, _ => 0);
        foreach (var e in network.Edges)
        {
            if (inDegree.TryGetValue(e.Target.Value, out var cur)) inDegree[e.Target.Value] = cur + 1;
        }
        return inDegree
            .OrderBy(kv => kv.Value)
            .ThenBy(kv => Array.IndexOf(network.Nodes.Select(n => n.Id.Value).ToArray(), kv.Key))
            .First().Key;
    }

    private static Dictionary<string, List<string>> BuildAdjacency(Network network)
    {
        var adj = new Dictionary<string, List<string>>(network.Nodes.Count);
        foreach (var n in network.Nodes) adj[n.Id.Value] = [];
        foreach (var e in network.Edges)
        {
            if (adj.TryGetValue(e.Source.Value, out var sList)) sList.Add(e.Target.Value);
            if (adj.TryGetValue(e.Target.Value, out var tList)) tList.Add(e.Source.Value);
        }
        return adj;
    }
}
