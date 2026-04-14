var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Sketch7_Multitenancy_Sample_Api>("sample-api");

builder.Build().Run();
