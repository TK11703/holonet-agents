using HolonetAgents.Web.Models;
using HolonetAgents.Web.Services.AgentResponses;

namespace HolonetAgents.Web.Services;

public sealed class HolonetWorkflowService(
    IFoundryAgentService agentService,
    AgentResponseParser responseParser)
{
    public const string OrchestratorAgentName = "holonet-orchestrator";
    public const string SynthesizerAgentName = "holonet-synthesizer-agent";

    public async Task<AgentResponseResult> ExecuteAsync(
        string request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request);

        var orchestratorReply = await agentService.SendMessageAsync(
            null,
            OrchestratorAgentName,
            request,
            cancellationToken);
        var orchestratorResult = responseParser.Parse(
            orchestratorReply.RawText,
            AgentResponseContract.Orchestrator);

        if (orchestratorResult is not ParsedOrchestratorAgentResponse orchestrator)
        {
            return orchestratorResult;
        }

        var specialistAgentName = GetSpecialistAgentName(orchestrator.Value.Category);
        var synthesisInput = request;

        if (specialistAgentName is not null)
        {
            var specialistReply = await agentService.SendMessageAsync(
                null,
                specialistAgentName,
                request,
                cancellationToken);
            var specialistResult = responseParser.Parse(
                specialistReply.RawText,
                AgentResponseContract.Specialist);

            if (specialistResult is not ParsedSpecialistAgentResponse)
            {
                return specialistResult;
            }

            synthesisInput = specialistReply.RawText;
        }

        var synthesizerReply = await agentService.SendMessageAsync(
            null,
            SynthesizerAgentName,
            synthesisInput,
            cancellationToken);

        return new PlainTextAgentResponse(synthesizerReply.RawText);
    }

    private static string? GetSpecialistAgentName(OrchestratorCategory category) => category switch
    {
        OrchestratorCategory.Character => "holonet-character-agent",
        OrchestratorCategory.Vehicle => "holonet-vehicle-agent",
        OrchestratorCategory.Planet => "holonet-planet-agent",
        OrchestratorCategory.Event => "holonet-event-agent",
        OrchestratorCategory.Other => null,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
    };
}