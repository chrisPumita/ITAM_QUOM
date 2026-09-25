# 02 — Modelo de datos ITAM QUOM (cerrado para implementación)

## Decisiones confirmadas
- Category/Brand/Model/Location como catálogos.
- Ownership: Owned | Rented (sin Comodato).
- Sin ParentAsset.
- Employee con IdentityUserId **opcional** (patrón Daikin, no hereda IdentityUser).
- Accesorios = Asset con AssetKind=Accessory (siempre fila en Assets).
- Responsiva multi-línea (CustodyForm + Lines), folio RES-yyyy-####.
- FolioCounter para consecutivos.
- SMTP en appsettings (credenciales del desarrollador).
- Tablas EN + comentarios ES en entidad/Fluent.
- Trazabilidad: AssignedByUserId / PerformedByUserId en assignment y movements.
- Serilog a archivo en API y WebApp + middleware de excepciones.

## Entidades (Domain)
Ver `ITAM.Domain/Entities/**` y configs en `ITAM.Infrastructure/Persistence/Configurations/**`.

## Migración sugerida
```bat
dotnet ef migrations add AddBusinessSchema --project ITAM.Infrastructure --startup-project ITAM.Api --output-dir Persistence/Migrations
dotnet ef database update --project ITAM.Infrastructure --startup-project ITAM.Api
```

## Logs
- API / WebApp: `{ContentRoot}/wwwroot/App_Data/logs/log-.log` (se crea al arrancar; .log ignorados en git)
