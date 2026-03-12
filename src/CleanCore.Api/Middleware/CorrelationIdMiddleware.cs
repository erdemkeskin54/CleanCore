using Serilog.Context;

namespace CleanCore.Api.Middleware;

// =============================================================================
// CorrelationIdMiddleware — request başına izlenebilir kimlik
// =============================================================================
// Akış:
//   1) Gelen request'te `X-Correlation-Id` header'ı varsa onu kullan
//      (frontend tarafı kendi correlation id'sini geçirebilir, end-to-end trace)
//   2) Yoksa yeni Guid üret (32-char "N" formatında — tire'sız)
//   3) Response'a aynı header'ı yaz → client kendi log'unda saklayabilsin
//   4) Serilog LogContext'e push'la → request süresince TÜM log satırları bu id ile etiketlenir
//
// Niye scoped ServiceProvider yerine LogContext?
//   `LogContext.PushProperty` Serilog'un scope mekanizması. Async'te de korunuyor
//   (AsyncLocal). Her log satırına otomatik prop eklemenin en clean yolu.
//
// Kullanım — log'da nasıl görünür?
//   `[INF] Handling LoginCommand {CorrelationId="a1b2c3..."}`
//   Elastic/Seq'te `CorrelationId="a1b2c3..."` filter'ıyla bir request'in
//   tüm satırlarını çekebiliyorsun. Bug ayıklamada hayat kurtarır.
//
// Microservice geçişi:
//   Şu anki implementasyon tek servis. Microservice mimarisinde upstream'den gelen
//   correlation id'yi downstream HTTP çağrılarına da geçirmek gerekir
//   (HttpClient default header). Şu an YAGNI.
// =============================================================================
public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // İlk seçim: client'ın gönderdiği id (varsa). Distributed tracing için kritik.
        // Yedek: yeni Guid. "N" format = "ab12cd34..." (32 char, tire yok) → URL'de daha temiz.
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        // Client'a geri ver — kendi log'larında bu id'yi saklayabilsin.
        context.Response.Headers[HeaderName] = correlationId;
        context.Items[HeaderName] = correlationId;

        // PushProperty'in `using` ile dispose'u önemli — request bitince LogContext'ten temizlenir.
        // Aksi halde async-flow'da yanlış request'in id'si "yapışıp" kalabilir.
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
