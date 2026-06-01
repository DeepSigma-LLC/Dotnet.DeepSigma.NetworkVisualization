using DeepSigma.NetworkVisualization.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace DeepSigma.NetworkVisualization.Renderers.SkiaSharp;

public static class SkiaServiceCollectionExtensions
{
    public static IServiceCollection AddSkiaRenderer(this IServiceCollection services)
    {
        services.AddSingleton<SkiaRenderer>();
        services.AddSingleton<RendererDescriptor>(new BinaryRendererDescriptor(
            SkiaRenderer.Metadata,
            (sp, net) => sp.GetRequiredService<SkiaRenderer>().Render(net)));
        return services;
    }
}
