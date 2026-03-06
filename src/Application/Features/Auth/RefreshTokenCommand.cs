using Application.DTOs;
using Domain.Commons;
using MediatR;

namespace Application.Features.Auth;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthResponse>>;
