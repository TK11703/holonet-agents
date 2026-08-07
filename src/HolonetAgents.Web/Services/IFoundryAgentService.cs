namespace HolonetAgents.Web.Services;

public interface IFoundryAgentService
{
    Task<IReadOnlyList<AgentSummary>> GetAgentsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentSummary>> RefreshAgentsAsync(CancellationToken cancellationToken = default);

    Task<AgentReply> SendMessageAsync(
        string? previousResponseId,
        string agentId,
        string message,
        CancellationToken cancellationToken = default);
}