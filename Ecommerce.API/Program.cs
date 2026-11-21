using Asp.Versioning;
using Ecommerce.API;
using Ecommerce.API.Extensions;
using Ecommerce.Application;
using Ecommerce.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddPresentation()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.ReportApiVersions = true;
    // The version is part of every route (api/v1/...), so the URL is the only place it is read from.
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddMvc()
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

app.UseExceptionHandler();

app.MapControllers();

await app.RunAsync();
