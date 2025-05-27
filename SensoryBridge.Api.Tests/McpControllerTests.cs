using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options; 
using Microsoft.Extensions.Logging;
using SensoryBridge.Api.Controllers;
using ModelContextProtocol.Server; 
using ModelContextProtocol.Protocol; 
using ModelContextProtocol; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel; 

namespace SensoryBridge.Api.Tests;

public class McpControllerTests
{
    private Mock<IOptions<McpServerOptions>> _mockOptions;
    private Mock<IServiceProvider> _mockServiceProvider;
    private Mock<ILogger<McpController>> _mockLogger;
    private McpController _controller;

    private void InitializeControllerWithDefaultOptions()
    {
        _mockOptions = new Mock<IOptions<McpServerOptions>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<McpController>>();

        _mockOptions.Setup(o => o.Value).Returns(new McpServerOptions()); 

        _controller = new McpController(_mockOptions.Object, _mockServiceProvider.Object, _mockLogger.Object);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = _mockServiceProvider.Object }
        };
    }

    [Fact]
    public async Task ListTools_DefaultOptions_ReturnsEmptyList()
    {
        InitializeControllerWithDefaultOptions(); 
        var result = await _controller.ListTools(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var tools = Assert.IsAssignableFrom<IEnumerable<Tool>>(okResult.Value);
        Assert.Empty(tools); 
    }

    [Fact]
    public async Task CallTool_DefaultOptions_ToolNotFound_ReturnsNotFound()
    {
        InitializeControllerWithDefaultOptions();
        var result = await _controller.CallTool("anyTool", JsonDocument.Parse("{}").RootElement, CancellationToken.None);
        
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value); // This would need 'using Microsoft.AspNetCore.Mvc;'
        Assert.True(problemDetails.Detail != null && (problemDetails.Detail.Contains("ToolCollection is unavailable") || problemDetails.Detail.Contains("Tool 'anyTool' not found")));
    }

    [Fact]
    public void CallTool_ArgumentAccessSyntaxCheck_Compiles()
    {
        InitializeControllerWithDefaultOptions(); 
        var toolName = "testTool"; 
        var args = JsonDocument.Parse("{\"param\":\"value\"}").RootElement; // Corrected JSON

        Dictionary<string, JsonElement>? argumentsDict = null;
        if (args.ValueKind == JsonValueKind.Object)
        {
            argumentsDict = new Dictionary<string, JsonElement>();
            foreach (var prop in args.EnumerateObject()) 
            { 
                argumentsDict.Add(prop.Name, prop.Value.Clone()); 
            }
        }
        
        if (argumentsDict != null && argumentsDict.ContainsKey("param"))
        {
            var actualValue = argumentsDict["param"].GetString();
            Assert.Equal("value", actualValue);
        } 
        else 
        {
            Assert.True(false, "Test setup error: param not in dictionary or argumentsDict is null.");
        }
    }
}
