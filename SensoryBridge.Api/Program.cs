using Microsoft.Extensions.DependencyInjection; // Should be there
using SensoryBridge.McpAdapter.McpTools; // For ContentTools
using ModelContextProtocol.Server; // For AddMcpServer
using SensoryBridge.McpAdapter.Services.SenseNet; // For service registration
using SensoryBridge.SenseNetClient.Services; // For service registration
using SensoryBridge.SenseNetClient.Models; // Added for SenseNetConfig

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure SenseNet settings
builder.Services.Configure<SenseNetConfig>(builder.Configuration.GetSection("SenseNet"));

// Ensure logging is configured (it usually is by default in ASP.NET Core)
// builder.Services.AddLogging(); // Typically already done by Host.CreateDefaultBuilder or WebApplication.CreateBuilder

// Register SenseNet Services
builder.Services.AddSingleton<IContentService, SenseNetContentService>();
// Add other SenseNet services (IUserService, IWorkflowService) here later as they are created

// Register MCP Server and Tools
builder.Services.AddMcpServer().WithTools<ContentTools>(); // Ensure StdioServerTransport is removed
    // Alternatively, if WithTools is not ideal,
    // tools can be registered individually or by scanning the entry assembly if tools are moved/also referenced here.
    // For now, this assumes ContentTools is in McpAdapter and that assembly is correctly referenced.

// Add standard ASP.NET Core services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map controller endpoints
app.MapControllers();

app.Run();

public partial class Program { } // Added for test visibility
