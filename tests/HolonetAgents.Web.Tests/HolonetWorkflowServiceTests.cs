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
    [InlineData("jedi", "holonet-jedi-agent")]
    [InlineData("sith", "holonet-sith-agent")]
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
            (await workflow.ExecuteAsync("User request")).Response);

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
            (await workflow.ExecuteAsync("User request")).Response);

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

        Assert.IsType<InvalidAgentResponse>((await workflow.ExecuteAsync("User request")).Response);
        Assert.Single(agentService.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidSpecialistResponse_DoesNotCallSynthesizer()
    {
        var agentService = new FakeFoundryAgentService(
            """{"category":"event"}""",
            "not json");
        var workflow = new HolonetWorkflowService(agentService, new AgentResponseParser());

        Assert.IsType<InvalidAgentResponse>((await workflow.ExecuteAsync("User request")).Response);
        Assert.Equal(2, agentService.Calls.Count);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulRun_TracesEveryStage()
    {
        var agentService = new FakeFoundryAgentService(
            """{"category":"event"}""",
            """{"answer":"The Battle of Yavin.","success":true}""",
            "Synthesized result");
        var workflow = new HolonetWorkflowService(agentService, new AgentResponseParser());

        var steps = (await workflow.ExecuteAsync("Tell me about the battle of yavin")).Steps;

        Assert.Collection(
            steps,
            step =>
            {
                Assert.Equal(HolonetWorkflowStage.Orchestrator, step.Stage);
                Assert.Equal("holonet-orchestrator", step.AgentName);
                Assert.Contains("Event", step.Detail);
                Assert.True(step.Succeeded);
            },
            step =>
            {
                Assert.Equal(HolonetWorkflowStage.Specialist, step.Stage);
                Assert.Equal("holonet-event-agent", step.AgentName);
                Assert.True(step.Succeeded);
                Assert.Equal("The Battle of Yavin.", step.Output);
            },
            step =>
            {
                Assert.Equal(HolonetWorkflowStage.Synthesizer, step.Stage);
                Assert.Equal("holonet-synthesizer-agent", step.AgentName);
                Assert.True(step.Succeeded);
            });
    }

    [Fact]
    public async Task ExecuteAsync_ReportsEachStepToProgressAsItHappens()
    {
        var agentService = new FakeFoundryAgentService(
            """{"category":"event"}""",
            """{"answer":"The Battle of Yavin.","success":true}""",
            "Synthesized result");
        var workflow = new HolonetWorkflowService(agentService, new AgentResponseParser());
        var reported = new List<HolonetWorkflowStep>();
        var progress = new SynchronousProgress<HolonetWorkflowStep>(reported.Add);

        var result = await workflow.ExecuteAsync("User request", progress);

        Assert.Equal(result.Steps, reported);
    }

    [Fact]
    public async Task ExecuteAsync_OtherCategory_TracesOrchestratorAndSynthesizerOnly()
    {
        var agentService = new FakeFoundryAgentService(
            """{"category":"other"}""",
            "Synthesized result");
        var workflow = new HolonetWorkflowService(agentService, new AgentResponseParser());

        var steps = (await workflow.ExecuteAsync("User request")).Steps;

        Assert.Equal(
            new[] { HolonetWorkflowStage.Orchestrator, HolonetWorkflowStage.Synthesizer },
            steps.Select(step => step.Stage));
        Assert.All(steps, step => Assert.True(step.Succeeded));
    }

    [Fact]
    public async Task ExecuteAsync_UnsuccessfulSpecialist_MarksStepFailedButStillSynthesizes()
    {
        var agentService = new FakeFoundryAgentService(
            """{"category":"planet"}""",
            """{"answer":"No record found.","success":false}""",
            "Synthesized result");
        var workflow = new HolonetWorkflowService(agentService, new AgentResponseParser());

        var result = await workflow.ExecuteAsync("User request");

        Assert.IsType<PlainTextAgentResponse>(result.Response);
        var specialistStep = Assert.Single(
            result.Steps,
            step => step.Stage == HolonetWorkflowStage.Specialist);
        Assert.False(specialistStep.Succeeded);
        Assert.Equal(3, result.Steps.Count);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidOrchestratorResponse_TracesFailureWithParserError()
    {
        var agentService = new FakeFoundryAgentService("not json");
        var workflow = new HolonetWorkflowService(agentService, new AgentResponseParser());

        var result = await workflow.ExecuteAsync("User request");

        var step = Assert.Single(result.Steps);
        Assert.Equal(HolonetWorkflowStage.Orchestrator, step.Stage);
        Assert.False(step.Succeeded);
        Assert.Equal(((InvalidAgentResponse)result.Response).Error, step.Detail);
    }

    [Theory]
    [InlineData("""{"category":"event"}""", """{"answer":"Answer","success":true}""", "holonet-synthesizer-agent")]
    [InlineData("""{"category":"other"}""", null, "holonet-synthesizer-agent")]
    [InlineData("not json", null, "holonet-orchestrator")]
    [InlineData("""{"category":"event"}""", "not json", "holonet-event-agent")]
    public async Task ExecuteAsync_LastStepNamesTheAgentThatProducedTheResponse(
        string orchestratorResponse,
        string? specialistResponse,
        string expectedResponder)
    {
        string[] responses = specialistResponse is null
            ? [orchestratorResponse, "Synthesized result"]
            : [orchestratorResponse, specialistResponse, "Synthesized result"];
        var agentService = new FakeFoundryAgentService(responses);
        var workflow = new HolonetWorkflowService(agentService, new AgentResponseParser());

        var result = await workflow.ExecuteAsync("User request");

        Assert.Equal(expectedResponder, result.Steps[^1].AgentName);
    }

    private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
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