using System.Text.Json;
using DeepSigma.NetworkVisualization.Layout.Msagl;
using DeepSigma.NetworkVisualization.Layouts;
using DeepSigma.NetworkVisualization.Renderers.Cytoscape;
using DeepSigma.NetworkVisualization.Renderers.D3;
using DeepSigma.NetworkVisualization.Renderers.Dot;
using DeepSigma.NetworkVisualization.Renderers.Mermaid;
using DeepSigma.NetworkVisualization.Renderers.ReactFlow;
using DeepSigma.NetworkVisualization.Renderers.SkiaSharp;
using DeepSigma.NetworkVisualization.Renderers.Svg;
using Xunit;

namespace DeepSigma.NetworkVisualization.Tests;

public class RendererTests
{
    [Fact]
    public void Mermaid_emits_flowchart_with_direction_nodes_and_edges()
    {
        var output = new MermaidRenderer().Render(Samples.OrgChart());
        Assert.Contains("flowchart TD", output, StringComparison.Ordinal);
        Assert.Contains("ceo", output, StringComparison.Ordinal);
        var flat = output.Replace("\r", "").Replace("\n", " ");
        Assert.Contains("ceo -->", flat, StringComparison.Ordinal);
        Assert.Contains("| cto", flat, StringComparison.Ordinal);
    }

    [Fact]
    public void Dot_emits_digraph_with_clusters()
    {
        var output = new DotRenderer().Render(Samples.Clusters());
        Assert.StartsWith("digraph", output, StringComparison.Ordinal);
        Assert.Contains("cluster_frontend", output, StringComparison.Ordinal);
        Assert.Contains("\"api\" -> \"svc1\"", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Svg_emits_well_formed_svg_with_nodes()
    {
        var output = new SvgRenderer().Render(Samples.OrgChart());
        Assert.StartsWith("<?xml", output, StringComparison.Ordinal);
        Assert.Contains("<svg", output, StringComparison.Ordinal);
        Assert.Contains("</svg>", output, StringComparison.Ordinal);
        Assert.Contains("CEO", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Skia_emits_png_bytes()
    {
        var bytes = new SkiaRenderer().Render(Samples.OrgChart());
        Assert.NotEmpty(bytes);
        // PNG magic
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, bytes.Take(4).ToArray());
    }

    [Fact]
    public void ReactFlow_emits_nodes_and_edges_with_positions()
    {
        var json = new ReactFlowRenderer().Render(Samples.OrgChart());
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("reactflow", root.GetProperty("format").GetString());
        var first = root.GetProperty("nodes").EnumerateArray().First();
        Assert.True(first.GetProperty("position").TryGetProperty("x", out _));
    }

    [Fact]
    public void Cytoscape_emits_elements_with_groups()
    {
        var json = new CytoscapeRenderer().Render(Samples.Clusters());
        using var doc = JsonDocument.Parse(json);
        var nodes = doc.RootElement.GetProperty("elements").GetProperty("nodes");
        Assert.Contains(nodes.EnumerateArray(),
            n => n.GetProperty("classes").GetString() == "group");
    }

    [Fact]
    public void D3_emits_nodes_and_links_arrays()
    {
        var json = new D3Renderer().Render(Samples.SocialNetwork());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("d3-force", doc.RootElement.GetProperty("format").GetString());
        Assert.True(doc.RootElement.GetProperty("nodes").GetArrayLength() > 0);
        Assert.True(doc.RootElement.GetProperty("links").GetArrayLength() > 0);
    }

    [Fact]
    public void Msagl_sugiyama_populates_positions()
    {
        var net = Samples.Pipeline();
        var positioned = new MsaglSugiyamaLayoutProvider().ApplyLayout(net);
        Assert.All(positioned.Nodes, n => Assert.NotNull(n.Position));
    }

    [Fact]
    public void Layouts_For_returns_built_in_provider_per_algorithm()
    {
        Assert.IsType<GridLayoutProvider>(LayoutProviders.For(new LayoutSettings { Algorithm = LayoutAlgorithm.Grid }));
        Assert.IsType<CircularLayoutProvider>(LayoutProviders.For(new LayoutSettings { Algorithm = LayoutAlgorithm.Circular }));
        Assert.IsType<TreeLayoutProvider>(LayoutProviders.For(new LayoutSettings { Algorithm = LayoutAlgorithm.Tree }));
        Assert.IsType<SimpleForceDirectedLayoutProvider>(LayoutProviders.For(new LayoutSettings { Algorithm = LayoutAlgorithm.ForceDirected }));
    }

    [Fact]
    public void EnsureLayout_populates_positions_when_missing()
    {
        var net = Samples.OrgChart();
        Assert.Contains(net.Nodes, n => !n.Position.HasValue);
        var positioned = net.EnsureLayout();
        Assert.All(positioned.Nodes, n => Assert.NotNull(n.Position));
    }

    [Fact]
    public void EnsureLayout_throws_when_autoApply_is_false_and_positions_missing()
    {
        var net = Samples.OrgChart();
        Assert.Throws<InvalidOperationException>(() => net.EnsureLayout(autoApply: false));
    }

    [Fact]
    public void MsaglLayouts_Register_overrides_Sugiyama_factory()
    {
        MsaglLayouts.Register();
        try
        {
            var p = LayoutProviders.For(new LayoutSettings { Algorithm = LayoutAlgorithm.Sugiyama });
            Assert.IsType<MsaglSugiyamaLayoutProvider>(p);
        }
        finally
        {
            LayoutProviders.Register(LayoutAlgorithm.Sugiyama, s => new TreeLayoutProvider { LevelGap = s.RankSpacing, SiblingGap = s.NodeSpacing });
        }
    }
}
