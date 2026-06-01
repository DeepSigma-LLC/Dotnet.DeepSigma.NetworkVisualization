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
}
