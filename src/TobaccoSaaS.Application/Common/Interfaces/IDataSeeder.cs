namespace TobaccoSaaS.Application.Common.Interfaces;

/// <summary>Idempotent platform seed runner (spec Appendix C).</summary>
public interface IDataSeeder
{
    Task SeedAsync(CancellationToken ct = default);
}
