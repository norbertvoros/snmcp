using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Net.Http.Json; // For ReadFromJsonAsync and PostAsJsonAsync
using System.Threading.Tasks;
using System.Collections.Generic;
using ModelContextProtocol.Protocol; // For Tool, Content
using System.Text.Json; // For JsonElement and JsonSerializer
using SensoryBridge.Api; // To access Program for WebApplicationFactory

namespace SensoryBridge.Api.Tests;

// Helper DTOs for expected responses if not using the exact protocol types directly for assertions
public class ToolInfo
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public JsonElement InputSchema { get; set; } // Or a more specific type if schema is known
}

public class ContentInfo
{
    public string? Type { get; set; }
    public string? Text { get; set; }
    // Add other Content fields if necessary, like 'uri' or 'error'
}


public class McpApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>> // Use Program from SensoryBridge.Api
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public McpApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        // Client is configured to not follow redirects and allow auto decompression.
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetTools_ReturnsListOfTools()
    {
        // Arrange

        // Act
        var response = await _client.GetAsync("/mcp/tools");

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        var tools = await response.Content.ReadFromJsonAsync<List<ToolInfo>>(); 
        
        Assert.NotNull(tools);
        Assert.NotEmpty(tools);
        var tool = Assert.Single(tools); // Expecting only GetSenseNetContentDetails for now
        Assert.Equal("GetSenseNetContentDetails", tool.Name);
        Assert.Equal("Retrieves details for a specific content item from SenseNet.", tool.Description);
        Assert.Equal(JsonValueKind.Object, tool.InputSchema.ValueKind); // Basic schema check
        Assert.True(tool.InputSchema.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "object");
    }

    [Fact]
    public async Task CallTool_GetSenseNetContentDetails_ReturnsMockData()
    {
        // Arrange
        var toolName = "GetSenseNetContentDetails";
        var requestBody = new { contentId = "integration-test-123" };

        // Act
        var response = await _client.PostAsJsonAsync($"/mcp/tools/{toolName}/call", requestBody);

        // Assert
        response.EnsureSuccessStatusCode();
        var contentList = await response.Content.ReadFromJsonAsync<List<ContentInfo>>();

        Assert.NotNull(contentList);
        var content = Assert.Single(contentList);
        Assert.Equal("text", content.Type);
        Assert.Equal("Mock details for content: integration-test-123", content.Text);
    }

    [Fact]
    public async Task CallTool_ToolNotFound_ReturnsNotFound()
    {
        // Arrange
        var toolName = "NonExistentTool";
        var requestBody = new { arg = "value" };

        // Act
        var response = await _client.PostAsJsonAsync($"/mcp/tools/{toolName}/call", requestBody);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(); // Microsoft.AspNetCore.Mvc.ProblemDetails
        Assert.NotNull(problemDetails);
        Assert.Equal("Tool 'NonExistentTool' not found.", problemDetails.Detail);
    }

    [Fact]
    public async Task CallTool_MissingRequiredArgument_ReturnsBadRequest() // Or specific error from tool if it handles it
    {
        // Arrange
        var toolName = "GetSenseNetContentDetails";
        // Missing 'contentId'
        var requestBody = new { otherArg = "value" }; 

        // Act
        var response = await _client.PostAsJsonAsync($"/mcp/tools/{toolName}/call", requestBody);

        // Assert
        // This depends on how the McpServerTool.InvokeAsync handles missing args.
        // The generic tool invoker might throw an McpException if a required arg is missing
        // based on the schema, or the tool method itself might throw.
        // Our current GetSenseNetContentDetails tool method takes 'string contentId'. If it's not provided,
        // model binding for the tool parameters might fail.
        // The current McpController's CallTool method catches McpException and returns Problem (400).
        // If parameter binding fails before InvokeAsync, it might be a different error.
        // For now, let's expect a 400 Bad Request due to McpException or similar.
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Contains("MCP Tool Error", problemDetails.Title); // From McpController's McpException handling
        // The detail might be "Required argument 'contentId' was not provided." or similar from the MCP SDK's tool invocation logic.
        Assert.NotNull(problemDetails.Detail); 
    }
}
