using CleanCore.Api.Middleware;
using CleanCore.Application;
using CleanCore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- Katmanlar
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- API altyapısı
builder.Services.AddControllers();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// --- Pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

// Integration test'lerin WebApplicationFactory<Program>'ı kullanabilmesi için.
public partial class Program { }
