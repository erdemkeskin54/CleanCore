using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanCore.Application.Behaviors;

// =============================================================================
// LoggingBehavior — request adı + elapsed ms structured log
// =============================================================================
// Her MediatR request'i için:
//   [BEFORE] "Handling LoginCommand"
//   [AFTER]  "Handled LoginCommand in 47ms"
//
// Niye structured log (template string + parametre)?
//   Serilog (Faz 4) bu pattern'i {RequestName} ve {ElapsedMs} property'leri olarak kaydediyor.
//   Elastic/Seq'te `RequestName=LoginCommand AND ElapsedMs > 1000` gibi sorgu yazılabiliyor.
//   String concat ile yazılsa bu sorgular yapılamaz.
//
// CorrelationId nereden geliyor?
//   `CorrelationIdMiddleware` (Faz 4) request scope'una `CorrelationId` push'lar.
//   Serilog LogContext bu satıra otomatik ekliyor — ayrıca bir şey yapmıyoruz burada.
//
// Sample logging?
//   Şu an her request log'lanıyor. Çok yüksek throughput'ta sampling
//   (her N'inci request) ya da level-based filter (sadece >100ms) eklenebilir.
// =============================================================================
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Handling {RequestName}", requestName);

        // Stopwatch.StartNew: Stopwatch.GetTimestamp + Frequency çift dönüştürmesinden
        // daha okunaklı. Allocation farkı negligible — micro-optimize etmiyoruz.
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        _logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms",
            requestName, sw.ElapsedMilliseconds);

        return response;
    }
}
