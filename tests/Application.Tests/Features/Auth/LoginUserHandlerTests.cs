using Application.Features.Auth;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Tests.Features.Auth;

public sealed class LoginUserHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly LoginUserHandler _handler;

    public LoginUserHandlerTests()
    {
        _handler = new LoginUserHandler(_userRepository, _passwordHasher, _tokenGenerator);
    }

    private static User CreateUser()
        => User.Create("Juan", "Pérez", Email.Create("juan@domain.com").Value, "hashed").Value;

    private static LoginUserCommand ValidCommand => new("juan@domain.com", "Password1");

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsSuccess()
    {
        var user = CreateUser();
        _userRepository.GetByEmailAsync(Arg.Any<Email>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _tokenGenerator.Generate(Arg.Any<User>()).Returns("token123");

        var result = await _handler.Handle(ValidCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("token123");
    }

    [Fact]
    public async Task Handle_WithNonExistingEmail_ReturnsInvalidCredentials()
    {
        _userRepository.GetByEmailAsync(Arg.Any<Email>()).Returns((User?)null);

        var result = await _handler.Handle(ValidCommand, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ReturnsInvalidCredentials()
    {
        var user = CreateUser();
        _userRepository.GetByEmailAsync(Arg.Any<Email>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await _handler.Handle(ValidCommand, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_WithInvalidEmailFormat_ReturnsInvalidCredentials()
    {
        var command = ValidCommand with { Email = "not-an-email" };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.InvalidCredentials);
    }
}
