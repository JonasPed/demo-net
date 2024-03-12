using KitNugs.Configuration;
using KitNugs.Logging;
using KitNugs.Services;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Wire up configuration
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddOptions<ServiceConfiguration>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Configure logging - we use serilog.
builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddScoped<IHelloService, HelloService>();
builder.Services.AddScoped<ISessionIdAccessor, DefaultSessionIdAccessor>();

builder.Services.AddHttpContextAccessor();

// Configure database
var connectionString = builder.Configuration.GetConnectionString("db");

// Add controllers
builder.Services.AddControllers().AddNewtonsoftJson(opts =>
{
    opts.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    opts.SerializerSettings.Converters.Add(new StringEnumConverter
    {
        NamingStrategy = new CamelCaseNamingStrategy()
    });
});

// Enable Swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

// Setup health checks and Prometheus endpoint
builder.Services.AddHealthChecks()
                .ForwardToPrometheus();

var app = builder.Build();

app.UseMiddleware<LogHeaderMiddleware>();

app.UseHttpMetrics();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

// Map controllers
app.MapControllers();

// Ensure health endpoint and Prometheus only respond on port 8081
app.UseHealthChecks("/healthz", 8081);
app.UseMetricServer(8081, "/metrics");

app.Run();

public partial class Program { }
