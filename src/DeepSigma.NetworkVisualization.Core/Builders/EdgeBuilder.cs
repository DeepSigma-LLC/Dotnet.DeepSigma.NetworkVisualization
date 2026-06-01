namespace DeepSigma.NetworkVisualization.Builders;

public sealed class EdgeBuilder
{
    private readonly string _id;
    private readonly string _source;
    private readonly string _target;
    private string? _label;
    private double? _weight;
    private Dictionary<string, object?>? _data;

    private Color? _stroke;
    private double? _strokeWidth;
    private LineStyle? _lineStyle;
    private ArrowStyle? _sourceArrow;
    private ArrowStyle? _targetArrow;
    private Color? _labelColor;
    private string? _fontFamily;
    private double? _fontSize;
    private double? _curvature;
    private string? _cssClass;

    internal EdgeBuilder(string id, string source, string target)
    {
        _id = id;
        _source = source;
        _target = target;
    }

    public EdgeBuilder Label(string label) { _label = label; return this; }
    public EdgeBuilder Weight(double weight) { _weight = weight; return this; }
    public EdgeBuilder Data(string key, object? value) { (_data ??= new()).Add(key, value); return this; }

    public EdgeBuilder Stroke(Color color) { _stroke = color; return this; }
    public EdgeBuilder Stroke(string hex) { _stroke = Color.FromHex(hex); return this; }
    public EdgeBuilder StrokeWidth(double width) { _strokeWidth = width; return this; }
    public EdgeBuilder Solid() { _lineStyle = NetworkVisualization.LineStyle.Solid; return this; }
    public EdgeBuilder Dashed() { _lineStyle = NetworkVisualization.LineStyle.Dashed; return this; }
    public EdgeBuilder Dotted() { _lineStyle = NetworkVisualization.LineStyle.Dotted; return this; }
    public EdgeBuilder Line(LineStyle style) { _lineStyle = style; return this; }
    public EdgeBuilder SourceArrow(ArrowStyle arrow) { _sourceArrow = arrow; return this; }
    public EdgeBuilder TargetArrow(ArrowStyle arrow) { _targetArrow = arrow; return this; }
    public EdgeBuilder NoArrows() { _sourceArrow = ArrowStyle.None; _targetArrow = ArrowStyle.None; return this; }
    public EdgeBuilder LabelColor(Color color) { _labelColor = color; return this; }
    public EdgeBuilder LabelColor(string hex) { _labelColor = Color.FromHex(hex); return this; }
    public EdgeBuilder Font(string family, double? size = null) { _fontFamily = family; if (size.HasValue) _fontSize = size; return this; }
    public EdgeBuilder Curvature(double value) { _curvature = value; return this; }
    public EdgeBuilder CssClass(string cssClass) { _cssClass = cssClass; return this; }

    internal Edge Build()
    {
        EdgeAppearance? style = null;
        if (_stroke is not null || _strokeWidth is not null || _lineStyle is not null
            || _sourceArrow is not null || _targetArrow is not null
            || _labelColor is not null || _fontFamily is not null || _fontSize is not null
            || _curvature is not null || _cssClass is not null)
        {
            style = new EdgeAppearance
            {
                Stroke = _stroke,
                StrokeWidth = _strokeWidth ?? 1.0,
                LineStyle = _lineStyle ?? NetworkVisualization.LineStyle.Solid,
                SourceArrow = _sourceArrow ?? ArrowStyle.None,
                TargetArrow = _targetArrow ?? ArrowStyle.Triangle,
                LabelColor = _labelColor,
                FontFamily = _fontFamily,
                FontSize = _fontSize ?? 10,
                Curvature = _curvature ?? 0,
                CssClass = _cssClass,
            };
        }
        return new Edge
        {
            Id = new EdgeId(_id),
            Source = new NodeId(_source),
            Target = new NodeId(_target),
            Label = _label,
            Weight = _weight,
            Style = style,
            Data = _data,
        };
    }
}
