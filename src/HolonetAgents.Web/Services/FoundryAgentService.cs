using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Core;
using Azure.Identity;
using HolonetAgents.Web.Models;
using Microsoft.Extensions.Options;

namespace HolonetAgents.Web.Services;

/// <summary>
/// Thin wrapper around the Azure AI Foundry persistent agents API that exposes the
/// operations needed by the chat window: listing agents, starting a conversation
/// thread and exchanging messages with an agent.
/// </summary>
public sealed class FoundryAgentService
{
    private readonly PersistentAgentsClient? _client;
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

        _client = new PersistentAgentsClient(_options.Endpoint, credential);
    }

    /// <summary>
    /// Lists the agents that already exist in the configured Foundry project.
    /// </summary>
    public async Task<IReadOnlyList<AgentSummary>> GetAgentsAsync(CancellationToken cancellationToken = default)
    {
        var client = EnsureClient();
        var agents = new List<AgentSummary>();

        await foreach (PersistentAgent agent in client.Administration
            .GetAgentsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false))
        {
            agents.Add(new AgentSummary(agent.Id, string.IsNullOrWhiteSpace(agent.Name) ? agent.Id : agent.Name));
        }

        return agents;
    }

    /// <summary>
    /// Creates a new conversation thread and returns its id.
    /// </summary>
    public async Task<string> CreateThreadAsync(CancellationToken cancellationToken = default)
    {
        Response<PersistentAgentThread> thread = await EnsureClient().Threads
            .CreateThreadAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return thread.Value.Id;
    }

    /// <summary>
    /// Sends a user message to the agent on the given thread and returns the agent reply text.
    /// </summary>
    public async Task<string> SendMessageAsync(
        string threadId,
        string agentId,
        string message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(threadId);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var client = EnsureClient();

        await client.Messages
            .CreateMessageAsync(threadId, MessageRole.User, message, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        Response<ThreadRun> runResponse = await client.Runs
            .CreateRunAsync(threadId, agentId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        ThreadRun run = runResponse.Value;

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.RunTimeoutSeconds)));

        while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), timeoutSource.Token).ConfigureAwait(false);
            run = (await client.Runs
                .GetRunAsync(threadId, run.Id, timeoutSource.Token)
                .ConfigureAwait(false)).Value;
        }

        if (run.Status != RunStatus.Completed)
        {
            _logger.LogWarning("Agent run {RunId} ended with status {Status}: {Error}", run.Id, run.Status, run.LastError?.Message);
            throw new InvalidOperationException(
                $"The agent run ended with status '{run.Status}'. {run.LastError?.Message}".TrimEnd());
        }

        return await GetLatestAgentReplyAsync(threadId, run.Id, cancellationToken).ConfigureAwait(false);
    }

    private PersistentAgentsClient EnsureClient() =>
        _client ?? throw new InvalidOperationException(
            $"'{FoundryOptions.SectionName}:{nameof(FoundryOptions.Endpoint)}' must be set to the Azure AI Foundry project endpoint.");

    private async Task<string> GetLatestAgentReplyAsync(string threadId, string runId, CancellationToken cancellationToken)
    {
        var parts = new List<string>();

        await foreach (PersistentThreadMessage threadMessage in EnsureClient().Messages
            .GetMessagesAsync(threadId, runId, order: ListSortOrder.Ascending, cancellationToken: cancellationToken)
            .ConfigureAwait(false))
        {
            if (threadMessage.Role != MessageRole.Agent)
            {
                continue;
            }

            foreach (MessageContent content in threadMessage.ContentItems)
            {
                if (content is MessageTextContent text && !string.IsNullOrWhiteSpace(text.Text))
                {
                    parts.Add(text.Text);
                }
            }
        }

        return parts.Count == 0
            ? "(The agent did not return any text content.)"
            : string.Join(Environment.NewLine + Environment.NewLine, parts);
    }
}

/// <summary>
/// Minimal projection of a Foundry agent for display in the UI.
/// </summary>
public sealed record AgentSummary(string Id, string Name);
