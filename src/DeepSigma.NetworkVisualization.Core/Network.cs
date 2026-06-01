namespace DeepSigma.NetworkVisualization;

public sealed record Network
{
    public bool Directed { get; init; } = true;
    public IReadOnlyList<Node> Nodes { get; init; } = [];
    public IReadOnlyList<Edge> Edges { get; init; } = [];
    public IReadOnlyList<Group> Groups { get; init; } = [];
    public LayoutSettings Layout { get; init; } = LayoutSettings.Default;
    public InteractionSettings Interaction { get; init; } = InteractionSettings.Default;
    public Theme Theme { get; init; } = Theme.Light;
    public string? Title { get; init; }
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }

    public Node? FindNode(string id) => Nodes.FirstOrDefault(n => n.Id.Value == id);
    public Edge? FindEdge(string id) => Edges.FirstOrDefault(e => e.Id.Value == id);
    public Group? FindGroup(string id) => Groups.FirstOrDefault(g => g.Id == id);
}

public sealed class NetworkValidationException(IReadOnlyList<string> errors)
    : Exception($"Network validation failed with {errors.Count} error(s):{Environment.NewLine}{string.Join(Environment.NewLine, errors)}")
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
