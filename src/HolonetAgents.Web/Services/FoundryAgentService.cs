using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Core;
using Azure.Identity;
using HolonetAgents.Web.Models;
using Microsoft.Extensions.Options;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace HolonetAgents.Web.Services;

/// <summary>
/// Thin wrapper around the Microsoft Foundry project API that exposes the
/// operations needed by the chat window: listing prompt agents and exchanging
/// responses with an agent.
/// </summary>
public sealed class FoundryAgentService
{
    private readonly AIProjectClient? _client;
    private readonly FoundryOptions _options;
    private readonly ILogger<FoundryAgentService> _logger;

    public FoundryAgentService(IOptions<FoundryOptions> options, ILogger<FoundryAgentService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (!_options.IsConfigured)
        {
            // The client is created lazily-guarded so the UI can show a friendly message
            // instead of failing at start-up when the project has not been configured yet.
            _logger.LogError(
                "'{Section}:{Key}' is not configured; the chat window will not be able to reach Azure AI Foundry.",
                FoundryOptions.SectionName,
                nameof(FoundryOptions.Endpoint));
            return;
        }

        TokenCredential credential = new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = string.IsNullOrWhiteSpace(_options.ManagedIdentityClientId)
                    ? null
                    : _options.ManagedIdentityClientId
            });

        _client = new AIProjectClient(new Uri(_options.Endpoint), credential);
    }

    /// <summary>
    /// Lists the agents that already exist in the configured Foundry project.
    /// </summary>
    public async Task<IReadOnlyList<AgentSummary>> GetAgentsAsync(CancellationToken cancellationToken = default)
    {
        var client = EnsureClient();
        var agents = new List<AgentSummary>();

        await foreach (ProjectsAgentRecord agent in client.AgentAdministrationClient
            .GetAgentsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false))
        {
            agents.Add(new AgentSummary(agent.Name, agent.Name));
        }

        return agents;
    }

    /// <summary>
    /// Sends a user message to the latest version of an agent and returns its response.
    /// </summary>
    public async Task<AgentReply> SendMessageAsync(
        string? previousResponseId,
        string agentId,
        string message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var client = EnsureClient();
        var responseClient = client.ProjectOpenAIClient
            .GetProjectResponsesClientForAgent(new AgentReference(agentId));
        var responseOptions = new CreateResponseOptions
        {
            PreviousResponseId = previousResponseId
        };
        responseOptions.InputItems.Add(ResponseItem.CreateUserMessageItem(message));

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.RunTimeoutSeconds)));

        try
        {
            ResponseResult response = await responseClient
                .CreateResponseAsync(responseOptions, timeoutSource.Token)
                .ConfigureAwait(false);

            string text = response.GetOutputText();
            return new AgentReply(
                response.Id,
                string.IsNullOrWhiteSpace(text) ? "(The agent did not return any text content.)" : text);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Agent response did not complete within {Timeout} seconds.", _options.RunTimeoutSeconds);
            throw new TimeoutException(
                $"The agent did not respond within {_options.RunTimeoutSeconds} seconds.");
        }
    }

    private AIProjectClient EnsureClient() =>
        _client ?? throw new InvalidOperationException(
            $"'{FoundryOptions.SectionName}:{nameof(FoundryOptions.Endpoint)}' must be set to the Azure AI Foundry project endpoint.");
}

/// <summary>
/// Minimal projection of a Foundry agent for display in the UI.
/// </summary>
public sealed record AgentSummary(string Id, string Name);

public sealed record AgentReply(string ResponseId, string Text);
