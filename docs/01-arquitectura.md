## Endpoints actuales

### Auth / Status

| Método | Ruta | Auth |
|---|---|---|
| GET | `/api/Status` | Anónimo |
| POST | `/api/Auth/login` | Anónimo (rate limit + lockout vía `LockoutSettings`) |
| GET | `/api/Auth/me` | Bearer JWT |
| GET | `/api/Auth/users` | Administrador (`onlyActive`, `onlyUnlinked`) |
| POST | `/api/Auth/users` | Administrador — crea Admin/Operador con contraseña temporal, envía correo (SMTP) |
| POST | `/api/Auth/users/{id}/unlock` | Administrador |
| POST | `/api/Auth/users/{id}/reset-password` | Administrador — resetea contraseña, opcionalmente envía correo |
| POST | `/api/Auth/change-password` | Bearer JWT — cambia la contraseña del usuario autenticado |
| POST | `/api/Auth/test-email` | Administrador — diagnóstico SMTP |

### Catálogos

| Método | Ruta | Auth |
|---|---|---|
| GET/POST | `/api/Categories` | Lectura Admin/Operador; escritura Admin |
| GET/PUT | `/api/Categories/{id}` | Idem |
| GET/POST | `/api/Brands` | Idem |
| GET/PUT | `/api/Brands/{id}` | Idem |
| GET/POST | `/api/Models` | Idem (`categoryId`, `brandId`) |
| GET/PUT | `/api/Models/{id}` | Idem |
| GET/POST | `/api/Locations` | Idem (`onlyWarehouses`) |
| GET/PUT | `/api/Locations/{id}` | Idem |

### Company

| Método | Ruta | Auth |
|---|---|---|
| GET/POST | `/api/Employees` | Lectura Admin/Operador; escritura Admin |
| GET/PUT | `/api/Employees/{id}` | Idem |
| GET/POST | `/api/Suppliers` | Idem |
| GET/PUT | `/api/Suppliers/{id}` | Idem |

### Assets

| Método | Ruta | Auth |
|---|---|---|
| GET | `/api/Assets` | Admin/Operador — paginado + facetas (OR dentro / AND entre): `search`, `statuses`/`status`, `categories`/`category`, `kinds`/`kind`, `modelIds`/`modelId`, `locationIds`/`locationId`, `categoryIds`; `page`, `pageSize≤100` |
| GET | `/api/Assets/export.xlsx` | ClosedXML inventario (mismas facetas, sin paginar) |
| GET | `/api/Assets/{id}` | Admin/Operador |
| POST/PUT | `/api/Assets`, `/api/Assets/{id}` | Administrador — escribe movimientos `Created` / `StatusChanged` / `LocationChanged` (`Assigned` solo por flujo de asignación) |

### Assignments

| Método | Ruta | Auth / notas |
|---|---|---|
| POST | `/api/Assignments/assign` | Admin/Operador — ADO.NET + SP → folio `RES-yyyy-####` |
| POST | `/api/Assignments/return` | Admin/Operador — ADO.NET + SP |
| GET | `/api/Assignments` | Dapper (`employeeId`, `assetId`, `onlyActive`) |
| GET | `/api/Assignments/{id}` | Dapper |
| GET | `/api/Assignments/movements` | Dapper (`assetId`, `employeeId`, `from`, `to` UTC) |
| GET | `/api/Assignments/custody` | Dapper (`employeeId`, `from`, `to` sobre IssuedAt) |
| GET | `/api/Assignments/custody/{id}` | Dapper |
| GET | `/api/Assignments/custody/{id}/pdf` | QuestPDF (cabecero + leyenda `Company`) |
| GET | `/api/Assignments/export/movements.xlsx` | ClosedXML historial (mismos filtros que movements) |

### Config relevante (`appsettings`)

- API: `JwtSettings`, `LockoutSettings`, `SmtpSettings`, `Company` (incluye `CustodyLegend`, `LogoPath`), `CorsSettings:AllowedOrigins` (solo browser→API; vacío = sin CORS).
- WebApp: `ApiConnect:BaseUrl` + `ApiConnect:Endpoints` (HttpRequestBuilder / `ApiConnectFactory`).

**CORS / MonsterASP:** llamadas server-side WebApp→API (ApiConnect) no atraviesan CORS. CORS solo aplica si el navegador llama a la API (JS/fetch). Al publicar WebApp, agrega su URL HTTPS a `CorsSettings:AllowedOrigins` en la API.

## Capas y deuda técnica: `ITAM.Application` sin uso

El PDF deja la estructura del proyecto a criterio del candidato ("se evaluará la lógica del
negocio, modelos, acceso a datos, base de datos y pruebas"), así que esto **no es un
incumplimiento**, pero conviene dejarlo explícito para la revisión técnica.

**Estado actual:** `ITAM.Application` existe en el `.sln` pero solo tiene su `.csproj` (referencia
a Domain y Shared, cero clases). La orquestación de reglas de negocio (`AssetService`,
`AssetAssignmentService`, `EmployeeService`, `SupplierService`, `BrandService`, `CategoryService`,
`ModelService`, `LocationService`, `FolioCounterService`, `AuthService`, más los servicios de
exportación Excel/PDF) vive en `ITAM.Infrastructure/Services/**`, junto con el acceso a datos
(EF Core, ADO.NET+SP, Dapper) y los detalles técnicos (Identity, SMTP, QuestPDF, ClosedXML). Los
**contratos** (`IAssetService`, `IEmployeeService`, etc.) sí están correctamente en
`ITAM.Domain/Interfaces/Services/**`, separados de su implementación.

**Por qué quedó así:** decisión pragmática por tiempo (prueba de 3 días) — se priorizó completar
las 8 funciones obligatorias y las reglas críticas antes que formalizar una cuarta capa.

**Cómo se vería la separación correcta (Clean Architecture / Application layer):**

| Debería vivir en `ITAM.Application` | Debería quedarse en `ITAM.Infrastructure` |
|---|---|
| `AssetService`, `AssetAssignmentService`, `EmployeeService`, `SupplierService`, `BrandService`, `CategoryService`, `ModelService`, `LocationService`, `FolioCounterService` — dependen solo de interfaces de `Domain`, la migración sería casi mecánica (mover archivo, cambiar namespace, referenciar `ITAM.Application` desde `ITAM.Api`) | `AssetRepository`, `AssetAssignmentRepository`, repos EF/Dapper, `SqlConnectionFactory` |
| — | `AssetExportService`, `AssignmentExportService`, `CustodyPdfService` (atados a ClosedXML/QuestPDF, son detalle técnico legítimo) |
| — | `AuthService` **tal como está hoy no se puede mover tal cual**: depende directo de `ApplicationDbContext` y `ApplicationUser` (tipos concretos de Infrastructure/Identity), rompiendo la regla de dependencia. Para moverlo primero habría que introducir una abstracción en `Domain` (p. ej. `IIdentityGateway`) que `Infrastructure` implemente. |

**Costo de migrar ahora:** ~13 archivos de servicios + `DependencyInjection.cs` (registro) + los
`using` de ~11 archivos de `ITAM.Tests` que referencian esos namespaces. Riesgo de romper las 52
pruebas unitarias y la entrega si se hace apurado cerca de la fecha límite.

**Recomendación:** si el tiempo lo permite, migrar los servicios "puros" (todos excepto Auth,
export y PDF) a `ITAM.Application` como mejora post-entrega; si no, dejar este documento como
evidencia de que la brecha es conocida y deliberada — es exactamente el tipo de "funcionalidad
pendiente + cómo continuaría la implementación + riesgo identificado" que el PDF pide declarar.
