using System.IdentityModel.Tokens.Jwt;
using System.Text;
using CleanCore.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace CleanCore.Api.Extensions;

// =============================================================================
// AuthenticationExtensions — JWT Bearer doğrulama yapılandırması
// =============================================================================
// Bu extension JwtProvider'ın ürettiği token'ları nasıl doğruladığımızı tanımlıyor.
//
// Validation parametreleri ne yapıyor?
//   ValidateIssuer           → Token'ı biz mi ürettik (Issuer claim eşleşmesi)?
//   ValidateAudience         → Token bizim audience'a mı yazılmış?
//   ValidateLifetime         → exp claim'i geçmiş mi?
//   ValidateIssuerSigningKey → Token imzası bizim secret'la mı yapılmış?
//   ClockSkew = 30s          → Sunucular arası saat sapmasına 30sn tolerans
//                              (default 5dk çok uzun — refresh token replay penceresi açar)
//   NameClaimType = "sub"    → User.Identity.Name → JWT'deki `sub` claim'inden gelir
//                              (default "name" claim'iydi, biz sub kullanıyoruz)
//
// `DefaultMapInboundClaims = false` neden static set?
//   .NET JWT library'si default olarak "sub" → ClaimTypes.NameIdentifier (URI)
//   "email" → ClaimTypes.Email (URI) gibi mapping yapar. Token üretirken kısa
//   ad yazıp okurken uzun URI ile aramak kafa karıştırıcı + bug kaynağı.
//   Static field olarak set ediliyor çünkü JwtSecurityTokenHandler'ın global config'i.
//   AddJwtAuthentication metodunun ilk satırı — handler henüz kullanılmadan ayar.
//
// SigningKey doğrulama:
//   Configuration'dan al, yoksa explicit hata fırlat. Boş key ile prod'a
//   gitmek = kapı açık bırakmak. Fail fast.
// =============================================================================
public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Static config — global etkili. Aksi halde claim adları uzun URI'lere maplenir.
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var signingKey = jwtSection["SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey appsettings'te tanımlı değil.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Sub
                };
            });

        services.AddAuthorization();
        return services;
    }
}
