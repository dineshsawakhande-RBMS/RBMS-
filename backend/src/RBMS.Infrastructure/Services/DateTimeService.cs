using RBMS.Application.Common.Interfaces;

namespace RBMS.Infrastructure.Services;

public class DateTimeService : IDateTime
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
