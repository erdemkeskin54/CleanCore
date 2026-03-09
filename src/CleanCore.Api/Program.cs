using CleanCore.Application;
using CleanCore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- Katmanlar
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- API altyapısı (controllers)
builder.Services.AddControllers();

var app = builder.Build();

// --- Pipeline
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

// Integration test'lerin WebApplicationFactory<Program>'ı kullanabilmesi için.
public partial class Program { }
