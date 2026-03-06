using Api.Extensions;
using Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    // ISender es la interfaz de MediatR para enviar requests
    public AuthController(ISender sender) => _sender = sender;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.ToActionResult();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginUserCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.ToActionResult();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.ToActionResult();
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(RevokeTokenCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.ToNoContentResult();
    }
}