using DeepSigma.NetworkVisualization;
using DeepSigma.NetworkVisualization.Demo.Web;
using DeepSigma.NetworkVisualization.Json;
using DeepSigma.NetworkVisualization.Layout.Msagl;
using DeepSigma.NetworkVisualization.Layouts;
using DeepSigma.NetworkVisualization.Renderers.Cytoscape;
using DeepSigma.NetworkVisualization.Renderers.D3;
using DeepSigma.NetworkVisualization.Renderers.Dot;
using DeepSigma.NetworkVisualization.Renderers.Mermaid;
using DeepSigma.NetworkVisualization.Renderers.ReactFlow;
using DeepSigma.NetworkVisualization.Renderers.SkiaSharp;
using DeepSigma.NetworkVisualization.Renderers.Svg;
using DeepSigma.NetworkVisualization.Rendering;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddSingleton<MermaidRenderer>();
builder.Services.AddSingleton<DotRenderer>();
builder.Services.AddSingleton<SvgRenderer>();
builder.Services.AddSingleton<SkiaRenderer>();
builder.Services.AddSingleton<ReactFlowRenderer>();
builder.Services.AddSingleton<CytoscapeRenderer>();
builder.Services.AddSingleton<D3Renderer>();

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/samples", () =>
    Results.Ok(Samples.All.Keys.Select(name =>
    {
        var net = Samples.All[name]();
        return new { name, title = net.Title ?? name, nodeCount = net.Nodes.Count, edgeCount = net.Edges.Count };
    })));

app.MapGet("/api/samples/{name}/core", (string name) =>
    GetSample(name) is { } net ? Results.Content(NetworkJsonSerializer.Serialize(net), "application/json") : Results.NotFound());

app.MapGet("/api/samples/{name}/mermaid", (string name, MermaidRenderer r) =>
    GetSample(name) is { } net ? Results.Text(r.Render(net), "text/plain") : Results.NotFound());

app.MapGet("/api/samples/{name}/dot", (string name, DotRenderer r) =>
    GetSample(name) is { } net ? Results.Text(r.Render(net), "text/plain") : Results.NotFound());

app.MapGet("/api/samples/{name}/svg", (string name, SvgRenderer r) =>
{
    if (GetSample(name) is not { } net) return Results.NotFound();
    return Results.Content(r.Render(WithMsaglIfNeeded(net)), "image/svg+xml");
});

app.MapGet("/api/samples/{name}/png", (string name, SkiaRenderer r) =>
{
    if (GetSample(name) is not { } net) return Results.NotFound();
    return Results.File(r.Render(WithMsaglIfNeeded(net)), "image/png");
});

app.MapGet("/api/samples/{name}/reactflow", (string name, ReactFlowRenderer r) =>
{
    if (GetSample(name) is not { } net) return Results.NotFound();
    return Results.Content(r.Render(WithMsaglIfNeeded(net)), "application/json");
});

app.MapGet("/api/samples/{name}/cytoscape", (string name, CytoscapeRenderer r) =>
    GetSample(name) is { } net ? Results.Content(r.Render(net), "application/json") : Results.NotFound());

app.MapGet("/api/samples/{name}/d3", (string name, D3Renderer r) =>
    GetSample(name) is { } net ? Results.Content(r.Render(net), "application/json") : Results.NotFound());

app.MapFallbackToFile("index.html");

app.Run();

static Network? GetSample(string name)
    => Samples.All.TryGetValue(name, out var factory) ? factory() : null;

static Network WithMsaglIfNeeded(Network net)
{
    if (net.Nodes.All(n => n.Position.HasValue)) return net;
    ILayoutProvider provider = net.Layout.Algorithm switch
    {
        LayoutAlgorithm.Sugiyama or LayoutAlgorithm.Hierarchical => new MsaglSugiyamaLayoutProvider
        {
            LayerSeparation = net.Layout.RankSpacing,
            NodeSeparation = net.Layout.NodeSpacing,
        },
        LayoutAlgorithm.Mds => new MsaglMdsLayoutProvider(),
        _ => LayoutProviders.For(net),
    };
    return provider.ApplyLayout(net);
}
