# 03 — Manual de operación

Guía funcional del sistema: qué resuelve, quién lo usa, cómo se opera día a día y cómo instalarlo/probarlo.
Para el modelo de datos ver [02-modelo-datos.md](02-modelo-datos.md); para endpoints y decisiones técnicas,
[01-arquitectura.md](01-arquitectura.md).

## 1. Qué es el sistema

ITAM_QUOM administra el ciclo de vida de los activos de TI de una empresa (laptops, monitores,
impresoras, celulares, periféricos y equipos arrendados) y responde en todo momento:

- Qué activos existen y en qué estado (Disponible, Asignado, Mantenimiento, Baja).
- A quién están asignados y desde cuándo.
- En qué ubicación física están.
- Qué proveedor los vendió, arrendó o da mantenimiento.
- Todo el historial de movimientos de cada activo, con quién hizo cada acción y cuándo.

El sistema es API-first: **ITAM.Api** contiene toda la lógica de negocio y es la única que toca la
base de datos. **ITAM.WebApp** es un cliente MVC que consume esa API por HTTP — no tiene lógica de
negocio propia, solo presentación y armado de peticiones.

## 2. Roles y qué puede hacer cada uno

| Acción | Administrador | Operador |
|---|---|---|
| Login / ver su perfil | ✅ | ✅ |
| Consultar activos, colaboradores, proveedores, historial | ✅ | ✅ |
| Alta de activo | ✅ | ✅ |
| Editar activo (datos, ubicación, mantenimiento) | ✅ | Solo enviar a/sacar de mantenimiento |
| Dar de baja / reactivar un activo | ✅ | ❌ |
| Asignar / devolver activos | ✅ | ✅ |
| Alta/edición de catálogos (marca, categoría, modelo, ubicación) | ✅ | Solo lectura |
| Alta/edición de colaboradores y proveedores | ✅ | Solo lectura |
| Gestión de usuarios (crear, resetear contraseña, desbloquear) | ✅ | ❌ |

## 3. Flujos operativos

### 3.1 Alta de un activo

1. WebApp: **Activos → Nuevo** (o `POST /api/Assets` directo).
2. Se captura marca/modelo (o se crean al vuelo desde el formulario), categoría, tipo de propiedad
   (Propio/Rentado — si es Rentado, proveedor obligatorio), número de serie (obligatorio si es Equipo),
   ubicación, fechas de compra/garantía/renta.
3. El activo nace siempre en estado **Disponible** y condición **Nuevo**; el código de inventario
   (`AssetCode`) se genera automático si no se indica uno.
4. Queda registrado un movimiento `Created` en el historial.

### 3.2 Asignación a un colaborador

1. WebApp: **Asignaciones → Asignar** (o `POST /api/Assignments/assign`).
2. Se elige un colaborador activo y uno o más activos disponibles (filtrables por marca/categoría/modelo).
3. El sistema valida — vía `sp_AssignAssets` — que cada activo exista y esté Disponible, que el
   colaborador exista y esté activo, y que ningún activo tenga ya una asignación abierta.
4. Si todo es válido: se crea la asignación, el activo pasa a **Asignado**, se emite una responsiva
   con folio consecutivo `RES-yyyy-####`, y se registra el movimiento `Assigned` con el usuario que
   ejecutó la acción.
5. Si otro usuario intenta asignar el mismo activo casi al mismo tiempo, la segunda solicitud recibe
   un error controlado (409) — nunca queda el activo asignado a dos personas.

### 3.3 Devolución

1. WebApp: **Asignaciones → Devolver**, buscando por folio de responsiva o por código de activo
   (o `POST /api/Assignments/return`).
2. Se valida que exista una asignación activa para ese activo.
3. Se cierra la asignación (fecha y condición de devolución), el activo vuelve a **Disponible**, y se
   registra el movimiento `Returned` con el usuario que hizo la devolución.

### 3.4 Mantenimiento y baja

- Un activo Disponible puede enviarse a **Mantenimiento** (Operador o Administrador) y regresar a
  Disponible. Un activo **Asignado** no puede tocarse por este flujo — primero debe devolverse.
- Solo un Administrador puede **dar de baja** (Retirado) o reactivar un activo dado de baja. Un activo
  retirado nunca puede volver a asignarse mientras esté en ese estado.

### 3.5 Consulta de historial

Cada activo tiene una línea de tiempo completa (`Assets/Details` en WebApp,
`GET /api/Assignments/movements?assetId=...` en la API) con: alta, asignaciones, devoluciones, cambios
de estado, cambios de ubicación — cada uno con usuario, fecha/hora y observaciones.

