# ITAM_QUOM — Sistema de Gestión y Resguardo de Activos TI

Prueba técnica 2026: API + WebApp para administrar activos de TI (laptops, monitores, impresoras,
celulares, periféricos y equipos arrendados) — quién los tiene, dónde están, quién los proveyó,
su historial de movimientos y su disponibilidad.

**Estado del proyecto: funcionalmente completo.** Los 8 requisitos obligatorios del PDF y las 7 reglas
críticas están implementados y probados. El frontend (opcional en el PDF) también está completo con
las 6 pantallas mínimas pedidas más funcionalidad adicional. Ver [Estado y pendientes](#estado-y-pendientes)
para lo que falta pulir antes de entregar.

## Contenido de la entrega

| Punto | Dónde está |
|---|---|
| Código fuente | Solución .NET en la raíz (`ITAM.Api`, `ITAM.WebApp`, `ITAM.Domain`, `ITAM.Application`, `ITAM.Infrastructure`, `ITAM.Shared`) |
| Scripts de base de datos | [schema.sql](schema.sql) y [migraciones EF](ITAM.Infrastructure/Persistence/Migrations/) |
| Stored procedures | [ITAM.Infrastructure/Sql](ITAM.Infrastructure/Sql) |
| Pasos de instalación | [Arranque rápido](#arranque-rápido) y [docs/03-operacion.md](docs/03-operacion.md) |
| Variables de entorno | [Variables / configuración](#variables--configuración) |
| Usuarios de prueba | [Usuarios de prueba](#usuarios-de-prueba) |
| Postman | [postman/](postman/README.md) |
| Pruebas automatizadas | [ITAM.Tests](ITAM.Tests) — `dotnet test` |
| Decisiones técnicas | [Resumen](#decisiones-técnicas-resumen) y [docs/01-arquitectura.md](docs/01-arquitectura.md) |
| Tiempo aproximado | [Tiempo aproximado](#tiempo-aproximado) |

## Estructura del repositorio

```
ITAM.Api            → Web API (controllers, middleware, Program.cs)
ITAM.WebApp          → Frontend MVC (opcional del PDF, consume la API vía HttpClient)
ITAM.Domain          → Entidades, interfaces de repos/servicios (sin dependencias externas)
ITAM.Infrastructure  → EF Core, ADO.NET/SP, Dapper, Identity, migraciones, SQL
ITAM.Application     → Reglas de negocio (servicios de activos, asignaciones, catálogos)
ITAM.Shared          → DTOs, enums, opciones de configuración (compartido Api/WebApp)
ITAM.Tests           → Pruebas unitarias (xUnit + Moq)
docs/                → Documentación técnica y operativa
postman/             → Colección Postman y entornos Local / Production
schema.sql           → Script SQL del esquema (tablas, índices, procedimientos)
```

Stack: **.NET 10**, SQL Server, ASP.NET Core Identity + JWT, EF Core (catálogos/CRUD),
ADO.NET puro + Stored Procedures (asignación/devolución — sección crítica de concurrencia),
Dapper (lecturas de asignaciones/movimientos/responsivas), Serilog, QuestPDF (PDF de responsiva),
ClosedXML (import/export Excel).

## Arranque rápido

1. SQL Server con la BD (local o MonsterASP/DatabaseASP).
2. Configurar `ITAM.Api/appsettings.json` (o variables de entorno — ver tabla abajo).
3. Aplicar migraciones:

```bat
dotnet ef database update --project ITAM.Infrastructure\ITAM.Infrastructure.csproj --startup-project ITAM.Api\ITAM.Api.csproj
```

4. Ejecutar **ITAM.Api** → Swagger: `/swagger` (habilitado también en producción).
5. Ejecutar **ITAM.WebApp** → login en `/Account/Login` (requiere que `ApiConnect:BaseUrl` apunte a la API corriendo).
6. Login con los usuarios de prueba (tabla abajo).

## Ambientes publicados

| Aplicación | URL |
|---|---|
| API | http://itam-api.runasp.net |
| Swagger | http://itam-api.runasp.net/swagger |
| WebApp | http://itam-app.runasp.net |

Login de la WebApp publicada: http://itam-app.runasp.net/Account/Login (usuarios de prueba de la tabla de abajo).

## Usuarios de prueba

Se crean automáticamente al arrancar la API (`SeedIdentityAsync`, ver
[DependencyInjection.cs](ITAM.Infrastructure/DependencyInjection.cs)):

| Email | Password | Rol |
|---|---|---|
| `admin@itam.local` | `Admin123!` | Administrador |
| `operador@itam.local` | `Operador123!` | Operador |

Administrador: acceso total (alta/edición de activos, baja, catálogos, usuarios, reseteo de contraseñas).
Operador: consulta, asignación/devolución, alta de activos — no puede editar catálogos ni gestionar usuarios.

## Variables / configuración

| Clave | Uso |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server |
| `JwtSettings:Key` / `Issuer` / `Audience` / `ExpirationHours` | Firma y expiración del JWT |
| `LockoutSettings` | Intentos fallidos de login antes de bloqueo (default 3 intentos / 15 min) |
| `CorsSettings:AllowedOrigins` | Solo necesario si un navegador llama directo a la API (JS/fetch) |
| `Company:*` | Cabecero/leyenda del PDF de responsiva |
| `SmtpSettings` | Correo saliente (invitaciones, reseteo de contraseña) — opcional |
| WebApp `ApiConnect:BaseUrl` | URL de la API que consume el WebApp |

⚠️ **Seguridad — acción requerida antes de compartir el repo**: `ITAM.Api/appsettings.json` y
`appsettings.Production.json` tienen actualmente secretos reales en texto plano (contraseña de BD,
`JwtSettings:Key` de producción y credenciales SMTP). Rotar esos tres valores y mover a variables de
entorno (`ConnectionStrings__DefaultConnection`, `JwtSettings__Key`, `SmtpSettings__Password`) antes de
hacer público o compartir el repositorio. En hosting, usar siempre el formato `Seccion__Clave`.

## Scripts SQL

- Script completo del esquema: [schema.sql](schema.sql) (tablas, índices y procedimientos).
- Migraciones EF: [ITAM.Infrastructure/Persistence/Migrations/](ITAM.Infrastructure/Persistence/Migrations/)
- Stored Procedures (asignación/devolución):
  [sp_AssignAssets.sql](ITAM.Infrastructure/Sql/sp_AssignAssets.sql),
  [sp_ReturnAsset.sql](ITAM.Infrastructure/Sql/sp_ReturnAsset.sql)
  (también embebidos en la migración `AddAssignReturnStoredProcedures`)

## Pruebas

```bat
dotnet test ITAM.Tests\ITAM.Tests.csproj
```

52 pruebas unitarias (xUnit + Moq) cubriendo servicios de Assets, Assignments, Employees, Suppliers,
catálogos (Brand/Category/Model/Location), export y `ApiResponseFactory`. Son unitarias contra
repositorios mockeados — **no hay pruebas de integración contra SQL real**, por lo que la regla crítica
de concurrencia (dos asignaciones simultáneas al mismo activo) está garantizada por el índice único
filtrado `UX_AssetAssignments_ActiveAsset` + el SP transaccional, pero no está demostrada con un test
automatizado. Ver [Estado y pendientes](#estado-y-pendientes).

Colección HTTP mínima: [ITAM.Api/ITAM.Api.http](ITAM.Api/ITAM.Api.http) (Status, login, me).
Colección Postman completa: ver [postman/README.md](postman/README.md).

## Documentación técnica

- [docs/01-arquitectura.md](docs/01-arquitectura.md) — endpoints y decisiones de arquitectura
- [docs/02-modelo-datos.md](docs/02-modelo-datos.md) — modelo de datos y entidades
- [docs/03-operacion.md](docs/03-operacion.md) — manual de operación: qué hace el sistema, roles,
  flujos de negocio, instalación y troubleshooting
- [postman/README.md](postman/README.md) — cómo importar y usar la colección Postman

### Decisiones técnicas (resumen)

- **Híbrido de acceso a datos:** EF Core para catálogos/CRUD (Assets, Employees, Suppliers, Category,
  Brand, Model, Location); **ADO.NET puro (`Microsoft.Data.SqlClient`) + Stored Procedures** para
  asignar/devolver, que es la ruta de concurrencia crítica del PDF; Dapper para las lecturas de
  asignaciones/movimientos/responsivas (Dapper corre sobre `SqlClient`, no es EF).
- **Concurrencia:** índice único filtrado `UX_AssetAssignments_ActiveAsset`
  (`AssetId` WHERE `ReturnedAt IS NULL`) + validaciones en `sp_AssignAssets` + `UPDLOCK`/`HOLDLOCK`
  en el folio consecutivo. Un segundo intento concurrente sobre el mismo activo recibe un 409
  controlado, nunca una asignación duplicada.
- **Historial:** movimientos generados por los SP (assign/return) y por el CRUD de activos
  (`Created`, `StatusChanged`, `LocationChanged`), siempre con `PerformedByUserId`.
- **Seguridad:** JWT + roles Admin/Operador; lockout de Identity (3 intentos/15 min) + rate limiter
  por IP (10/min) en login; middleware global que nunca expone detalles de SQL/excepciones internas.
- **Catálogo de activos:** `Category`/`Brand`/`CurrentLocation` del PDF se modelan como catálogos
  normalizados (`Model → Category/Brand`, `LocationId → Location`) en vez de campos planos en `Asset`.
- **Consulta de activos:** paginado + facetas multi-valor (estilo ecommerce): `search`, `status(es)`,
  `category(ies)`, `kind(s)`, `modelId(s)`, `locationId(s)`, `brandId(s)`.

## Frontend (WebApp)

Opcional según el PDF, pero **completo**: las 6 pantallas mínimas exigidas están implementadas y
funcionando contra la API, más funcionalidad adicional.

| Pantalla mínima del PDF | Implementación |
|---|---|
| a. Inicio de sesión | [Account/Login](ITAM.WebApp/Views/Account/Login.cshtml) |
| b. Listado de activos | [Assets/Index](ITAM.WebApp/Views/Assets/Index.cshtml) (filtros + paginado) |
| c. Alta de activo | [Assets/Create](ITAM.WebApp/Views/Assets/Create.cshtml) |
| d. Asignación de activo | [Assignments/Assign](ITAM.WebApp/Views/Assignments/Assign.cshtml) |
| e. Devolución de activo | [Assignments/Return](ITAM.WebApp/Views/Assignments/Return.cshtml) |
| f. Consulta de historial | [Assets/Details](ITAM.WebApp/Views/Assets/Details.cshtml) (movimientos por activo) + [Assignments/Responsivas](ITAM.WebApp/Views/Assignments/Responsivas.cshtml) |

Adicional no pedido por el PDF: edición de activos, importación/exportación Excel, tarjetas de
"Asignados" por colaborador, PDF de responsiva descargable, gestión de usuarios/roles, cambio de
contraseña, envío de correo de prueba SMTP.

## Estado y pendientes

Checklist completo de requisitos del PDF vs. código revisado — sin brechas funcionales encontradas en
la API. Pendientes reales para cerrar la entrega:

| Ítem | Prioridad | Notas |
|---|---|---|
| Rotar secretos en `appsettings*.json` (BD, JWT, SMTP) | 🔴 Alta | Ver aviso de seguridad arriba |
| Evidencia de la regla de concurrencia crítica | 🟡 Media | El índice único + SP la garantizan; falta un test de integración o guion documentado que dispare 2 asignaciones simultáneas sobre el mismo activo |
| ~~Colección Postman en el repo~~ | ✅ Listo | Ver [postman/README.md](postman/README.md) — colección + entornos Production/Local, corregida (URLs, auth, JSON inválido, ruta de export) |
| Pruebas de integración contra SQL real | 🟢 Baja | Hoy solo unitarias con mocks |

## Uso de IA

- **Herramienta:** Cursor / Claude Code (agentes de código).
- **Uso:** scaffolding de capas, SPs, PDF/Excel, ApiConnect, filtros/paginado, revisión punto a punto
  del código contra el PDF de requisitos, redacción de documentación.
- **Validación humana:** reglas de negocio del PDF, concurrencia en SP/índice, pruebas unitarias,
  publish MonsterASP, contraste con patrones Daikin/Nexus.
- **Decisiones propias:** híbrido EF/ADO/Dapper, facetas multi-select, Excel de inventario en Assets
  (no por asignación), export vacío → 404, normalización de Category/Brand/Location vía catálogos.

## Tiempo aproximado

~3 días de implementación iterativa (API + SPs + PDF/Excel + hardening + WebApp completo).
