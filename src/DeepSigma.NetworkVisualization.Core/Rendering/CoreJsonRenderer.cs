using DeepSigma.NetworkVisualization.Json;

namespace DeepSigma.NetworkVisualization.Rendering;

public sealed class CoreJsonRenderer : IJsonNetworkRenderer
{
    public string FormatId => NetworkJsonSerializer.FormatId;
    public string FormatVersion => NetworkJsonSerializer.FormatVersion;
    public string Render(Network network) => NetworkJsonSerializer.Serialize(network);
}
