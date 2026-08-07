using HolonetAgents.Web.Models;

namespace HolonetAgents.Web.Services.AgentResponses;

public sealed class AgentResponseContractResolver
{
    private static readonly IReadOnlyDictionary<string, AgentResponseContract> Contracts =
        new Dictionary<string, AgentResponseContract>(StringComparer.Ordinal)
        {
            ["holonet-orchestrator"] = AgentResponseContract.Orchestrator,
            ["holonet-character-agent"] = AgentResponseContract.Specialist,
            ["holonet-event-agent"] = AgentResponseContract.Specialist,
            ["holonet-jedi-agent"] = AgentResponseContract.Specialist,
            ["holonet-planet-agent"] = AgentResponseContract.Specialist,
            ["holonet-sith-agent"] = AgentResponseContract.Specialist,
            ["holonet-vehicle-agent"] = AgentResponseContract.Specialist
        };

    public AgentResponseContract Resolve(string? agentName) =>
        agentName is not null && Contracts.TryGetValue(agentName, out var contract)
            ? contract
            : AgentResponseContract.PlainText;
}