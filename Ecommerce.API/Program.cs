using Asp.Versioning;
using Ecommerce.API;
using Ecommerce.API.Extensions;
using Ecommerce.API.Logging;
using Ecommerce.Application;
using Ecommerce.Infrastructure;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Diagnostics;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;

builder.Host.UseSerilog((context, loggerConfigruration) => loggerConfigruration.ReadFrom.Configuration(builder.Configuration)
.Enrich.With<ActivityEnricher>(), writeToProviders: true);

string? otlpLogsEndpoint = builder.Configuration["Otlp:LogsEndpoint"];
string? otlpTracesEndpoint = builder.Configuration["Otlp:TracesEndpoint"];
string? otlpMetricsEndpoint = builder.Configuration["Otlp:MetricsEndpoint"];


builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName).AddAttributes(new Dictionary<string, object>
    {
        ["environment.name"] = builder.Environment.EnvironmentName,
        ["service.name"] = builder.Environment.ApplicationName
    }))
    .WithLogging(logging =>
    {
        if (!string.IsNullOrWhiteSpace(otlpLogsEndpoint) &&
            Uri.TryCreate(otlpLogsEndpoint, UriKind.Absolute, out Uri logsUri))
        {
            logging.AddOtlpExporter(exporterOptions =>
            {
                exporterOptions.Endpoint = logsUri;
                exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            });
        }
    })
    .WithTracing(tracing =>
    {
        tracing.AddSource(builder.Environment.ApplicationName);
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();

        if (!string.IsNullOrWhiteSpace(otlpTracesEndpoint) &&
            Uri.TryCreate(otlpTracesEndpoint, UriKind.Absolute, out Uri tracesUri))
        {
            tracing.AddOtlpExporter(exporterOptions =>
            {
                exporterOptions.Endpoint = tracesUri;
                exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            });
        }
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
        metrics.AddRuntimeInstrumentation();

        if (!string.IsNullOrWhiteSpace(otlpMetricsEndpoint) &&
            Uri.TryCreate(otlpMetricsEndpoint, UriKind.Absolute, out Uri metricsUri))
        {
            metrics.AddOtlpExporter((exporterOptions, metricReaderOptions) =>
            {
                exporterOptions.Endpoint = metricsUri;
                exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 60000; // 60 seconds
            });
        }
    });


builder.Services
    .AddApplication()
    .AddPresentation()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
})
.AddMvc() // This is needed for controllers
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithUi();
    app.ApplyMigrations();
}

app.UseRequestContextLogging();

app.UseSerilogRequestLogging();

app.UseExceptionHandler();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
