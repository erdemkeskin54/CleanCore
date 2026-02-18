using FluentValidation;
using MediatR;

namespace CleanCore.Application.Behaviors;

// =============================================================================
// ValidationBehavior — request başına validator'ları otomatik koşturan pipeline
// =============================================================================
// Akış:
//   1) DI'dan TRequest için register edilmiş tüm IValidator<TRequest>'leri al
//   2) Validator yoksa (kayıt komutu vs ile validator olmayan request'ler için) bypass
//   3) Validator(lar)ı paralel koştur (Task.WhenAll)
//   4) Hata varsa → ValidationException fırlat (Faz 4 GlobalExceptionHandler 400'e çevirir)
//
// Niye exception, niye Result.Failure değil?
//   Validation hatası ≠ business hata. Tipler farklı, error path farklı:
//     - Validation hatası: request malformed → 400 ProblemDetails + field bazlı errors dict
//     - Business hata: business rule conflict → 4xx Result.Failure(...)
//   İkisini ayrı tutmak mapping'i basit yapar (UnhandledExceptionBehavior bu istisnayı
//   bilinçli olarak rethrow eder).
//
// Niye paralel validator çalıştırma (Task.WhenAll)?
//   Tek request için birden çok validator olabilir (rare). Paralel koşmak gecikmeyi azaltır.
//   Validator'lar genelde stateless ve bağımsız → paralel güvenli.
// =============================================================================
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Validator yoksa (örn. parametresiz query) bypass — hızlı yol.
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        // Tüm validator'ları paralel çalıştır, hatalarını birleştir.
        var failures = (await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
