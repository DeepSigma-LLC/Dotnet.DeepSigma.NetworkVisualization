# USAGE

Copy-pasteable recipes. Every example is self-contained — you can drop it into a `Program.cs` with the listed `using` directives and the right package references.

## 1. Build a network

```csharp
using DeepSigma.NetworkVisualization;
using DeepSigma.NetworkVisualization.Builders;

var network = NetworkBuilder.Create()
    .Directed()
    .Title("Demo")
    .WithTheme(Theme.Light)
    .WithLayout(l => l.Hierarchical().Direction(LayoutDirection.TopToBottom))
    .AddNode("a", n => n.Label("Alice").Shape(NodeShape.RoundedRectangle).Fill("#1976D2").LabelColor("#FFFFFF"))
    .AddNode("b", n => n.Label("Bob"))
    .AddEdge("a", "b", e => e.Label("knows").Dashed())
    .Group("team", g => g.Label("Team A").Contains("a", "b"))
    .Build();
```

The builder validates on `Build()` — duplicate ids, dangling edge endpoints, missing groups, and group cycles all throw `NetworkValidationException` with every error aggregated.

## 2. Render to every format

```csharp
using DeepSigma.NetworkVisualization.Renderers.Mermaid;
using DeepSigma.NetworkVisualization.Renderers.Dot;
using DeepSigma.NetworkVisualization.Renderers.Svg;
using DeepSigma.NetworkVisualization.Renderers.SkiaSharp;
using DeepSigma.NetworkVisualization.Renderers.ReactFlow;
using DeepSigma.NetworkVisualization.Renderers.Cytoscape;
using DeepSigma.NetworkVisualization.Renderers.D3;
using DeepSigma.NetworkVisualization.Renderers.Sigma;

string mermaid    = new MermaidRenderer().Render(network);
string dot        = new DotRenderer().Render(network);
string svgXml     = new SvgRenderer().Render(network);
byte[] pngBytes   = new SkiaRenderer().Render(network);
string reactFlow  = new ReactFlowRenderer().Render(network);  // JSON
string cytoscape  = new CytoscapeRenderer().Render(network);  // JSON
string d3         = new D3Renderer().Render(network);          // JSON
string sigma      = new SigmaRenderer().Render(network);       // Graphology JSON
```

## 3. Use the canonical JSON contract

```csharp
using DeepSigma.NetworkVisualization.Json;

// Serialize
string json = NetworkJsonSerializer.Serialize(network);

// Deserialize
Network? roundTripped = NetworkJsonSerializer.Deserialize(json);
```

The envelope shape:

```json
{
  "format": "deepsigma.network",
  "version": "1.0",
  "network": {
    "directed": true,
    "nodes": [...],
    "edges": [...],
    "groups": [...],
    "layout": {...},
    "interaction": {...},
    "theme": {...}
  }
}
```

## 4. Apply a layout explicitly

Renderers that need positions (SVG, SkiaSharp, ReactFlow, Sigma) auto-apply a layout via `Network.EnsureLayout()` — but you can apply one yourself.

```csharp
using DeepSigma.NetworkVisualization.Layouts;

// Choose by the network's declared algorithm
Network positioned = LayoutProviders.For(network).ApplyLayout(network);

// Or choose directly
Network grid = new GridLayoutProvider { CellWidth = 120, CellHeight = 80 }.ApplyLayout(network);
Network radial = new RadialLayoutProvider { RingSpacing = 140 }.ApplyLayout(network);
```

Built-in providers in `Core`:
- `NoLayoutProvider` (positions must already be set)
- `GridLayoutProvider`
- `CircularLayoutProvider`
- `TreeLayoutProvider` (Reingold–Tilford-ish)
- `RadialLayoutProvider` (BFS from root, concentric rings)
- `SimpleForceDirectedLayoutProvider`

## 5. Use MSAGL for layered (Sugiyama) layout

Microsoft.Msagl ships through a small adapter package. Register it once at startup; `LayoutProviders.For` then returns MSAGL providers for the relevant algorithms transparently — every renderer benefits.

```csharp
using DeepSigma.NetworkVisualization.Layout.Msagl;

MsaglLayouts.Register();
// Now any network whose Layout.Algorithm is Sugiyama/Hierarchical/Mds will use MSAGL.
```

## 6. Import data

