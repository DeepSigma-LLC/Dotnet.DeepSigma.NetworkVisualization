namespace DeepSigma.NetworkVisualization.Rendering;

/// <summary>
/// Self-describing metadata exposed by each renderer as a <c>static</c> property. Used by hosts to wire endpoints
/// (the <see cref="FormatId"/> becomes a URL segment), pick the right HTTP <see cref="MimeType"/>, and decide
/// whether to apply a layout up front.
/// </summary>
/// <param name="FormatId">Short, URL-safe identifier — e.g. <c>"mermaid"</c>, <c>"png"</c>, <c>"reactflow"</c>.</param>
/// <param name="MimeType">The Content-Type to serve when handing the rendered output to an HTTP client.</param>
/// <param name="RequiresLayout">If <c>true</c>, the renderer needs pre-computed node positions and will call <see cref="Layouts.LayoutExtensions.EnsureLayout"/>.</param>
public sealed record RendererMetadata(string FormatId, string MimeType, bool RequiresLayout = false);
