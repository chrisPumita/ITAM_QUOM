# ITAM_QUOM

API de gestión y resguardo de activos TI (prueba técnica). Stack: .NET 10, SQL Server, Identity + JWT, ADO.NET/SPs para negocio.

## Arranque rápido

1. Crear BD local `ITAM_QUOM` (SQL Express).
2. Revisar `ITAM.Api/appsettings.json` (`DefaultConnection`, `JwtSettings`).
3. **Aplicar migraciones Identity (manual):**

```bat
dotnet ef database update --project ITAM.Infrastructure\ITAM.Infrastructure.csproj --startup-project ITAM.Api\ITAM.Api.csproj
```

4. F5 en **ITAM.Api** → Swagger `http://localhost:5151/swagger`
5. Login: `admin@itam.local` / `Admin123!`

Documentación de arquitectura e interfaces: [docs/01-arquitectura.md](docs/01-arquitectura.md).

Los comandos `dotnet ef` completos están comentados al inicio de `ITAM.Api/Program.cs`.
