using Microsoft.Extensions.Logging;
using TobaccoSaaS.Application.Common.Interfaces;

namespace TobaccoSaaS.Infrastructure.Email;

/// <summary>
/// Foundation email sender. Logs only the subject/recipient — never the token body — so
/// secrets never reach the log sinks (AGENTS.md §20). Replaced by the Notifications module.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("Email queued. To={To} Subject={Subject}", to, subject);
        return Task.CompletedTask;
    }
}
