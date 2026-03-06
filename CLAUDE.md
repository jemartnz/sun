# CLAUDE.md — Proyecto Sun

## Descripcion General

Sun es una API REST construida con **.NET 10** y **C# 14** siguiendo **Clean Architecture**. El proyecto implementa buenas practicas modernas de desarrollo backend incluyendo CQRS, Result Pattern, Value Objects, JWT Authentication, RBAC, Aggregate Root, FluentValidation y tests automatizados.

---

## Arquitectura

El proyecto sigue Clean Architecture con 4 capas. Las dependencias apuntan hacia adentro:

```
[Api] -> [Infrastructure] -> [Application] -> [Domain]
```

### Estructura de Carpetas

```
Sun/
|-- Sun.slnx
|-- src/
|   |-- Domain/                          <- Sin dependencias externas. Cero paquetes NuGet.
|   |   |-- Commons/
|   |   |   |-- BaseEntity.cs            <- Clase base: Id (Guid), CreatedAtUtc, UpdatedAtUtc
|   |   |   |-- Error.cs                 <- Record Error(Code, Message) para Result Pattern
|   |   |   +-- Result.cs                <- Result y Result<T> para flujo sin excepciones
|   |   |-- Entities/
|   |   |   |-- User.cs                  <- Email, PasswordHash, Address (VO, opcional), Role (UserRole), AssignRole()
|   |   |   |-- Product.cs               <- Price (VO), Stock, ReduceStock() con validacion
|   |   |   |-- Order.cs                 <- Aggregate Root: UserId, Status, Items, Create() valida y reduce stock
|   |   |   |-- OrderItem.cs             <- ProductId, Quantity, UnitPrice (snapshot Price VO). Create() es internal
|   |   |   +-- RefreshToken.cs          <- Token, UserId, ExpiresAtUtc, IsRevoked. IsActive, Revoke(), Errors anidados
|   |   |-- Enums/
|   |   |   |-- UserRole.cs              <- enum UserRole { User, Admin }
|   |   |   +-- OrderStatus.cs           <- enum OrderStatus { Pending }
|   |   |-- ValueObjects/
|   |   |   |-- Email.cs                 <- Validacion Regex, normaliza a lowercase
|   |   |   |-- Password.cs              <- Min 8 chars, mayuscula, digito
|   |   |   |-- Address.cs               <- Street, City, Country, ZipCode
|   |   |   +-- Price.cs                 <- Amount (decimal) + Currency (string, normaliza uppercase)
|   |   +-- Domain.csproj
|   |
|   |-- Application/                     <- Casos de uso. Depende solo de Domain.
|   |   |-- Behaviors/
|   |   |   +-- ValidationBehavior.cs    <- IPipelineBehavior MediatR: lanza ValidationException -> 422
|   |   |-- Common/
|   |   |   +-- PagedRequest.cs / PagedResponse.cs <- Paginacion generica
|   |   |-- DTOs/
|   |   |   |-- AuthResponse.cs          <- record(Token, RefreshToken, UserId, Email)
|   |   |   |-- UserResponse.cs          <- record(Id, FirstName, LastName, Email, Role, CreatedAtUtc)
|   |   |   |-- ProductResponse.cs       <- record(Id, Name, Description, PriceAmount, PriceCurrency, Stock)
|   |   |   |-- OrderResponse.cs         <- record(Id, UserId, Status, Items, CreatedAtUtc)
|   |   |   +-- OrderItemResponse.cs     <- record(ProductId, Quantity, UnitPriceAmount, UnitPriceCurrency)
|   |   |-- Features/
|   |   |   |-- Auth/                    <- Register + Login + RefreshToken + RevokeToken (Commands + Handlers + Validators)
|   |   |   |-- Products/                <- CRUD completo + Stock (Commands/Queries + Handlers + Validators)
|   |   |   |-- Users/                   <- CRUD completo (Commands/Queries + Handlers)
|   |   |   +-- Orders/                  <- CreateOrder (Command + Handler + Validator)
|   |   |-- Interfaces/
|   |   |   |-- IUserRepository.cs       <- GetByIdAsync, GetByEmailAsync, AddAsync, RemoveAsync, SaveChangesAsync
|   |   |   |-- IProductRepository.cs    <- GetByIdAsync, GetAllAsync, AddAsync, RemoveAsync, SaveChangesAsync
|   |   |   |-- IOrderRepository.cs      <- AddAsync, GetByIdAsync (Include Items), SaveChangesAsync
|   |   |   |-- IRefreshTokenRepository.cs <- AddAsync, GetByTokenAsync, SaveChangesAsync
|   |   |   |-- IPasswordHasher.cs       <- Hash(string), Verify(string, string)
|   |   |   +-- ITokenGenerator.cs       <- Generate(User)
|   |   +-- Application.csproj           <- Paquetes: MediatR 14, FluentValidation, Microsoft.Extensions.Configuration.Abstractions
|   |
|   |-- Infrastructure/                  <- Implementaciones tecnicas. Depende de Application.
|   |   |-- Persistence/
|   |   |   |-- Configs/
|   |   |   |   |-- UserConfig.cs        <- OwnsOne Email, OwnsOne Address (nullable), Role como string
|   |   |   |   |-- ProductConfig.cs     <- OwnsOne Price
|   |   |   |   |-- OrderConfig.cs       <- HasMany Items (cascade), Status como string
|   |   |   |   |-- OrderItemConfig.cs   <- OwnsOne UnitPrice
|   |   |   |   +-- RefreshTokenConfig.cs <- HasIndex Token (unique), IsRevoked, ExpiresAtUtc
|   |   |   |-- Migrations/              <- Generadas con dotnet ef, auto-aplicadas al arrancar
|   |   |   |-- AppDbContext.cs          <- DbSet<User>, DbSet<Product>, DbSet<Order>, DbSet<RefreshToken>
|   |   |   |-- DatabaseMigrator.cs      <- ApplyMigrationsAsync()
|   |   |   |-- DatabaseSeeder.cs        <- SeedAdminAsync() — crea admin si no existe (lee AdminSeed config)
|   |   |   |-- DatabaseHealthCheck.cs   <- IHealthCheck: context.Database.CanConnectAsync()
|   |   |   |-- UserRepository.cs
|   |   |   |-- ProductRepository.cs
|   |   |   |-- OrderRepository.cs       <- Include(o => o.Items) en GetByIdAsync
|   |   |   +-- RefreshTokenRepository.cs <- GetByTokenAsync (FirstOrDefault por Token)
|   |   |-- Security/
|   |   |   |-- Argon2PasswordHasher.cs  <- Argon2id: salt 16B, hash 32B, 64MB RAM, 3 iteraciones
|   |   |   +-- JwtTokenGenerator.cs     <- JsonWebTokenHandler + SecurityTokenDescriptor + ClaimTypes.Role
|   |   |-- DependencyInjection.cs       <- AddInfrastructure() registra repos, hasher, token generator, health check
|   |   +-- Infrastructure.csproj
|   |
|   +-- Api/                             <- Punto de entrada HTTP.
|       |-- Controllers/
|       |   |-- AuthController.cs        <- [AllowAnonymous] — POST register, login, refresh, revoke
|       |   |-- UsersController.cs       <- CRUD completo; DELETE solo Admin
|       |   |-- ProductsController.cs    <- CRUD completo
|       |   +-- OrdersController.cs      <- POST /api/orders — extrae UserId del claim "sub"
|       |-- Extensions/
|       |   +-- ResultExtensions.cs      <- ToActionResult() y ToNoContentResult() — convierte Result a HTTP
|       |-- Middlewares/
|       |   +-- ExceptionMiddleware.cs   <- ValidationException -> 422, Exception -> 500 + log
|       |-- Program.cs                   <- Serilog+Seq, JWT, CORS, RBAC, Rate Limiting, Swagger, Health Checks
|       +-- Api.csproj                   <- Paquetes: Serilog.AspNetCore, Serilog.Sinks.File, Serilog.Sinks.Seq, Swashbuckle
|
+-- tests/
    |-- Domain.Tests/                    <- 66 tests — xUnit + FluentAssertions
    |   |-- GlobalUsings.cs
    |   |-- Commons/                     <- ResultTests
    |   |-- ValueObjects/                <- Email, Password, Price, Address
    |   +-- Entities/                    <- User, Product, Order
    |-- Application.Tests/               <- 24 tests — xUnit + FluentAssertions + NSubstitute
    |   |-- GlobalUsings.cs
    |   +-- Features/
    |       |-- Auth/                    <- RegisterUserHandler, LoginUserHandler
    |       |-- Users/                   <- GetUserByIdHandler
    |       |-- Products/                <- CreateProductHandler
    |       +-- Orders/                  <- CreateOrderHandler
    +-- Api.Tests/                       <- 38 tests — xUnit + FluentAssertions + Testcontainers + Respawn
        |-- GlobalUsings.cs
        |-- ApiFactory.cs                <- WebApplicationFactory<Program> + MsSqlContainer + override rate limiter (PermitLimit=MaxValue)
        |-- DatabaseCleaner.cs           <- Respawn wrapper, ignora __EFMigrationsHistory
        |-- BaseIntegrationTest.cs       <- [Collection("Integration")], re-siembra admin en InitializeAsync
        |-- Auth/                        <- AuthEndpointsTests, RefreshTokenEndpointsTests
        |-- Health/                      <- HealthEndpointsTests
        |-- Users/
        |-- Products/
        +-- Orders/
```

