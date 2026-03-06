using Application.Interfaces;
using Domain.Commons;
using Domain.Entities;
using MediatR;

namespace Application.Features.Auth;

public sealed class RevokeTokenHandler : IRequestHandler<RevokeTokenCommand, Result>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public RevokeTokenHandler(IRefreshTokenRepository refreshTokenRepository)
        => _refreshTokenRepository = refreshTokenRepository;

    public async Task<Result> Handle(RevokeTokenCommand request, CancellationToken ct)
    {
        var token = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, ct);
        if (token is null)
            return Result.Failure(RefreshToken.Errors.NotFound);

        token.Revoke();
        await _refreshTokenRepository.SaveChangesAsync(ct);

        return Result.Success();
    }
}
