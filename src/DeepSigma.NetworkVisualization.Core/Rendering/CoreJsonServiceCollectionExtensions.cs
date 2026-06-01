using Microsoft.Extensions.DependencyInjection;

namespace DeepSigma.NetworkVisualization.Rendering;

public static class CoreJsonServiceCollectionExtensions
{
    public static IServiceCollection AddCoreJsonRenderer(this IServiceCollection services)
    {
        services.AddSingleton<CoreJsonRenderer>();
        services.AddSingleton<RendererDescriptor>(new TextRendererDescriptor(
            CoreJsonRenderer.Metadata,
            (sp, net) => sp.GetRequiredService<CoreJsonRenderer>().Render(net)));
        return services;
    }
}
