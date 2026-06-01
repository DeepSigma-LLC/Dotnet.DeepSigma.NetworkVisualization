using DeepSigma.NetworkVisualization;
using DeepSigma.NetworkVisualization.Builders;

namespace DeepSigma.NetworkVisualization.Demo.Web;

public static class Samples
{
    public static readonly IReadOnlyDictionary<string, Func<Network>> All = new Dictionary<string, Func<Network>>
    {
        ["org-chart"] = OrgChart,
        ["pipeline"] = Pipeline,
        ["social-network"] = SocialNetwork,
        ["clusters"] = Clusters,
    };

    public static Network OrgChart() => NetworkBuilder.Create()
        .Directed()
        .Title("Org Chart")
        .WithLayout(l => l.Hierarchical().Direction(LayoutDirection.TopToBottom).NodeSpacing(60).RankSpacing(100))
        .AddNode("ceo", n => n.Label("CEO").Shape(NodeShape.RoundedRectangle).Fill("#1976D2").LabelColor("#FFFFFF").Size(140, 60))
        .AddNode("cto", n => n.Label("CTO").Shape(NodeShape.RoundedRectangle).Fill("#42A5F5").LabelColor("#FFFFFF").Size(130, 55))
        .AddNode("cfo", n => n.Label("CFO").Shape(NodeShape.RoundedRectangle).Fill("#42A5F5").LabelColor("#FFFFFF").Size(130, 55))
        .AddNode("coo", n => n.Label("COO").Shape(NodeShape.RoundedRectangle).Fill("#42A5F5").LabelColor("#FFFFFF").Size(130, 55))
        .AddNode("eng1", n => n.Label("Eng A").Size(110, 50))
        .AddNode("eng2", n => n.Label("Eng B").Size(110, 50))
        .AddNode("eng3", n => n.Label("Eng C").Size(110, 50))
        .AddNode("acct", n => n.Label("Accountant").Size(120, 50))
        .AddNode("ops1", n => n.Label("Ops Lead").Size(120, 50))
        .AddEdge("ceo", "cto", e => e.Label("reports"))
        .AddEdge("ceo", "cfo", e => e.Label("reports"))
        .AddEdge("ceo", "coo", e => e.Label("reports"))
        .AddEdge("cto", "eng1")
        .AddEdge("cto", "eng2")
        .AddEdge("cto", "eng3")
        .AddEdge("cfo", "acct")
        .AddEdge("coo", "ops1")
        .Build();

    public static Network Pipeline() => NetworkBuilder.Create()
        .Directed()
        .Title("CI/CD Pipeline")
        .WithLayout(l => l.Sugiyama().Direction(LayoutDirection.LeftToRight).NodeSpacing(40).RankSpacing(80))
        .AddNode("src", n => n.Label("Source").Shape(NodeShape.Cylinder).Fill("#90CAF9").Size(110, 60))
        .AddNode("build", n => n.Label("Build").Shape(NodeShape.RoundedRectangle).Fill("#FFCA28").Size(110, 50))
        .AddNode("lint", n => n.Label("Lint").Shape(NodeShape.RoundedRectangle).Fill("#FFCA28").Size(110, 50))
        .AddNode("test", n => n.Label("Test").Shape(NodeShape.RoundedRectangle).Fill("#FFCA28").Size(110, 50))
        .AddNode("scan", n => n.Label("Security Scan").Shape(NodeShape.RoundedRectangle).Fill("#FFCA28").Size(140, 50))
        .AddNode("approve", n => n.Label("Approve?").Shape(NodeShape.Diamond).Fill("#FF7043").Size(120, 80))
        .AddNode("staging", n => n.Label("Staging").Shape(NodeShape.Cylinder).Fill("#26A69A").Size(110, 60))
        .AddNode("prod", n => n.Label("Production").Shape(NodeShape.Cylinder).Fill("#16A34A").Size(130, 70))
        .AddEdge("src", "build")
        .AddEdge("build", "lint")
        .AddEdge("build", "test")
        .AddEdge("build", "scan")
        .AddEdge("lint", "approve")
        .AddEdge("test", "approve")
        .AddEdge("scan", "approve")
        .AddEdge("approve", "staging", e => e.Label("yes"))
        .AddEdge("approve", "src", e => e.Label("no").Dashed())
        .AddEdge("staging", "prod", e => e.Label("promote"))
        .Build();

