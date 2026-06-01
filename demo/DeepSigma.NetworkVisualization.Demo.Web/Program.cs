using DeepSigma.NetworkVisualization;
using DeepSigma.NetworkVisualization.Demo.Web;
using DeepSigma.NetworkVisualization.Importers;
using DeepSigma.NetworkVisualization.Json;
using DeepSigma.NetworkVisualization.Layout.Msagl;
using DeepSigma.NetworkVisualization.Renderers.Cytoscape;
using DeepSigma.NetworkVisualization.Renderers.D3;
using DeepSigma.NetworkVisualization.Renderers.Dot;
using DeepSigma.NetworkVisualization.Renderers.Mermaid;
using DeepSigma.NetworkVisualization.Renderers.ReactFlow;
using DeepSigma.NetworkVisualization.Renderers.Sigma;
using DeepSigma.NetworkVisualization.Renderers.SkiaSharp;
using DeepSigma.NetworkVisualization.Renderers.Svg;
using DeepSigma.NetworkVisualization.Rendering;
using DeepSigma.NetworkVisualization.Samples;

MsaglLayouts.Register();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddSingleton<EditStore>();
builder.Services.AddSingleton<ImportStore>();

builder.Services
    .AddCoreJsonRenderer()
    .AddMermaidRenderer()
    .AddDotRenderer()
    .AddSvgRenderer()
    .AddSkiaRenderer()
    .AddReactFlowRenderer()
    .AddCytoscapeRenderer()
    .AddD3Renderer()
    .AddSigmaRenderer();

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

Network? Resolve(string name, EditStore edits, ImportStore imports, string? theme)
{
    Network? net = null;
    if (imports.TryGet(name, out var imported)) net = imported;
    else if (SampleNetworks.All.TryGetValue(name, out var factory))
        net = edits.TryGet(name, out var edited) ? edited : factory();
    if (net is null) return null;
    if (string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase))
        net = net with { Theme = Theme.Dark };
    return net;
}

app.MapGet("/api/samples", (EditStore edits, ImportStore imports) =>
{
    var builtIns = SampleNetworks.All.Keys.Select(name =>
    {
        var net = edits.TryGet(name, out var edited) ? edited : SampleNetworks.All[name]();
        return new
        {
            name,
            title = net.Title ?? name,
            nodeCount = net.Nodes.Count,
            edgeCount = net.Edges.Count,
            edited = edits.TryGet(name, out _),
            imported = false,
        };
    });
    var importedItems = imports.All.Select(kv => new
    {
        name = kv.Key,
        title = kv.Value.Title ?? kv.Key,
        nodeCount = kv.Value.Nodes.Count,
        edgeCount = kv.Value.Edges.Count,
        edited = false,
        imported = true,
    });
    return builtIns.Concat(importedItems);
});

app.MapGet("/api/edit/{name}/status",
    (string name, EditStore store) => Results.Ok(new { edited = store.HasEdit(name) }));

app.MapPost("/api/edit/{name}",
    async (string name, HttpContext ctx, EditStore store) =>
    {
        if (!SampleNetworks.All.ContainsKey(name)) return Results.NotFound();
        using var reader = new StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var net = NetworkJsonSerializer.Deserialize(body);
        if (net is null) return Results.BadRequest(new { error = "Could not deserialize Core network JSON." });
        store.Set(name, net);
        return Results.Ok(new { saved = true, nodeCount = net.Nodes.Count, edgeCount = net.Edges.Count });
    });

app.MapDelete("/api/edit/{name}",
    (string name, EditStore store) => Results.Ok(new { cleared = store.Clear(name) }));

app.MapPost("/api/import",
    async (HttpContext ctx, string format, string? title, ImportStore store) =>
    {
        using var reader = new StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        Network net;
        try
        {
            net = format.ToLowerInvariant() switch
            {
                "json" => NetworkImporter.FromJson(body),
                "csv" => ParseCsvBody(body),
                _ => throw new FormatException($"Unknown format '{format}'. Supported: 'json', 'csv'."),
            };
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        if (!string.IsNullOrWhiteSpace(title))
            net = net with { Title = title };
        var id = store.Add(net);
        return Results.Ok(new { id, nodeCount = net.Nodes.Count, edgeCount = net.Edges.Count });
    });

app.MapDelete("/api/import/{id}",
    (string id, ImportStore store) => Results.Ok(new { removed = store.Remove(id) }));

foreach (var descriptor in app.Services.GetServices<RendererDescriptor>())
{
    var desc = descriptor;
    app.MapGet($"/api/samples/{{name}}/{desc.Metadata.FormatId}",
        (HttpContext ctx, string name, string? theme, EditStore edits, ImportStore imports) =>
        {
            var net = Resolve(name, edits, imports, theme);
            if (net is null) return Results.NotFound();
            return desc.Render(net, ctx.RequestServices) switch
            {
                TextOutput t => Results.Content(t.Content, t.MimeType),
                BinaryOutput b => Results.File(b.Bytes, b.MimeType),
                _ => Results.StatusCode(500),
            };
        });
}

app.MapFallbackToFile("index.html");

app.Run();

static Network ParseCsvBody(string body)
{
    // Expect two CSV sections separated by a line `## edges` (everything before is nodes).
    const string sep = "## edges";
    var idx = body.IndexOf(sep, StringComparison.OrdinalIgnoreCase);
    if (idx < 0) throw new FormatException("CSV import body must contain a '## edges' separator between the nodes and edges sections.");
    var nodes = body[..idx].Trim();
    if (nodes.StartsWith("## nodes", StringComparison.OrdinalIgnoreCase))
        nodes = nodes[(nodes.IndexOf('\n') + 1)..].Trim();
    var edges = body[(idx + sep.Length)..].Trim();
    return NetworkImporter.FromCsv(nodes, edges);
}
