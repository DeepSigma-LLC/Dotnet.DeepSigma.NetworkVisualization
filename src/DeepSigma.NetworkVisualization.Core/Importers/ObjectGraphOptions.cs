using System.Reflection;

namespace DeepSigma.NetworkVisualization.Importers;

/// <summary>
/// Knobs for <see cref="NetworkImporter.FromObject"/>. All have sensible defaults; override what you need.
/// </summary>
public sealed record ObjectGraphOptions
{
    /// <summary>Maximum recursion depth before a placeholder node is emitted. Default <c>5</c>.</summary>
    public int MaxDepth { get; init; } = 5;

    /// <summary>Maximum number of items to walk from any single collection property. Default <c>100</c>.</summary>
    public int MaxCollectionItems { get; init; } = 100;

    /// <summary>Long string values are truncated with an ellipsis to this many characters. Default <c>80</c>.</summary>
    public int MaxStringLength { get; init; } = 80;

    /// <summary>When <c>false</c>, null-valued properties are skipped entirely instead of rendered. Default <c>false</c>.</summary>
    public bool IncludeNullValues { get; init; }

    /// <summary>Label to use on the root node. Defaults to the root object's type name.</summary>
    public string? RootLabel { get; init; }

    /// <summary>
    /// Decides whether a given <see cref="Type"/> should be rendered as a leaf value (its <c>ToString()</c>) or walked into
    /// as a sub-object. By default, everything in <c>System.*</c>/<c>Microsoft.*</c> namespaces, plus primitives, enums,
    /// strings, <see cref="DateTime"/>, <see cref="Guid"/>, etc., are treated as leaves — so framework types don't explode
    /// into noise. Provide your own predicate to override.
    /// </summary>
    public Func<Type, bool>? IsLeafType { get; init; }

    /// <summary>Skip individual properties matching this predicate (e.g. ignore a `Password` or `Connection` member). Defaults to no filter.</summary>
    public Func<PropertyInfo, bool>? PropertyFilter { get; init; }

    internal bool IsLeaf(Type type)
    {
        if (IsLeafType is { } custom) return custom(type);
        return DefaultIsLeaf(type);
    }

    internal static bool DefaultIsLeaf(Type type)
    {
        if (type.IsPrimitive || type.IsEnum) return true;
        if (type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime)
            || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid)
            || type == typeof(Uri) || type == typeof(Version)) return true;
        if (Nullable.GetUnderlyingType(type) is { } underlying) return DefaultIsLeaf(underlying);
        var ns = type.Namespace ?? string.Empty;
        // Treat framework types as leaves so we don't recurse into e.g. List<T>'s 30 internal properties.
        // We still walk collections via IEnumerable detection, separately from "leaf" decisions.
        if (ns.StartsWith("System", StringComparison.Ordinal) || ns.StartsWith("Microsoft", StringComparison.Ordinal))
            return true;
        return false;
    }
}
