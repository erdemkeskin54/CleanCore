using CleanCore.Application.Abstractions.Authentication;
using CleanCore.Domain.Shared;
using MediatR;

namespace CleanCore.Application.Auth.Refresh;

// Eski refresh token'ı verip yeni access + refresh pair almanın sözleşmesi.
// Handler içinde **rotation** uygulanıyor: eski token revoke + yeni pair issue.
// Detay: RefreshTokenCommandHandler.cs
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthResponse>>;
