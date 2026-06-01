namespace DeepSigma.NetworkVisualization.Rendering;

/// <summary>
/// A renderer projects a <see cref="Network"/> into a target output shape — text, JSON, raster, …
/// Implementations are stateless and free of side effects; the same network must render to the same output.
/// </summary>
/// <typeparam name="TOutput">The rendered shape. Typically <c>string</c> for text/JSON renderers and <c>byte[]</c> for raster.</typeparam>
public interface INetworkRenderer<out TOutput>
{
    /// <summary>Short identifier (e.g. <c>"mermaid"</c>, <c>"svg"</c>); used in route paths and content negotiation.</summary>
    string FormatId { get; }

    /// <summary>Project the network into the output shape. Should never mutate the input.</summary>
    TOutput Render(Network network);
}

/// <summary>Marker for renderers whose output is JSON; allows hosts to set Content-Type to <c>application/json</c> without sniffing.</summary>
public interface IJsonNetworkRenderer : INetworkRenderer<string>
{
    /// <summary>Schema version of the emitted JSON (independent of the <see cref="Network"/>'s own version).</summary>
    string FormatVersion { get; }
}

/// <summary>
/// Computes node positions for a network. Producers should return a new <see cref="Network"/> with <see cref="Node.Position"/>
/// populated; the input must not be mutated. Built-in providers live in <see cref="Layouts.LayoutProviders"/>;
/// adapters (e.g. MSAGL) can override them via <see cref="Layouts.LayoutProviders.Register"/>.
/// </summary>
public interface ILayoutProvider
{
    /// <summary>Compute positions for every node and return a new network with them set.</summary>
    Network ApplyLayout(Network network);
}
