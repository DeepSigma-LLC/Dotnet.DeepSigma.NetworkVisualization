using DeepSigma.NetworkVisualization.Importers;
using DeepSigma.NetworkVisualization.Json;
using DeepSigma.NetworkVisualization.Samples;
using Xunit;

namespace DeepSigma.NetworkVisualization.Tests;

public class ImporterTests
{
    [Fact]
    public void NetworkImporter_FromJson_round_trips_a_serialized_sample()
    {
        var original = SampleNetworks.OrgChart();
        var serialized = NetworkJsonSerializer.Serialize(original);
        var reread = NetworkImporter.FromJson(serialized);
        Assert.Equal(original.Nodes.Count, reread.Nodes.Count);
        Assert.Equal(original.Edges.Count, reread.Edges.Count);
        Assert.Equal(original.Directed, reread.Directed);
    }

    [Fact]
    public void NetworkImporter_FromJson_throws_with_helpful_message_on_garbage()
    {
        var ex = Assert.Throws<FormatException>(() => NetworkImporter.FromJson("""{"foo": "bar"}"""));
        Assert.Contains("deepsigma.network", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Csv_imports_with_basic_columns_and_extra_data()
    {
        const string nodes = "id,label,color,team\nalice,Alice,#FF0000,sales\nbob,Bob,#00FF00,eng\n";
        const string edges = "source,target,label,weight\nalice,bob,knows,1.5\n";
        var net = NetworkImporter.FromCsv(nodes, edges);
        Assert.Equal(2, net.Nodes.Count);
        Assert.Single(net.Edges);
        Assert.Equal("Alice", net.FindNode("alice")!.Label);
        Assert.NotNull(net.FindNode("alice")!.Data);
        Assert.Equal("sales", net.FindNode("alice")!.Data!["team"]);
        Assert.Equal(1.5, net.Edges[0].Weight);
    }

    [Fact]
    public void Csv_throws_when_required_columns_missing()
    {
        Assert.Throws<FormatException>(() =>
            NetworkImporter.FromCsv("name\nalice\n", "source,target\na,b\n"));
        Assert.Throws<FormatException>(() =>
            NetworkImporter.FromCsv("id\nalice\n", "from,to\na,b\n"));
    }

    private sealed class TestNode
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
        public TestNode? Next { get; set; }
        public List<TestNode> Children { get; } = new();
    }

    [Fact]
    public void FromObject_emits_root_and_walks_primitive_properties()
    {
        var obj = new TestNode { Name = "alpha", Value = 7 };
        var net = NetworkImporter.FromObject(obj);
        // 1 root + 2 leaf value nodes (Name, Value) — Next and Children are null/empty and not emitted by default
        Assert.True(net.Nodes.Count >= 3);
        Assert.Contains(net.Nodes, n => n.Label?.Contains("Name") == true && n.Label.Contains("alpha"));
        Assert.Contains(net.Nodes, n => n.Label?.Contains("Value") == true && n.Label.Contains('7'));
    }

    [Fact]
    public void FromObject_walks_nested_complex_objects()
    {
        var obj = new TestNode { Name = "root", Next = new TestNode { Name = "child", Value = 1 } };
        var net = NetworkImporter.FromObject(obj);
        Assert.Contains(net.Nodes, n => n.Label?.Contains("Next") == true);
        Assert.Contains(net.Nodes, n => n.Label?.Contains("child") == true);
    }

    [Fact]
    public void FromObject_detects_cycles_via_reference_identity()
    {
        var a = new TestNode { Name = "a" };
        var b = new TestNode { Name = "b", Next = a };
        a.Next = b; // a → b → a
        var net = NetworkImporter.FromObject(a);
        // The cycle edge should be dashed with label "ref"
        Assert.Contains(net.Edges, e => e.Label == "ref");
    }

    [Fact]
    public void FromObject_renders_collections_as_groups()
    {
        var obj = new TestNode
        {
            Name = "parent",
            Children = { new TestNode { Name = "c1" }, new TestNode { Name = "c2" } },
        };
        var net = NetworkImporter.FromObject(obj);
        // Group node for Children + two child nodes inside
        Assert.Contains(net.Nodes, n => n.Label?.Contains("Children") == true);
        Assert.Contains(net.Nodes, n => n.Label?.Contains("c1") == true);
        Assert.Contains(net.Nodes, n => n.Label?.Contains("c2") == true);
    }

    [Fact]
    public void FromObject_respects_MaxCollectionItems()
    {
        var obj = new TestNode { Name = "parent" };
        for (int i = 0; i < 20; i++) obj.Children.Add(new TestNode { Name = $"c{i}" });
        var net = NetworkImporter.FromObject(obj, new ObjectGraphOptions { MaxCollectionItems = 3 });
        // 3 children rendered + a "more" placeholder
        Assert.Contains(net.Nodes, n => n.Label == "… +more");
    }

    [Fact]
    public void FromObject_respects_MaxDepth_with_placeholder()
    {
        // Build a chain a → b → c → d → e
        var e = new TestNode { Name = "e" };
        var d = new TestNode { Name = "d", Next = e };
        var c = new TestNode { Name = "c", Next = d };
        var b = new TestNode { Name = "b", Next = c };
        var a = new TestNode { Name = "a", Next = b };
        var net = NetworkImporter.FromObject(a, new ObjectGraphOptions { MaxDepth = 2 });
        Assert.Contains(net.Nodes, n => n.Label == "…");
    }

    [Fact]
    public void FromObject_PropertyFilter_skips_named_properties()
    {
        var obj = new TestNode { Name = "alpha", Value = 7 };
        var net = NetworkImporter.FromObject(obj, new ObjectGraphOptions
        {
            PropertyFilter = p => p.Name != "Value",
        });
        Assert.DoesNotContain(net.Nodes, n => n.Label?.Contains("Value =", StringComparison.Ordinal) == true);
        Assert.Contains(net.Nodes, n => n.Label?.Contains("Name") == true);
    }
}
