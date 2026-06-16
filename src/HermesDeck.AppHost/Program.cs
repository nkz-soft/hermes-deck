var builder = DistributedApplication.CreateBuilder(args);

// T135: Add API project reference for Aspire local orchestration
builder.AddProject<Projects.HermesDeck_Api>("hermes-api");

builder.Build().Run();
