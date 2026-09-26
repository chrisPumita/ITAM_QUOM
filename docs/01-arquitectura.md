## Endpoints actuales

### Auth / Status

| Método | Ruta | Auth |
|---|---|---|
| GET | `/api/Status` | Anónimo |
| POST | `/api/Auth/login` | Anónimo (rate limit + lockout vía `LockoutSettings`) |
| GET | `/api/Auth/me` | Bearer JWT |
| GET | `/api/Auth/users` | Administrador (`onlyActive`, `onlyUnlinked`) |
| POST | `/api/Auth/users/{id}/unlock` | Administrador |

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
