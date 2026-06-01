using DeepSigma.NetworkVisualization.Json;

namespace DeepSigma.NetworkVisualization.Rendering;

public sealed class CoreJsonRenderer : IJsonNetworkRenderer
{
    public static RendererMetadata Metadata { get; } = new("core", "application/json");

    public string FormatId => Metadata.FormatId;
    public string FormatVersion => NetworkJsonSerializer.FormatVersion;
    public string Render(Network network) => NetworkJsonSerializer.Serialize(network);
}
