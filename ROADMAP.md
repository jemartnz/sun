# ROADMAP.md — Proyecto Sun

> Visión de alto nivel del producto. Qué existe, qué viene, en qué orden.
> Última actualización: 6 Marzo 2026

---

## Leyenda

| Estado | Significado |
|--------|-------------|
| ✅ | Implementado y funcionando |
| 🔧 | En progreso o parcialmente implementado |
| 📋 | Planificado — próximo en la cola |
| 💡 | Idea futura — sin prioridad definida |

---

## Estructura del Proyecto

```
Sun/
├── Sun.slnx
├── src/
│   ├── Domain/                          ← Sin dependencias externas
│   │   ├── Commons/                     ← BaseEntity, Error, Result, Result<T>
│   │   ├── Entities/                    ← User, Product, Order, OrderItem
│   │   ├── Enums/                       ← UserRole, OrderStatus
│   │   └── ValueObjects/                ← Email, Password, Address, Price
│   ├── Application/                     ← Casos de uso. Depende de Domain
│   │   ├── Behaviors/                   ← ValidationBehavior (MediatR pipeline)
│   │   ├── Common/                      ← PagedRequest, PagedResponse<T>
│   │   ├── DTOs/                        ← AuthResponse, UserResponse, ProductResponse, OrderResponse, OrderItemResponse
│   │   ├── Features/
│   │   │   ├── Auth/                    ← Register, Login (Commands + Handlers + Validators)
│   │   │   ├── Products/                ← CRUD completo + Stock (Commands/Queries + Handlers + Validators)
│   │   │   ├── Users/                   ← CRUD completo (Commands/Queries + Handlers)
│   │   │   └── Orders/                  ← CreateOrder (Command + Handler + Validator)
│   │   └── Interfaces/                  ← IUserRepository, IProductRepository, IOrderRepository, IPasswordHasher, ITokenGenerator
│   ├── Infrastructure/                  ← Implementaciones técnicas. Depende de Application
│   │   ├── Persistence/
│   │   │   ├── Configs/                 ← UserConfig, ProductConfig, OrderConfig, OrderItemConfig
│   │   │   ├── Migrations/              ← Auto-aplicadas al arrancar
│   │   │   ├── AppDbContext.cs          ← DbSet<User>, DbSet<Product>, DbSet<Order>
│   │   │   ├── DatabaseMigrator.cs      ← ApplyMigrationsAsync()
│   │   │   ├── DatabaseSeeder.cs        ← SeedAdminAsync() — crea admin si no existe
│   │   │   ├── UserRepository.cs
│   │   │   ├── ProductRepository.cs
│   │   │   └── OrderRepository.cs
│   │   ├── Security/
│   │   │   ├── Argon2PasswordHasher.cs  ← Argon2id: salt 16B, hash 32B, 64MB RAM
│   │   │   └── JwtTokenGenerator.cs     ← JsonWebTokenHandler + SecurityTokenDescriptor
│   │   └── DependencyInjection.cs       ← AddInfrastructure()
│   └── Api/                             ← Punto de entrada HTTP
│       ├── Controllers/                 ← AuthController, UsersController, ProductsController, OrdersController
│       ├── Extensions/                  ← ResultExtensions (ToActionResult, ToNoContentResult)
│       ├── Middlewares/                 ← ExceptionMiddleware (422 ValidationException, 500 genérico)
│       └── Program.cs
└── tests/
    ├── Domain.Tests/                    ← 66 tests — xUnit + FluentAssertions
    │   ├── Commons/                     ← ResultTests
    │   ├── ValueObjects/                ← Email, Password, Price, Address
    │   └── Entities/                    ← User, Product, Order
    ├── Application.Tests/               ← 24 tests — xUnit + FluentAssertions + NSubstitute
    │   └── Features/
    │       ├── Auth/                    ← RegisterUserHandler, LoginUserHandler
    │       ├── Users/                   ← GetUserByIdHandler
    │       ├── Products/                ← CreateProductHandler
    │       └── Orders/                  ← CreateOrderHandler
    └── Api.Tests/                       ← 26 tests — xUnit + FluentAssertions + Testcontainers + Respawn
        ├── ApiFactory.cs                ← WebApplicationFactory + MsSqlContainer
        ├── DatabaseCleaner.cs           ← Respawn entre tests
        ├── BaseIntegrationTest.cs       ← Helpers: RegisterAsync, LoginAsync, AuthenticatedClient, LoginAsAdminAsync
        ├── Auth/
        ├── Users/
        ├── Products/
        └── Orders/
```

