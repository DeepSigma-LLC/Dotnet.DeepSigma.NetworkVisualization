# DeepSigma.NetworkVisualization

A family of .NET packages and React components for rendering network/graph visualizations through swappable renderers. **One Network model, eight rendering targets, one consistent fluent API.**

```csharp
var network = NetworkBuilder.Create()
    .Directed()
    .Title("My Pipeline")
    .WithLayout(l => l.Sugiyama().Direction(LayoutDirection.LeftToRight))
    .AddNode("src",   n => n.Label("Source").Shape(NodeShape.Cylinder))
    .AddNode("build", n => n.Label("Build"))
    .AddNode("prod",  n => n.Label("Prod").Fill("#16A34A"))
    .AddEdge("src",   "build", e => e.Label("git"))
    .AddEdge("build", "prod",  e => e.Label("deploy").Dashed())
    .Build();

var mermaid    = new MermaidRenderer().Render(network);     // → flowchart text
var svg        = new SvgRenderer().Render(network);          // → SVG document
var reactFlow  = new ReactFlowRenderer().Render(network);    // → JSON for ReactFlow
var png        = new SkiaRenderer().Render(network);         // → PNG bytes
```

Swap renderers; the input is identical.

## What's in the box

| Renderer | Output | Engine |
| --- | --- | --- |
| **Mermaid** | flowchart text | (consumed by mermaid.js in the browser) |
| **GraphViz DOT** | DOT text | (consumed by @hpcc-js/wasm-graphviz in the browser) |
| **SVG** | SVG document | pure C# emitter |
| **SkiaSharp** | PNG / JPEG / WebP | SkiaSharp |
| **ReactFlow** | JSON | consumed by `reactflow` in React |
| **Cytoscape.js** | JSON | consumed by `cytoscape` (with dagre layout extension) |
| **D3 force-graph** | JSON | consumed by `d3` |
| **Sigma.js** | Graphology JSON | consumed by `sigma` + `graphology` (WebGL, large graphs) |

Plus a **`CoreJsonRenderer`** that emits the canonical Core JSON envelope — the same shape `NetworkImporter.FromJson` consumes.

```mermaid
flowchart LR
    Builder["NetworkBuilder<br/>(fluent C#)"] --> Network
    CSV["CSV file"] --> Importer
    Json["Core JSON"] --> Importer
    Importer["NetworkImporter"] --> Network
    Network[("Network<br/>(canonical model)")]
    Network --> Layout["LayoutProviders<br/>(Sugiyama/Radial/Tree/…)"]
    Layout --> Network
    Network --> Mermaid["MermaidRenderer"]
    Network --> Dot["DotRenderer"]
    Network --> Svg["SvgRenderer"]
    Network --> Skia["SkiaRenderer"]
    Network --> RF["ReactFlowRenderer"]
    Network --> Cy["CytoscapeRenderer"]
    Network --> D3["D3Renderer"]
    Network --> Sigma["SigmaRenderer"]
```

## Repository layout

```
src/
├── DeepSigma.NetworkVisualization.Core               # types, fluent builder, JSON contract, layouts, importers (FromJson / FromCsv / FromObject)
├── DeepSigma.NetworkVisualization.Layout.Msagl       # MSAGL adapter (Sugiyama / MDS via NuGet)
├── DeepSigma.NetworkVisualization.Renderers.Mermaid
├── DeepSigma.NetworkVisualization.Renderers.Dot
├── DeepSigma.NetworkVisualization.Renderers.Svg
├── DeepSigma.NetworkVisualization.Renderers.SkiaSharp
├── DeepSigma.NetworkVisualization.Renderers.ReactFlow
├── DeepSigma.NetworkVisualization.Renderers.Cytoscape
├── DeepSigma.NetworkVisualization.Renderers.D3
├── DeepSigma.NetworkVisualization.Renderers.Sigma
└── js/
    ├── deepsigma-network-core                        # TypeScript types mirroring the JSON contract
    └── deepsigma-network-react                       # React components: ReactFlowNetwork, CytoscapeNetwork, D3Network, SigmaNetwork, MermaidNetwork, DotNetwork

samples/DeepSigma.NetworkVisualization.Samples        # OrgChart, Pipeline, SocialNetwork, Clusters, RadialTaxonomy, ObjectGraphSample
samples/DeepSigma.NetworkVisualization.Samples.Console # Headless console runner — renders any sample to .svg/.png/.mmd/.dot/.json files
test/DeepSigma.NetworkVisualization.Tests             # xUnit v3
demo/DeepSigma.NetworkVisualization.Demo.Web          # ASP.NET minimal API host
demo/demo-react                                        # Vite + React frontend (interactive viewer + editor + import UI)
aspire/DeepSigma.NetworkVisualization.AppHost         # .NET Aspire orchestrator
```

