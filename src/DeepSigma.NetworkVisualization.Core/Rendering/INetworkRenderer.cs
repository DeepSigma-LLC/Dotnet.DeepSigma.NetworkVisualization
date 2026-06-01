namespace DeepSigma.NetworkVisualization.Rendering;

public interface INetworkRenderer<out TOutput>
{
    string FormatId { get; }
    TOutput Render(Network network);
}

public interface IJsonNetworkRenderer : INetworkRenderer<string>
{
    string FormatVersion { get; }
}

public interface ILayoutProvider
{
    Network ApplyLayout(Network network);
}
