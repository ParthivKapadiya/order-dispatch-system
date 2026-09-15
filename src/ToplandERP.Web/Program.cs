using ToplandERP.Application;
using ToplandERP.Infrastructure;
using ToplandERP.Web;
using ToplandERP.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddWebServices(builder.Environment);

var app = builder.Build();

app.UseWebPipeline();
await app.InitializeDatabaseAsync();
await app.RunAsync();

public partial class Program;
