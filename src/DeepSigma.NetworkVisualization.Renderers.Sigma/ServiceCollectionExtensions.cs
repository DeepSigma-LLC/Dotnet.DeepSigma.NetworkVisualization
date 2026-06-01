using DeepSigma.NetworkVisualization.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace DeepSigma.NetworkVisualization.Renderers.Sigma;

public static class SigmaServiceCollectionExtensions
{
    public static IServiceCollection AddSigmaRenderer(this IServiceCollection services)
    {
        services.AddSingleton<SigmaRenderer>();
        services.AddSingleton<RendererDescriptor>(new TextRendererDescriptor(
            SigmaRenderer.Metadata,
            (sp, net) => sp.GetRequiredService<SigmaRenderer>().Render(net)));
        return services;
    }
}
