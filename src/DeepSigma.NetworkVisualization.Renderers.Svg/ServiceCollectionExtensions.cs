using DeepSigma.NetworkVisualization.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace DeepSigma.NetworkVisualization.Renderers.Svg;

public static class SvgServiceCollectionExtensions
{
    public static IServiceCollection AddSvgRenderer(this IServiceCollection services)
    {
        services.AddSingleton<SvgRenderer>();
        services.AddSingleton<RendererDescriptor>(new TextRendererDescriptor(
            SvgRenderer.Metadata,
            (sp, net) => sp.GetRequiredService<SvgRenderer>().Render(net)));
        return services;
    }
}
