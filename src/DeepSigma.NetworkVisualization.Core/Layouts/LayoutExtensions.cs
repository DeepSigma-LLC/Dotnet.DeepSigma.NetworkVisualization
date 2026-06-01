using DeepSigma.NetworkVisualization.Rendering;

namespace DeepSigma.NetworkVisualization.Layouts;

/// <summary>Extension methods on <see cref="Network"/> related to layout.</summary>
public static class LayoutExtensions
{
    /// <summary>
    /// Returns the network unchanged if every node already has a <see cref="Node.Position"/>; otherwise runs the
    /// provider returned by <see cref="LayoutProviders.For(Network)"/> and returns a new network with positions populated.
    /// </summary>
    /// <param name="network">The network to ensure positions for.</param>
    /// <param name="autoApply">When <c>false</c> and positions are missing, throws instead of running a layout.</param>
    /// <exception cref="InvalidOperationException">Positions are missing and <paramref name="autoApply"/> is <c>false</c>.</exception>
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
