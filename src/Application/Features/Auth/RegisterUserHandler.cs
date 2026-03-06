using Application.DTOs;
using Application.Interfaces;
using Domain.Commons;
using Domain.Entities;
using Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Application.Features.Auth;

public sealed class RegisterUserHandler : IRequestHandler<RegisterUserCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly int _refreshTokenExpiryDays;

    public RegisterUserHandler(
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

    public async Task<Result<AuthResponse>> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        // 1. Crear Value Object Email (se valida solo)
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result<AuthResponse>.Failure(emailResult.Error);

        // 2. Crear Value Object Password (se valida solo)
        var passwordResult = Password.Create(request.Password);
        if (passwordResult.IsFailure)
            return Result<AuthResponse>.Failure(passwordResult.Error);

        // 3. Verificar que no exista otro usuario con ese email
        var existingUser = await _userRepository.GetByEmailAsync(emailResult.Value, ct);
        if (existingUser is not null)
            return Result<AuthResponse>.Failure(UserErrors.EmailAlreadyExists);

        // 4. Hashear password
        var hash = _passwordHasher.Hash(passwordResult.Value.Value);

        // 5. Crear entidad User
        var userResult = User.Create(request.FirstName, request.LastName, emailResult.Value, hash);
        if (userResult.IsFailure)
            return Result<AuthResponse>.Failure(userResult.Error);

        // 6. Persistir usuario
        await _userRepository.AddAsync(userResult.Value, ct);
        await _userRepository.SaveChangesAsync(ct);

        // 7. Generar access token y refresh token
        var accessToken = _tokenGenerator.Generate(userResult.Value);
        var refreshTokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshToken = RefreshToken.Create(
            userResult.Value.Id, refreshTokenValue,
            DateTime.UtcNow.AddDays(_refreshTokenExpiryDays));

        await _refreshTokenRepository.AddAsync(refreshToken, ct);
        await _refreshTokenRepository.SaveChangesAsync(ct);

        return Result<AuthResponse>.Success(
            new AuthResponse(accessToken, refreshTokenValue, userResult.Value.Id, emailResult.Value.Value));
    }
}
