using HolonetAgents.Web.Models;
using HolonetAgents.Web.Services.AgentResponses;

namespace HolonetAgents.Web.Tests;

public sealed class AgentResponseContractResolverTests
{
    private readonly AgentResponseContractResolver _resolver = new();

    [Theory]
    [InlineData("holonet-character-agent")]
    [InlineData("holonet-event-agent")]
    [InlineData("holonet-jedi-agent")]
    [InlineData("holonet-planet-agent")]
    [InlineData("holonet-sith-agent")]
    [InlineData("holonet-vehicle-agent")]
    public void Resolve_SpecialistAgent_ReturnsSpecialist(string agentName)
    {
        Assert.Equal(AgentResponseContract.Specialist, _resolver.Resolve(agentName));
    }

    [Fact]
    public void Resolve_ExactOrchestratorName_ReturnsOrchestrator()
    {
        Assert.Equal(
            AgentResponseContract.Orchestrator,
            _resolver.Resolve("holonet-orchestrator"));
    }

    [Theory]
    [InlineData("HolonetOrchestrator")]
    [InlineData("holonet-synthesizer-agent")]
    [InlineData("unknown-agent")]
    [InlineData(null)]
    public void Resolve_UnmappedAgent_ReturnsPlainText(string? agentName)
    {
        Assert.Equal(AgentResponseContract.PlainText, _resolver.Resolve(agentName));
    }
}