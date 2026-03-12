using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CleanCore.Api.Middleware;

// =============================================================================
// GlobalExceptionHandler — .NET 8+ IExceptionHandler pattern
// =============================================================================
// Niye `IExceptionHandler` (custom middleware değil)?
//   - DI-friendly: ILogger, options vs ctor'a alır (eski middleware static-method-like)
//   - `app.UseExceptionHandler()` zincirine kayıt edilir, birden çok handler kayıt edilebilir
//   - Cancellation, response writing API'ları daha modern (ValueTask)
//   - Eski custom middleware: try/catch + manual response write — daha kırılgan
//
// Mapping kuralı:
//   ValidationException     → 400 (FluentValidation hataları, field bazlı `errors` dict)
//   Diğer her şey           → 500
//
// Niye DomainException özel handle edilmiyor?
//   Önceden (Faz 1-2) `DomainException` tipi vardı, hiç fırlatılmıyordu (YAGNI).
//   Code review'da kaldırıldı — gerçek ihtiyaç doğunca eklenecek (ör. concurrency conflict).
//   Şu an business hataları **Result.Failure** ile dönüyor, exception olarak fırlatılmıyor.
//
// ProblemDetails (RFC 7807):
//   Standart hata response formatı. Status, Title, Detail, Type, Instance.
//   Client tarafı bu yapıyı bilirse parser yazmak zorunda kalmıyor.
// =============================================================================
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = MapException(exception);

        // 5xx = gerçek hata, log seviyesi error.
        // 4xx = caller hatası (validation), info/warning yeterli olur — log spam'i önlemek için
        // burada hiç log atmıyoruz (CorrelationId middleware request log zaten atıyor).
        if (status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception — {Path}", httpContext.Request.Path);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
            Type = $"https://httpstatuses.com/{status}",
            Instance = httpContext.Request.Path
        };

        // FluentValidation: `errors` field bazlı dictionary olarak gönderiliyor.
        // {
        //   "errors": {
        //     "Email": ["E-posta zorunludur"],
        //     "Password": ["En az 8 karakter olmalıdır"]
        //   }
        // }
        // Form bağlama yapan UI tarafı (React Hook Form vs) bu yapıyı doğrudan tüketebiliyor.
        if (exception is ValidationException validation)
        {
            problem.Extensions["errors"] = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (int Status, string Title) MapException(Exception ex) => ex switch
    {
        ValidationException => (StatusCodes.Status400BadRequest, "Doğrulama hatası"),
        // İleride: DbUpdateConcurrencyException → 409, OperationCanceledException → 499 vs.
        _ => (StatusCodes.Status500InternalServerError, "Beklenmedik hata")
    };
}
