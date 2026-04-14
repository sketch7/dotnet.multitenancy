var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

var api = builder.AddProject<Projects.Sketch7_Multitenancy_Sample_Api>("sample-api")
	.WithReference(redis)
	.WaitFor(redis);

builder.Build().Run();