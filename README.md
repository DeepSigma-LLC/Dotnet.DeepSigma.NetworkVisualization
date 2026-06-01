# DeepSigma.NetworkVisualization

A family of .NET packages for building network/graph visualizations through swappable renderers. A single fluent-built `Network` model fans out to **Mermaid, GraphViz DOT, SVG, SkiaSharp PNG, ReactFlow, Cytoscape.js, and D3 force-graph** — pick any (or several) without changing the source.

## Solution layout

```
src/
├── DeepSigma.NetworkVisualization.Core              # types, fluent builder, JSON contract, built-in layouts
├── DeepSigma.NetworkVisualization.Layout.Msagl      # MSAGL adapter (Sugiyama, MDS, Ranking)
├── DeepSigma.NetworkVisualization.Renderers.Mermaid
├── DeepSigma.NetworkVisualization.Renderers.Dot
├── DeepSigma.NetworkVisualization.Renderers.Svg
├── DeepSigma.NetworkVisualization.Renderers.SkiaSharp
├── DeepSigma.NetworkVisualization.Renderers.ReactFlow
├── DeepSigma.NetworkVisualization.Renderers.Cytoscape
├── DeepSigma.NetworkVisualization.Renderers.D3
└── js/
    ├── deepsigma-network-core                       # TS types for the JSON contract
    └── deepsigma-network-react                      # React components (ReactFlow / Cytoscape / D3 / Mermaid)

test/DeepSigma.NetworkVisualization.Tests            # xUnit v3
demo/DeepSigma.NetworkVisualization.Demo.Web         # ASP.NET minimal API host
demo/demo-react                                       # Vite + React frontend
aspire/DeepSigma.NetworkVisualization.AppHost        # Aspire orchestrator
```

## Fluent API

```csharp
using DeepSigma.NetworkVisualization;
using DeepSigma.NetworkVisualization.Builders;
using DeepSigma.NetworkVisualization.Renderers.Mermaid;
using DeepSigma.NetworkVisualization.Renderers.ReactFlow;

var network = NetworkBuilder.Create()
    .Directed()
    .Title("My Pipeline")
    .WithTheme(Theme.Dark)
    .WithLayout(l => l.Sugiyama().Direction(LayoutDirection.LeftToRight).NodeSpacing(50))
    .AddNode("src",   n => n.Label("Source").Shape(NodeShape.Cylinder))
    .AddNode("build", n => n.Label("Build"))
    .AddNode("prod",  n => n.Label("Prod").Fill("#16A34A"))
    .AddEdge("src",   "build", e => e.Label("git"))
    .AddEdge("build", "prod",  e => e.Label("deploy").Dashed())
    .Group("ci", g => g.Label("CI").Contains("src", "build"))
    .Build();

var mermaidText = new MermaidRenderer().Render(network);
var reactFlowJson = new ReactFlowRenderer().Render(network);
```

Swap to a different renderer — that's the entire change.

## Running the demo

### One-shot with .NET Aspire (recommended)

The Aspire AppHost orchestrates the backend API and the Vite dev server together, with hot-reload on the React side and a dashboard that links to both:

```bash
npm install                                                       # one-time
dotnet run --project aspire/DeepSigma.NetworkVisualization.AppHost --launch-profile http
```

That single command starts:

| URL                       | Resource                                       |
| ------------------------- | ---------------------------------------------- |
| http://localhost:15080    | Aspire dashboard (resources, logs, traces)     |
| http://localhost:5180     | ASP.NET API (`/api/...` endpoints)             |
| http://localhost:5173     | Vite dev server — proxies `/api` to the backend |

Open http://localhost:5173 for the React demo with HMR, or http://localhost:5180 for the prebuilt SPA.

### Without Aspire

```bash
dotnet build
npm install
npm run build                                                # builds JS libs + Vite bundle into wwwroot
dotnet run --project demo/DeepSigma.NetworkVisualization.Demo.Web   # → http://localhost:5180
```

The page has a sidebar of sample networks (org chart, CI/CD pipeline, social network, clustered topology) and tabs for each renderer — ReactFlow / Cytoscape / D3 / Mermaid / DOT / SVG / PNG / Raw JSON.

## Running tests

xUnit v3 uses Microsoft Testing Platform — run the test project directly:

```bash
dotnet run --project test/DeepSigma.NetworkVisualization.Tests
```

## Backend endpoints

The demo host (`Demo.Web`) exposes the same network in every format:

| Endpoint                                  | Returns                       |
| ----------------------------------------- | ----------------------------- |
| `GET /api/samples`                        | List of sample networks       |
| `GET /api/samples/{name}/core`            | Canonical Core JSON envelope  |
| `GET /api/samples/{name}/mermaid`         | Mermaid flowchart syntax      |
| `GET /api/samples/{name}/dot`             | GraphViz DOT                  |
| `GET /api/samples/{name}/svg`             | SVG document                  |
| `GET /api/samples/{name}/png`             | PNG (SkiaSharp)               |
| `GET /api/samples/{name}/reactflow`       | ReactFlow JSON                |
| `GET /api/samples/{name}/cytoscape`       | Cytoscape.js elements JSON    |
| `GET /api/samples/{name}/d3`              | D3 force-graph JSON           |

Samples requiring layout (SVG / PNG / ReactFlow) auto-apply the MSAGL Sugiyama provider when the network requests it; pure JSON formats (Mermaid / DOT / Cytoscape / D3) don't need pre-computed positions — the consumer's engine lays them out.

## License

MIT.
