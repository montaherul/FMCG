using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TobaccoSaaS.Domain.Entities.Audit;

namespace TobaccoSaaS.Infrastructure.Data.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).HasMaxLength(120).IsRequired();
        b.Property(x => x.Module).HasMaxLength(80).IsRequired();
        b.Property(x => x.EntityType).HasMaxLength(120);
        b.Property(x => x.EntityId).HasMaxLength(120);
        b.Property(x => x.IpAddress).HasMaxLength(64);
        b.Property(x => x.UserAgent).HasMaxLength(512);

        b.HasIndex(x => new { x.TenantId, x.CreatedAt });
        b.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}