---

## Endpoints

| Método | Ruta | Auth | Rol | Descripción |
|--------|------|------|-----|-------------|
| POST | `/api/auth/register` | No | — | Registro. Body: FirstName, LastName, Email, Password |
| POST | `/api/auth/login` | No | — | Login. Retorna JWT |
| GET | `/api/users` | JWT | Any | Listar usuarios paginados. ?page, ?pageSize, ?sortBy, ?sortOrder |
| GET | `/api/users/me` | JWT | Any | Usuario autenticado actual |
| GET | `/api/users/{id}` | JWT | Any | Usuario por Id |
| PUT | `/api/users/{id}` | JWT | Any | Actualizar usuario |
| PUT | `/api/users/{id}/address` | JWT | Any | Actualizar dirección |
| DELETE | `/api/users/{id}` | JWT | Admin | Eliminar usuario (HTTP 204) |
| GET | `/api/products` | JWT | Any | Listar productos. ?page, ?pageSize, ?name, ?minPrice, ?maxPrice, ?sortBy, ?sortOrder |
| GET | `/api/products/{id}` | JWT | Any | Producto por Id |
| POST | `/api/products` | JWT | Any | Crear producto |
| PUT | `/api/products/{id}` | JWT | Any | Actualizar producto |
| PATCH | `/api/products/{id}/stock` | JWT | Any | Actualizar stock |
| DELETE | `/api/products/{id}` | JWT | Any | Eliminar producto (HTTP 204) |
| POST | `/api/orders` | JWT | Any | Crear orden — reduce stock automáticamente |

---

## Decisiones Técnicas Clave

| Decisión | Detalle |
|----------|---------|
| `MapInboundClaims = false` | Requerido en AddJwtBearer para que `User.FindFirst("sub")` funcione. Sin esto ASP.NET mapea `sub` a `ClaimTypes.NameIdentifier` |
| `[AllowAnonymous]` en AuthController | Necesario cuando FallbackPolicy está activa. Sin esto `/api/auth/register` y `/api/auth/login` retornan 401 |
| `Order.Create()` valida y reduce stock | La lógica de negocio vive en el Domain. EF Core change tracking persiste los cambios de stock con un único `SaveChangesAsync` |
| `OrderItem.Create()` es `internal` | Solo `Order` puede crear OrderItems — refuerza el patrón de Aggregate Root |
| `UserRole` como enum (no entidad) | Para 2 roles sin propiedades adicionales, enum + columna string es más simple que tabla many-to-many |
| Testcontainers sobre EF InMemory | SQL Server real garantiza que migraciones, constraints y `OwnsOne` funcionen igual que en producción |
| Respawn + re-seed en `InitializeAsync` | Respawn limpia datos entre tests. El admin se re-siembra antes de cada test para que `LoginAsAdminAsync` funcione |
| `ValidationBehavior` lanza excepción | Lanza `FluentValidation.ValidationException`. `ExceptionMiddleware` la captura y retorna HTTP 422 |

---

## Stack Tecnológico

| Componente | Tecnología |
|------------|------------|
| Framework | .NET 10, C# 14 |
| Base de datos | SQL Server en Docker (puerto 1433) |
| ORM | Entity Framework Core 10 |
| CQRS | MediatR 14 |
| Autenticación | JWT con JsonWebTokenHandler (API moderna) |
| Hashing | Argon2id (Konscious.Security.Cryptography) |
| Validación | FluentValidation + ValidationBehavior pipeline |
| Logging | Serilog → Consola + Archivo (logs/log-{fecha}.txt) |
| Documentación | Swagger/Swashbuckle |
| Rate Limiting | Fixed Window: 60 req/min por IP |
| Tests unitarios | xUnit + FluentAssertions + NSubstitute |
| Tests integración | xUnit + Testcontainers (SQL Server) + Respawn + WebApplicationFactory |

---

## Fases

### Fase 1 — Fundamentos de Arquitectura ✅

> Objetivo: establecer la estructura base de Clean Architecture con todos los patrones core funcionando de punta a punta.

