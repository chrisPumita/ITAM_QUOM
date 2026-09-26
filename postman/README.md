# Colección Postman — ITAM QUOM

Colección revisada y corregida a partir de la que exportaste, más los endpoints que faltaban.
**51 de 51 endpoints de la API cubiertos.** Contiene:

- `ITAM.postman_collection.json` — CRUD de Marcas, Categorías, Modelos, Empleados, Lugares,
  Proveedores, Activos (incluye Resumen, Exportar Excel, Plantilla/Importar, Buscar por código) +
  la operación crítica de Asignar/Devolver + consultas (Asignaciones, Historial, Responsivas, PDF,
  Export) + Auth completo (Login, me, Users, Crear usuario, Desbloquear, Restablecer/Cambiar
  contraseña, Probar SMTP) + Status.
- `ITAM-Production.postman_environment.json` — `uri = http://itam-api.runasp.net`.
- `ITAM-Local.postman_environment.json` — `uri = https://localhost:7229` (puerto real de
  `ITAM.Api`, perfil `https` en `Properties/launchSettings.json`).

## Qué se corrigió respecto a tu export original

1. **Todas las URLs estaban hardcodeadas a `https://localhost:7229`**, incluso los requests que
   colgaban del entorno `Production` — cambiar de entorno no tenía efecto. Ahora las 40 requests
   originales usan `{{uri}}` de forma consistente.
2. **El request `Login` estaba roto**: la URL quedó como `{{uri}}api/Auth/login` (sin `/`) y
   `"api"` se había fusionado al host en vez de ser parte del path. Correspondía literalmente a
   `http://itam-api.runasp.netapi/Auth/login` → 404. Corregido a `{{uri}}/api/Auth/login`.
3. **Tokens JWT reales pegados a mano** en 26 requests (Empleados, Lugares, Proveedores, Activos,
   Operaciones, Reportes, Users) en vez de usar variable — quedaban fijos y expiraban en 12h.
   Además había dos variables distintas (`bearer_token_12u8`, `bearer_token_0gc2`) usadas de forma
   inconsistente en Marcas/Categorías/Modelos. Todo quedó unificado en una sola variable `{{token}}`,
   usada como `Authorization: Bearer {{token}}` en las 40 requests originales.
4. **El request `Login` ahora captura el token automáticamente**: se agregó un script *Tests* que
   corre después de cada login y guarda `data.token` de la respuesta en la variable de colección
   `token` — ya no hay que copiar/pegar el JWT a mano.
5. **JSON inválido en 3 requests** (`Empleados > Editar`, `Lugares > Nuevo`, `Lugares > Editar`):
   tenían un campo comentado con `//` dentro del body (`//"identityUserId": ...`,
   `//"parentLocationId": 0`), lo cual no es JSON válido — la API habría respondido error de
   deserialización. Se descomentaron correctamente (`identityUserId`/`parentLocationId` son campos
   opcionales reales del DTO).
6. **Ruta incorrecta en `Reportes`**: apuntaba a `.../export/assignments.xlsx`, pero el endpoint
   real es `.../export/movements.xlsx` ([AssignmentsController.cs](../ITAM.Api/Controllers/AssignmentsController.cs))
   → hubiera dado 404. Corregido.
7. **`Activos > Editar` tenía valores de plantilla de Swagger sin reemplazar** (`modelId:
   2147483647`, `locationId: 0`) que no existen en la BD y hubieran fallado la validación de
   negocio ("El modelo no existe" / "La ubicación no existe"). Se dejaron valores de ejemplo
   consistentes con `Activos > Nuevo` (`modelId: 1`, `locationId: 1`) — ajusta a tus IDs reales.
8. **`Operaciones > Activo Asignado`** llamaba `GET /api/Assignments/{id}` pasando el **Id del
   activo**, pero ese endpoint espera el **Id de la asignación** (`AssetAssignment.Id`), no el del
   activo — con el GUID de ejemplo hubiera devuelto 404 "Asignación no encontrada". Se renombró a
   *"Asignacion por Id (usar Id de ASIGNACION, no de activo)"* para dejarlo explícito; reemplaza el
   GUID por el `id` que te devuelve `Operaciones > Asignaciones` o el que arma `Asignar`.
9. **Faltaban 11 endpoints** (detectados con un audit dedicado): se agregaron en `Activos` —
   *Resumen*, *Exportar Excel*, *Plantilla de importación*, *Importar* (sube archivo, `form-data`),
   *Buscar por código*; y como items nuevos a nivel raíz — *Crear usuario*, *Desbloquear usuario*,
   *Restablecer contraseña*, *Cambiar contraseña*, *Probar SMTP*, *Status*. Todos siguen el mismo
   patrón (`{{uri}}` + `Bearer {{token}}`), con bodies armados según los DTOs actuales
   (`CreateAdminUserDto`, `AdminResetPasswordDto`, `ChangePasswordDto`, `TestEmailDto`).
   - *Cambiar contraseña* trae `currentPassword = newPassword = "Admin123!"` a propósito (no cambia
     nada al correr) para no bloquearte accidentalmente el usuario admin de prueba — edita los
     valores si quieres probar un cambio real.
   - *Desbloquear usuario* y *Restablecer contraseña* traen como placeholder el `identityUserId`
     del admin visto en un JWT de ejemplo — reemplázalo por el id de un usuario real antes de
     correrlos.
   - *Importar* usa `form-data` con un campo `file` vacío — selecciona un `.xlsx` (puedes generar
     uno con *Plantilla de importación*) antes de enviarlo.

## Cómo usarla

1. Postman → **Import** → arrastra los 3 archivos de esta carpeta.
2. Selecciona el entorno **ITAM - Production (MonsterASP)** o **ITAM - Local** en el selector
   superior derecho.
3. Corre **Login** (carpeta raíz de la colección) con `admin@itam.local` / `Admin123!` — el script
   *Tests* guarda el token solo, no hace falta copiarlo.
4. Corre cualquier otro request: ya usan `Authorization: Bearer {{token}}` automáticamente.
5. Para probar la asignación/devolución real, ajusta en `Operaciones > Asignar` el `employeeId` y
   el `assetId` por unos que existan en tu BD (los que trae el export de ejemplo son de tu entorno
   local original y probablemente no existan en Production).

## Variable `uri`

Definida en el entorno activo (`Production` → `http://itam-api.runasp.net`, mismo valor que
`ApiConnect:BaseUrl` en `ITAM.WebApp/appsettings.json`; `Local` → `https://localhost:7229`). La
colección también trae `uri`/`token` como variables de colección con el valor de Production por
defecto, por si se usa sin seleccionar un entorno.

## Cobertura

51/51 endpoints de la API. Verificado contra `ITAM.Api/Controllers/*.cs` (conteo de `[Http*]`) el
2026-09-27.
