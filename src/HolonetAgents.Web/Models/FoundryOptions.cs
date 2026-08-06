namespace HolonetAgents.Web.Models;

/// <summary>
/// Configuration for connecting to an existing Azure AI Foundry project and its agents.
/// </summary>
public sealed class FoundryOptions
{
    public const string SectionName = "Foundry";

    /// <summary>
    /// The Foundry project endpoint, e.g. https://&lt;resource&gt;.services.ai.azure.com/api/projects/&lt;project&gt;.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Optional default agent id to pre-select in the chat window.
    /// </summary>
    public string? DefaultAgentId { get; set; }

    /// <summary>
    /// Optional client id of a user-assigned managed identity used by DefaultAzureCredential.
    /// </summary>
    public string? ManagedIdentityClientId { get; set; }

    /// <summary>
    /// How long to wait when loading the agent list from Foundry.
    /// </summary>
    public int AgentListTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// How long to wait for an agent run to complete before giving up.
    /// </summary>
    public int RunTimeoutSeconds { get; set; } = 120;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Endpoint);
}
