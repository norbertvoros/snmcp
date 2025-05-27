using ModelContextProtocol.Server; // From the ModelContextProtocol NuGet package
using SensoryBridge.McpAdapter.Services.SenseNet;
using System.ComponentModel;
using System.Threading.Tasks; // Required for Task
using System.Threading; // Required for CancellationToken

namespace SensoryBridge.McpAdapter.McpTools;

[McpServerToolType]
public class ContentTools
{
    private readonly IContentService _contentService;

    public ContentTools(IContentService contentService)
    {
        _contentService = contentService;
    }

    [McpServerTool, Description("Retrieves details for a specific content item from SenseNet.")]
    public async Task<string> GetSenseNetContentDetails(
        [Description("The ID or path of the content item.")] string contentId,
        CancellationToken cancellationToken)
    {
        // The CancellationToken is automatically provided by the MCP infrastructure
        return await _contentService.GetContentDetailsAsync(contentId, cancellationToken);
    }
}
