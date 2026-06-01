namespace DeepSigma.NetworkVisualization;

public sealed record NodeStyle
{
    public Color? Fill { get; init; }
    public Color? Stroke { get; init; }
    public double StrokeWidth { get; init; } = 1.0;
    public NodeShape Shape { get; init; } = NodeShape.Ellipse;
    public Color? LabelColor { get; init; }
    public string? FontFamily { get; init; }
    public double FontSize { get; init; } = 12;
    public double? Width { get; init; }
    public double? Height { get; init; }
    public string? Icon { get; init; }
    public string? CssClass { get; init; }
    public IReadOnlyDictionary<string, string>? CustomAttributes { get; init; }
}

public sealed record EdgeAppearance
{
    public Color? Stroke { get; init; }
    public double StrokeWidth { get; init; } = 1.0;
    public LineStyle LineStyle { get; init; } = LineStyle.Solid;
    public ArrowStyle SourceArrow { get; init; } = ArrowStyle.None;
    public ArrowStyle TargetArrow { get; init; } = ArrowStyle.Triangle;
    public Color? LabelColor { get; init; }
    public string? FontFamily { get; init; }
    public double FontSize { get; init; } = 10;
    public double Curvature { get; init; }
    public string? CssClass { get; init; }
}

public sealed record Theme
{
    public Color Background { get; init; } = Color.White;
    public Color DefaultNodeFill { get; init; } = Color.FromHex("#E3F2FD");
    public Color DefaultNodeStroke { get; init; } = Color.FromHex("#1976D2");
    public Color DefaultEdgeStroke { get; init; } = Color.FromHex("#616161");
    public Color DefaultLabelColor { get; init; } = Color.FromHex("#212121");
    public string DefaultFontFamily { get; init; } = "Segoe UI, sans-serif";
    public double DefaultFontSize { get; init; } = 12;

    public static Theme Light { get; } = new();

    public static Theme Dark { get; } = new()
    {
        Background = Color.FromHex("#0F172A"),
        DefaultNodeFill = Color.FromHex("#1E293B"),
        DefaultNodeStroke = Color.FromHex("#64748B"),
        DefaultEdgeStroke = Color.FromHex("#94A3B8"),
        DefaultLabelColor = Color.FromHex("#F1F5F9"),
    };
}
