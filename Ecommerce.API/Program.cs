using Asp.Versioning;
using Ecommerce.API;
using Ecommerce.API.Extensions;
using Ecommerce.API.Middleware;
using Ecommerce.Application;
using Ecommerce.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddPresentation(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.ReportApiVersions = true;
    // The version is part of every route (api/v1/...), so the URL is the only place it is read from.
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
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
    await app.PrepareDevelopmentDatabaseAsync();
}

// First, so everything after it sees the real client address from a trusted proxy.
app.UseForwardedHeaders();

app.UseExceptionHandler();

// Before authentication: a request that could be forged from another site is rejected before anything runs.
app.UseMiddleware<CsrfHeaderMiddleware>();

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

await app.RunAsync();
