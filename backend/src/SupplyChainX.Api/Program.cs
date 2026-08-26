using Microsoft.OpenApi.Models;
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
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SupplyChainX API",
        Version = "v0.9.0",
        Description = "Enterprise Inventory & Order Management Platform API (v0.9 Authentication & Authorization)"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid JWT token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
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

// Correlation ID Tracing Middleware
app.UseMiddleware<CorrelationIdMiddleware>();

// Configure Centralized Exception Handling Middleware
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Configure Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SupplyChainX API v0.9.0");
    });
}

// Structured HTTP Request Logging with Serilog
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms [CorrelationId: {CorrelationId}]";
});

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
