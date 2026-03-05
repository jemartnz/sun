using Application.DTOs;
using Application.Interfaces;
using Domain.Commons;
using Domain.Entities;
using Domain.ValueObjects;
using MediatR;

namespace Application.Features.Users;

public sealed class UpdateUserHandler : IRequestHandler<UpdateUserCommand, Result<UserResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UpdateUserHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<UserResponse>> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return Result<UserResponse>.Failure(UserErrors.NotFound);

        if (string.IsNullOrWhiteSpace(request.FirstName))
            return Result<UserResponse>.Failure(UserErrors.FirstNameRequired);

        if (string.IsNullOrWhiteSpace(request.LastName))
            return Result<UserResponse>.Failure(UserErrors.LastNameRequired);

        var emailResult = Email.Create(request.Email);
        if (!emailResult.IsSuccess)
            return Result<UserResponse>.Failure(emailResult.Error);

        var existingUser = await _userRepository.GetByEmailAsync(emailResult.Value, ct);
        if (existingUser is not null && existingUser.Id != request.UserId)
            return Result<UserResponse>.Failure(UserErrors.EmailAlreadyExists);

        var passwordResult = Password.Create(request.Password);
        if (!passwordResult.IsSuccess)
            return Result<UserResponse>.Failure(passwordResult.Error);

        var passwordHash = _passwordHasher.Hash(passwordResult.Value.Value);

        user.UpdateInfo(request.FirstName.Trim(), request.LastName.Trim(), emailResult.Value, passwordHash);

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
