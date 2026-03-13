using Asp.Versioning.ApiExplorer;
using CleanCore.Api.Extensions;
using CleanCore.Api.Middleware;
using CleanCore.Application;
using CleanCore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- Katmanlar
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- API altyapısı
builder.Services.AddControllers();
builder.Services.AddApiVersion();
builder.Services.AddSwaggerWithJwt();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// --- Pipeline
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
app.MapControllers();

app.Run();

// Integration test'lerin WebApplicationFactory<Program>'ı kullanabilmesi için.
public partial class Program { }
