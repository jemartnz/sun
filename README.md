# ☀️ Sun API

API REST moderna construida con **.NET 10** y **C# 14** siguiendo **Clean Architecture** y buenas prácticas de desarrollo backend.

---

## Stack Tecnológico

| Componente | Tecnología |
|---|---|
| Framework | .NET 10 / C# 14 |
| Arquitectura | Clean Architecture (4 capas) |
| Base de datos | SQL Server 2025 (Docker) |
| ORM | Entity Framework Core 10 |
| CQRS | MediatR 14 |
| Validación | FluentValidation + pipeline behavior |
| Autenticación | JWT con Refresh Tokens (JsonWebTokenHandler) |
| Hashing | Argon2id |
| Logging | Serilog (consola + archivo + Seq opcional) |
| Documentación | Swagger / Swashbuckle |
| Rate Limiting | Fixed Window (60 req/min por IP) |
| Health Checks | GET /health — estado de la BD |
| Contenedores | Dockerfile multi-stage + Docker Compose |
| Tests unitarios | xUnit + FluentAssertions + NSubstitute |
| Tests integración | Testcontainers (SQL Server) + Respawn + WebApplicationFactory |

---

## Inicio Rápido

### Opción A — Docker Compose (recomendado)

Levanta la API, SQL Server y Seq en un solo comando:

```bash
docker compose up -d
```

| Servicio | URL |
|---|---|
| API | http://localhost:8080 |
| Swagger UI | http://localhost:8080/swagger |
| Health Check | http://localhost:8080/health |
| Seq (logs) | http://localhost:8081 |

### Opción B — Desarrollo local

**Requisitos previos:**
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

```bash
# 1. Levantar SQL Server
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" \
  -p 1433:1433 --name sqlserver \
  -d mcr.microsoft.com/mssql/server:2025-latest

# 2. Ejecutar la API (aplica migraciones automáticamente)
dotnet run --project src/Api
```

La API estará disponible en:

- **HTTP:** http://localhost:5000
- **Swagger UI:** http://localhost:5000/swagger/
- **Health Check:** http://localhost:5000/health

---

## Endpoints

### Autenticación (sin JWT)

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/auth/register` | Registro. Retorna `token` + `refreshToken` |
| POST | `/api/auth/login` | Login. Retorna `token` + `refreshToken` |
| POST | `/api/auth/refresh` | Renueva el JWT con un refresh token (rotación) |
| POST | `/api/auth/revoke` | Revoca un refresh token (logout) |

### Usuarios (requiere JWT)

| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| GET | `/api/users` | Any | Listar usuarios paginados |
| GET | `/api/users/me` | Any | Usuario autenticado actual |
| GET | `/api/users/{id}` | Any | Usuario por Id |
| PUT | `/api/users/{id}` | Any | Actualizar usuario |
| PUT | `/api/users/{id}/address` | Any | Actualizar dirección |
| DELETE | `/api/users/{id}` | Admin | Eliminar usuario |

### Productos (requiere JWT)

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/products` | Listar productos. ?page, ?pageSize, ?name, ?minPrice, ?maxPrice, ?sortBy, ?sortOrder |
| GET | `/api/products/{id}` | Producto por Id |
| POST | `/api/products` | Crear producto |
| PUT | `/api/products/{id}` | Actualizar producto |
| PATCH | `/api/products/{id}/stock` | Actualizar stock |
| DELETE | `/api/products/{id}` | Eliminar producto |

### Órdenes (requiere JWT)

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/orders` | Crear orden — reduce stock automáticamente |

### Sistema (sin JWT)

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/health` | Estado de la API y conexión a BD |

---

## Ejemplos de Uso

**Registro:**

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Juan",
    "lastName": "Pérez",
    "email": "juan@email.com",
    "password": "MiPassword123"
  }'
```

Respuesta:
```json
{
  "token": "eyJ...",
  "refreshToken": "base64string...",
  "userId": "guid...",
  "email": "juan@email.com"
}
```

**Renovar JWT (cuando expira en 15 min):**

```bash
curl -X POST http://localhost:5000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{ "refreshToken": "base64string..." }'
```

**Crear orden:**

```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{
    "items": [
      { "productId": "guid...", "quantity": 2 }
    ]
  }'
