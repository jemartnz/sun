using Domain.Commons;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class User : BaseEntity
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; }
    public Address? Address { get; private set; }
    public UserRole Role { get; private set; }

    private User()
    {
        FirstName = null!;
        LastName = null!;
        PasswordHash = null!;
        Email = null!;
        Role = UserRole.User;
    }

    private User(string firstName, string lastName, Email email, string passwordHash, UserRole role)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }

    public static Result<User> Create(
        string firstName,
        string lastName,
        Email email,
        string passwordHash,
        UserRole role = UserRole.User)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return Result<User>.Failure(UserErrors.FirstNameRequired);

        if (string.IsNullOrWhiteSpace(lastName))
            return Result<User>.Failure(UserErrors.LastNameRequired);

        var user = new User(firstName.Trim(), lastName.Trim(), email, passwordHash, role);
        return Result<User>.Success(user);
    }

    public void UpdateAddress(Address address)
    {
        Address = address;
        MarkAsUpdated();
    }

    public void UpdateInfo(string firstName, string lastName, Email email, string passwordHash)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PasswordHash = passwordHash;
        MarkAsUpdated();
    }

    public void AssignRole(UserRole role)
    {
        Role = role;
        MarkAsUpdated();
    }
}

public static class UserErrors
{
    public static readonly Error FirstNameRequired = new("User.FirstNameRequired", "El nombre es obligatorio.");
    public static readonly Error LastNameRequired = new("User.LastNameRequired", "El apellido es obligatorio.");
    public static readonly Error EmailAlreadyExists = new("User.EmailAlreadyExists", "Ya existe un usuario con ese email.");
    public static readonly Error NotFound = new("User.NotFound", "Usuario no encontrado.");
    public static readonly Error InvalidCredentials = new("User.InvalidCredentials", "Email o contraseña incorrectos.");
}
