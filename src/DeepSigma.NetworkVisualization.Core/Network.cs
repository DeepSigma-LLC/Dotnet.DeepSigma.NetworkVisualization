namespace DeepSigma.NetworkVisualization;

/// <summary>
/// The canonical graph model. Immutable; create instances via <see cref="Builders.NetworkBuilder"/>
/// (recommended), <c>with</c>-expressions on an existing instance, or
/// <see cref="Json.NetworkJsonSerializer.Deserialize"/>. All renderers consume this shape.
/// </summary>
public sealed record Network
{
    /// <summary>When <c>true</c>, edges have a direction from <see cref="Edge.Source"/> to <see cref="Edge.Target"/>; renderers draw arrowheads by default.</summary>
    public bool Directed { get; init; } = true;

    /// <summary>The graph's nodes. Order is preserved across serialization.</summary>
    public IReadOnlyList<Node> Nodes { get; init; } = [];

    /// <summary>The graph's edges. References to <see cref="Edge.Source"/>/<see cref="Edge.Target"/> must exist in <see cref="Nodes"/>; the builder enforces this.</summary>
    public IReadOnlyList<Edge> Edges { get; init; } = [];

    /// <summary>Logical groupings of nodes (think 'cluster' or 'subgraph'). Renderers visualize these as bounded containers.</summary>
    public IReadOnlyList<Group> Groups { get; init; } = [];

    /// <summary>Hints for layout providers: algorithm, direction, spacing, seed.</summary>
    public LayoutSettings Layout { get; init; } = LayoutSettings.Default;

    /// <summary>Hints for interactive renderers: zoom/pan/drag/selection toggles.</summary>
    public InteractionSettings Interaction { get; init; } = InteractionSettings.Default;

    /// <summary>Default colors and font for rendering. <see cref="Theme.Light"/> and <see cref="Theme.Dark"/> are built-in presets.</summary>
    public Theme Theme { get; init; } = Theme.Light;

    /// <summary>Optional human-friendly title; renderers may surface it in chart titles or window headings.</summary>
    public string? Title { get; init; }

    /// <summary>Free-form metadata passed through serialization. Use this for application-specific data.</summary>
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }

    /// <summary>Find a node by its id. Returns <c>null</c> if not present. O(n) linear scan.</summary>
    public Node? FindNode(string id) => Nodes.FirstOrDefault(n => n.Id.Value == id);

    /// <summary>Find an edge by its id. Returns <c>null</c> if not present. O(n) linear scan.</summary>
    public Edge? FindEdge(string id) => Edges.FirstOrDefault(e => e.Id.Value == id);

    /// <summary>Find a group by its id. Returns <c>null</c> if not present. O(n) linear scan.</summary>
    public Group? FindGroup(string id) => Groups.FirstOrDefault(g => g.Id == id);
}

/// <summary>
/// Thrown by <see cref="Builders.NetworkBuilder.Build"/> when the builder detects structural problems —
/// duplicate ids, dangling edge endpoints, group cycles, etc. Aggregates every error in <see cref="Errors"/>.
/// </summary>
public sealed class NetworkValidationException(IReadOnlyList<string> errors)
    : Exception($"Network validation failed with {errors.Count} error(s):{Environment.NewLine}{string.Join(Environment.NewLine, errors)}")
{
    /// <summary>The full list of validation problems. Always at least one entry.</summary>
    public IReadOnlyList<string> Errors { get; } = errors;
}
