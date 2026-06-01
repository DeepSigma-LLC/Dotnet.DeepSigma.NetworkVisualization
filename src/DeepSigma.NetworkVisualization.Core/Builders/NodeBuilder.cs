namespace DeepSigma.NetworkVisualization.Builders;

public sealed class NodeBuilder
{
    private readonly string _id;
    private string? _label;
    private Position? _position;
    private string? _groupId;
    private string? _tooltip;
    private string? _url;
    private Dictionary<string, object?>? _data;

    private Color? _fill;
    private Color? _stroke;
    private double? _strokeWidth;
    private NodeShape? _shape;
    private Color? _labelColor;
    private string? _fontFamily;
    private double? _fontSize;
    private double? _width;
    private double? _height;
    private string? _icon;
    private string? _cssClass;
    private Dictionary<string, string>? _customAttrs;

    internal NodeBuilder(string id) { _id = id; }

    public NodeBuilder Label(string label) { _label = label; return this; }
    public NodeBuilder At(double x, double y) { _position = new Position(x, y); return this; }
    public NodeBuilder At(Position position) { _position = position; return this; }
    public NodeBuilder InGroup(string groupId) { _groupId = groupId; return this; }
    public NodeBuilder Tooltip(string text) { _tooltip = text; return this; }
    public NodeBuilder Url(string url) { _url = url; return this; }
    public NodeBuilder Data(string key, object? value) { (_data ??= new()).Add(key, value); return this; }

    public NodeBuilder Shape(NodeShape shape) { _shape = shape; return this; }
    public NodeBuilder Fill(Color color) { _fill = color; return this; }
    public NodeBuilder Fill(string hex) { _fill = Color.FromHex(hex); return this; }
    public NodeBuilder Stroke(Color color) { _stroke = color; return this; }
    public NodeBuilder Stroke(string hex) { _stroke = Color.FromHex(hex); return this; }
    public NodeBuilder StrokeWidth(double width) { _strokeWidth = width; return this; }
    public NodeBuilder LabelColor(Color color) { _labelColor = color; return this; }
    public NodeBuilder LabelColor(string hex) { _labelColor = Color.FromHex(hex); return this; }
    public NodeBuilder Font(string family, double? size = null) { _fontFamily = family; if (size.HasValue) _fontSize = size; return this; }
    public NodeBuilder FontSize(double size) { _fontSize = size; return this; }
    public NodeBuilder Size(double width, double height) { _width = width; _height = height; return this; }
    public NodeBuilder Icon(string icon) { _icon = icon; return this; }
    public NodeBuilder CssClass(string cssClass) { _cssClass = cssClass; return this; }
    public NodeBuilder Attr(string key, string value) { (_customAttrs ??= new())[key] = value; return this; }

    internal Node Build()
    {
        NodeStyle? style = null;
        if (_fill is not null || _stroke is not null || _strokeWidth is not null || _shape is not null
            || _labelColor is not null || _fontFamily is not null || _fontSize is not null
            || _width is not null || _height is not null || _icon is not null
            || _cssClass is not null || _customAttrs is not null)
        {
            style = new NodeStyle
            {
                Fill = _fill,
                Stroke = _stroke,
                StrokeWidth = _strokeWidth ?? 1.0,
                Shape = _shape ?? NodeShape.Ellipse,
                LabelColor = _labelColor,
                FontFamily = _fontFamily,
                FontSize = _fontSize ?? 12,
                Width = _width,
                Height = _height,
                Icon = _icon,
                CssClass = _cssClass,
                CustomAttributes = _customAttrs,
            };
        }
        return new Node
        {
            Id = new NodeId(_id),
            Label = _label,
            Style = style,
            Position = _position,
            GroupId = _groupId,
            Tooltip = _tooltip,
            Url = _url,
            Data = _data,
        };
    }
}
