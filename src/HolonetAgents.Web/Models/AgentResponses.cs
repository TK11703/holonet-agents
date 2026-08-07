using System.Text.Json.Serialization;

namespace HolonetAgents.Web.Models;

public enum AgentResponseContract
{
    PlainText,
    Orchestrator,
    Specialist
}

public enum OrchestratorCategory
{
    Character,
    Vehicle,
    Planet,
    Event,
    Other
}

public sealed record OrchestratorAgentResponse
{
    [JsonRequired]
    public required OrchestratorCategory Category { get; init; }
}

public sealed record SpecialistAgentResponse
{
    [JsonRequired]
    public required string Answer { get; init; }

    [JsonRequired]
    public required bool Success { get; init; }
}

public abstract record AgentResponseResult(string RawText);

public sealed record PlainTextAgentResponse(string Text)
    : AgentResponseResult(Text);

public sealed record ParsedOrchestratorAgentResponse(
    string RawText,
    OrchestratorAgentResponse Value)
    : AgentResponseResult(RawText);

public sealed record ParsedSpecialistAgentResponse(
    string RawText,
    SpecialistAgentResponse Value)
    : AgentResponseResult(RawText);

public sealed record InvalidAgentResponse(
    string RawText,
    AgentResponseContract ExpectedContract,
    string Error)
    : AgentResponseResult(RawText);