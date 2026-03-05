using Api.Extensions;
using Application.Features.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]  // Requiere JWT válido
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetUsersQuery query, CancellationToken ct)
    {
        var result = await _sender.Send(query, ct);
        return result.ToActionResult();
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var sub = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var result = await _sender.Send(new GetUserByIdQuery(userId), ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetUserByIdQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest request, CancellationToken ct)
    {
        var command = new UpdateUserCommand(id, request.FirstName, request.LastName, request.Email, request.Password);
        var result = await _sender.Send(command, ct);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new DeleteUserCommand(id), ct);
        return result.ToNoContentResult();
    }

    [HttpPut("{id:guid}/address")]
    public async Task<IActionResult> UpdateAddress(Guid id, UpdateUserAddressRequest request, CancellationToken ct)
    {
        var command = new UpdateUserAddressCommand(id, request.Street, request.City, request.Country, request.ZipCode);
        var result = await _sender.Send(command, ct);
        return result.ToActionResult();
    }
}

public sealed record UpdateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password);

public sealed record UpdateUserAddressRequest(
    string Street,
    string City,
    string Country,
    string? ZipCode);
