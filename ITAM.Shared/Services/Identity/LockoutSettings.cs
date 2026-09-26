namespace ITAM.Shared.Services.Identity;

/// <summary>
/// Bloqueo por intentos fallidos de login. Sección <see cref="SectionName"/> en appsettings.
/// </summary>
public class LockoutSettings
{
    public const string SectionName = "LockoutSettings";

    /// <summary>Intentos fallidos antes de bloquear (default 3).</summary>
    public int MaxFailedAccessAttempts { get; set; } = 3;

    /// <summary>Minutos de bloqueo temporal (default 15).</summary>
    public int DefaultLockoutMinutes { get; set; } = 15;

    /// <summary>Si los usuarios nuevos tienen lockout habilitado.</summary>
    public bool AllowedForNewUsers { get; set; } = true;
}
