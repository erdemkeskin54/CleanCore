using CleanCore.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace CleanCore.Api.Extensions;

// =============================================================================
// ResultExtensions — Result/Result<T> → IActionResult mapping
// =============================================================================
// Niye merkezi extension?
//   Her controller'da `if (result.IsSuccess) ... else ...` boilerplate'ından kurtul.
//   Hata türü → HTTP status mapping tek yerde — tutarsızlık riski sıfır.
//
// Mapping tablosu:
//   ErrorType.NotFound      → 404 Not Found
//   ErrorType.Validation    → 400 Bad Request
//   ErrorType.Conflict      → 409 Conflict
//   ErrorType.Unauthorized  → 401 Unauthorized
//   ErrorType.Forbidden     → 403 Forbidden
//   ErrorType.Failure       → 500 Internal Server Error
//
// `onSuccess` delegate niye opsiyonel?
//   - Default: Result<T>.IsSuccess → 200 OK + value JSON
//   - Default: Result.IsSuccess → 204 No Content
//   - Custom: Create endpoint'i 201 Created + Location header döner — onSuccess ile
//     `CreatedAtRoute(...)` build edilir.
//
// Future-proofing:
//   Yeni HTTP status gerekirse (429, 422 vs) ErrorType enum + switch'e tek satır.
//   Mapping kuralı tek yerde değişiyor → tüm endpoint'ler otomatik tutarlı.
// =============================================================================
public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        Func<T, IActionResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess is not null
                ? onSuccess(result.Value)
                : new OkObjectResult(result.Value);
        }

        return ToProblem(result.Error);
    }

    public static IActionResult ToActionResult(
        this Result result,
        Func<IActionResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess is not null
                ? onSuccess()
                : new NoContentResult();
        }

        return ToProblem(result.Error);
    }

    // ProblemDetails (RFC 7807) — endüstri standardı hata response.
    // Tüm hata response'ları aynı şekle sahip → client parser yazmak zorunda değil.
    private static ObjectResult ToProblem(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        var problem = new ProblemDetails
        {
            Status = status,
            // `Title` — error code (i18n key gibi davranır, client çevirmek isterse)
            // `Detail` — human-readable mesaj
            Title = error.Code,
            Detail = error.Message,
            Type = $"https://httpstatuses.com/{status}"
        };

        return new ObjectResult(problem) { StatusCode = status };
    }
}
