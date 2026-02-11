namespace CleanCore.Domain.Shared;

// =============================================================================
// Error — Result pattern'in "failure" tarafı. Static Factory Method pattern.
// =============================================================================
// Neden factory metotlar (NotFound/Validation/...) public ctor yerine?
//   - Intent net: `Error.NotFound(...)` = "bu NotFound bir hata"
//   - ErrorType'ı yanlış yere set etme imkanı yok (ctor private olsaydı da aynı etki)
//   - Yeni error türü eklemek istediğimizde tek yer: yeni factory metot.
//
// Type alanı niye var?
//   Api katmanında `Error.Type` → HTTP status code mapping için:
//       NotFound     → 404
//       Validation   → 400
//       Conflict     → 409
//       Unauthorized → 401
//       Forbidden    → 403
//       Failure      → 500
//   Mapping: `src/CleanCore.Api/Extensions/ResultExtensions.cs`.
//
// Code alanı niye var?
//   Client taraf çeviri/i18n veya özel handling yapmak isterse ("User.NotFound"
//   karşısında "Order.NotFound") ayırt edilebilir string anahtar.
//
// record type niye?
//   Değer eşitliği gerekli — "aynı code + message + type" → aynı error.
//   UserErrors.NotFound karşılaştırmaları da record sayesinde referans değil, değer üzerinden.
// =============================================================================
public sealed record Error(string Code, string Message, ErrorType Type)
{
    // Sentinel "no error" — Success result'larda Error alanı boş kalmasın diye.
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    // Genel kullanım: argüman null geldiyse (son çare) bu error.
    public static readonly Error NullValue =
        new("General.Null", "Null değer sağlandı.", ErrorType.Failure);

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    public static Error Unauthorized(string code, string message) =>
        new(code, message, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string message) =>
        new(code, message, ErrorType.Forbidden);

    public static Error Failure(string code, string message) =>
        new(code, message, ErrorType.Failure);
}

// Numeric değerler sabit — appsettings veya log'a integer olarak yansırsa anlamlı kalır.
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5
}
