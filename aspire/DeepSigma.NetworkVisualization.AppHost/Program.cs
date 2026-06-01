var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.DeepSigma_NetworkVisualization_Demo_Web>("api")
    .WithEndpoint("http", e =>
    {
        e.IsProxied = false;
        e.Port = 5180;
        e.TargetPort = 5180;
    })
    .WithExternalHttpEndpoints();

builder.AddNpmApp("frontend", "../../demo/demo-react", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 5173, isProxied: false)
    .WithEnvironment("BROWSER", "none")
    .WithExternalHttpEndpoints();

builder.Build().Run();
