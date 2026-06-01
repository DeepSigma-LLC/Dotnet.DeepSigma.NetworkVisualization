using System.Collections.Generic;

namespace DeepSigma.NetworkVisualization;

/// <summary>A graph node. Use <see cref="Builders.NodeBuilder"/> via <see cref="Builders.NetworkBuilder.AddNode(string)"/> to construct.</summary>
public sealed record Node
{
    /// <summary>Stable identifier; unique within the network. Used by edges and by the renderer output.</summary>
    public required NodeId Id { get; init; }

    /// <summary>Human-readable label; falls back to <see cref="Id"/> in renderers when null.</summary>
    public string? Label { get; init; }

    /// <summary>Visual style overrides (shape, fill, stroke, size, font). When <c>null</c>, renderers use the network <see cref="Theme"/> defaults.</summary>
    public NodeStyle? Style { get; init; }

    /// <summary>Pre-computed position; if <c>null</c>, renderers that need positions run a layout via <see cref="Layouts.LayoutExtensions.EnsureLayout"/>.</summary>
    public Position? Position { get; init; }

    /// <summary>Optional <see cref="Group.Id"/> this node belongs to. Group membership affects rendering (clusters/subgraphs) and layout grouping.</summary>
    public string? GroupId { get; init; }

    /// <summary>Tooltip text shown on hover by interactive renderers.</summary>
    public string? Tooltip { get; init; }

    /// <summary>Optional link the renderer may attach to the node (e.g. <c>href</c> on the SVG element).</summary>
    public string? Url { get; init; }

    /// <summary>Free-form payload that round-trips through JSON and flows to interactive renderers' click callbacks.</summary>
    public IReadOnlyDictionary<string, object?>? Data { get; init; }
}

/// <summary>A graph edge connecting two nodes. Use <see cref="Builders.EdgeBuilder"/> via <see cref="Builders.NetworkBuilder.AddEdge(string,string)"/>.</summary>
public sealed record Edge
{
    /// <summary>Stable identifier; unique within the network.</summary>
    public required EdgeId Id { get; init; }

    /// <summary>The source node's id. Must exist in <see cref="Network.Nodes"/>; builder validates this.</summary>
    public required NodeId Source { get; init; }

    /// <summary>The target node's id. Must exist in <see cref="Network.Nodes"/>.</summary>
    public required NodeId Target { get; init; }

    /// <summary>Edge label rendered on or near the line.</summary>
    public string? Label { get; init; }

    /// <summary>Numeric weight; some layout providers (e.g. force-directed) use this to bias spring length.</summary>
    public double? Weight { get; init; }

    /// <summary>Visual style overrides (color, width, dash pattern, arrows). When <c>null</c>, renderers use theme defaults.</summary>
    public EdgeAppearance? Style { get; init; }

    /// <summary>Free-form payload, mirror of <see cref="Node.Data"/>.</summary>
    public IReadOnlyDictionary<string, object?>? Data { get; init; }
}

/// <summary>A logical grouping (cluster/subgraph). Renderers visualize these as containers around member nodes.</summary>
public sealed record Group
{
    /// <summary>Stable identifier; nodes reference this via <see cref="Node.GroupId"/>.</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable label for the group (cluster title).</summary>
    public string? Label { get; init; }

    /// <summary>Style overrides for the group's container (background fill, border).</summary>
    public NodeStyle? Style { get; init; }

    /// <summary>Optional parent group id, enabling nested clusters. Cycles are rejected by the builder.</summary>
    public string? ParentGroupId { get; init; }

    /// <summary>If <c>true</c>, renderers may collapse this group's members into a single placeholder. Honored on a best-effort basis.</summary>
    public bool Collapsed { get; init; }

    /// <summary>Convenience list of member node ids; kept in sync with <see cref="Node.GroupId"/> by the builder.</summary>
    public IReadOnlyList<string> MemberNodeIds { get; init; } = [];
}
