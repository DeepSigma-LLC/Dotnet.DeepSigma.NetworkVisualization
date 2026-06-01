namespace DeepSigma.NetworkVisualization.Builders;

public sealed class NetworkBuilder
{
    private bool _directed = true;
    private string? _title;
    private Theme _theme = Theme.Light;
    private LayoutSettings _layout = LayoutSettings.Default;
    private InteractionSettings _interaction = InteractionSettings.Default;
    private Dictionary<string, object?>? _metadata;

    private readonly Dictionary<string, NodeBuilder> _nodes = [];
    private readonly List<EdgeBuilder> _edges = [];
    private readonly Dictionary<string, GroupBuilder> _groups = [];
    private int _edgeCounter;

    public static NetworkBuilder Create() => new();

    public NetworkBuilder Directed(bool directed = true) { _directed = directed; return this; }
    public NetworkBuilder Undirected() { _directed = false; return this; }
    public NetworkBuilder Title(string title) { _title = title; return this; }
    public NetworkBuilder WithTheme(Theme theme) { _theme = theme; return this; }
    public NetworkBuilder WithTheme(Action<ThemeBuilder> configure)
    {
        var b = new ThemeBuilder(_theme);
        configure(b);
        _theme = b.Build();
        return this;
    }
    public NetworkBuilder WithLayout(Action<LayoutBuilder> configure)
    {
        var b = new LayoutBuilder();
        configure(b);
        _layout = b.Build();
        return this;
    }
    public NetworkBuilder WithInteraction(Action<InteractionBuilder> configure)
    {
        var b = new InteractionBuilder();
        configure(b);
        _interaction = b.Build();
        return this;
    }
    public NetworkBuilder Metadata(string key, object? value) { (_metadata ??= new())[key] = value; return this; }

    public NetworkBuilder AddNode(string id) => AddNode(id, _ => { });
    public NetworkBuilder AddNode(string id, Action<NodeBuilder> configure)
    {
        if (_nodes.ContainsKey(id))
            throw new InvalidOperationException($"Node '{id}' already added.");
        var nb = new NodeBuilder(id);
        configure(nb);
        _nodes[id] = nb;
        return this;
    }

    public NetworkBuilder AddNodes(params string[] ids)
    {
        foreach (var id in ids) AddNode(id);
        return this;
    }

    public NetworkBuilder AddEdge(string source, string target) => AddEdge(source, target, _ => { });
    public NetworkBuilder AddEdge(string source, string target, Action<EdgeBuilder> configure)
        => AddEdge($"e{++_edgeCounter}", source, target, configure);

    public NetworkBuilder AddEdge(string id, string source, string target, Action<EdgeBuilder> configure)
    {
        var eb = new EdgeBuilder(id, source, target);
        configure(eb);
        _edges.Add(eb);
        return this;
    }

    public NetworkBuilder Group(string id, Action<GroupBuilder> configure)
    {
        if (!_groups.TryGetValue(id, out var gb))
        {
            gb = new GroupBuilder(id);
            _groups[id] = gb;
        }
        configure(gb);
        return this;
    }

    public Network Build()
    {
        var errors = new List<string>();
        var nodes = _nodes.Values.Select(n => n.Build()).ToArray();
        var nodeIds = new HashSet<string>(nodes.Select(n => n.Id.Value));
        var edges = _edges.Select(e => e.Build()).ToArray();
        var groups = _groups.Values.Select(g => g.Build()).ToArray();
        var groupIds = new HashSet<string>(groups.Select(g => g.Id));

        var edgeIds = new HashSet<string>();
        foreach (var e in edges)
        {
            if (!edgeIds.Add(e.Id.Value)) errors.Add($"Duplicate edge id '{e.Id}'.");
            if (!nodeIds.Contains(e.Source.Value)) errors.Add($"Edge '{e.Id}' source '{e.Source}' is not a known node.");
            if (!nodeIds.Contains(e.Target.Value)) errors.Add($"Edge '{e.Id}' target '{e.Target}' is not a known node.");
        }

        foreach (var n in nodes)
        {
            if (n.GroupId is { } gid && !groupIds.Contains(gid))
                errors.Add($"Node '{n.Id}' references unknown group '{gid}'.");
        }

        foreach (var g in groups)
        {
            foreach (var m in g.MemberNodeIds)
                if (!nodeIds.Contains(m)) errors.Add($"Group '{g.Id}' member '{m}' is not a known node.");
            if (g.ParentGroupId is { } pid && !groupIds.Contains(pid))
                errors.Add($"Group '{g.Id}' parent '{pid}' is not a known group.");
        }

        if (HasGroupCycle(groups, out var cyclePath))
            errors.Add($"Group hierarchy contains a cycle: {cyclePath}.");

        if (errors.Count > 0)
            throw new NetworkValidationException(errors);

        var nodesWithGroups = ApplyGroupMembership(nodes, groups);

        return new Network
        {
            Directed = _directed,
            Title = _title,
            Theme = _theme,
            Layout = _layout,
            Interaction = _interaction,
            Nodes = nodesWithGroups,
            Edges = edges,
            Groups = groups,
            Metadata = _metadata,
        };
    }

    private static Node[] ApplyGroupMembership(Node[] nodes, Group[] groups)
    {
        var nodeToGroup = new Dictionary<string, string>();
        foreach (var g in groups)
            foreach (var m in g.MemberNodeIds)
                nodeToGroup[m] = g.Id;

        var result = new Node[nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
        {
            var n = nodes[i];
            result[i] = n.GroupId is null && nodeToGroup.TryGetValue(n.Id.Value, out var gid)
                ? n with { GroupId = gid }
                : n;
        }
        return result;
    }

    private static bool HasGroupCycle(Group[] groups, out string path)
    {
        var byId = groups.ToDictionary(g => g.Id);
        foreach (var start in groups)
        {
            var seen = new List<string>();
            var cur = start.Id;
            while (cur is not null)
            {
                if (seen.Contains(cur))
                {
                    path = string.Join(" -> ", seen) + " -> " + cur;
                    return true;
                }
                seen.Add(cur);
                cur = byId.TryGetValue(cur, out var g) ? g.ParentGroupId : null;
            }
        }
        path = string.Empty;
        return false;
    }
}

public sealed class ThemeBuilder
{
    private Theme _theme;
    internal ThemeBuilder(Theme baseline) { _theme = baseline; }

    public ThemeBuilder Background(string hex) { _theme = _theme with { Background = Color.FromHex(hex) }; return this; }
    public ThemeBuilder NodeFill(string hex) { _theme = _theme with { DefaultNodeFill = Color.FromHex(hex) }; return this; }
    public ThemeBuilder NodeStroke(string hex) { _theme = _theme with { DefaultNodeStroke = Color.FromHex(hex) }; return this; }
    public ThemeBuilder EdgeStroke(string hex) { _theme = _theme with { DefaultEdgeStroke = Color.FromHex(hex) }; return this; }
    public ThemeBuilder LabelColor(string hex) { _theme = _theme with { DefaultLabelColor = Color.FromHex(hex) }; return this; }
    public ThemeBuilder Font(string family, double size = 12) { _theme = _theme with { DefaultFontFamily = family, DefaultFontSize = size }; return this; }
    public ThemeBuilder Dark() { _theme = Theme.Dark; return this; }
    public ThemeBuilder Light() { _theme = Theme.Light; return this; }

    internal Theme Build() => _theme;
}
