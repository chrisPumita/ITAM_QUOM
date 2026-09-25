namespace ITAM.Domain.Entities;

/// <summary>
/// Base de entidades de negocio (Guid). Soft delete + auditoría de fechas.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Borrado lógico; no se elimina físicamente el registro.</summary>
    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
