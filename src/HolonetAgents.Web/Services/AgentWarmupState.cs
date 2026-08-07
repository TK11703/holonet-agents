namespace HolonetAgents.Web.Services;

public enum AgentWarmupStatus
{
    Running,
    Completed,
    Failed
}

public sealed record AgentWarmupSnapshot(AgentWarmupStatus Status, int AgentCount, string? Error);

/// <summary>
/// Publishes the progress of the startup agent-cache warmup so the UI can report it.
/// </summary>
public sealed class AgentWarmupState
{
    private AgentWarmupSnapshot _snapshot = new(AgentWarmupStatus.Running, 0, null);

    /// <summary>Raised on the warmup thread; subscribers must marshal to their own context.</summary>
    public event Action<AgentWarmupSnapshot>? Changed;

    public AgentWarmupSnapshot Current => Volatile.Read(ref _snapshot);

    public void MarkCompleted(int agentCount) =>
        Publish(new AgentWarmupSnapshot(AgentWarmupStatus.Completed, agentCount, null));

    public void MarkFailed(string error) =>
        Publish(new AgentWarmupSnapshot(AgentWarmupStatus.Failed, 0, error));

    private void Publish(AgentWarmupSnapshot snapshot)
    {
        Volatile.Write(ref _snapshot, snapshot);
        Changed?.Invoke(snapshot);
    }
}
