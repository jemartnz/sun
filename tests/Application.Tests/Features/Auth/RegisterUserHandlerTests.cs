using Application.Features.Auth;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Tests.Features.Auth;

public sealed class RegisterUserHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _handler = new RegisterUserHandler(_userRepository, _passwordHasher, _tokenGenerator);
    }

    private static RegisterUserCommand ValidCommand => new(
        "Juan", "Pérez", "juan@domain.com", "Password1");

    [Fact]
    public async Task Handle_WithValidData_ReturnsSuccess()
    {
        _userRepository.GetByEmailAsync(Arg.Any<Email>()).Returns((User?)null);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed");
        _tokenGenerator.Generate(Arg.Any<User>()).Returns("token123");

        var result = await _handler.Handle(ValidCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("token123");
        result.Value.Email.Should().Be("juan@domain.com");
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ReturnsEmailAlreadyExists()
    {
        var existingUser = User.Create("Otro", "User", Email.Create("juan@domain.com").Value, "hash").Value;
        _userRepository.GetByEmailAsync(Arg.Any<Email>()).Returns(existingUser);

        var result = await _handler.Handle(ValidCommand, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.EmailAlreadyExists);
    }

    [Fact]
    public async Task Handle_WithInvalidEmailFormat_ReturnsEmailError()
    {
        var command = ValidCommand with { Email = "not-an-email" };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmailErrors.InvalidFormat);
    }

    [Fact]
    public async Task Handle_WithTooShortPassword_ReturnsPasswordError()
    {
        var command = ValidCommand with { Password = "Short1" };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PasswordErrors.TooShort);
    }

    [Fact]
    public async Task Handle_WithPasswordWithoutUppercase_ReturnsPasswordError()
    {
        var command = ValidCommand with { Password = "password123" };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PasswordErrors.MissingUppercase);
    }

    [Fact]
    public async Task Handle_WithValidData_PersistsUser()
    {
        _userRepository.GetByEmailAsync(Arg.Any<Email>()).Returns((User?)null);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed");
        _tokenGenerator.Generate(Arg.Any<User>()).Returns("token");

        await _handler.Handle(ValidCommand, CancellationToken.None);

        await _userRepository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
