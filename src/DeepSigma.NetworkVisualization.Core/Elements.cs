using System.Collections.Generic;

namespace DeepSigma.NetworkVisualization;

public sealed record Node
{
    public required NodeId Id { get; init; }
    public string? Label { get; init; }
    public NodeStyle? Style { get; init; }
    public Position? Position { get; init; }
    public string? GroupId { get; init; }
    public string? Tooltip { get; init; }
    public string? Url { get; init; }
    public IReadOnlyDictionary<string, object?>? Data { get; init; }
}

public sealed record Edge
{
    public required EdgeId Id { get; init; }
    public required NodeId Source { get; init; }
    public required NodeId Target { get; init; }
    public string? Label { get; init; }
    public double? Weight { get; init; }
    public EdgeAppearance? Style { get; init; }
    public IReadOnlyDictionary<string, object?>? Data { get; init; }
}

public sealed record Group
{
    public required string Id { get; init; }
    public string? Label { get; init; }
    public NodeStyle? Style { get; init; }
    public string? ParentGroupId { get; init; }
    public bool Collapsed { get; init; }
    public IReadOnlyList<string> MemberNodeIds { get; init; } = [];
}
