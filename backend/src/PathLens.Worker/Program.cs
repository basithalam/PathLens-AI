using PathLens.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

// The worker runs the database-backed analysis job processor (registered by
// Infrastructure) without serving HTTP. Run either this worker or rely on
// the API host's processor for local demos — not both against one database,
// or jobs will be shared between them.
builder.Services.AddInfrastructure(builder.Configuration);

var host = builder.Build();
host.Run();
