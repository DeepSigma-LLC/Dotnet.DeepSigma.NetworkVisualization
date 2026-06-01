using DeepSigma.NetworkVisualization.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace DeepSigma.NetworkVisualization.Renderers.Dot;

public static class DotServiceCollectionExtensions
{
    public static IServiceCollection AddDotRenderer(this IServiceCollection services)
    {
        services.AddSingleton<DotRenderer>();
        services.AddSingleton<RendererDescriptor>(new TextRendererDescriptor(
            DotRenderer.Metadata,
            (sp, net) => sp.GetRequiredService<DotRenderer>().Render(net)));
        return services;
    }
}