## Running the demo

```bash
npm install                                                       # one-time JS deps
dotnet run --project aspire/DeepSigma.NetworkVisualization.AppHost --launch-profile http
```

That single command starts:

| URL                       | Resource                                                  |
| ------------------------- | --------------------------------------------------------- |
| http://localhost:15080    | Aspire dashboard (resources, logs, traces)                |
| http://localhost:5180     | ASP.NET API (auto-discovers every registered renderer)    |
| http://localhost:5173     | Vite dev server with HMR — open this for the demo         |

The demo includes:
- **6 sample networks** (Org Chart, CI/CD Pipeline, Social Network, Service Topology, Knowledge Taxonomy, and **Object Graph: Customer** — a live C# object hierarchy reflected into a network, exercises cycle detection)
- **9 view tabs** per sample (ReactFlow / Cytoscape.js / D3 / Sigma.js / Mermaid / GraphViz DOT / SVG / PNG / Core JSON)
- **Interactive selection** — click any node in an interactive renderer; the sidebar shows its data payload
- **Theme toggle** — light/dark, switches the entire render server-side
- **Editor mode** — add/move/delete nodes, drag-to-connect edges; saves persist across all renderers via an in-memory edit store
- **Import** — paste Core JSON or CSV, get an `imported` virtual sample renderable in every viz

## Quick start (library)

```csharp
using DeepSigma.NetworkVisualization;
using DeepSigma.NetworkVisualization.Builders;
using DeepSigma.NetworkVisualization.Renderers.Mermaid;

var network = NetworkBuilder.Create()
    .Directed()
    .AddNode("a", n => n.Label("Alice"))
    .AddNode("b", n => n.Label("Bob"))
    .AddEdge("a", "b", e => e.Label("knows"))
    .Build();

Console.WriteLine(new MermaidRenderer().Render(network));
```

For more recipes (every renderer, custom layouts, imports, registering new renderers, using the React components), see **[USAGE.md](USAGE.md)**.

## Headless rendering — no frontend required

The React demo is *one* consumer; the .NET side stands on its own. Four of the eight renderers produce final artifacts you can write straight to disk — no web server, no browser, no JS runtime:

| Renderer | Output | Self-contained? |
| --- | --- | --- |
| **Mermaid** | Mermaid text (`.mmd`) | ✅ Drops into any GitHub/GitLab/Azure DevOps README and renders natively. |
| **GraphViz DOT** | DOT text (`.dot`) | ✅ Pipe through `dot -Tpng` or render in any DOT-aware tool. |
| **SVG** | SVG document (`.svg`) | ✅ Open in any browser/image viewer; embed in HTML; convert to PNG with `rsvg-convert`/Inkscape/ImageMagick. |
| **SkiaSharp** | PNG/JPEG/WebP bytes (`.png`) | ✅ Write to disk directly. SkiaSharp ships native binaries for Win/Linux/macOS via NuGet. |
| ReactFlow / Cytoscape / D3 / Sigma | JSON | Needs a browser + the corresponding JS library to render. Still portable as data. |

```csharp
using DeepSigma.NetworkVisualization;
using DeepSigma.NetworkVisualization.Builders;
using DeepSigma.NetworkVisualization.Renderers.Mermaid;
using DeepSigma.NetworkVisualization.Renderers.Dot;
using DeepSigma.NetworkVisualization.Renderers.Svg;
using DeepSigma.NetworkVisualization.Renderers.SkiaSharp;

var network = NetworkBuilder.Create()
    .Directed()
    .AddNode("a", n => n.Label("Alice"))
    .AddNode("b", n => n.Label("Bob"))
    .AddEdge("a", "b", e => e.Label("knows"))
    .Build();

File.WriteAllText("graph.mmd",  new MermaidRenderer().Render(network));
File.WriteAllText("graph.dot",  new DotRenderer().Render(network));
File.WriteAllText("graph.svg",  new SvgRenderer().Render(network));
File.WriteAllBytes("graph.png", new SkiaRenderer().Render(network));
```

That's the whole pattern. No web server, no JS bundle, no browser — just `dotnet run`. Use it from:

- **CI documentation pipelines** — render diagrams during the build, commit them alongside source
- **Server-side report generation** — embed graphs in PDFs, email attachments, Slack messages
- **CLI tools** — `mytool render-deps > deps.svg`
- **Background workers** — visualize event/job graphs into blob storage
- **Test artifacts** — write SVGs from fixtures so regressions show up in CI

### The bundled console runner

`samples/DeepSigma.NetworkVisualization.Samples.Console` is a ~30-line demonstration that exercises this end-to-end. It walks the built-in sample library and writes every standalone format for each one:

```bash
# Render every sample into ./out/
dotnet run --project samples/DeepSigma.NetworkVisualization.Samples.Console

# Render a single sample into a custom directory
dotnet run --project samples/DeepSigma.NetworkVisualization.Samples.Console -- org-chart ./diagrams
```

Output:

```text
✓ org-chart            9 nodes / 8 edges  → ./out/org-chart.{mmd,dot,svg,png,json}
✓ pipeline             8 nodes / 10 edges → ./out/pipeline.{mmd,dot,svg,png,json}
✓ social-network       10 nodes / 15 edges → ./out/social-network.{mmd,dot,svg,png,json}
✓ clusters             9 nodes / 11 edges → ./out/clusters.{mmd,dot,svg,png,json}
✓ radial-taxonomy      19 nodes / 18 edges → ./out/radial-taxonomy.{mmd,dot,svg,png,json}
✓ object-graph         41 nodes / 42 edges → ./out/object-graph.{mmd,dot,svg,png,json}

Wrote 30 files to /…/out
```

The console runner is also a working template — copy [its `Program.cs`](samples/DeepSigma.NetworkVisualization.Samples.Console/Program.cs) into your own project, replace `SampleNetworks.All[name]()` with your own `Network` source, and you have a CI build step.

### Caveats

- **GraphViz DOT** is just text — to convert `.dot` to a PNG/SVG file you need GraphViz installed locally (`apt install graphviz`, `brew install graphviz`, `winget install graphviz`), or you can use the browser-side `@hpcc-js/wasm-graphviz` package (that's what our React demo uses).
- **SkiaSharp** needs platform-specific native binaries, declared as `SkiaSharp.NativeAssets.Win32` / `.Linux` / `.macOS` NuGet packages — they ship transitively, so consuming projects don't usually need to do anything. AOT compilation requires you to publish with the right RID.

## Adding your own renderer

Each renderer is a small package that ships its own DI extension. Adding a new one is one method, one extension, one demo line — the demo backend auto-discovers it via `IEnumerable<RendererDescriptor>` and registers an endpoint for it without any other changes.

```csharp
public sealed class MyRenderer : INetworkRenderer<string>
{
    public static RendererMetadata Metadata { get; } = new("myformat", "text/plain");
    public string FormatId => Metadata.FormatId;
    public string Render(Network network) { /* ... */ }
}

public static class MyServiceCollectionExtensions
{
    public static IServiceCollection AddMyRenderer(this IServiceCollection s)
    {
        s.AddSingleton<MyRenderer>();
        s.AddSingleton<RendererDescriptor>(new TextRendererDescriptor(
            MyRenderer.Metadata,
            (sp, net) => sp.GetRequiredService<MyRenderer>().Render(net)));
        return s;
    }
}

// In the host:
builder.Services.AddMyRenderer();
```

The demo will automatically expose `GET /api/samples/{name}/myformat`. See **[ARCHITECTURE.md](ARCHITECTURE.md)** for why this works.

## Importing data

Three opinionated entry points. Same `Network` result; pick the one that matches what you actually have.

```csharp
using DeepSigma.NetworkVisualization.Importers;

// 1) Core JSON envelope (what NetworkJsonSerializer.Serialize produces)
var net = NetworkImporter.FromJson(jsonString);

// 2) CSV — two strings, one per table
var net = NetworkImporter.FromCsv(
    nodesCsv: "id,label,color\na,Alice,#FF0000\nb,Bob,#00FF00",
    edgesCsv: "source,target,label\na,b,knows");

// 3) A runtime .NET object — walk its public properties, follow references,
//    render the resulting hierarchy. Cycles are detected and shown as dashed edges.
var net = NetworkImporter.FromObject(customer);
```

`FromObject` is the introspection tool — pass a `Customer`, `Order`, configuration object, ORM-hydrated graph, anything. It treats framework types (`DateTime`, `Guid`, `string`, `List<T>` items, …) as leaf values, walks user-defined types as sub-nodes, renders collections as group containers, and detects reference cycles. Knobs via `ObjectGraphOptions`:

```csharp
var net = NetworkImporter.FromObject(myObject, new ObjectGraphOptions {
    MaxDepth = 8,                  // recursion cap
    MaxCollectionItems = 50,       // per-collection cap
    IncludeNullValues = true,
    PropertyFilter = p => p.Name != "Password",   // skip noisy/sensitive properties
    IsLeafType = t => t == typeof(byte[]),        // override what counts as a leaf
});
```

We deliberately don't ship importers for D3/Cytoscape/Graphology/Gephi JSON dialects. Real-world data arrives from a database, a domain object, a custom API response, or a CSV export — the mapping is the caller's concern. **Documented JSON + CSV + reflection over runtime objects** covers the practical case without the maintenance burden of chasing every ecosystem's JSON variant.

## Running tests

xUnit v3 uses Microsoft Testing Platform — run the test project directly:

```bash
dotnet run --project test/DeepSigma.NetworkVisualization.Tests
```

## Demo backend endpoints

Every renderer is auto-discovered from DI; endpoint paths come from `RendererMetadata.FormatId`.

| Endpoint                                  | Returns                       |
| ----------------------------------------- | ----------------------------- |
| `GET /api/samples`                        | List of sample networks       |
| `GET /api/samples/{name}/core`            | Canonical Core JSON envelope  |
| `GET /api/samples/{name}/mermaid`         | Mermaid flowchart text        |
| `GET /api/samples/{name}/dot`             | GraphViz DOT                  |
| `GET /api/samples/{name}/svg`             | SVG document                  |
| `GET /api/samples/{name}/png`             | PNG (SkiaSharp)               |
| `GET /api/samples/{name}/reactflow`       | ReactFlow JSON                |
| `GET /api/samples/{name}/cytoscape`       | Cytoscape elements JSON       |
| `GET /api/samples/{name}/d3`              | D3 force-graph JSON           |
| `GET /api/samples/{name}/sigma`           | Graphology / Sigma.js JSON    |
| `GET /api/samples/{name}/{any}?theme=dark`| Same renderer, dark theme     |
| `POST /api/edit/{name}` body=Core JSON    | Persist an edited version     |
| `DELETE /api/edit/{name}`                 | Revert an edit                |
| `POST /api/import?format=json\|csv`        | Add a temporary virtual sample |

## License

MIT.
