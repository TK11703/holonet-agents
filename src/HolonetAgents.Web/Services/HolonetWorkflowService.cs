using HolonetAgents.Web.Models;
using HolonetAgents.Web.Services.AgentResponses;

namespace HolonetAgents.Web.Services;

public sealed class HolonetWorkflowService(
    IFoundryAgentService agentService,
    AgentResponseParser responseParser)
{
    public const string OrchestratorAgentName = "holonet-orchestrator";
    public const string SynthesizerAgentName = "holonet-synthesizer-agent";

    public async Task<HolonetWorkflowResult> ExecuteAsync(
        string request,
        IProgress<HolonetWorkflowStep>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request);

        var steps = new List<HolonetWorkflowStep>();

        void Record(HolonetWorkflowStage stage, string agentName, string detail, bool succeeded, string? output = null)
        {
            var step = new HolonetWorkflowStep(stage, agentName, detail, succeeded, output);
            steps.Add(step);
            progress?.Report(step);
        }

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
            Record(
                HolonetWorkflowStage.Orchestrator,
                OrchestratorAgentName,
                DescribeFailure(orchestratorResult),
                false);
            return new HolonetWorkflowResult(orchestratorResult, steps);
        }

        var category = orchestrator.Value.Category;
        var specialistAgentName = GetSpecialistAgentName(category);
        Record(
            HolonetWorkflowStage.Orchestrator,
            OrchestratorAgentName,
            specialistAgentName is null
                ? $"Classified the request as {category}, so no specialist was needed."
                : $"Classified the request as {category}.",
            true);

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

            if (specialistResult is not ParsedSpecialistAgentResponse specialist)
            {
                Record(
                    HolonetWorkflowStage.Specialist,
                    specialistAgentName,
                    DescribeFailure(specialistResult),
                    false);
                return new HolonetWorkflowResult(specialistResult, steps);
            }

            Record(
                HolonetWorkflowStage.Specialist,
                specialistAgentName,
                specialist.Value.Success
                    ? "Answered the request."
                    : "Reported that it could not answer the request.",
                specialist.Value.Success,
                specialist.Value.Answer);

            synthesisInput = specialistReply.RawText;
        }

        var synthesizerReply = await agentService.SendMessageAsync(
            null,
            SynthesizerAgentName,
            synthesisInput,
            cancellationToken);
        Record(
            HolonetWorkflowStage.Synthesizer,
            SynthesizerAgentName,
            "Wrote the final answer.",
            true);

        return new HolonetWorkflowResult(new PlainTextAgentResponse(synthesizerReply.RawText), steps);
    }

    private static string DescribeFailure(AgentResponseResult result) => result switch
    {
        InvalidAgentResponse invalid => invalid.Error,
        _ => "Returned an unexpected response."
    };

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