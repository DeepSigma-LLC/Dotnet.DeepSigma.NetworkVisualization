using DeepSigma.NetworkVisualization;
using DeepSigma.NetworkVisualization.Demo.Web;
using DeepSigma.NetworkVisualization.Json;
using DeepSigma.NetworkVisualization.Layout.Msagl;
using DeepSigma.NetworkVisualization.Renderers.Cytoscape;
using DeepSigma.NetworkVisualization.Renderers.D3;
using DeepSigma.NetworkVisualization.Renderers.Dot;
using DeepSigma.NetworkVisualization.Renderers.Mermaid;
using DeepSigma.NetworkVisualization.Renderers.ReactFlow;
using DeepSigma.NetworkVisualization.Renderers.SkiaSharp;
using DeepSigma.NetworkVisualization.Renderers.Svg;
using Microsoft.AspNetCore.Http;

MsaglLayouts.Register();

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
    Samples.All.Keys.Select(name =>
    {
        var net = Samples.All[name]();
        return new { name, title = net.Title ?? name, nodeCount = net.Nodes.Count, edgeCount = net.Edges.Count };
    }));

MapText<MermaidRenderer>("mermaid", "text/plain", (r, n) => r.Render(n));
MapText<DotRenderer>("dot", "text/plain", (r, n) => r.Render(n));
MapText<SvgRenderer>("svg", "image/svg+xml", (r, n) => r.Render(n));
MapText<ReactFlowRenderer>("reactflow", "application/json", (r, n) => r.Render(n));
MapText<CytoscapeRenderer>("cytoscape", "application/json", (r, n) => r.Render(n));
MapText<D3Renderer>("d3", "application/json", (r, n) => r.Render(n));
MapText<object>("core", "application/json", (_, n) => NetworkJsonSerializer.Serialize(n));
MapBinary<SkiaRenderer>("png", "image/png", (r, n) => r.Render(n));

app.MapFallbackToFile("index.html");

app.Run();

void MapText<TRenderer>(string suffix, string contentType, Func<TRenderer, Network, string> render) where TRenderer : class =>
    app.MapGet($"/api/samples/{{name}}/{suffix}",
        (HttpContext ctx, string name) => Samples.All.TryGetValue(name, out var factory)
            ? Results.Content(render(ctx.RequestServices.GetService<TRenderer>()!, factory()), contentType)
            : Results.NotFound());

void MapBinary<TRenderer>(string suffix, string contentType, Func<TRenderer, Network, byte[]> render) where TRenderer : class =>
    app.MapGet($"/api/samples/{{name}}/{suffix}",
        (HttpContext ctx, string name) => Samples.All.TryGetValue(name, out var factory)
            ? Results.File(render(ctx.RequestServices.GetService<TRenderer>()!, factory()), contentType)
            : Results.NotFound());
