using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure.Persistence;

public sealed class DatabaseHealthCheck(AppDbContext context) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext ctx, CancellationToken ct = default)
    {
        try
        {
            await context.Database.CanConnectAsync(ct);
            return HealthCheckResult.Healthy("SQL Server is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server is unreachable.", ex);
        }
    }
}
