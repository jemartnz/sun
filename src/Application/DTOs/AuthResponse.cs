namespace Application.DTOs;

public sealed record AuthResponse(string Token, string RefreshToken, Guid UserId, string Email);
