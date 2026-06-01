using DeepSigma.NetworkVisualization.Json;

namespace DeepSigma.NetworkVisualization.Importers;

/// <summary>
/// Public entry points for importing external graph data into a Core <see cref="Network"/>.
/// We deliberately support exactly three shapes:
/// <list type="bullet">
///   <item><description><b>Core JSON</b> — the envelope produced by <see cref="NetworkJsonSerializer"/>. Document your data to this schema.</description></item>
///   <item><description><b>CSV</b> — two CSV strings (nodes + edges) with documented column conventions.</description></item>
///   <item><description><b>Object graph</b> — reflection-walk a runtime .NET object; nodes become objects, leaves become labeled values, collections become groups.</description></item>
/// </list>
/// Translating from framework-specific JSON dialects (D3, Cytoscape, Graphology, Gephi, custom DB rows, …) is a mapping
/// concern that lives in the caller's code, not here. We keep the contract small so it stays stable.
/// </summary>
public static class NetworkImporter
{
    /// <summary>
    /// Parse a Core JSON envelope (the same shape <see cref="NetworkJsonSerializer.Serialize"/> produces).
    /// </summary>
    public static Network FromJson(string json)
        => NetworkJsonSerializer.Deserialize(json)
           ?? throw new FormatException(
               "Could not deserialize Core network JSON. " +
               "Expected the envelope { \"format\": \"deepsigma.network\", \"version\": \"1.0\", \"network\": { directed, nodes, edges, ... } }. " +
               "Use NetworkJsonSerializer.Serialize(network) on a sample to see the canonical shape.");

    /// <summary>
    /// Parse two CSV strings into a network. <paramref name="nodesCsv"/> must have an <c>id</c> column;
    /// <paramref name="edgesCsv"/> must have <c>source</c> and <c>target</c>. See <see cref="CsvImporter"/> for the full column conventions.
    /// </summary>
    public static Network FromCsv(string nodesCsv, string edgesCsv, bool directed = true)
        => CsvImporter.Import(nodesCsv, edgesCsv, directed);

    /// <summary>
    /// Walk a runtime .NET object's public instance properties and produce a network showing the object hierarchy.
    /// Complex (user-defined) types become nodes; primitives, strings, dates, Guids, and other framework types
    /// become labeled value leaves. Collections become hexagonal group nodes containing one child per item.
    /// Reference cycles are detected and rendered as dashed "ref" edges back to the previously visited node.
    /// </summary>
    /// <param name="root">The object to walk. Must be non-null.</param>
    /// <param name="options">Optional knobs (depth cap, collection cap, filters). See <see cref="ObjectGraphOptions"/>.</param>
    public static Network FromObject(object root, ObjectGraphOptions? options = null)
        => ObjectGraphImporter.Import(root, options ?? new ObjectGraphOptions());
}
