using DeepSigma.NetworkVisualization.Layout.Msagl;
using DeepSigma.NetworkVisualization.Renderers.Cytoscape;
using DeepSigma.NetworkVisualization.Renderers.D3;
using DeepSigma.NetworkVisualization.Renderers.Dot;
using DeepSigma.NetworkVisualization.Renderers.Mermaid;
using DeepSigma.NetworkVisualization.Renderers.ReactFlow;
using DeepSigma.NetworkVisualization.Renderers.SkiaSharp;
using DeepSigma.NetworkVisualization.Renderers.Svg;
using DeepSigma.NetworkVisualization.Rendering;
using DeepSigma.NetworkVisualization.Samples;

MsaglLayouts.Register();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services
    .AddCoreJsonRenderer()
    .AddMermaidRenderer()
    .AddDotRenderer()
    .AddSvgRenderer()
    .AddSkiaRenderer()
    .AddReactFlowRenderer()
    .AddCytoscapeRenderer()
    .AddD3Renderer();

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/samples", () =>
    SampleNetworks.All.Keys.Select(name =>
    {
        var net = SampleNetworks.All[name]();
        return new { name, title = net.Title ?? name, nodeCount = net.Nodes.Count, edgeCount = net.Edges.Count };
    }));

foreach (var descriptor in app.Services.GetServices<RendererDescriptor>())
{
    var desc = descriptor;
    app.MapGet($"/api/samples/{{name}}/{desc.Metadata.FormatId}",
        (HttpContext ctx, string name) =>
        {
            if (!SampleNetworks.All.TryGetValue(name, out var factory)) return Results.NotFound();
            return desc.Render(factory(), ctx.RequestServices) switch
            {
                TextOutput t => Results.Content(t.Content, t.MimeType),
                BinaryOutput b => Results.File(b.Bytes, b.MimeType),
                _ => Results.StatusCode(500),
            };
        });
}

app.MapFallbackToFile("index.html");

app.Run();
