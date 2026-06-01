namespace DeepSigma.NetworkVisualization;

public enum NodeShape
{
    Ellipse,
    Circle,
    Rectangle,
    RoundedRectangle,
    Diamond,
    Hexagon,
    Triangle,
    Parallelogram,
    Cylinder,
    Custom
}

public enum LineStyle
{
    Solid,
    Dashed,
    Dotted
}

public enum ArrowStyle
{
    None,
    Triangle,
    Open,
    Diamond,
    Circle,
    Vee
}

public enum LayoutAlgorithm
{
    None,
    Grid,
    Circular,
    Tree,
    ForceDirected,
    Hierarchical,
    Sugiyama,
    Radial,
    Mds
}

public enum LayoutDirection
{
    TopToBottom,
    BottomToTop,
    LeftToRight,
    RightToLeft
}
