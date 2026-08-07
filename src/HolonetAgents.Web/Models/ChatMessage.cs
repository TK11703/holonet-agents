namespace HolonetAgents.Web.Models;

public enum ChatRole
{
    User,
    Agent
}

public sealed class ChatMessage
{
    public required ChatRole Role { get; init; }

    public required string Text { get; set; }

    public string? AuthorName { get; init; }

    public AgentResponseResult? AgentResponse { get; init; }

    public IReadOnlyList<HolonetWorkflowStep>? WorkflowSteps { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    public bool IsError { get; init; }
}
