namespace DeepSigma.NetworkVisualization.Builders;

public sealed class LayoutBuilder
{
    private LayoutAlgorithm _algorithm = LayoutAlgorithm.ForceDirected;
    private LayoutDirection _direction = LayoutDirection.TopToBottom;
    private double _nodeSpacing = 50;
    private double _rankSpacing = 80;
    private double _padding = 20;
    private int? _seed;
    private Dictionary<string, object?>? _options;

    internal LayoutBuilder() { }

    public LayoutBuilder Algorithm(LayoutAlgorithm algorithm) { _algorithm = algorithm; return this; }
    public LayoutBuilder None() { _algorithm = LayoutAlgorithm.None; return this; }
    public LayoutBuilder Grid() { _algorithm = LayoutAlgorithm.Grid; return this; }
    public LayoutBuilder Circular() { _algorithm = LayoutAlgorithm.Circular; return this; }
    public LayoutBuilder Tree() { _algorithm = LayoutAlgorithm.Tree; return this; }
    public LayoutBuilder ForceDirected() { _algorithm = LayoutAlgorithm.ForceDirected; return this; }
    public LayoutBuilder Hierarchical() { _algorithm = LayoutAlgorithm.Hierarchical; return this; }
    public LayoutBuilder Sugiyama() { _algorithm = LayoutAlgorithm.Sugiyama; return this; }
    public LayoutBuilder Radial() { _algorithm = LayoutAlgorithm.Radial; return this; }

    public LayoutBuilder Direction(LayoutDirection direction) { _direction = direction; return this; }
    public LayoutBuilder NodeSpacing(double spacing) { _nodeSpacing = spacing; return this; }
    public LayoutBuilder RankSpacing(double spacing) { _rankSpacing = spacing; return this; }
    public LayoutBuilder Padding(double padding) { _padding = padding; return this; }
    public LayoutBuilder Seed(int seed) { _seed = seed; return this; }
    public LayoutBuilder Option(string key, object? value) { (_options ??= new())[key] = value; return this; }

    internal LayoutSettings Build() => new()
    {
        Algorithm = _algorithm,
        Direction = _direction,
        NodeSpacing = _nodeSpacing,
        RankSpacing = _rankSpacing,
        Padding = _padding,
        RandomSeed = _seed,
        Options = _options,
    };
}

public sealed class InteractionBuilder
{
    private bool _zoom = true, _pan = true, _drag = true, _selection = true, _hover = true, _fit = true;
    private double _minZoom = 0.1, _maxZoom = 5.0;

    internal InteractionBuilder() { }

    public InteractionBuilder Zoom(bool enabled = true) { _zoom = enabled; return this; }
    public InteractionBuilder Pan(bool enabled = true) { _pan = enabled; return this; }
    public InteractionBuilder Drag(bool enabled = true) { _drag = enabled; return this; }
    public InteractionBuilder Selection(bool enabled = true) { _selection = enabled; return this; }
    public InteractionBuilder Hover(bool enabled = true) { _hover = enabled; return this; }
    public InteractionBuilder FitOnLoad(bool enabled = true) { _fit = enabled; return this; }
    public InteractionBuilder ZoomRange(double min, double max) { _minZoom = min; _maxZoom = max; return this; }
    public InteractionBuilder ReadOnly() { _drag = false; _selection = false; return this; }

    internal InteractionSettings Build() => new()
    {
        ZoomEnabled = _zoom,
        PanEnabled = _pan,
        NodeDragEnabled = _drag,
        SelectionEnabled = _selection,
        HoverHighlightEnabled = _hover,
        FitOnLoad = _fit,
        MinZoom = _minZoom,
        MaxZoom = _maxZoom,
    };
}
