namespace HolonetAgents.Web.Models;

public enum HolonetWorkflowStage
{
    Orchestrator,
    Specialist,
    Synthesizer
}

public sealed record HolonetWorkflowStep(
    HolonetWorkflowStage Stage,
    string AgentName,
    string Detail,
    bool Succeeded,
    string? Output = null);

public sealed record HolonetWorkflowResult(
    AgentResponseResult Response,
    IReadOnlyList<HolonetWorkflowStep> Steps);
