namespace CleanCore.Domain.Shared;

// =============================================================================
// Result Pattern (aka "Railway-Oriented Programming")
// =============================================================================
// Business hatalarını exception fırlatmadan taşımanın yolu. İki raydan biri:
//   Success rayı → değer taşır, bir sonraki handler'a geçer
//   Failure rayı → hata taşır, zincirin sonuna kadar handler'ları atlar
//
// Neden exception değil?
//   - Exception pahalı: stack unwinding maliyetli
//   - Exception "control flow" değil — "DB down" gibi gerçek arıza için
//   - İmza yalan söylemiyor: handler `Result<User>` döndürüyorsa "ya user ya error"
//     olduğunu tip sistemi garantiliyor — caller zincire devam etmeden kontrol eder.
//
// Exception'ı ne zaman fırlatıyoruz?
//   - ValidationException → FluentValidation pipeline'ı → 400
//   - Gerçek arıza (DbUpdateException, HttpRequestException vs) → 500
//
// Kullanım:
//   Result.Success();                                   // void başarı
//   Result.Failure(Error.Validation("...", "..."));     // void hata
//   Result<User> r = user;                              // implicit cast → Success
//   Result<User> r = UserErrors.NotFound;               // implicit cast → Failure
// =============================================================================
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        // Invariant check'leri: Success+error veya Failure+None mantıksız; ctor'da kilitliyoruz.
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Başarılı result'ın error'ı olamaz.");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Başarısız result error olmadan kurulamaz.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
}

// Result<T>: "Başarılı + değer" veya "başarısız + hata". Handler return tiplerinin büyük çoğunluğu bu.
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    // Sadece IsSuccess=true iken okunmalı — `!` operator'ü bu invariant'ı dokümante ediyor.
    // IsFailure iken çağırmak tanımsız davranış değil, explicit exception: "yanlış kullanım".
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Başarısız result'ın value'su okunamaz.");

    public static Result<T> Success(T value) => new(value, true, Error.None);

    // `new` keyword: base class'ın Failure(Error) metodunu gölgeliyor (Result<T> döndürüyoruz, Result değil).
    public static new Result<T> Failure(Error error) => new(default, false, error);

    // -------------------------------------------------------------------------
    // Implicit cast operator'ları — handler ergonomisi için.
    // -------------------------------------------------------------------------
    // `return user;` → otomatik `Result<User>.Success(user)`
    // `return UserErrors.NotFound;` → otomatik `Result<User>.Failure(error)`
    //
    // Tradeoff: Bazı takımlar implicit cast sevmez ("magic"). Biz tercih ettik:
    // handler kodunun gürültüsünü azaltıyor. İstenirse silip explicit Success/Failure çağrılır.
    // -------------------------------------------------------------------------
    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
