using HolonetAgents.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HolonetAgents.Web.Tests;

public sealed class AgentWarmupStateTests
{
    [Fact]
    public void Current_BeforeWarmupFinishes_ReportsRunning()
    {
        var state = new AgentWarmupState();

        Assert.Equal(AgentWarmupStatus.Running, state.Current.Status);
    }

    [Fact]
    public void MarkCompleted_NotifiesSubscribersWithAgentCount()
    {
        var state = new AgentWarmupState();
        AgentWarmupSnapshot? observed = null;
        state.Changed += snapshot => observed = snapshot;

        state.MarkCompleted(8);

        Assert.Equal(new AgentWarmupSnapshot(AgentWarmupStatus.Completed, 8, null), observed);
        Assert.Equal(observed, state.Current);
    }

    [Fact]
    public void MarkFailed_NotifiesSubscribersWithError()
    {
        var state = new AgentWarmupState();
        AgentWarmupSnapshot? observed = null;
        state.Changed += snapshot => observed = snapshot;

        state.MarkFailed("Endpoint is not configured.");

        Assert.Equal(AgentWarmupStatus.Failed, observed?.Status);
        Assert.Equal("Endpoint is not configured.", observed?.Error);
    }

    [Fact]
    public void Changed_AfterUnsubscribe_NoLongerNotifies()
    {
        var state = new AgentWarmupState();
        var notifications = 0;
        void Handler(AgentWarmupSnapshot _) => notifications++;

        state.Changed += Handler;
        state.Changed -= Handler;
        state.MarkCompleted(1);

        Assert.Equal(0, notifications);
    }

    [Fact]
    public async Task WarmupService_WhenAgentsLoad_ReportsCompleted()
    {
        var state = new AgentWarmupState();
        var service = new FoundryAgentCacheWarmupService(
            new StubFoundryAgentService(agentCount: 3),
            state,
            NullLogger<FoundryAgentCacheWarmupService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!;
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(new AgentWarmupSnapshot(AgentWarmupStatus.Completed, 3, null), state.Current);
    }

    [Fact]
    public async Task WarmupService_WhenLoadFails_ReportsFailure()
    {
        var state = new AgentWarmupState();
        var service = new FoundryAgentCacheWarmupService(
            new StubFoundryAgentService(failure: new InvalidOperationException("Endpoint missing.")),
            state,
            NullLogger<FoundryAgentCacheWarmupService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!;
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(AgentWarmupStatus.Failed, state.Current.Status);
        Assert.Equal("Endpoint missing.", state.Current.Error);
    }

    private sealed class StubFoundryAgentService(int agentCount = 0, Exception? failure = null) : IFoundryAgentService
    {
        public Task<IReadOnlyList<AgentSummary>> GetAgentsAsync(CancellationToken cancellationToken = default)
        {
            if (failure is not null)
            {
                return Task.FromException<IReadOnlyList<AgentSummary>>(failure);
            }

            IReadOnlyList<AgentSummary> agents = Enumerable
                .Range(0, agentCount)
                .Select(index => new AgentSummary($"agent-{index}", $"agent-{index}"))
                .ToArray();
            return Task.FromResult(agents);
        }

        public Task<IReadOnlyList<AgentSummary>> RefreshAgentsAsync(CancellationToken cancellationToken = default) =>
            GetAgentsAsync(cancellationToken);

        public Task<AgentReply> SendMessageAsync(
            string? previousResponseId,
            string agentId,
            string message,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
