using DeepSigma.NetworkVisualization.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace DeepSigma.NetworkVisualization.Renderers.D3;

public static class D3ServiceCollectionExtensions
{
    public static IServiceCollection AddD3Renderer(this IServiceCollection services)
    {
        services.AddSingleton<D3Renderer>();
        services.AddSingleton<RendererDescriptor>(new TextRendererDescriptor(
            D3Renderer.Metadata,
            (sp, net) => sp.GetRequiredService<D3Renderer>().Render(net)));
        return services;
    }
}
