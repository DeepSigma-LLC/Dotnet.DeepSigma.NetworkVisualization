using DeepSigma.NetworkVisualization;
using DeepSigma.NetworkVisualization.Json;
using DeepSigma.NetworkVisualization.Layout.Msagl;
using DeepSigma.NetworkVisualization.Renderers.Dot;
using DeepSigma.NetworkVisualization.Renderers.Mermaid;
using DeepSigma.NetworkVisualization.Renderers.SkiaSharp;
using DeepSigma.NetworkVisualization.Renderers.Svg;
using DeepSigma.NetworkVisualization.Samples;

// Headless rendering: take a built-in sample (or all of them) and write every standalone
// renderer's output to disk. No web server, no frontend, no browser — just .NET.
//
// Usage:
//   deepsigma-render                       # render every sample into ./out
//   deepsigma-render org-chart             # render one sample
//   deepsigma-render org-chart ./diagrams  # custom output directory

MsaglLayouts.Register();

var requestedSample = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) ? args[0] : null;
var outputDir = args.Length > 1 ? args[1] : "out";
Directory.CreateDirectory(outputDir);

var samples = requestedSample is null
    ? SampleNetworks.All.Keys.ToArray()
    : SampleNetworks.All.ContainsKey(requestedSample)
        ? new[] { requestedSample }
        : null;

if (samples is null)
{
    Console.Error.WriteLine($"Unknown sample '{requestedSample}'.");
    Console.Error.WriteLine($"Available samples: {string.Join(", ", SampleNetworks.All.Keys)}");
    return 1;
}

var mermaid = new MermaidRenderer();
var dot = new DotRenderer();
var svg = new SvgRenderer();
var skia = new SkiaRenderer();

foreach (var name in samples)
{
    var network = SampleNetworks.All[name]();
    var basePath = Path.Combine(outputDir, name);

    File.WriteAllText($"{basePath}.mmd", mermaid.Render(network));
    File.WriteAllText($"{basePath}.dot", dot.Render(network));
    File.WriteAllText($"{basePath}.svg", svg.Render(network));
    File.WriteAllBytes($"{basePath}.png", skia.Render(network));
    File.WriteAllText($"{basePath}.json", NetworkJsonSerializer.Serialize(network));

    Console.WriteLine($"✓ {name,-20} {network.Nodes.Count} nodes / {network.Edges.Count} edges → {basePath}.{{mmd,dot,svg,png,json}}");
}

Console.WriteLine();
Console.WriteLine($"Wrote {samples.Length * 5} files to {Path.GetFullPath(outputDir)}");
return 0;
