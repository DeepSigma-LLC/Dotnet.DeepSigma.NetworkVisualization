namespace DeepSigma.NetworkVisualization;

public readonly record struct Position(double X, double Y)
{
    public static Position Origin { get; } = new(0, 0);
}

public readonly record struct Size(double Width, double Height)
{
    public static Size Empty { get; } = new(0, 0);
}

public readonly record struct Bounds(double MinX, double MinY, double MaxX, double MaxY)
{
    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;

    public static Bounds FromPoints(IEnumerable<Position> points)
    {
        double minX = double.PositiveInfinity, minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;
        bool any = false;
        foreach (var p in points)
        {
            any = true;
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }
        return any ? new Bounds(minX, minY, maxX, maxY) : new Bounds(0, 0, 0, 0);
    }
}
