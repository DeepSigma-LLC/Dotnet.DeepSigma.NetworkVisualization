namespace DeepSigma.NetworkVisualization.Rendering;

public static class RenderResolutions
{
    public static Color ResolvedFill(this Node node, Theme theme, Color? fallback = null)
        => node.Style?.Fill ?? fallback ?? theme.DefaultNodeFill;

    public static Color ResolvedStroke(this Node node, Theme theme, Color? fallback = null)
        => node.Style?.Stroke ?? fallback ?? theme.DefaultNodeStroke;

    public static Color ResolvedLabelColor(this Node node, Theme theme, Color? fallback = null)
        => node.Style?.LabelColor ?? fallback ?? theme.DefaultLabelColor;

    public static double ResolvedStrokeWidth(this Node node, double fallback = 1.0)
        => node.Style?.StrokeWidth ?? fallback;

    public static NodeShape ResolvedShape(this Node node)
        => node.Style?.Shape ?? NodeShape.Ellipse;

    public static (double Width, double Height) ResolvedSize(this Node node, double defaultWidth, double defaultHeight)
        => (node.Style?.Width ?? defaultWidth, node.Style?.Height ?? defaultHeight);

    public static string ResolvedFontFamily(this Node node, Theme theme)
        => node.Style?.FontFamily ?? theme.DefaultFontFamily;

    public static double ResolvedFontSize(this Node node, Theme theme)
        => node.Style?.FontSize ?? theme.DefaultFontSize;

    public static string ResolvedLabel(this Node node)
        => node.Label ?? node.Id.Value;

    public static Color ResolvedStroke(this Edge edge, Theme theme, Color? fallback = null)
        => edge.Style?.Stroke ?? fallback ?? theme.DefaultEdgeStroke;

    public static double ResolvedStrokeWidth(this Edge edge, double fallback = 1.0)
        => edge.Style?.StrokeWidth ?? fallback;

    public static LineStyle ResolvedLineStyle(this Edge edge)
        => edge.Style?.LineStyle ?? LineStyle.Solid;

    public static ArrowStyle ResolvedTargetArrow(this Edge edge, ArrowStyle directedDefault = ArrowStyle.Triangle)
        => edge.Style?.TargetArrow ?? directedDefault;

    public static ArrowStyle ResolvedSourceArrow(this Edge edge)
        => edge.Style?.SourceArrow ?? ArrowStyle.None;

    public static Color ResolvedLabelColor(this Edge edge, Theme theme, Color? fallback = null)
        => edge.Style?.LabelColor ?? fallback ?? theme.DefaultLabelColor;

    public static double ResolvedFontSize(this Edge edge, double fallback = 10)
        => edge.Style?.FontSize ?? fallback;

    public static bool HasArrowHead(this Edge edge, bool directed)
        => directed && edge.ResolvedTargetArrow() != ArrowStyle.None;
}