---

## Patrones y Decisiones Arquitectonicas

### Result Pattern
Las entidades y Value Objects retornan `Result<T>` en vez de lanzar excepciones para errores de negocio. `Result` (no generico) se usa para operaciones sin cuerpo de respuesta (HTTP 204). Las excepciones se reservan para errores inesperados (capturados por ExceptionMiddleware).

### Value Objects con EF Core
Los Value Objects (Email, Address, Price) se mapean con `OwnsOne()` en las configuraciones de EF Core. Se almacenan como columnas en la tabla padre, no como tablas separadas. Las entidades tienen un constructor privado sin parametros (`private User() { ... }`) con `null!` para que EF Core pueda materializarlas.

### CQRS con MediatR
Commands (escritura) y Queries (lectura) separados. El Controller envia a MediatR -> MediatR encuentra el Handler -> Handler usa interfaces de repositorio.

### Aggregate Root
`Order` es el Aggregate Root. `OrderItem.Create()` es `internal` — solo `Order` puede crear items. `Order.Create()` valida, llama `product.ReduceStock()` y crea OrderItems con snapshot de precio. EF Core change tracking persiste los cambios de stock junto con la orden en un unico `SaveChangesAsync`.

### RBAC (Control de Acceso Basado en Roles)
`UserRole` enum con valores `User` y `Admin`. El JWT incluye `ClaimTypes.Role`. `FallbackPolicy` requiere usuario autenticado por defecto. Politica `AdminOnly` para endpoints sensibles (DELETE /api/users/{id}).

