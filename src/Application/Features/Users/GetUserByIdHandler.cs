using Application.DTOs;
using Application.Interfaces;
using Domain.Commons;
using Domain.Entities;
using MediatR;

namespace Application.Features.Users;

public sealed class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, Result<UserResponse>>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserResponse>> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);

        if (user is null)
            return Result<UserResponse>.Failure(UserErrors.NotFound);

        return Result<UserResponse>.Success(new UserResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email.Value,
            user.Role.ToString(),
            user.CreatedAtUtc));
    }
}
