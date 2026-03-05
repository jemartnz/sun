using Application.DTOs;
using Application.Interfaces;
using Domain.Commons;
using Domain.Entities;
using Domain.ValueObjects;
using MediatR;

namespace Application.Features.Users;

public sealed class UpdateUserAddressHandler : IRequestHandler<UpdateUserAddressCommand, Result<UserResponse>>
{
    private readonly IUserRepository _userRepository;

    public UpdateUserAddressHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserResponse>> Handle(UpdateUserAddressCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);

        if (user is null)
            return Result<UserResponse>.Failure(UserErrors.NotFound);

        var addressResult = Address.Create(request.Street, request.City, request.Country, request.ZipCode ?? "");

        if (!addressResult.IsSuccess)
            return Result<UserResponse>.Failure(addressResult.Error);

        user.UpdateAddress(addressResult.Value);

        await _userRepository.SaveChangesAsync(ct);

        return Result<UserResponse>.Success(new UserResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email.Value,
            user.Role.ToString(),
            user.CreatedAtUtc));
    }
}
