using Serilog;
using SupplyChainX.Api.Conventions;
using SupplyChainX.Api.Middleware;
using SupplyChainX.Application;
using SupplyChainX.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog structured logging foundation
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

// Register Modular Layers (Application & Infrastructure)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Register Controllers with /api/v1 routing convention
builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new ApiVersionRouteConvention("api/v1"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SupplyChainX API",
        Version = "v0.2.0",
        Description = "Enterprise Inventory & Order Management Platform API (v0.2 Core Backend & PostgreSQL)"
    });
});

// Configure CORS for Angular frontend initialization
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure Centralized Exception Handling Middleware
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Configure Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SupplyChainX API v0.2.0");
    });
}

// Structured HTTP Request Logging
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();
