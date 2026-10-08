namespace TobaccoSaaS.Application.Common.Interfaces;

/// <summary>
/// Outbound email abstraction. The Notifications module (build step 23) will provide the
/// production provider; the foundation ships a logging implementation so flows are testable.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}
