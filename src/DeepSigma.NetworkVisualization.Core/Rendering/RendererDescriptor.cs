namespace DeepSigma.NetworkVisualization.Rendering;

public abstract record RendererOutput(string MimeType);

public sealed record TextOutput(string Content, string MimeType) : RendererOutput(MimeType);

public sealed record BinaryOutput(byte[] Bytes, string MimeType) : RendererOutput(MimeType);

public abstract record RendererDescriptor(RendererMetadata Metadata)
{
    public abstract RendererOutput Render(Network network, IServiceProvider services);
}

public sealed record TextRendererDescriptor(
    RendererMetadata Metadata,
    Func<IServiceProvider, Network, string> RenderFn) : RendererDescriptor(Metadata)
{
    public override RendererOutput Render(Network network, IServiceProvider services)
        => new TextOutput(RenderFn(services, network), Metadata.MimeType);
}

public sealed record BinaryRendererDescriptor(
    RendererMetadata Metadata,
    Func<IServiceProvider, Network, byte[]> RenderFn) : RendererDescriptor(Metadata)
{
    public override RendererOutput Render(Network network, IServiceProvider services)
        => new BinaryOutput(RenderFn(services, network), Metadata.MimeType);
}
