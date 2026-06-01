using DeepSigma.NetworkVisualization.Rendering;

namespace DeepSigma.NetworkVisualization.Layouts;

public static class LayoutExtensions
{
    public static Network EnsureLayout(this Network network, bool autoApply = true)
    {
        if (network.Nodes.All(n => n.Position.HasValue)) return network;
        if (!autoApply)
            throw new InvalidOperationException(
                "Network has nodes without positions and autoApply is false. " +
                "Apply a layout provider first or call EnsureLayout(autoApply: true).");
        return LayoutProviders.For(network).ApplyLayout(network);
    }
}
