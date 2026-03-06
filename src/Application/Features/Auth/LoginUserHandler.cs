using Application.DTOs;
using Application.Interfaces;
using Domain.Commons;
using Domain.Entities;
using Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Application.Features.Auth;

public sealed class LoginUserHandler : IRequestHandler<LoginUserCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly int _refreshTokenExpiryDays;

    public LoginUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IRefreshTokenRepository refreshTokenRepository,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenExpiryDays = int.TryParse(configuration["Jwt:RefreshTokenExpiryDays"], out var days) ? days : 7;
    }

    public async Task<Result<AuthResponse>> Handle(LoginUserCommand request, CancellationToken ct)
    {
        // 1. Validar formato email
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result<AuthResponse>.Failure(UserErrors.InvalidCredentials);

        // 2. Buscar usuario
        var user = await _userRepository.GetByEmailAsync(emailResult.Value, ct);
        if (user is null)
            return Result<AuthResponse>.Failure(UserErrors.InvalidCredentials);

        // 3. Verificar password (contra el hash almacenado)
        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponse>.Failure(UserErrors.InvalidCredentials);

        // 4. Generar access token y refresh token
        var accessToken = _tokenGenerator.Generate(user);
        var refreshTokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshToken = RefreshToken.Create(
            user.Id, refreshTokenValue,
            DateTime.UtcNow.AddDays(_refreshTokenExpiryDays));

        await _refreshTokenRepository.AddAsync(refreshToken, ct);
        await _refreshTokenRepository.SaveChangesAsync(ct);

        return Result<AuthResponse>.Success(
            new AuthResponse(accessToken, refreshTokenValue, user.Id, user.Email.Value));
    }
}
