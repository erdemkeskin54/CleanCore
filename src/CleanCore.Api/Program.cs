using Asp.Versioning.ApiExplorer;
using CleanCore.Api.Extensions;
using CleanCore.Api.Middleware;
using CleanCore.Application;
using CleanCore.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog — config'ten okur, LogContext enrichment.
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
                 .Enrich.FromLogContext());

// --- Katmanlar
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- API altyapısı
builder.Services.AddControllers();
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddApiVersion();
builder.Services.AddSwaggerWithJwt();
builder.Services.AddApiHealthChecks(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// --- Pipeline (sıra önemli)
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }
    });
}

app.UseHttpsRedirection();
app.UseCors(CorsConfigurationExtensions.DefaultPolicy);

app.MapControllers();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

// Integration test'lerin WebApplicationFactory<Program>'ı kullanabilmesi için.
public partial class Program { }
