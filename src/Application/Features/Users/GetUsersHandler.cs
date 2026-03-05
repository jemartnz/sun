using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Commons;
using MediatR;

namespace Application.Features.Users;

public sealed class GetUsersHandler : IRequestHandler<GetUsersQuery, Result<PagedResponse<UserResponse>>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<PagedResponse<UserResponse>>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var (users, totalCount) = await _userRepository.GetAllAsync(
            request.Page, request.PageSize,
            request.SortBy, request.SortOrder, ct);

        var items = users
            .Select(u => new UserResponse(u.Id, u.FirstName, u.LastName, u.Email.Value, u.Role.ToString(), u.CreatedAtUtc))
            .ToList();

        return Result<PagedResponse<UserResponse>>.Success(
            new PagedResponse<UserResponse>(items, totalCount, request.Page, request.PageSize));
    }
}
