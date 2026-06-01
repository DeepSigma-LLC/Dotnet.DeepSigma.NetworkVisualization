using DeepSigma.NetworkVisualization.Layouts;

namespace DeepSigma.NetworkVisualization.Layout.Msagl;

public static class MsaglLayouts
{
    public static void Register()
    {
        LayoutProviders.Register(LayoutAlgorithm.Sugiyama, s => new MsaglSugiyamaLayoutProvider
        {
            LayerSeparation = s.RankSpacing,
            NodeSeparation = s.NodeSpacing,
        });
        LayoutProviders.Register(LayoutAlgorithm.Hierarchical, s => new MsaglSugiyamaLayoutProvider
        {
            LayerSeparation = s.RankSpacing,
            NodeSeparation = s.NodeSpacing,
        });
        LayoutProviders.Register(LayoutAlgorithm.Mds, _ => new MsaglMdsLayoutProvider());
    }
}