| Feature | Estado | Detalle |
|---------|--------|---------|
| Clean Architecture (4 capas) | ✅ | Domain, Application, Infrastructure, Api |
| Entidad Base | ✅ | Id, CreatedAtUtc, UpdatedAtUtc |
| Result Pattern | ✅ | Result, Result\<T\>, Error record |
| Value Objects | ✅ | Email, Password, Address, Price — con validación encapsulada |
| CQRS con MediatR | ✅ | Commands y Queries separados con handlers |
| Dependency Injection | ✅ | AddInfrastructure() centralizado |
| EF Core con SQL Server | ✅ | Docker, configuraciones separadas por entidad (IEntityTypeConfiguration) |
| Migraciones automáticas | ✅ | ApplyMigrationsAsync() al arrancar la API |
| Constructores privados para EF Core | ✅ | Constructor sin parámetros con null! en entidades |

---

### Fase 2 — Autenticación y Seguridad ✅

> Objetivo: registro, login y protección de endpoints con JWT.

| Feature | Estado | Detalle |
|---------|--------|---------|
| Registro de usuario | ✅ | POST /api/auth/register |
| Login | ✅ | POST /api/auth/login |
| JWT con API moderna | ✅ | JsonWebTokenHandler + SecurityTokenDescriptor |
| Argon2id password hashing | ✅ | Salt 16 bytes, hash 32 bytes, 64MB RAM, timing-safe verify |
| Endpoints protegidos con [Authorize] | ✅ | FallbackPolicy: todo requiere JWT salvo AuthController ([AllowAnonymous]) |
| Swagger con botón Authorize | ✅ | OpenApiSecurityScheme + AddSecurityRequirement configurados en Program.cs |

---

### Fase 3 — Infraestructura Transversal ✅

> Objetivo: logging, manejo de errores y protección contra abuso.

| Feature | Estado | Detalle |
|---------|--------|---------|
| Serilog a consola | ✅ | Logs estructurados en terminal |
| Serilog a archivo | ✅ | logs/log-{fecha}.txt, rotación diaria, retención 30 días |
| Request logging | ✅ | UseSerilogRequestLogging() en pipeline |
| Middleware global de errores | ✅ | ExceptionMiddleware → 422 ValidationException, 500 genérico |
| Result → HTTP mapping | ✅ | ResultExtensions: 400, 401, 404, 409 según Error.Code |
| Rate Limiting | ✅ | Fixed Window: 60 req/min por IP |
| Swagger UI | ✅ | Disponible en /swagger/ (Development) |

---

### Fase 4 — CRUD Completo de Entidades ✅

> Objetivo: completar todas las operaciones CRUD para Users y Products.

| Feature | Estado | Detalle |
|---------|--------|---------|
| **Users** | | |
| Obtener usuario por Id | ✅ | GET /api/users/{id} |
| Listar usuarios | ✅ | GET /api/users?page=1&pageSize=10 |
| Actualizar usuario | ✅ | PUT /api/users/{id} |
| Actualizar dirección | ✅ | PUT /api/users/{id}/address |
| Eliminar usuario | ✅ | DELETE /api/users/{id} (hard delete, HTTP 204, solo Admin) |
| Obtener usuario actual | ✅ | GET /api/users/me (extrae Id del claim `sub` del JWT) |
| **Products** | | |
| Crear producto | ✅ | POST /api/products |
| Obtener producto por Id | ✅ | GET /api/products/{id} |
| Listar productos | ✅ | GET /api/products?page=1&pageSize=10 |
| Actualizar producto | ✅ | PUT /api/products/{id} |
| Actualizar stock | ✅ | PATCH /api/products/{id}/stock |
| Eliminar producto | ✅ | DELETE /api/products/{id} (hard delete, HTTP 204) |

---

### Fase 5 — Paginación y Filtrado ✅

> Objetivo: respuestas paginadas y búsquedas eficientes para endpoints de listado.

| Feature | Estado | Detalle |
|---------|--------|---------|
| Objeto de paginación genérico | ✅ | PagedRequest(Page, PageSize), PagedResponse\<T\>(Items, TotalCount, TotalPages, HasNextPage, HasPreviousPage) |
| Paginación en listado de productos | ✅ | GET /api/products?page=1&pageSize=10 |
| Paginación en listado de usuarios | ✅ | GET /api/users?page=1&pageSize=10 |
| Filtrado por nombre/precio | ✅ | ?name=x&minPrice=0&maxPrice=100 |
| Ordenamiento | ✅ | ?sortBy=name&sortOrder=asc |