```csharp
using DeepSigma.NetworkVisualization.Importers;

// Core JSON (round-trips with NetworkJsonSerializer.Serialize)
Network a = NetworkImporter.FromJson(jsonString);

// CSV
Network b = NetworkImporter.FromCsv(
    nodesCsv: """
    id,label,color,team
    alice,Alice,#FF0000,sales
    bob,Bob,#00FF00,eng
    """,
    edgesCsv: """
    source,target,label,weight
    alice,bob,knows,1.5
    """);
```

CSV column conventions:
- **Nodes** require `id`. Optional: `label`, `color`/`fill`, `group`. Every other column becomes a `Node.Data` entry.
- **Edges** require `source` and `target`. Optional: `id`, `label`, `weight`.

## 7. Register a new renderer

Three pieces: the renderer class with a static `Metadata`, a DI extension that contributes a `RendererDescriptor`, and a call from your host.

```csharp
using DeepSigma.NetworkVisualization;
using DeepSigma.NetworkVisualization.Rendering;
using Microsoft.Extensions.DependencyInjection;

// 1) The renderer
public sealed class HtmlRenderer : INetworkRenderer<string>
{
    public static RendererMetadata Metadata { get; } = new("html", "text/html");
    public string FormatId => Metadata.FormatId;
    public string Render(Network network) => $"<ul>{string.Join("", network.Nodes.Select(n => $"<li>{n.ResolvedLabel()}</li>"))}</ul>";
}

// 2) The DI extension
public static class HtmlServiceCollectionExtensions
{
    public static IServiceCollection AddHtmlRenderer(this IServiceCollection s)
    {
        s.AddSingleton<HtmlRenderer>();
        s.AddSingleton<RendererDescriptor>(new TextRendererDescriptor(
            HtmlRenderer.Metadata,
            (sp, net) => sp.GetRequiredService<HtmlRenderer>().Render(net)));
        return s;
    }
}

// 3) Wire it
builder.Services.AddHtmlRenderer();
```

If you're using the demo's auto-discovery loop (`foreach (var d in app.Services.GetServices<RendererDescriptor>())`), the `GET /api/samples/{name}/html` endpoint appears for free.

## 8. Register a custom layout provider

```csharp
using DeepSigma.NetworkVisualization.Layouts;
using DeepSigma.NetworkVisualization.Rendering;

public sealed class MyConcentricLayout : ILayoutProvider
{
    public Network ApplyLayout(Network network) { /* … */ }
}

// At startup, override an algorithm with your provider:
LayoutProviders.Register(LayoutAlgorithm.Radial, _ => new MyConcentricLayout());
```

Every renderer that calls `Network.EnsureLayout()` will use it.

## 9. Customize the theme

```csharp
var network = NetworkBuilder.Create()
    .WithTheme(t => t
        .Background("#0F172A")
        .NodeFill("#1E293B")
        .NodeStroke("#64748B")
        .EdgeStroke("#94A3B8")
        .LabelColor("#F1F5F9")
        .Font("Segoe UI", 13))
    .AddNode("x")
    .Build();
```

Or use the two presets:

```csharp
.WithTheme(Theme.Light)
.WithTheme(Theme.Dark)
```

## 10. Use the React components

```bash
npm install deepsigma-network-react deepsigma-network-core \
    reactflow cytoscape cytoscape-dagre d3 mermaid sigma graphology @hpcc-js/wasm-graphviz
```

```tsx
import {
  ReactFlowNetwork,
  CytoscapeNetwork,
  D3Network,
  SigmaNetwork,
  MermaidNetwork,
  DotNetwork,
} from 'deepsigma-network-react';
import type { ReactFlowPayload } from 'deepsigma-network-core';

function App() {
  const [payload, setPayload] = useState<ReactFlowPayload | null>(null);
  useEffect(() => {
    fetch('/api/samples/org-chart/reactflow')
      .then(r => r.json())
      .then(setPayload);
  }, []);
  if (!payload) return null;
  return (
    <ReactFlowNetwork
      data={payload}
      height={600}
      onNodeClick={(id, data) => console.log('clicked', id, data)}
    />
  );
}
```

All renderer components accept the same event handlers from `NetworkEventHandlers`:

```ts
interface NetworkEventHandlers {
  onNodeClick?: (id: string, data?: Record<string, unknown>) => void;
  onEdgeClick?: (id: string, data?: Record<string, unknown>) => void;
  onNodeHover?: (id: string | null) => void;
}
```