    public static Network SocialNetwork()
    {
        var nb = NetworkBuilder.Create()
            .Undirected()
            .Title("Social Network")
            .WithLayout(l => l.ForceDirected().Seed(7).NodeSpacing(80));
        (string id, string label, string fill)[] people = [
            ("alice","Alice","#EF5350"),("bob","Bob","#AB47BC"),("carol","Carol","#7E57C2"),
            ("dave","Dave","#5C6BC0"),("eve","Eve","#42A5F5"),("frank","Frank","#26A69A"),
            ("grace","Grace","#66BB6A"),("heidi","Heidi","#FFCA28"),("ivan","Ivan","#FF7043"),
            ("judy","Judy","#8D6E63"),
        ];
        foreach (var (id, label, fill) in people)
            nb.AddNode(id, n => n.Label(label).Shape(NodeShape.Circle).Fill(fill).Size(60, 60));

        (string, string)[] friendships = [
            ("alice","bob"),("alice","carol"),("bob","dave"),("carol","dave"),
            ("dave","eve"),("eve","frank"),("frank","grace"),("grace","heidi"),
            ("heidi","alice"),("bob","frank"),("ivan","judy"),("judy","alice"),
            ("ivan","carol"),("eve","heidi"),("dave","grace"),
        ];
        foreach (var (a, b) in friendships) nb.AddEdge(a, b);
        return nb.Build();
    }

    public static Network Clusters() => NetworkBuilder.Create()
        .Directed()
        .Title("Service Topology")
        .WithLayout(l => l.Sugiyama().Direction(LayoutDirection.TopToBottom).NodeSpacing(40).RankSpacing(80))
        .AddNode("web", n => n.Label("Web UI").Shape(NodeShape.RoundedRectangle).Fill("#90CAF9").Size(120, 50).InGroup("frontend"))
        .AddNode("mobile", n => n.Label("Mobile").Shape(NodeShape.RoundedRectangle).Fill("#90CAF9").Size(120, 50).InGroup("frontend"))
        .AddNode("gateway", n => n.Label("API Gateway").Shape(NodeShape.Hexagon).Fill("#FFCA28").Size(140, 60).InGroup("edge"))
        .AddNode("auth", n => n.Label("Auth Svc").Shape(NodeShape.RoundedRectangle).Fill("#FF7043").Size(120, 50).InGroup("backend"))
        .AddNode("orders", n => n.Label("Orders Svc").Shape(NodeShape.RoundedRectangle).Fill("#FF7043").Size(120, 50).InGroup("backend"))
        .AddNode("billing", n => n.Label("Billing Svc").Shape(NodeShape.RoundedRectangle).Fill("#FF7043").Size(120, 50).InGroup("backend"))
        .AddNode("primary", n => n.Label("Primary DB").Shape(NodeShape.Cylinder).Fill("#66BB6A").Size(120, 60).InGroup("data"))
        .AddNode("replica", n => n.Label("Read Replica").Shape(NodeShape.Cylinder).Fill("#A5D6A7").Size(130, 60).InGroup("data"))
        .AddNode("cache", n => n.Label("Cache").Shape(NodeShape.Cylinder).Fill("#FFD54F").Size(100, 50).InGroup("data"))
        .Group("frontend", g => g.Label("Frontend").Fill("#E3F2FD"))
        .Group("edge", g => g.Label("Edge").Fill("#FFF3E0"))
        .Group("backend", g => g.Label("Backend Services").Fill("#FBE9E7"))
        .Group("data", g => g.Label("Data Layer").Fill("#E8F5E9"))
        .AddEdge("web", "gateway")
        .AddEdge("mobile", "gateway")
        .AddEdge("gateway", "auth")
        .AddEdge("gateway", "orders")
        .AddEdge("gateway", "billing")
        .AddEdge("orders", "primary")
        .AddEdge("billing", "primary")
        .AddEdge("auth", "primary")
        .AddEdge("orders", "replica", e => e.Label("read").Dashed())
        .AddEdge("orders", "cache")
        .AddEdge("billing", "cache")
        .Build();
}
