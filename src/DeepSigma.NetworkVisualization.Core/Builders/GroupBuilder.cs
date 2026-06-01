namespace DeepSigma.NetworkVisualization.Builders;

public sealed class GroupBuilder
{
    private readonly string _id;
    private string? _label;
    private string? _parentGroupId;
    private bool _collapsed;
    private NodeStyle? _style;
    private readonly List<string> _members = [];

    internal GroupBuilder(string id) { _id = id; }

    public GroupBuilder Label(string label) { _label = label; return this; }
    public GroupBuilder Parent(string parentGroupId) { _parentGroupId = parentGroupId; return this; }
    public GroupBuilder Collapsed(bool collapsed = true) { _collapsed = collapsed; return this; }
    public GroupBuilder Fill(string hex) { _style = (_style ?? new NodeStyle()) with { Fill = Color.FromHex(hex) }; return this; }
    public GroupBuilder Stroke(string hex) { _style = (_style ?? new NodeStyle()) with { Stroke = Color.FromHex(hex) }; return this; }
    public GroupBuilder Contains(params string[] nodeIds) { _members.AddRange(nodeIds); return this; }

    internal Group Build() => new()
    {
        Id = _id,
        Label = _label,
        ParentGroupId = _parentGroupId,
        Collapsed = _collapsed,
        Style = _style,
        MemberNodeIds = _members.ToArray(),
    };
}
