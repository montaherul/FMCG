namespace TobaccoSaaS.Infrastructure.Security;

/// <summary>Bindable JWT settings (spec §21.1). Signing key must come from configuration/secrets.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TobaccoSaaS";

    public string Audience { get; set; } = "TobaccoSaaS.Client";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 30;
}