### ValidationBehavior (FluentValidation)
`IPipelineBehavior` ejecuta los validators antes de cada Handler. Si falla, lanza `ValidationException`. `ExceptionMiddleware` la captura y retorna HTTP 422 con errores agrupados por campo.

### Configuraciones de Entidades Separadas
Cada entidad tiene su propio archivo `IEntityTypeConfiguration<T>` en `Infrastructure/Persistence/Configs/`. AppDbContext las detecta automaticamente con `ApplyConfigurationsFromAssembly()`.

### Migraciones Automaticas
`DatabaseMigrator.ApplyMigrationsAsync()` se ejecuta al arrancar la API. Detecta migraciones pendientes y las aplica automaticamente.

### Refresh Tokens
Login y Register retornan `{ token, refreshToken, userId, email }`. El JWT dura 15 min (configurable via `Jwt:ExpiryMinutes`). El refresh token dura 7 dias (configurable via `Jwt:RefreshTokenExpiryDays`). Al renovar se revoca el token anterior (rotacion). `RefreshToken.IsActive` = !IsRevoked && !IsExpired.

### Health Check
`GET /health` usa `DatabaseHealthCheck` (implementa `IHealthCheck`) que llama `context.Database.CanConnectAsync()`. Registrado en `DependencyInjection.cs`. Excluido de autenticacion (`AllowAnonymous`) y rate limiting.

### CORS
Politica `AllowConfigured`. En Development permite cualquier origen. En Production usa la lista de `Cors:AllowedOrigins` en config. Override via env vars: `Cors__AllowedOrigins__0=https://...`

### Seq (Logging)
`Serilog.Sinks.Seq` agregado a `Api.csproj`. Solo activo si `Seq:ServerUrl` no esta vacio. En docker-compose, Seq corre en puerto 5341 (API) y 8081 (UI).

---

## Decisiones Tecnicas Clave

