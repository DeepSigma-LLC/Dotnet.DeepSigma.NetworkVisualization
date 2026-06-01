namespace DeepSigma.NetworkVisualization;

public sealed record LayoutSettings
{
    public LayoutAlgorithm Algorithm { get; init; } = LayoutAlgorithm.ForceDirected;
    public LayoutDirection Direction { get; init; } = LayoutDirection.TopToBottom;
    public double NodeSpacing { get; init; } = 50;
    public double RankSpacing { get; init; } = 80;
    public double Padding { get; init; } = 20;
    public int? RandomSeed { get; init; }
    public IReadOnlyDictionary<string, object?>? Options { get; init; }

    public static LayoutSettings Default { get; } = new();
}

public sealed record InteractionSettings
{
    public bool ZoomEnabled { get; init; } = true;
    public bool PanEnabled { get; init; } = true;
    public bool NodeDragEnabled { get; init; } = true;
    public bool SelectionEnabled { get; init; } = true;
    public bool HoverHighlightEnabled { get; init; } = true;
    public bool FitOnLoad { get; init; } = true;
    public double MinZoom { get; init; } = 0.1;
    public double MaxZoom { get; init; } = 5.0;

    public static InteractionSettings Default { get; } = new();
}