### 3.6 Importación / exportación masiva

- **Exportar inventario** a Excel desde el listado de activos (mismos filtros que la búsqueda).
- **Importar** activos desde una plantilla Excel (`Assets/Import` en WebApp) — crea marca/modelo/categoría
  si no existen y valida proveedor obligatorio para activos rentados.

## 4. Reglas de negocio clave (resumen operativo)

- Código de inventario y número de serie son únicos en todo el sistema.
- Un colaborador inactivo no puede recibir activos nuevos (pero conserva su historial).
- Un activo nunca puede tener dos asignaciones activas simultáneas (garantizado a nivel de base de
  datos, no solo de aplicación).
- Todo movimiento queda auditado con el usuario autenticado que lo ejecutó — no hay operaciones anónimas
  sobre activos.

## 5. Instalación y ejecución local

1. Requisitos: SQL Server (local o remoto), SDK de .NET 10.
2. Configurar `ITAM.Api/appsettings.json` (o variables de entorno — ver tabla en el [README](../README.md)):
   cadena de conexión, `JwtSettings`, `SmtpSettings` (opcional).
3. Aplicar migraciones (crean tablas, índices únicos y los Stored Procedures):

   ```bat
   dotnet ef database update --project ITAM.Infrastructure\ITAM.Infrastructure.csproj --startup-project ITAM.Api\ITAM.Api.csproj
   ```

4. Levantar `ITAM.Api` — al arrancar siembra los roles (Administrador/Operador) y los dos usuarios de
   prueba (ver README). Swagger queda en `/swagger`.
5. Levantar `ITAM.WebApp` con `ApiConnect:BaseUrl` apuntando a la API del paso anterior. Login en
   `/Account/Login`.

## 6. Cómo probar el sistema

- **Swagger** (`/swagger`): autenticar con `POST /api/Auth/login`, copiar el `token`, usar el botón
  *Authorize* con `Bearer {token}` y probar cualquier endpoint.
- **WebApp**: flujo completo alta → asignar → devolver → ver historial, con los usuarios de prueba.
- **Postman**: ver [postman/README.md](../postman/README.md).
- **Pruebas automatizadas**: `dotnet test ITAM.Tests\ITAM.Tests.csproj` (52 pruebas unitarias).
- **Concurrencia** (regla crítica del PDF): disparar dos `POST /api/Assignments/assign` casi
  simultáneos con el mismo `assetId` (dos pestañas de Postman con "Send" casi a la vez, o un script con
  `Promise.all`/dos hilos). Solo una debe responder 201; la otra, 409 `Conflict` con mensaje de
  "ya tienen asignación activa".

## 7. Troubleshooting común

| Síntoma | Causa probable | Solución |
|---|---|---|
| WebApp no puede iniciar sesión | `ApiConnect:BaseUrl` no apunta a la API activa | Verificar la URL y que la API esté corriendo/publicada |
| 401 en Swagger tras login exitoso | Falta anteponer `Bearer ` al token en *Authorize* | Usar `Bearer {token}` completo |
| Cuenta bloqueada tras varios intentos | Lockout de Identity (3 intentos / 15 min) | Esperar o desbloquear vía `POST /api/Auth/users/{id}/unlock` (Admin) |
| "Un activo rentado requiere proveedor" | Falta `SupplierId` con `OwnershipType=Rented` | Asignar proveedor antes de guardar |
| 409 al asignar | El activo ya tiene una asignación activa (incluida la carrera de concurrencia) | Verificar estado del activo; si es carrera, es el comportamiento esperado |
| Error 500 genérico | El middleware global oculta el detalle real | Revisar `wwwroot/App_Data/logs/log-*.log` (Serilog) |

## 8. Glosario

- **Responsiva (CustodyForm)**: documento/folio (`RES-yyyy-####`) que respalda la entrega de uno o más
  activos a un colaborador en una misma asignación.
- **Folio**: consecutivo único de una responsiva, generado por `FolioCounters` con bloqueo (`UPDLOCK`)
  para evitar duplicados bajo concurrencia.
- **Movimiento (AssetMovement)**: registro de auditoría/historial de un activo (alta, asignación,
  devolución, cambio de estado, cambio de ubicación).
- **Kind (Equipment/Accessory)**: distingue equipos serializados de accesorios de inventario simple.
- **Condition (New/Used)**: condición física del activo al entregarse o devolverse.
