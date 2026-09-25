using Microsoft.AspNetCore.Identity;

namespace ITAM.Infrastructure.Identity;

/// <summary>
/// Usuario de Identity con PK <see cref="Guid"/>.
/// Las tablas AspNet* se crean por migraciones EF (solo capa de auth).
/// El dominio de negocio (activos, asignaciones) usará ADO.NET + SPs.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Nombre visible en UI y claims del JWT (<c>displayName</c>).</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Si es false, el login responde 403 aunque la contraseña sea correcta.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Último login exitoso (UTC).</summary>
    public DateTime? LastLoginAt { get; set; }
}
