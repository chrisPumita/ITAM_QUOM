# ITAM_QUOM

API de gestión y resguardo de activos TI (prueba técnica 2026).  
Stack: **.NET 10**, SQL Server, Identity + JWT, ADO.NET/SPs para asignación/devolución, EF Core para catálogos/CRUD, Dapper para consultas de assignments.

## Arranque rápido

1. SQL Server con la BD (local o DatabaseASP).
2. Configurar `ITAM.Api/appsettings.json` (o variables de entorno).
3. Aplicar migraciones:

```bat
dotnet ef database update --project ITAM.Infrastructure\ITAM.Infrastructure.csproj --startup-project ITAM.Api\ITAM.Api.csproj
```

4. Ejecutar **ITAM.Api** → Swagger: `/swagger` (también en producción).
5. Login seed: `admin@itam.local` / `Admin123!`

API publicada de referencia: `http://itam-api.runasp.net/swagger`

## Variables / configuración

| Clave | Uso |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server |
| `JwtSettings:Key` / `Issuer` / `Audience` / `ExpirationHours` | JWT |
| `LockoutSettings` | Intentos fallidos (default 3 / 15 min) |
| `CorsSettings:AllowedOrigins` | Solo si el navegador llama a la API |
| `Company:*` | Cabecero/leyenda PDF responsiva |
| `SmtpSettings` | Correo (opcional) |
| WebApp `ApiConnect:BaseUrl` | URL de la API (ej. `http://itam-api.runasp.net`) |

En hosting: preferir `ConnectionStrings__DefaultConnection` y `JwtSettings__Key` por entorno.

## Scripts SQL

- Migraciones EF: `ITAM.Infrastructure/Persistence/Migrations/`
- SPs principales: `ITAM.Infrastructure/Sql/sp_AssignAssets.sql`, `sp_ReturnAsset.sql` (también embebidos en migración)

## Usuarios de prueba

| Email | Password | Rol |
|---|---|---|
| `admin@itam.local` | `Admin123!` | Administrador |

(Se crea en seed Identity al arrancar la API.)

## Pruebas

```bat
dotnet test ITAM.Tests\ITAM.Tests.csproj
```

Colección HTTP de ejemplo: `ITAM.Api/ITAM.Api.http` (Status, login, me). Swagger en vivo cubre el resto.

## Documentación técnica

- [docs/01-arquitectura.md](docs/01-arquitectura.md) — endpoints y decisiones
- [docs/02-modelo-datos.md](docs/02-modelo-datos.md) — modelo

### Decisiones técnicas (resumen)

- **Híbrido de acceso a datos:** EF para catálogos/CRUD; **ADO.NET + SP** para assign/return (concurrencia crítica); Dapper para listados de assignments/movimientos.
- **Concurrencia:** índice único filtrado `UX_AssetAssignments_ActiveAsset` + validaciones en `sp_AssignAssets` + `UPDLOCK` en folio.
- **Historial:** movimientos en SP (assign/return) y en CRUD de activos (`Created`, `StatusChanged`, `LocationChanged`) con `PerformedByUserId`.
- **Seguridad:** JWT + roles Admin/Operador; lockout + rate limit login; middleware global sin filtrar SQL/secretos.
- **Consulta activos:** paginado + facetas multi-valor (estilo ecommerce).

## Frontend (WebApp)

Opcional según el PDF. Estado actual: ApiConnect (HttpClient builder) + indicador de conexión a `/api/Status`.  
**Pendiente UI:** login, listado/alta de activos, asignación, devolución, historial.

## Pendientes / riesgos

| Ítem | Notas |
|---|---|
| Pantallas WebApp | Valor adicional; API ya lista para consumir |
| Colección Postman completa | Sustituible por Swagger; parcial en `.http` |
| SMTP / logo branding | Configurados; logo en `wwwroot/branding` |
| Secretos en appsettings | Rotar password DB/JWT si el repo es compartido; preferir env vars |

## Uso de IA

- **Herramienta:** Cursor (agente de código).
- **Uso:** scaffolding de capas, SPs, PDF/Excel, ApiConnect, filtros/paginado, revisión vs PDF de requisitos.
- **Validación humana:** reglas de negocio del PDF, concurrencia en SP/índice, pruebas unitarias, publish MonsterASP, contraste con patrones Daikin/Nexus.
- **Decisiones propias:** híbrido EF/ADO/Dapper, facetas multi-select, Excel de inventario en Assets (no por asignación), export vacío → 404.

## Tiempo aproximado

~3 días de implementación iterativa (API + SPs + PDF/Excel + hardening + WebApp base).
