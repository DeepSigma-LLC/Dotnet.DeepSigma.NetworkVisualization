using System.Text.Json;
using DeepSigma.NetworkVisualization.Json;
using Xunit;

namespace DeepSigma.NetworkVisualization.Tests;

public class JsonTests
{
    [Fact]
    public void Round_trip_preserves_basic_structure()
    {
        var original = Samples.OrgChart();
        var json = NetworkJsonSerializer.Serialize(original);
        var back = NetworkJsonSerializer.Deserialize(json);

        Assert.NotNull(back);
        Assert.Equal(original.Nodes.Count, back!.Nodes.Count);
        Assert.Equal(original.Edges.Count, back.Edges.Count);
        Assert.Equal(original.Directed, back.Directed);
    }

    [Fact]
    public void Serialized_envelope_has_format_metadata()
    {
        var json = NetworkJsonSerializer.Serialize(Samples.OrgChart());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("deepsigma.network", doc.RootElement.GetProperty("format").GetString());
        Assert.Equal("1.0", doc.RootElement.GetProperty("version").GetString());
        Assert.True(doc.RootElement.GetProperty("network").GetProperty("nodes").GetArrayLength() > 0);
    }

    [Fact]
    public void Color_is_serialized_as_hex_with_alpha()
    {
        var net = Samples.OrgChart();
        var json = NetworkJsonSerializer.Serialize(net);
        Assert.Contains("#1976D2FF", json);
    }
}
