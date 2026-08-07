using HolonetAgents.Web.Models;
using HolonetAgents.Web.Services.AgentResponses;

namespace HolonetAgents.Web.Tests;

public sealed class AgentResponseParserTests
{
    private readonly AgentResponseParser _parser = new();

    [Fact]
    public void Parse_ValidOrchestratorResponse_ReturnsTypedCategory()
    {
        var result = Assert.IsType<ParsedOrchestratorAgentResponse>(
            _parser.Parse("""{"category":"character"}""", AgentResponseContract.Orchestrator));

        Assert.Equal(OrchestratorCategory.Character, result.Value.Category);
    }

    [Fact]
    public void Parse_ValidSpecialistResponse_ReturnsTypedValue()
    {
        var result = Assert.IsType<ParsedSpecialistAgentResponse>(
            _parser.Parse("""{"answer":"Luke Skywalker","success":true}""", AgentResponseContract.Specialist));

        Assert.Equal("Luke Skywalker", result.Value.Answer);
        Assert.True(result.Value.Success);
    }

    [Fact]
    public void Parse_FencedSpecialistResponse_ReturnsTypedValueAndPreservesRawText()
    {
        const string rawText = """
            ```json
            {"answer":"The Battle of Yavin","success":true}
            ```
            """;

        var result = Assert.IsType<ParsedSpecialistAgentResponse>(
            _parser.Parse(rawText, AgentResponseContract.Specialist));

        Assert.Equal("The Battle of Yavin", result.Value.Answer);
        Assert.True(result.Value.Success);
        Assert.Equal(rawText, result.RawText);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"answer\":\"Luke\",\"success\":true,\"extra\":1}")]
    [InlineData("{\"answer\":null,\"success\":true}")]
    [InlineData("{\"answer\":\"Luke\",\"success\":\"yes\"}")]
    [InlineData("not json")]
    public void Parse_InvalidSpecialistResponse_ReturnsErrorAndPreservesRawText(string rawText)
    {
        var result = Assert.IsType<InvalidAgentResponse>(
            _parser.Parse(rawText, AgentResponseContract.Specialist));

        Assert.Equal(rawText, result.RawText);
        Assert.Equal(AgentResponseContract.Specialist, result.ExpectedContract);
    }

    [Theory]
    [InlineData("{\"category\":\"person\"}")]
    [InlineData("{\"category\":0}")]
    [InlineData("{\"Category\":\"character\"}")]
    public void Parse_InvalidOrchestratorResponse_ReturnsContractError(string rawText)
    {
        Assert.IsType<InvalidAgentResponse>(
            _parser.Parse(rawText, AgentResponseContract.Orchestrator));
    }

    [Fact]
    public void Parse_PlainText_PreservesContentWithoutJsonParsing()
    {
        const string rawText = "A synthesized answer.";

        var result = Assert.IsType<PlainTextAgentResponse>(
            _parser.Parse(rawText, AgentResponseContract.PlainText));

        Assert.Equal(rawText, result.Text);
        Assert.Equal(rawText, result.RawText);
    }
}