| Decision | Detalle |
|----------|---------|
| `MapInboundClaims = false` | Requerido en AddJwtBearer para que `User.FindFirst("sub")` funcione. Sin esto ASP.NET mapea `sub` a `ClaimTypes.NameIdentifier` |
| `[AllowAnonymous]` en AuthController | Necesario cuando FallbackPolicy esta activa. Sin esto `/api/auth/*` retorna 401 |
| `Order.Create()` valida y reduce stock | La logica de negocio vive en el Domain. Un unico `SaveChangesAsync` persiste orden + cambios de stock |
| `OrderItem.Create()` es `internal` | Solo `Order` puede crear OrderItems — refuerza Aggregate Root |
| `UserRole` como enum | Para 2 roles sin propiedades adicionales, enum + columna string es mas simple que tabla many-to-many |
| Testcontainers sobre EF InMemory | SQL Server real garantiza que migraciones, constraints y `OwnsOne` funcionen igual que produccion |
| Respawn + re-seed en `InitializeAsync` | Respawn limpia datos entre tests. El admin se re-siembra antes de cada test para que `LoginAsAdminAsync` funcione |
| `ValidationBehavior` lanza excepcion | Lanza `FluentValidation.ValidationException`. `ExceptionMiddleware` la captura -> HTTP 422 |
| `GetValue<T>` no disponible en Abstractions | `Microsoft.Extensions.Configuration.Abstractions` solo tiene `IConfiguration`. Usar `int.TryParse(config["key"], out var v)` en Application layer |
| Rate limiter override en ApiFactory | `IConfigureOptions<RateLimiterOptions>` removidos y reemplazados con `PermitLimit=int.MaxValue` para evitar 429 en tests. Requiere `<FrameworkReference Include="Microsoft.AspNetCore.App" />` en Api.Tests.csproj |
| RefreshToken con errores anidados | `RefreshToken.Errors.Invalid` -> 400, `RefreshToken.Errors.NotFound` -> 404 (el codigo contiene "NotFound") |

---

## Stack Tecnologico

| Componente | Tecnologia |
|---|---|
| Framework | .NET 10, C# 14 |
| Base de datos | SQL Server en Docker (puerto 1433) |
| ORM | Entity Framework Core 10 |
| CQRS | MediatR 14 |
| Validacion | FluentValidation + ValidationBehavior pipeline |
| Autenticacion | JWT con JsonWebTokenHandler (API moderna) |
| Hashing | Argon2id (Konscious.Security.Cryptography) |
| Logging | Serilog -> Consola + Archivo (logs/log-{fecha}.txt) |
| Documentacion | Swagger/Swashbuckle |
| Rate Limiting | Fixed Window: 60 requests/minuto por IP |
| Health Checks | Microsoft.Extensions.Diagnostics.HealthChecks (built-in) + DatabaseHealthCheck custom |
| Logging produccion | Serilog.Sinks.Seq (opcional, configurable) |
| Contenedores | Dockerfile multi-stage + docker-compose (API + SQL Server + Seq) |
| Tests unitarios | xUnit + FluentAssertions + NSubstitute |
| Tests integracion | xUnit + Testcontainers (SQL Server) + Respawn + WebApplicationFactory |

---

## Endpoints

| Metodo | Ruta | Auth | Rol | Descripcion |
|--------|------|------|-----|-------------|
| POST | `/api/auth/register` | No | — | Registro. Body: FirstName, LastName, Email, Password |
| POST | `/api/auth/login` | No | — | Login. Retorna JWT |
| GET | `/api/users` | JWT | Any | Listar usuarios paginados. ?page, ?pageSize, ?sortBy, ?sortOrder |
| GET | `/api/users/me` | JWT | Any | Usuario autenticado actual (extrae Id del claim "sub") |
| GET | `/api/users/{id}` | JWT | Any | Usuario por Id |
| PUT | `/api/users/{id}` | JWT | Any | Actualizar usuario |
| PUT | `/api/users/{id}/address` | JWT | Any | Actualizar direccion |
| DELETE | `/api/users/{id}` | JWT | Admin | Eliminar usuario (HTTP 204) |
| GET | `/api/products` | JWT | Any | Listar productos. ?page, ?pageSize, ?name, ?minPrice, ?maxPrice, ?sortBy, ?sortOrder |
| GET | `/api/products/{id}` | JWT | Any | Producto por Id |
| POST | `/api/products` | JWT | Any | Crear producto |
| PUT | `/api/products/{id}` | JWT | Any | Actualizar producto |
| PATCH | `/api/products/{id}/stock` | JWT | Any | Actualizar stock |
| DELETE | `/api/products/{id}` | JWT | Any | Eliminar producto (HTTP 204) |
| POST | `/api/orders` | JWT | Any | Crear orden — reduce stock automaticamente |
| POST | `/api/auth/refresh` | No | — | Renovar JWT con refresh token (rotacion) |
| POST | `/api/auth/revoke` | No | — | Revocar refresh token (logout) |
| GET | `/health` | No | — | Estado de la API y BD. Responde "Healthy" / "Unhealthy" |

