namespace HolonetAgents.Web.Services;

public sealed class FoundryAgentCacheWarmupService(
    FoundryAgentService agentService,
    ILogger<FoundryAgentCacheWarmupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await agentService.GetAgentsAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Application is stopping.
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not warm the Azure AI Foundry agent cache during startup.");
        }
    }
}