using CleanCore.Application.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CleanCore.Application;

// =============================================================================
// AddApplication — Application katmanı composition root
// =============================================================================
// Tek metot ile tüm Application bağımlılıklarını DI'a kaydet.
// Program.cs'te `builder.Services.AddApplication()` ile çağrılır — Application
// katmanı kendi iç yapısını dış dünyadan saklı tutuyor.
//
// `AssemblyReference`: Bu assembly'nin "type'ını verme" yolu. Tek satır marker
// class. typeof(AssemblyReference).Assembly = bu projenin assembly'si.
//   Alternatif: typeof(CreateUserCommand).Assembly — ama o handler taşınırsa kırılır.
//   AssemblyReference dummy class olarak hep yerinde kalır, kırılmaz.
// =============================================================================
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(AssemblyReference).Assembly;

        services.AddMediatR(cfg =>
        {
            // Tüm IRequestHandler<,>'ları otomatik bul ve kaydet.
            cfg.RegisterServicesFromAssembly(assembly);

            // -----------------------------------------------------------------
            // Pipeline behavior **sırası** kritik.
            // MediatR'da kayıt sırası = pipeline sırası. En dıştaki en önce kaydedilen.
            //
            //   Request → Unhandled → Logging → Validation → Handler
            //                ▲          ▲          ▲
            //                │          │          └── ValidationException → middleware 400 yapacak
            //                │          └── "Handled in Xms" log'u (success ve fail dahil)
            //                └── Beklenmedik exception (ValidationException hariç) → log + rethrow
            //
            // Niye bu sıra?
            //   Unhandled DIŞTA: validation hariç her exception'ı yakalar — single point error log.
            //   Logging ORTADA: validation hatası dahil her request'i ölç (latency telemetry).
            //   Validation İÇTE: handler'a sadece valid request gitsin.
            //
            // İleride: TransactionBehavior eklenebilir (en içe, handler'ın hemen dışına).
            // Şu an handler'lar SaveChangesAsync çağırıyor + interceptor'lar audit/soft delete yapıyor.
            // -----------------------------------------------------------------
            cfg.AddOpenBehavior(typeof(UnhandledExceptionBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // FluentValidation tarayıcısı: tüm AbstractValidator<T>'leri DI'a kaydet.
        // ValidationBehavior bunları IEnumerable<IValidator<TRequest>> olarak alır.
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
