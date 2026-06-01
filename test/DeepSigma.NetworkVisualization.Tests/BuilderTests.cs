using DeepSigma.NetworkVisualization.Builders;
using Xunit;

namespace DeepSigma.NetworkVisualization.Tests;

public class BuilderTests
{
    [Fact]
    public void Build_validates_unknown_edge_endpoints()
    {
        var act = () => NetworkBuilder.Create()
            .AddNode("a")
            .AddEdge("a", "missing")
            .Build();

        var ex = Assert.Throws<NetworkValidationException>(act);
        Assert.Contains(ex.Errors, m => m.Contains("missing"));
    }

    [Fact]
    public void Build_rejects_duplicate_node_ids()
    {
        Assert.Throws<InvalidOperationException>(() => NetworkBuilder.Create().AddNode("a").AddNode("a"));
    }

    [Fact]
    public void Build_assigns_group_membership_from_node_InGroup()
    {
        var net = NetworkBuilder.Create()
            .AddNode("a", n => n.InGroup("g1"))
            .Group("g1", g => g.Label("Group 1"))
            .Build();
        Assert.Equal("g1", net.FindNode("a")!.GroupId);
    }

    [Fact]
    public void Build_assigns_group_membership_from_Contains()
    {
        var net = NetworkBuilder.Create()
            .AddNode("a")
            .Group("g1", g => g.Label("Group 1").Contains("a"))
            .Build();
        Assert.Equal("g1", net.FindNode("a")!.GroupId);
    }

    [Fact]
    public void Fluent_smoke_compiles_and_builds()
    {
        var net = NetworkBuilder.Create()
            .Directed()
            .Title("smoke")
            .WithTheme(Theme.Dark)
            .WithLayout(l => l.Hierarchical().Direction(LayoutDirection.LeftToRight).NodeSpacing(60))
            .WithInteraction(i => i.Zoom().Pan().Drag(false))
            .AddNode("a", n => n.Label("A").Shape(NodeShape.Circle).Fill("#4f46e5"))
            .AddNode("b", n => n.Label("B"))
            .AddEdge("a", "b", e => e.Label("knows").Dashed())
            .Build();

        Assert.Equal("smoke", net.Title);
        Assert.True(net.Directed);
        Assert.Equal(2, net.Nodes.Count);
        Assert.Single(net.Edges);
        Assert.False(net.Interaction.NodeDragEnabled);
    }
}
