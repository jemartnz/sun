using Domain.Commons;
using MediatR;

namespace Application.Features.Auth;

public sealed record RevokeTokenCommand(string RefreshToken) : IRequest<Result>;
