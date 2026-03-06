using Application.Features.Users;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Tests.Features.Users;

public sealed class GetUserByIdHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly GetUserByIdHandler _handler;

    public GetUserByIdHandlerTests()
    {
        _handler = new GetUserByIdHandler(_userRepository);
    }

    private static User CreateUser(UserRole role = UserRole.User)
        => User.Create("Juan", "Pérez", Email.Create("juan@domain.com").Value, "hash", role).Value;

    [Fact]
    public async Task Handle_WithExistingUser_ReturnsUserResponse()
    {
        var user = CreateUser();
        _userRepository.GetByIdAsync(user.Id).Returns(user);

        var result = await _handler.Handle(new GetUserByIdQuery(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(user.Id);
        result.Value.Email.Should().Be("juan@domain.com");
        result.Value.FirstName.Should().Be("Juan");
    }

    [Fact]
    public async Task Handle_WithNonExistingUser_ReturnsNotFound()
    {
        _userRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((User?)null);

        var result = await _handler.Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Handle_ReturnsCorrectRole()
    {
        var adminUser = CreateUser(UserRole.Admin);
        _userRepository.GetByIdAsync(adminUser.Id).Returns(adminUser);

        var result = await _handler.Handle(new GetUserByIdQuery(adminUser.Id), CancellationToken.None);

        result.Value.Role.Should().Be("Admin");
    }
}
