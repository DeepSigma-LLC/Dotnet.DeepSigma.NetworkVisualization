namespace DeepSigma.NetworkVisualization.Rendering;

/// <summary>
/// Framework-neutral output discriminator returned by a <see cref="RendererDescriptor"/>. Hosts (ASP.NET, console, …)
/// match on the concrete subtype to produce the appropriate response. Lives in Core so descriptors stay independent
/// of any web framework.
/// </summary>
public abstract record RendererOutput(string MimeType);

/// <summary>Rendered output as text (mermaid, DOT, SVG XML, JSON).</summary>
public sealed record TextOutput(string Content, string MimeType) : RendererOutput(MimeType);

/// <summary>Rendered output as raw bytes (PNG, JPEG, WebP).</summary>
public sealed record BinaryOutput(byte[] Bytes, string MimeType) : RendererOutput(MimeType);

/// <summary>
/// A self-describing handle that lets a host iterate <c>IEnumerable&lt;RendererDescriptor&gt;</c> from DI
/// and wire endpoints generically — see the demo's <c>Program.cs</c> for the canonical use case.
/// Concrete subclasses are <see cref="TextRendererDescriptor"/> and <see cref="BinaryRendererDescriptor"/>.
/// </summary>
public abstract record RendererDescriptor(RendererMetadata Metadata)
{
    /// <summary>Render the network and wrap the result in a <see cref="RendererOutput"/>. <paramref name="services"/> is the live DI scope used to resolve the renderer.</summary>
    public abstract RendererOutput Render(Network network, IServiceProvider services);
}

/// <summary>A descriptor for renderers that produce text (mermaid, DOT, SVG, JSON).</summary>
public sealed record TextRendererDescriptor(
    RendererMetadata Metadata,
    Func<IServiceProvider, Network, string> RenderFn) : RendererDescriptor(Metadata)
{
    /// <inheritdoc/>
    public override RendererOutput Render(Network network, IServiceProvider services)
        => new TextOutput(RenderFn(services, network), Metadata.MimeType);
}

/// <summary>A descriptor for renderers that produce raw bytes (PNG, JPEG, WebP).</summary>
public sealed record BinaryRendererDescriptor(
    RendererMetadata Metadata,
    Func<IServiceProvider, Network, byte[]> RenderFn) : RendererDescriptor(Metadata)
{
    /// <inheritdoc/>
    public override RendererOutput Render(Network network, IServiceProvider services)
        => new BinaryOutput(RenderFn(services, network), Metadata.MimeType);
}
