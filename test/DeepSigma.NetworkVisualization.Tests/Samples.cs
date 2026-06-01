using DeepSigma.NetworkVisualization;
using DeepSigma.NetworkVisualization.Builders;

namespace DeepSigma.NetworkVisualization.Tests;

internal static class Samples
{
    public static Network OrgChart() => NetworkBuilder.Create()
        .Directed()
        .Title("Org Chart")
        .WithLayout(l => l.Hierarchical().Direction(LayoutDirection.TopToBottom).NodeSpacing(50).RankSpacing(80))
        .AddNode("ceo", n => n.Label("CEO").Shape(NodeShape.RoundedRectangle).Fill("#1976D2").LabelColor("#FFFFFF"))
        .AddNode("cto", n => n.Label("CTO").Shape(NodeShape.RoundedRectangle))
        .AddNode("cfo", n => n.Label("CFO").Shape(NodeShape.RoundedRectangle))
        .AddNode("eng1", n => n.Label("Eng A"))
        .AddNode("eng2", n => n.Label("Eng B"))
        .AddNode("acct", n => n.Label("Accountant"))
        .AddEdge("ceo", "cto", e => e.Label("reports"))
        .AddEdge("ceo", "cfo", e => e.Label("reports"))
        .AddEdge("cto", "eng1")
        .AddEdge("cto", "eng2")
        .AddEdge("cfo", "acct")
        .Build();

    public static Network Pipeline() => NetworkBuilder.Create()
        .Directed()
        .Title("Build Pipeline")
        .WithTheme(Theme.Dark)
        .WithLayout(l => l.Sugiyama().Direction(LayoutDirection.LeftToRight))
        .AddNode("src", n => n.Label("Source").Shape(NodeShape.Cylinder))
        .AddNode("build", n => n.Label("Build"))
        .AddNode("test", n => n.Label("Test"))
        .AddNode("deploy", n => n.Label("Deploy").Shape(NodeShape.Diamond))
        .AddNode("prod", n => n.Label("Prod").Shape(NodeShape.Cylinder).Fill("#16A34A"))
        .AddEdge("src", "build")
        .AddEdge("build", "test")
        .AddEdge("test", "deploy")
        .AddEdge("deploy", "prod", e => e.Label("on success"))
        .AddEdge("deploy", "test", e => e.Label("on fail").Dashed())
        .Build();

    public static Network SocialNetwork()
    {
        var nb = NetworkBuilder.Create().Undirected().Title("Social Network")
            .WithLayout(l => l.ForceDirected().Seed(7));
        string[] people = ["alice", "bob", "carol", "dave", "eve", "frank", "grace", "heidi"];
        foreach (var p in people)
            nb.AddNode(p, n => n.Label(char.ToUpperInvariant(p[0]) + p[1..]).Shape(NodeShape.Circle));
        (string, string)[] friendships = [
            ("alice","bob"),("alice","carol"),("bob","dave"),("carol","dave"),
            ("dave","eve"),("eve","frank"),("frank","grace"),("grace","heidi"),
            ("heidi","alice"),("bob","frank")
        ];
        foreach (var (a, b) in friendships) nb.AddEdge(a, b);
        return nb.Build();
    }

    public static Network Clusters() => NetworkBuilder.Create()
        .Directed()
        .Title("Clustered System")
        .WithLayout(l => l.Sugiyama())
        .AddNode("api", n => n.Label("API").InGroup("frontend"))
        .AddNode("web", n => n.Label("Web").InGroup("frontend"))
        .AddNode("svc1", n => n.Label("Service 1").InGroup("backend"))
        .AddNode("svc2", n => n.Label("Service 2").InGroup("backend"))
        .AddNode("db", n => n.Label("DB").Shape(NodeShape.Cylinder).InGroup("data"))
        .AddNode("cache", n => n.Label("Cache").Shape(NodeShape.Cylinder).InGroup("data"))
        .Group("frontend", g => g.Label("Frontend"))
        .Group("backend", g => g.Label("Backend"))
        .Group("data", g => g.Label("Data Layer"))
        .AddEdge("web", "api")
        .AddEdge("api", "svc1")
        .AddEdge("api", "svc2")
        .AddEdge("svc1", "db")
        .AddEdge("svc2", "db")
        .AddEdge("svc1", "cache")
        .Build();
}
