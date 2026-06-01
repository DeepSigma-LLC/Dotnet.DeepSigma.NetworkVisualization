using DeepSigma.NetworkVisualization.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace DeepSigma.NetworkVisualization.Renderers.Cytoscape;

public static class CytoscapeServiceCollectionExtensions
{
    public static IServiceCollection AddCytoscapeRenderer(this IServiceCollection services)
    {
        services.AddSingleton<CytoscapeRenderer>();
        services.AddSingleton<RendererDescriptor>(new TextRendererDescriptor(
            CytoscapeRenderer.Metadata,
            (sp, net) => sp.GetRequiredService<CytoscapeRenderer>().Render(net)));
        return services;
    }
}