```

---

## Estructura del Proyecto

```
Sun/
├── Dockerfile                  ← Build multi-stage (.NET 10, usuario no-root)
├── docker-compose.yml          ← API + SQL Server + Seq
├── .env.example                ← Variables de entorno documentadas
├── src/
│   ├── Domain/                 ← Entidades, Value Objects, Result Pattern, Enums
│   │   ├── Commons/            ← BaseEntity, Error, Result, Result<T>
│   │   ├── Entities/           ← User, Product, Order, OrderItem, RefreshToken
│   │   ├── Enums/              ← UserRole, OrderStatus
│   │   └── ValueObjects/       ← Email, Password, Address, Price
│   │
│   ├── Application/            ← Casos de uso (CQRS), DTOs, Interfaces, Validators
│   │   ├── Behaviors/          ← ValidationBehavior (MediatR pipeline → HTTP 422)
│   │   ├── Common/             ← PagedRequest, PagedResponse<T>
│   │   ├── DTOs/               ← AuthResponse, UserResponse, ProductResponse, OrderResponse
│   │   ├── Features/           ← Auth, Users, Products, Orders (Commands + Queries + Handlers)
│   │   └── Interfaces/         ← IUserRepository, IProductRepository, IOrderRepository, IRefreshTokenRepository, etc.
│   │
│   ├── Infrastructure/         ← EF Core, Repositorios, JWT, Argon2id, Health Check
│   │   ├── Persistence/        ← AppDbContext, Configs, Repositories, DatabaseHealthCheck, Migrations
│   │   └── Security/           ← Argon2PasswordHasher, JwtTokenGenerator
│   │
│   └── Api/                    ← Controllers, Middleware, Program.cs
│       ├── Controllers/        ← Auth, Users, Products, Orders
│       ├── Extensions/         ← ResultExtensions (Result → HTTP response)
│       └── Middlewares/        ← ExceptionMiddleware (422 + 500)
│
└── tests/
    ├── Domain.Tests/           ← 66 tests — Value Objects, Entidades, Result Pattern
    ├── Application.Tests/      ← 24 tests — Handlers con mocks (NSubstitute)
    └── Api.Tests/              ← 38 tests — Integración con SQL Server real (Testcontainers)
```

---

## Patrones Implementados

- **Clean Architecture** — Separación estricta en 4 capas con dependencias hacia adentro
- **CQRS** — Commands y Queries separados con MediatR 14
- **Result Pattern** — Errores de negocio como valores de retorno, sin excepciones
- **Value Objects** — Email, Password, Address, Price con validación encapsulada
- **Aggregate Root** — Order gestiona OrderItems; lógica de negocio en el dominio
- **Repository Pattern** — Interfaces en Application, implementaciones en Infrastructure
- **Refresh Token Rotation** — Cada uso del refresh token emite uno nuevo e invalida el anterior
- **ValidationBehavior** — FluentValidation como pipeline de MediatR → HTTP 422

---

## Seguridad

- **Argon2id** — Hashing de contraseñas (salt 16 bytes, hash 32 bytes, 64MB RAM, 3 iteraciones)
- **JWT de corta duración** — Expiry configurable (15 min por defecto)
- **Refresh Tokens** — Opacos, de larga duración (7 días), con rotación automática
- **RBAC** — Roles `User` y `Admin` incluidos en el JWT. `FallbackPolicy` requiere autenticación por defecto
- **Rate Limiting** — 60 requests por minuto por IP (Fixed Window)
- **Mensajes de error genéricos** en login para prevenir enumeración de emails

---

## Observabilidad

| Feature | Detalle |
|---|---|
| Request logging | `UseSerilogRequestLogging()` — método, ruta, status, duración |
| Archivo de logs | `logs/log-{fecha}.txt` — rotación diaria, retención 30 días |
| Seq | Sink opcional para búsqueda de logs estructurados. Activar con `Seq:ServerUrl` en config |
| Health Check | `GET /health` — verifica conectividad con SQL Server |

---

## Variables de Entorno

En producción las claves sensibles se inyectan via variables de entorno (ver `.env.example`):

| Variable | Descripción |
|---|---|
| `ConnectionStrings__Sun` | Connection string de SQL Server |
| `Jwt__Secret` | Clave para firmar tokens (Base64, ≥32 chars) |
| `Jwt__ExpiryMinutes` | Duración del JWT (default: 15) |
| `Jwt__RefreshTokenExpiryDays` | Duración del refresh token (default: 7) |
| `Cors__AllowedOrigins__0` | Origen permitido en producción |
| `Seq__ServerUrl` | URL del servidor Seq (vacío = deshabilitado) |
| `AdminSeed__Email` | Email del usuario admin inicial |
| `AdminSeed__Password` | Contraseña del usuario admin inicial |

---

## Tests

```bash
# Todos los tests
dotnet test

# Por proyecto
dotnet test tests/Domain.Tests
dotnet test tests/Application.Tests
dotnet test tests/Api.Tests       # requiere Docker (Testcontainers)
```

| Proyecto | Tests | Cobertura |
|---|---|---|
| Domain.Tests | 66 | Value Objects, Entidades, Result Pattern |
| Application.Tests | 24 | Handlers con repositorios mockeados |
| Api.Tests | 38 | Endpoints completos con SQL Server real |
| **Total** | **128** | |

---

## Comandos Útiles

```bash
# Ejecutar la API
dotnet run --project src/Api

# Agregar migración
dotnet ef migrations add NombreMigracion \
  --project src/Infrastructure \
  --startup-project src/Api \
  --output-dir Persistence/Migrations

# Levantar todo con Docker Compose
docker compose up -d

# Ver logs de la API
docker compose logs api -f
```

---

## Documentación del Proyecto

| Archivo | Descripción |
|---|---|
| `CLAUDE.md` | Contexto técnico completo — arquitectura, patrones, decisiones clave |
| `ROADMAP.md` | Estado de features: completadas y planificadas |
| `.env.example` | Variables de entorno requeridas para producción |

---

## Licencia

Este proyecto es de uso educativo y de aprendizaje de arquitectura de software.
