using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // For StatusCodes
using Microsoft.Extensions.Options; // For IOptions
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol; // For Tool, Content etc.
using ModelContextProtocol; // For McpException
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SensoryBridge.Api.Controllers
{
    [ApiController]
    [Route("mcp")]
    public class McpController : ControllerBase
    {
        private readonly IOptions<McpServerOptions> _mcpServerOptionsSnapshot;
        private readonly IServiceProvider _requestServices;
        private readonly ILogger<McpController> _logger;
        private readonly ToolsCapability? _toolsCapability;

        public McpController(IOptions<McpServerOptions> mcpServerOptionsSnapshot, IServiceProvider requestServices, ILogger<McpController> logger)
        {
            _mcpServerOptionsSnapshot = mcpServerOptionsSnapshot ?? throw new ArgumentNullException(nameof(mcpServerOptionsSnapshot));
            _requestServices = requestServices ?? throw new ArgumentNullException(nameof(requestServices)); // Get IServiceProvider from HttpContext later if preferred
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _toolsCapability = _mcpServerOptionsSnapshot.Value?.Capabilities?.Tools;

            if (_toolsCapability == null)
            {
                _logger.LogWarning("ToolsCapability is null after initializing from IOptions<McpServerOptions>.Value.Capabilities.Tools");
            }
            else
            {
                _logger.LogInformation("ToolsCapability obtained. ListToolsHandler is {L अयोध्या}, CallToolHandler is {CH अयोध्या}, ToolCollection count is {Count}",
                    _toolsCapability.ListToolsHandler == null ? "null" : "NOT null",
                    _toolsCapability.CallToolHandler == null ? "null" : "NOT null",
                    _toolsCapability.ToolCollection?.Count ?? -1);
            }
        }

        // Minimal IMcpServer implementation for RequestContext
        private class MinimalMcpServer : IMcpServer
        {
            public McpServerOptions ServerOptions { get; }
            public IServiceProvider Services { get; }
            public ClientCapabilities? ClientCapabilities => null;
            public Implementation? ClientInfo => null;
            public LoggingLevel? LoggingLevel => null;

            public MinimalMcpServer(McpServerOptions options, IServiceProvider services)
            {
                ServerOptions = options;
                Services = services;
            }

            public Task RunAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            
            private class NoopDisposable : IAsyncDisposable { public ValueTask DisposeAsync() => ValueTask.CompletedTask; }
            public IAsyncDisposable RegisterNotificationHandler(string method, Func<JsonRpcNotification, CancellationToken, ValueTask> handler) => new NoopDisposable();
            public Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task<JsonRpcResponse> SendRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken = default)
                {
                    // Returning null! as a temporary measure to overcome persistent constructor issues
                    // and test the main controller logic. This part of MinimalMcpServer is not directly used by the methods under test.
                    return Task.FromResult<JsonRpcResponse>(null!);
                }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }

        [HttpGet("tools")]
        public Task<IActionResult> ListTools(CancellationToken cancellationToken)
        {
            if (_toolsCapability?.ToolCollection == null)
            {
                _logger.LogWarning("ListTools called but ToolCollection is null.");
                return Task.FromResult<IActionResult>(Ok(new List<Tool>()));
            }

            var tools = new List<Tool>();
            foreach (var serverTool in _toolsCapability.ToolCollection)
            {
                if (serverTool?.ProtocolTool != null)
                {
                    tools.Add(new Tool
                    {
                        Name = serverTool.ProtocolTool.Name,
                        Description = serverTool.ProtocolTool.Description,
                        InputSchema = serverTool.ProtocolTool.InputSchema
                        // OutputSchema removed as it's not on ModelContextProtocol.Protocol.Tool
                    });
                }
            }
            return Task.FromResult<IActionResult>(Ok(tools.DistinctBy(t => t.Name).ToList()));
        }

        [HttpPost("tools/{toolName}/call")]
        public async Task<IActionResult> CallTool(string toolName, [FromBody] JsonElement argumentsRaw, CancellationToken cancellationToken)
        {
            if (_toolsCapability?.ToolCollection == null)
            {
                 _logger.LogWarning("CallTool called for '{ToolName}' but ToolCollection is null.", toolName);
                return NotFound(new ProblemDetails { Title = "Tool not found", Detail = $"Tool '{toolName}' not found as ToolCollection is unavailable." });
            }

            var targetServerTool = _toolsCapability.ToolCollection
                .FirstOrDefault(st => st?.ProtocolTool?.Name == toolName);

            if (targetServerTool == null)
            {
                _logger.LogWarning("Tool '{ToolName}' not found in ToolCollection.", toolName);
                return NotFound(new ProblemDetails { Title = "Tool not found", Detail = $"Tool '{toolName}' not found." });
            }

            Dictionary<string, JsonElement>? argumentsDict = null;
            if (argumentsRaw.ValueKind == JsonValueKind.Object)
            {
                argumentsDict = new Dictionary<string, JsonElement>();
                foreach (var prop in argumentsRaw.EnumerateObject())
                {
                    argumentsDict.Add(prop.Name, prop.Value.Clone());
                }
            }
            
            var callParams = new CallToolRequestParams
            {
                Name = toolName,
                Arguments = argumentsDict
            };
            
            // Use HttpContext.RequestServices for the current request's scope
            var minimalServer = new MinimalMcpServer(_mcpServerOptionsSnapshot.Value, HttpContext.RequestServices);
            var requestContext = new RequestContext<CallToolRequestParams>(minimalServer)
            {
                Params = callParams,
                Services = HttpContext.RequestServices 
            };

            try
            {
                _logger.LogInformation("Invoking tool '{ToolName}' via InvokeAsync.", toolName);
                CallToolResponse response = await targetServerTool.InvokeAsync(requestContext, cancellationToken);
                return Ok(response.Content);
            }
            catch (McpException ex)
            {
                _logger.LogWarning(ex, "McpException while calling tool '{ToolName}'. ErrorCode: {ErrorCode}", toolName, ex.ErrorCode);
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "MCP Tool Error (" + ex.ErrorCode.ToString() + ")");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while calling tool '{ToolName}'.", toolName);
                return Problem(detail: $"An unexpected error occurred: {ex.Message}", statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
