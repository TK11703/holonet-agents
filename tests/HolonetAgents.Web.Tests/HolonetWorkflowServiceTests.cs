using HolonetAgents.Web.Models;
using HolonetAgents.Web.Services;
using HolonetAgents.Web.Services.AgentResponses;

namespace HolonetAgents.Web.Tests;

public sealed class HolonetWorkflowServiceTests
{
    [Theory]
    [InlineData("character", "holonet-character-agent")]
    [InlineData("vehicle", "holonet-vehicle-agent")]
    [InlineData("planet", "holonet-planet-agent")]
    [InlineData("event", "holonet-event-agent")]
    public async Task ExecuteAsync_RoutesToSpecialistThenSynthesizer(
        string category,
        string expectedSpecialist)
    {
        const string specialistResponse = """{"answer":"Specialist result","success":true}""";
        var agentService = new FakeFoundryAgentService(
            $$"""{"category":"{{category}}"}""",
            specialistResponse,
            "Synthesized result");
        var workflow = new HolonetWorkflowService(agentService, new AgentResponseParser());

        var result = Assert.IsType<PlainTextAgentResponse>(
            await workflow.ExecuteAsync("User request"));

        Assert.Equal("Synthesized result", result.Text);
        Assert.Collection(
            agentService.Calls,
            call => Assert.Equal(("holonet-orchestrator", "User request"), call),
            call => Assert.Equal((expectedSpecialist, "User request"), call),
            call => Assert.Equal(("holonet-synthesizer-agent", specialistResponse), call));
    }

    [Fact]
    public async Task ExecuteAsync_OtherCategory_RoutesDirectlyToSynthesizer()
    {
        var agentService = new FakeFoundryAgentService(
            """{"category":"other"}""",
            "Synthesized result");
        var workflow = new HolonetWorkflowService(agentService, new AgentResponseParser());

        var result = Assert.IsType<PlainTextAgentResponse>(
            await workflow.ExecuteAsync("User request"));

        Assert.Equal("Synthesized result", result.Text);
        Assert.Collection(
            agentService.Calls,
            call => Assert.Equal(("holonet-orchestrator", "User request"), call),
            call => Assert.Equal(("holonet-synthesizer-agent", "User request"), call));
    }

    [Fact]
    public async Task ExecuteAsync_InvalidOrchestratorResponse_StopsWorkflow()
    {
        var agentService = new FakeFoundryAgentService("not json");
        var workflow = new HolonetWorkflowService(agentService, new AgentResponseParser());

        Assert.IsType<InvalidAgentResponse>(await workflow.ExecuteAsync("User request"));
        Assert.Single(agentService.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidSpecialistResponse_DoesNotCallSynthesizer()
    {
        var agentService = new FakeFoundryAgentService(
            """{"category":"event"}""",
            "not json");
        var workflow = new HolonetWorkflowService(agentService, new AgentResponseParser());

        Assert.IsType<InvalidAgentResponse>(await workflow.ExecuteAsync("User request"));
        Assert.Equal(2, agentService.Calls.Count);
    }

    private sealed class FakeFoundryAgentService(params string[] responses) : IFoundryAgentService
    {
        private readonly Queue<string> _responses = new(responses);

        public List<(string AgentName, string Message)> Calls { get; } = [];

        public Task<IReadOnlyList<AgentSummary>> GetAgentsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AgentSummary>>([]);

        public Task<IReadOnlyList<AgentSummary>> RefreshAgentsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AgentSummary>>([]);

        public Task<AgentReply> SendMessageAsync(
            string? previousResponseId,
            string agentId,
            string message,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((agentId, message));
            return Task.FromResult(new AgentReply(Guid.NewGuid().ToString(), _responses.Dequeue()));
        }
    }
}