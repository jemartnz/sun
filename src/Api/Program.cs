using Api.Middlewares;
using Application;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

namespace Api;

public class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ============================================================
        // 1. SERILOG — Configuración de Logging
        // ============================================================
        // Serilog reemplaza el logger por defecto de .NET.
        // Escribe logs estructurados a consola Y a un archivo de texto.
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
            .WriteTo.Console()
            .WriteTo.File(
                path: "logs/log-.txt",
                rollingInterval: RollingInterval.Day,  // Un archivo por día: log-2026-02-11.txt
                retainedFileCountLimit: 30,             // Mantener últimos 30 días
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        builder.Host.UseSerilog();

        // ============================================================
        // 2. SERVICIOS
        // ============================================================

        // Application: MediatR + FluentValidation validators + ValidationBehavior
        builder.Services.AddApplication();

        // Infrastructure: repositorios, hashers, token generator, DbContext
        builder.Services.AddInfrastructure(builder.Configuration);

        // ============================================================
        // 3. JWT AUTHENTICATION
        // ============================================================
        // Configura ASP.NET para validar tokens JWT en cada request protegido.
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));

            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        // ============================================================
        // 4. RATE LIMITING
        // ============================================================
        // Limita la cantidad de requests por IP para evitar abuso.
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("fixed", config =>
            {
                config.PermitLimit = 60;                         // 60 requests...
                config.Window = TimeSpan.FromMinutes(1);         // ...por minuto
                config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                config.QueueLimit = 0;                           // No encolar, rechazar directo
            });
        });

        // Controllers y Swagger
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        //builder.Services.AddSwaggerGen();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Ingrese el token JWT"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement

            {
                [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, document)] = []

            });
        });

        // ============================================================
        // BUILD & PIPELINE
        // ============================================================
        var app = builder.Build();

        // Aplicar Migraciones Automaticamente
        await app.Services.ApplyMigrationsAsync();

        // Seed de usuario administrador
        await app.Services.SeedAdminAsync();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Orden del pipeline importa:
        app.UseCors("AllowAll");
        app.UseMiddleware<ExceptionMiddleware>();          // 1° Atrapa excepciones
        app.UseSerilogRequestLogging();                    // 2° Loguea cada request HTTP
        app.UseRateLimiter();                              // 3° Limita requests
        app.UseAuthentication();                           // 4° Valida JWT
        app.UseAuthorization();                            // 5° Verifica permisos
        app.MapControllers().RequireRateLimiting("fixed"); // Aplica rate limit a todos los endpoints

        await app.RunAsync();
    }
}
