using Application.DTOs;
using Application.Interfaces;
using Domain.Commons;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Application.Features.Auth;

public sealed class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly int _refreshTokenExpiryDays;

    public RefreshTokenHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        ITokenGenerator tokenGenerator,
        IConfiguration configuration)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
        _refreshTokenExpiryDays = int.TryParse(configuration["Jwt:RefreshTokenExpiryDays"], out var days) ? days : 7;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var existing = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, ct);
        if (existing is null || !existing.IsActive)
            return Result<AuthResponse>.Failure(RefreshToken.Errors.Invalid);

        var user = await _userRepository.GetByIdAsync(existing.UserId, ct);
        if (user is null)
            return Result<AuthResponse>.Failure(RefreshToken.Errors.Invalid);

        // Rotacion: revocar el token anterior
        existing.Revoke();

        // Emitir nuevo refresh token
        var newTokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var newRefreshToken = RefreshToken.Create(
            user.Id, newTokenValue,
            DateTime.UtcNow.AddDays(_refreshTokenExpiryDays));

        await _refreshTokenRepository.AddAsync(newRefreshToken, ct);
        await _refreshTokenRepository.SaveChangesAsync(ct);

        var accessToken = _tokenGenerator.Generate(user);

        return Result<AuthResponse>.Success(
            new AuthResponse(accessToken, newTokenValue, user.Id, user.Email.Value));
    }
}
