namespace DeepSigma.NetworkVisualization;

public readonly record struct NodeId(string Value)
{
    public override string ToString() => Value;
    public static implicit operator string(NodeId id) => id.Value;
    public static implicit operator NodeId(string value) => new(value);
}

public readonly record struct EdgeId(string Value)
{
    public override string ToString() => Value;
    public static implicit operator string(EdgeId id) => id.Value;
    public static implicit operator EdgeId(string value) => new(value);
}

public readonly record struct GroupId(string Value)
{
    public override string ToString() => Value;
    public static implicit operator string(GroupId id) => id.Value;
    public static implicit operator GroupId(string value) => new(value);
}
