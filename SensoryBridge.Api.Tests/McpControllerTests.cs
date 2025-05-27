using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Required for StatusCodes
using SensoryBridge.Api.Controllers;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol; // For Tool, ListToolsRequestParams, RequestContext, etc.
using ModelContextProtocol; // Added for McpException and McpErrorCode
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SensoryBridge.Api.Tests;

public class McpControllerTests
{
    private readonly Mock<IMcpServer> _mockMcpServer;
    private readonly Mock<McpServerOptions> _mockMcpServerOptions;
    private readonly Mock<ServerCapabilities> _mockServerCapabilities;
    private readonly Mock<ToolsCapability> _mockToolsCapability;
    private McpController _controller;

    public McpControllerTests()
    {
        _mockMcpServer = new Mock<IMcpServer>();
        _mockMcpServerOptions = new Mock<McpServerOptions>();
        _mockServerCapabilities = new Mock<ServerCapabilities>();
        _mockToolsCapability = new Mock<ToolsCapability>();

        _mockMcpServer.Setup(s => s.ServerOptions).Returns(_mockMcpServerOptions.Object);
        // Removed: _mockMcpServerOptions.Setup(o => o.Capabilities).Returns(_mockServerCapabilities.Object); 
        // This avoids NotSupportedException. Controller's _toolsCapability will be from default instances.
        // Tests needing specific tool setups must fully configure the chain for _mockMcpServer.ServerOptions.

        _controller = new McpController(_mockMcpServer.Object);
        
        // Mock HttpContext for RequestServices if RequestContext needs it.
        var serviceProviderMock = new Mock<IServiceProvider>();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = serviceProviderMock.Object }
        };
    }

    [Fact]
    public async Task ListTools_ToolsCapabilityNotConfigured_ReturnsProblem()
    {
        // Arrange
        _mockServerCapabilities.Setup(c => c.Tools).Returns((ToolsCapability)null); // Make ToolsCapability null
        var controllerWithNullTools = new McpController(_mockMcpServer.Object); // Re-init with new setup

        // Act
        var result = await controllerWithNullTools.ListTools(CancellationToken.None);

        // Assert
        var problemResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemResult.StatusCode); // Default Problem status
        Assert.Contains("Tools capability is not configured", (problemResult.Value as ProblemDetails)?.Detail);
    }

    [Fact]
    public async Task ListTools_NoTools_ReturnsEmptyList()
    {
        // Arrange
        _mockToolsCapability.Setup(tc => tc.ToolCollection).Returns((McpServerPrimitiveCollection<McpServerTool>)null);
        _mockToolsCapability.Setup(tc => tc.ListToolsHandler).Returns((Func<RequestContext<ListToolsRequestParams>, CancellationToken, ValueTask<ListToolsResult>>)null);

        // Act
        var result = await _controller.ListTools(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var tools = Assert.IsAssignableFrom<IEnumerable<Tool>>(okResult.Value);
        Assert.Empty(tools);
    }

    [Fact]
    public async Task ListTools_FromToolCollection_ReturnsTools()
    {
        // Arrange
        var mockServerTool = new Mock<McpServerTool>(); // McpServerTool is abstract, Moq can mock it.
        var expectedProtocolTool = new Tool { Name = "tool1", Description = "Desc1" };
        mockServerTool.Setup(st => st.ProtocolTool).Returns(expectedProtocolTool);
        
        var serverToolsList = new List<McpServerTool> { mockServerTool.Object };

        // Mock McpServerPrimitiveCollection itself and set up its IEnumerable behavior
        var mockToolCollection = new Mock<McpServerPrimitiveCollection<McpServerTool>>();
        mockToolCollection.As<IEnumerable<McpServerTool>>().Setup(x => x.GetEnumerator()).Returns(() => serverToolsList.GetEnumerator());
        // Optionally, set up Count and indexer if the code under test uses them, though foreach only needs GetEnumerator.
        // mockToolCollection.As<ICollection<McpServerTool>>().Setup(x => x.Count).Returns(serverToolsList.Count);
        // mockToolCollection.As<IList<McpServerTool>>().Setup(x => x[0]).Returns(mockServerTool.Object);

        _mockToolsCapability.Setup(tc => tc.ToolCollection).Returns(mockToolCollection.Object);
        _mockToolsCapability.Setup(tc => tc.ListToolsHandler).Returns((Func<RequestContext<ListToolsRequestParams>, CancellationToken, ValueTask<ListToolsResult>>)null);
        
        // Ensure the main _mockServerCapabilities returns the _mockToolsCapability which is now configured.
        _mockServerCapabilities.Setup(c => c.Tools).Returns(_mockToolsCapability.Object);

        // Act
        var result = await _controller.ListTools(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var tools = Assert.IsAssignableFrom<IEnumerable<Tool>>(okResult.Value).ToList();
        Assert.Single(tools);
        Assert.Equal("tool1", tools[0].Name);
        Assert.Equal("Desc1", tools[0].Description);
    }
    
    [Fact]
    public async Task ListTools_FromHandler_ReturnsTools()
    {
        // Arrange
        var handlerResult = new ListToolsResult { Tools = new List<Tool> { new Tool { Name = "handlerTool", Description = "Handler Desc" } } };
        _mockToolsCapability.Setup(tc => tc.ListToolsHandler)
            .Returns((RequestContext<ListToolsRequestParams> ctx, CancellationToken ct) => ValueTask.FromResult(handlerResult));
        _mockToolsCapability.Setup(tc => tc.ToolCollection).Returns((McpServerPrimitiveCollection<McpServerTool>)null);

        // Act
        var result = await _controller.ListTools(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var tools = Assert.IsAssignableFrom<IEnumerable<Tool>>(okResult.Value).ToList();
        Assert.Single(tools);
        Assert.Equal("handlerTool", tools[0].Name);
    }

    [Fact]
    public async Task CallTool_ToolsCapabilityNotConfigured_ReturnsProblem()
    {
        // Arrange
        _mockServerCapabilities.Setup(c => c.Tools).Returns((ToolsCapability)null);
         var controllerWithNullTools = new McpController(_mockMcpServer.Object); // Re-init

        // Act
        var result = await controllerWithNullTools.CallTool("anyTool", JsonDocument.Parse("{}").RootElement, CancellationToken.None);

        // Assert
        var problemResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemResult.StatusCode);
        Assert.Contains("Tools capability is not configured", (problemResult.Value as ProblemDetails)?.Detail);
    }

    [Fact]
    public async Task CallTool_HandlerNotConfigured_ReturnsProblem()
    {
        // Arrange
        _mockToolsCapability.Setup(tc => tc.CallToolHandler).Returns((Func<RequestContext<CallToolRequestParams>, CancellationToken, ValueTask<CallToolResponse>>)null);

        // Act
        var result = await _controller.CallTool("anyTool", JsonDocument.Parse("{}").RootElement, CancellationToken.None);

        // Assert
        var problemResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemResult.StatusCode);
        Assert.Contains("CallToolHandler is not configured", (problemResult.Value as ProblemDetails)?.Detail);
    }

    [Fact]
    public async Task CallTool_HandlerInvoked_ReturnsOk()
    {
        // Arrange
        var toolName = "testTool";
        var args = JsonDocument.Parse("{\"param\":\"value\"}").RootElement; // Corrected JSON format
        var expectedResponseContent = new List<Content> { new Content { Text = "Tool success" } };
        var callToolResponse = new CallToolResponse { Content = expectedResponseContent };

        _mockToolsCapability.Setup(tc => tc.CallToolHandler)
            .Returns((RequestContext<CallToolRequestParams> ctx, CancellationToken ct) => 
                {
                    Assert.Equal(toolName, ctx.Params.Name);
                    Assert.NotNull(ctx.Params.Arguments);
                    Assert.Equal("value", ctx.Params.Arguments["param"].GetString());
                    return ValueTask.FromResult(callToolResponse);
                });

        // Act
        var result = await _controller.CallTool(toolName, args, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResponseContent, okResult.Value);
         _mockToolsCapability.Verify(tc => tc.CallToolHandler(It.IsAny<RequestContext<CallToolRequestParams>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CallTool_HandlerThrowsMcpException_ReturnsProblem()
    {
        // Arrange
        _mockToolsCapability.Setup(tc => tc.CallToolHandler(It.IsAny<RequestContext<CallToolRequestParams>>(), It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException<CallToolResponse>(new McpException("Tool error", McpErrorCode.InternalError)));
        
        // Act
        var result = await _controller.CallTool("errorTool", JsonDocument.Parse("{}").RootElement, CancellationToken.None);

        // Assert
        var problemResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problemResult.StatusCode); // As per controller's catch block
        Assert.Contains("Tool error", (problemResult.Value as ProblemDetails)?.Detail);
    }
}
