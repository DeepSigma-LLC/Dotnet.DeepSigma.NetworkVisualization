using DeepSigma.NetworkVisualization.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace DeepSigma.NetworkVisualization.Renderers.Mermaid;

public static class MermaidServiceCollectionExtensions
{
    public static IServiceCollection AddMermaidRenderer(this IServiceCollection services)
    {
        services.AddSingleton<MermaidRenderer>();
        services.AddSingleton<RendererDescriptor>(new TextRendererDescriptor(
            MermaidRenderer.Metadata,
            (sp, net) => sp.GetRequiredService<MermaidRenderer>().Render(net)));
        return services;
    }
}