---

### Fase 6 — Validación con FluentValidation ✅

> Objetivo: validar Commands/Queries antes de que lleguen al Handler, usando un pipeline behavior de MediatR.

| Feature | Estado | Detalle |
|---------|--------|---------|
| Paquete FluentValidation | ✅ | FluentValidation.DependencyInjectionExtensions |
| ValidationBehavior para MediatR | ✅ | IPipelineBehavior que lanza ValidationException → ExceptionMiddleware → HTTP 422 |
| RegisterUserCommand validator | ✅ | Campos requeridos, formato email, largo password |
| LoginUserCommand validator | ✅ | Email y password requeridos |
| CreateProductCommand validator | ✅ | Nombre requerido, precio > 0, stock >= 0 |
| CreateOrderCommand validator | ✅ | Items no vacíos, ProductId requerido, quantity > 0 |
| Respuesta de validación unificada | ✅ | HTTP 422 con errores agrupados por campo |

---

### Fase 7 — Roles y Autorización ✅

> Objetivo: control de acceso basado en roles (RBAC).

| Feature | Estado | Detalle |
|---------|--------|---------|
| Entidad Role | ✅ | Enum UserRole { User, Admin } en Domain/Enums |
| Relación User-Role | ✅ | Columna Role (string) en tabla Users, propiedad en User entity |
| Claims de rol en JWT | ✅ | ClaimTypes.Role incluido en el token |
| Políticas de autorización | ✅ | Política AdminOnly + FallbackPolicy (usuario autenticado requerido por defecto) |
| Endpoint admin-only | ✅ | DELETE /api/users/{id} solo para Admin |
| Seed de usuario admin | ✅ | DatabaseSeeder crea admin al arrancar si no existe (AdminSeed en appsettings) |

---

### Fase 8 — Relaciones entre Entidades ✅

> Objetivo: modelar relaciones reales entre entidades del dominio.

| Feature | Estado | Detalle |
|---------|--------|---------|
| Entidad Order | ✅ | UserId, Status (Pending), Items — factory Order.Create() con lógica de negocio |
| Entidad OrderItem | ✅ | ProductId, Quantity, UnitPrice (snapshot Price VO) — factory internal Create() |
| Relación User → Orders | ✅ | UserId en Order, tabla Orders con FK |
| Relación Order → OrderItems | ✅ | HasMany con cascade delete, tabla OrderItems |
| Reducción de stock al crear orden | ✅ | Product.ReduceStock() llamado en Order.Create(), persistido en una sola transacción |

---

### Fase 9 — Testing ✅

> Objetivo: confianza en el código con tests automatizados.
> Total: 116 tests — 0 fallidos.

| Feature | Estado | Detalle |
|---------|--------|---------|
| Tests unitarios de Domain | ✅ | 66 tests — Value Objects, entidades, Result Pattern (xUnit + FluentAssertions) |
| Tests unitarios de Application | ✅ | 24 tests — Handlers con repositorios mockeados (NSubstitute) |
| Tests de integración | ✅ | 26 tests — WebApplicationFactory + Testcontainers SQL Server + Respawn |
| Test de autenticación | ✅ | Flujo register → login → acceso protegido cubierto en Api.Tests |

---

### Fase 10 — Mejoras de Infraestructura 📋

> Objetivo: preparar el proyecto para producción.

| Feature | Estado | Detalle |
|---------|--------|---------|
| Health Checks | 💡 | /health endpoint para monitoreo (BD, servicios) |
| CORS | 💡 | Configurar orígenes permitidos |
| Response Caching | 💡 | Cache para endpoints de lectura frecuente |
| Refresh Tokens | 💡 | JWT de corta duración + refresh token para renovar |
| Dockerización de la API | 💡 | Dockerfile + docker-compose (API + SQL Server) |
| Variables de entorno | 💡 | Mover secrets de appsettings a env vars / User Secrets |
| Seq o Elasticsearch | 💡 | Sink de Serilog para búsqueda de logs en producción |

---

## Notas

- Cada fase se construye sobre la anterior. No saltar fases.
- Las fases ✅ están completas pero pueden recibir mejoras incrementales.
- Las fases 📋 son el trabajo inmediato siguiente.
- Las fases 💡 son ideas que se priorizarán cuando las anteriores estén completas.
- Este documento se actualiza conforme avanza el desarrollo.
