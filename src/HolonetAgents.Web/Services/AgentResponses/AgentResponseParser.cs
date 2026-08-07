using System.Text.Json;
using System.Text.Json.Serialization;
using HolonetAgents.Web.Models;

namespace HolonetAgents.Web.Services.AgentResponses;

public sealed class AgentResponseParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };

    public AgentResponseResult Parse(string rawText, AgentResponseContract contract)
    {
        ArgumentNullException.ThrowIfNull(rawText);

        if (contract == AgentResponseContract.PlainText)
        {
            return new PlainTextAgentResponse(rawText);
        }

        try
        {
            return contract switch
            {
                AgentResponseContract.Orchestrator => new ParsedOrchestratorAgentResponse(
                    rawText,
                    DeserializeRequired<OrchestratorAgentResponse>(rawText)),
                AgentResponseContract.Specialist => ParseSpecialist(rawText),
                _ => throw new ArgumentOutOfRangeException(nameof(contract), contract, null)
            };
        }
        catch (JsonException ex)
        {
            return new InvalidAgentResponse(
                rawText,
                contract,
                $"The agent response did not match the expected {contract.ToString().ToLowerInvariant()} contract: {ex.Message}");
        }
    }

    private static T DeserializeRequired<T>(string rawText) where T : class =>
        JsonSerializer.Deserialize<T>(UnwrapJsonCodeFence(rawText), SerializerOptions)
        ?? throw new JsonException("The response must be a JSON object.");

    private static string UnwrapJsonCodeFence(string rawText)
    {
        var trimmed = rawText.Trim();
        var openingLineEnd = trimmed.IndexOf('\n');
        var closingLineStart = trimmed.LastIndexOf('\n');

        if (openingLineEnd < 0 || closingLineStart <= openingLineEnd)
        {
            return rawText;
        }

        var openingLine = trimmed[..openingLineEnd].TrimEnd('\r');
        var closingLine = trimmed[(closingLineStart + 1)..].TrimEnd('\r');
        if (openingLine is not ("```" or "```json") || closingLine != "```")
        {
            return rawText;
        }

        return trimmed[(openingLineEnd + 1)..closingLineStart].Trim();
    }

    private static ParsedSpecialistAgentResponse ParseSpecialist(string rawText)
    {
        var response = DeserializeRequired<SpecialistAgentResponse>(rawText);
        if (response.Answer is null)
        {
            throw new JsonException("The 'answer' property must be a string.");
        }

        return new ParsedSpecialistAgentResponse(rawText, response);
    }
}