Swagger UI disponible en: `http://localhost:5000/swagger/`

---

## Configuracion

### appsettings.json
```json
{
  "ConnectionStrings": {
    "Sun": "Server=localhost,1433;Database=SunDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true"
  },
  "Jwt": {
    "Secret": "RXN0YUVzVW5hQ2xhdmVTZWNyZXRhTXV5TGFyZ2FRdWVEZWJlVGVuZXJBbE1lbm9zMzJDYXJhY3RlcmVzIQ==",
    "Issuer": "SunApp",
    "Audience": "SunApp",
    "ExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "AdminSeed": {
    "Email": "admin@sun.app",
    "Password": "Admin1234!"
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:3000", "http://localhost:5173" ]
  },
  "Seq": {
    "ServerUrl": ""
  }
}
```

### Docker Compose (API + SQL Server + Seq)
```bash
docker compose up -d
# API en http://localhost:8080
# Seq UI en http://localhost:8081
```

### Docker SQL Server (standalone)
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" \
  -p 1433:1433 --name sqlserver \
  -d mcr.microsoft.com/mssql/server:2025-latest
```

---

## Comandos Utiles

```bash
# Crear migracion
dotnet ef migrations add NombreMigracion --project src/Infrastructure --startup-project src/Api --output-dir Persistence/Migrations

# Listar migraciones
dotnet ef migrations list --project src/Infrastructure --startup-project src/Api

# Revertir ultima migracion (si no se aplico)
dotnet ef migrations remove --project src/Infrastructure --startup-project src/Api

# Ejecutar la API (migraciones se aplican automaticamente)
dotnet run --project src/Api

# Ejecutar todos los tests
dotnet test

# Ejecutar tests de un proyecto especifico
dotnet test tests/Domain.Tests
dotnet test tests/Application.Tests
dotnet test tests/Api.Tests
```

---

## Flujo de Errores

```
Error de validacion FluentValidation  -> ValidationException  -> ExceptionMiddleware -> HTTP 422
Error de validacion (Value Object)    -> Result.Failure       -> Controller -> HTTP 400
Error de negocio (email duplicado)    -> Result.Failure       -> Controller -> HTTP 409
Credenciales invalidas                -> Result.Failure       -> Controller -> HTTP 401
Entidad no encontrada                 -> Result.Failure       -> Controller -> HTTP 404
Stock insuficiente                    -> Result.Failure       -> Controller -> HTTP 400
Error inesperado (BD caida)           -> Excepcion            -> ExceptionMiddleware -> HTTP 500 + log
```

---

## Dependencia entre Capas (Regla Estricta)

- **Domain**: No referencia a ningun otro proyecto. Cero paquetes NuGet.
- **Application**: Referencia solo a Domain. Paquetes: MediatR, FluentValidation, Microsoft.Extensions.Configuration.Abstractions.
- **Infrastructure**: Referencia a Application y Domain. Paquetes: EF Core, Argon2, JWT.
- **Api**: Referencia a Application, Domain e Infrastructure. Paquetes: Serilog, Swashbuckle, Serilog.Sinks.Seq.

---

## Convenciones del Proyecto

- Namespaces sin prefijo de solucion: `Domain.Entities`, `Application.Features.Auth`, `Infrastructure.Persistence`, `Api.Controllers`
- Entidades tienen factory method estatico `Create()` que retorna `Result<T>`
- Entidades tienen constructor privado sin parametros para EF Core con `null!`
- Value Objects tienen constructor privado y factory method `Create()` con validacion
- Errores de dominio agrupados en clases estaticas anidadas: `UserErrors`, `ProductErrors`, `OrderErrors`, `EmailErrors`, `RefreshToken.Errors`
- Cada feature tiene su propia carpeta con Command/Query + Handler (+ Validator si aplica)
- Controllers solo reciben request -> envian a MediatR -> convierten Result a HTTP response
- Connection string se llama `"Sun"` en appsettings.json
- Tests usan `GlobalUsings.cs` para imports comunes por proyecto

## Estado del proyecto
Ver ROADMAP.md para el estado actual de features y el plan en progreso.
