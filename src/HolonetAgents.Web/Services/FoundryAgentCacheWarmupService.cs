namespace HolonetAgents.Web.Services;

public sealed class FoundryAgentCacheWarmupService(
    IFoundryAgentService agentService,
    AgentWarmupState warmupState,
    ILogger<FoundryAgentCacheWarmupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var agents = await agentService.GetAgentsAsync(stoppingToken);
            warmupState.MarkCompleted(agents.Count);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Application is stopping.
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not warm the Azure AI Foundry agent cache during startup.");
            warmupState.MarkFailed(ex.Message);
        }
    }
}