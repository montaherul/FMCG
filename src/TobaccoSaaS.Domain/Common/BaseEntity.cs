namespace TobaccoSaaS.Domain.Common;

/// <summary>
/// Standard audit column block applied to every table (spec §18.2).
/// Uses UTC timestamps; soft delete is represented by <see cref="DeletedAt"/>.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;

    public void MarkDeleted(Guid? by = null)
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedBy = by;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
