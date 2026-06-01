namespace DeepSigma.NetworkVisualization.Rendering;

/// <summary>
/// Pure-geometry resolver for <see cref="NodeShape"/>. Returns the points or dimensions a renderer needs to draw
/// a shape centered at <c>(cx, cy)</c> with extent <c>(w, h)</c>, but does not produce any output format.
/// Renderers (SVG, Skia) call this and format the result in their own output dialect, so a new shape only
/// needs the math added in one place.
/// </summary>
public static class ShapeGeometry
{
    /// <summary>Vertices for a diamond (rotated square), starting top and going clockwise.</summary>
    public static Position[] Diamond(double cx, double cy, double w, double h) => new[]
    {
        new Position(cx, cy - h / 2),
        new Position(cx + w / 2, cy),
        new Position(cx, cy + h / 2),
        new Position(cx - w / 2, cy),
    };

    /// <summary>Vertices for a flat-topped hexagon inscribed in <paramref name="w"/>×<paramref name="h"/>.</summary>
    public static Position[] Hexagon(double cx, double cy, double w, double h)
    {
        var hw = w / 2; var hh = h / 2; var qw = w / 4;
        return new[]
        {
            new Position(cx - hw + qw, cy - hh),
            new Position(cx + hw - qw, cy - hh),
            new Position(cx + hw, cy),
            new Position(cx + hw - qw, cy + hh),
            new Position(cx - hw + qw, cy + hh),
            new Position(cx - hw, cy),
        };
    }

    /// <summary>Vertices for an upward-pointing isoceles triangle inscribed in <paramref name="w"/>×<paramref name="h"/>.</summary>
    public static Position[] Triangle(double cx, double cy, double w, double h) => new[]
    {
        new Position(cx, cy - h / 2),
        new Position(cx + w / 2, cy + h / 2),
        new Position(cx - w / 2, cy + h / 2),
    };

    /// <summary>
    /// Returns the polygon vertices for a shape, or <c>null</c> if the shape is not polygonal
    /// (e.g. <see cref="NodeShape.Circle"/>, <see cref="NodeShape.Ellipse"/>, <see cref="NodeShape.Rectangle"/> —
    /// renderers handle those with their native primitives).
    /// </summary>
    public static Position[]? PolygonFor(NodeShape shape, double cx, double cy, double w, double h) => shape switch
    {
        NodeShape.Diamond => Diamond(cx, cy, w, h),
        NodeShape.Hexagon => Hexagon(cx, cy, w, h),
        NodeShape.Triangle => Triangle(cx, cy, w, h),
        _ => null,
    };

    /// <summary>Radius for the inscribed circle (used by <see cref="NodeShape.Circle"/>).</summary>
    public static double CircleRadius(double w, double h) => Math.Min(w, h) / 2;
}
