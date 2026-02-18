using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanCore.Application.Behaviors;

// =============================================================================
// UnhandledExceptionBehavior — beklenmedik hatalar için merkezi log noktası
// =============================================================================
// Pipeline'ın en dışında. Handler ya da iç behavior'lar fırlatan TÜM exception'ları
// yakalar, log'lar ve **rethrow** eder. Yutmaz.
//
// Niye yutmuyoruz, sadece log + rethrow?
//   - Yukarıda GlobalExceptionHandler (Api katmanı) zaten 500 ProblemDetails dönüyor.
//   - Burada yutarsak request "success" gibi davranır → caller hatalı state ile devam eder.
//
// Neden ValidationException'ı bilinçli rethrow ediyoruz (catch dışına)?
//   ValidationException kasıtlı fırlatılan, anlamlı bir akış: middleware bunu 400'e
//   çevirecek. "Beklenmedik" değil — log'da error olarak işaretlenmesi yanlış olur.
//   Bu yüzden ValidationException catch bloğu boş bir rethrow ile geçilir,
//   genel `Exception` catch'i sadece BEKLENMEDİK olanı işler.
//
// Performans:
//   Try/catch sıfır maliyetli (.NET CLR). Exception fırlatma maliyetli ama o
//   olağan akış değil — gerçekten arıza varsa zaten o overhead kabul edilebilir.
// =============================================================================
public sealed class UnhandledExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> _logger;

    public UnhandledExceptionBehavior(ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (ValidationException)
        {
            // Bu kasıtlı fırlatıldı — middleware'in görevi 400 yapmak.
            // Burada error log'una düşürmek "noise" yaratır.
            throw;
        }
        catch (Exception ex)
        {
            // GERÇEKTEN beklenmedik. Stack trace + request adı log'a düşsün ki tespit edebilelim.
            _logger.LogError(ex, "Beklenmedik exception — {RequestName}", typeof(TRequest).Name);
            throw;
        }
    }
}
