using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAdminAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        var adminExists = await context.Users.AnyAsync(u => u.Role == UserRole.Admin);
        if (adminExists)
        {
            logger.LogInformation("Usuario administrador ya existe. Seed omitido.");
            return;
        }

        var email = config["AdminSeed:Email"] ?? "admin@sun.app";
        var password = config["AdminSeed:Password"] ?? "Admin1234!";

        var emailResult = Email.Create(email);
        var passwordResult = Password.Create(password);

        if (emailResult.IsFailure || passwordResult.IsFailure)
        {
            logger.LogError("Credenciales de admin inválidas en la configuración. Seed abortado.");
            return;
        }

        var hash = hasher.Hash(passwordResult.Value.Value);
        var userResult = User.Create("Admin", "Sun", emailResult.Value, hash, UserRole.Admin);

        if (userResult.IsFailure)
        {
            logger.LogError("Error al crear el usuario admin: {Error}", userResult.Error.Message);
            return;
        }

        context.Users.Add(userResult.Value);
        await context.SaveChangesAsync();

        logger.LogInformation("Usuario administrador creado: {Email}", email);
    }
}
