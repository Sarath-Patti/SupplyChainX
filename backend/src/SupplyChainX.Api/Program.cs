using Serilog;

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

// Register Core Application & Web Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SupplyChainX API",
        Version = "v0.1.0",
        Description = "Enterprise Inventory & Order Management Platform Foundation API"
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

// Configure Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SupplyChainX API v0.1.0");
    });
}

app.UseSerilogRequestLogging();
app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();
