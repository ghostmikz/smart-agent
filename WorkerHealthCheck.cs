using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class WorkerHealthCheck : BackgroundService
{
    private readonly ILogger<WorkerHealthCheck> _logger;
    public WorkerHealthCheck(ILogger<WorkerHealthCheck> logger) => _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Heartbeat OK: {time}", DateTimeOffset.Now);
            await Task.Delay(TimeSpan.FromSeconds(60 * 4), stoppingToken);
        }
    }
}
