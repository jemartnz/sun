using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.Entities;

public sealed class UserTests
{
    private static Email ValidEmail => Email.Create("user@domain.com").Value;
    private const string ValidHash = "hash123";

    [Fact]
    public void Create_WithValidData_ReturnsSuccess()
    {
        var result = User.Create("Juan", "Pérez", ValidEmail, ValidHash);
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("Juan");
        result.Value.LastName.Should().Be("Pérez");
        result.Value.Email.Should().Be(ValidEmail);
    }

    [Fact]
    public void Create_WithEmptyFirstName_ReturnsFirstNameRequired()
    {
        var result = User.Create("", "Pérez", ValidEmail, ValidHash);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.FirstNameRequired);
    }

    [Fact]
    public void Create_WithWhitespaceFirstName_ReturnsFirstNameRequired()
    {
        var result = User.Create("   ", "Pérez", ValidEmail, ValidHash);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.FirstNameRequired);
    }

    [Fact]
    public void Create_WithEmptyLastName_ReturnsLastNameRequired()
    {
        var result = User.Create("Juan", "", ValidEmail, ValidHash);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.LastNameRequired);
    }

    [Fact]
    public void Create_DefaultRole_IsUserRole()
    {
        var result = User.Create("Juan", "Pérez", ValidEmail, ValidHash);
        result.Value.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public void Create_WithAdminRole_HasAdminRole()
    {
        var result = User.Create("Admin", "Sun", ValidEmail, ValidHash, UserRole.Admin);
        result.Value.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public void Create_TrimsFirstAndLastName()
    {
        var result = User.Create("  Juan  ", "  Pérez  ", ValidEmail, ValidHash);
        result.Value.FirstName.Should().Be("Juan");
        result.Value.LastName.Should().Be("Pérez");
    }

    [Fact]
    public void AssignRole_ChangesRole()
    {
        var user = User.Create("Juan", "Pérez", ValidEmail, ValidHash).Value;
        user.AssignRole(UserRole.Admin);
        user.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public void AssignRole_SetsUpdatedAtUtc()
    {
        var user = User.Create("Juan", "Pérez", ValidEmail, ValidHash).Value;
        user.AssignRole(UserRole.Admin);
        user.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void UpdateAddress_ChangesAddress()
    {
        var user = User.Create("Juan", "Pérez", ValidEmail, ValidHash).Value;
        var address = Address.Create("Calle 1", "Madrid", "España", "28001").Value;

        user.UpdateAddress(address);

        user.Address.Should().NotBeNull();
        user.Address!.Street.Should().Be("Calle 1");
    }
}
