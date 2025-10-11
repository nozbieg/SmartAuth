var builder = DistributedApplication.CreateBuilder(args);

// API (.NET 9)
var api = builder.AddProject<Projects.SmartAuth_Api>("apiservice")
    .WithExternalHttpEndpoints(); 


var web = builder.AddProject<Projects.SmartAuth_Web>("web")
    .WithReference(api)          
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
