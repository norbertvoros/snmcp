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

namespace SensoryBridge.Api.Controllers;

[ApiController]
[Route("mcp")]
public class McpController : ControllerBase
{
    private readonly IOptions<McpServerOptions> _mcpServerOptions;
    private readonly ToolsCapability? _toolsCapability;
    private readonly ILogger<McpController> _logger;

    // IMcpServer removed for now to test IOptions<McpServerOptions>
    public McpController(IOptions<McpServerOptions> mcpServerOptions, ILogger<McpController> logger)
    {
        _mcpServerOptions = mcpServerOptions ?? throw new ArgumentNullException(nameof(mcpServerOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (_mcpServerOptions.Value == null)
        {
            _logger.LogError("IOptions<McpServerOptions>.Value is null.");
        }
        else
        {
            _logger.LogInformation("McpServerOptions.Value obtained.");
            if (_mcpServerOptions.Value.Capabilities == null)
            {
                _logger.LogError("McpServerOptions.Value.Capabilities is null.");
            }
            else
            {
                _logger.LogInformation("ServerCapabilities obtained from Options.");
                _toolsCapability = _mcpServerOptions.Value.Capabilities.Tools;
                if (_toolsCapability == null)
                {
                    _logger.LogError("ToolsCapability from Options is null.");
                }
                else
                {
                    _logger.LogInformation("ToolsCapability obtained from Options.");
                    _logger.LogInformation("ListToolsHandler is {Status}", _toolsCapability.ListToolsHandler == null ? "null" : "NOT null");
                    _logger.LogInformation("CallToolHandler is {Status}", _toolsCapability.CallToolHandler == null ? "null" : "NOT null");
                    _logger.LogInformation("ToolCollection count is {Count}", _toolsCapability.ToolCollection?.Count ?? -1);
                }
            }
        }
    }

    [HttpGet("tools")]
    public async Task<IActionResult> ListTools(CancellationToken cancellationToken)
    {
        if (_toolsCapability == null)
        {
            _logger.LogError("ListTools called but _toolsCapability is null.");
            return Problem("Tools capability is not configured or accessible.", statusCode: StatusCodes.Status500InternalServerError);
        }
        _logger.LogInformation("ListTools endpoint hit. ToolsCapability.ListToolsHandler is {L अयोध्या}", _toolsCapability.ListToolsHandler == null ? "null" : "NOT null");


        // Temporarily disable actual handler invocation as we don't have IMcpServer for RequestContext
        await Task.Delay(10, cancellationToken); // Simulate work
        return Ok(new List<Tool> { new Tool { Name = "TestToolFromOptions", Description = "Checking if options are populated" } });
    }

    [HttpPost("tools/{toolName}/call")]
    public async Task<IActionResult> CallTool(string toolName, [FromBody] JsonElement argumentsRaw, CancellationToken cancellationToken)
    {
        if (_toolsCapability == null)
        {
             _logger.LogError("CallTool called but _toolsCapability is null.");
            return Problem("Tools capability is not configured or accessible.", statusCode: StatusCodes.Status500InternalServerError);
        }
         _logger.LogInformation("CallTool endpoint hit for {ToolName}. ToolsCapability.CallToolHandler is {CH अयोध्या}", toolName, _toolsCapability.CallToolHandler == null ? "null" : "NOT null");

        // Temporarily disable actual handler invocation
        await Task.Delay(10, cancellationToken);
        return Ok(new List<Content> { new Content { Text = $"Called {toolName} - options test" } });
    }
}
