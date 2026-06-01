using DeepSigma.NetworkVisualization.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace DeepSigma.NetworkVisualization.Renderers.ReactFlow;

public static class ReactFlowServiceCollectionExtensions
{
    public static IServiceCollection AddReactFlowRenderer(this IServiceCollection services)
    {
        services.AddSingleton<ReactFlowRenderer>();
        services.AddSingleton<RendererDescriptor>(new TextRendererDescriptor(
            ReactFlowRenderer.Metadata,
            (sp, net) => sp.GetRequiredService<ReactFlowRenderer>().Render(net)));
        return services;
    }
}
