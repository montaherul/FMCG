using TobaccoSaaS.Application.Common.Interfaces;

namespace TobaccoSaaS.Application.Common;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
