using Microsoft.Extensions.Options; // For IOptions
using Microsoft.Extensions.Logging; // For ILogger
using SensoryBridge.McpAdapter.Services.SenseNet;
using SensoryBridge.SenseNetClient.Models; // For SenseNetConfig
using System; // For ArgumentNullException
using System.Threading;
using System.Threading.Tasks;

namespace SensoryBridge.SenseNetClient.Services;

public class SenseNetContentService : IContentService
{
    private readonly SenseNetConfig _config;
    private readonly ILogger<SenseNetContentService> _logger;

    public SenseNetContentService(IOptions<SenseNetConfig> configOptions, ILogger<SenseNetContentService> logger)
    {
        _config = configOptions?.Value ?? throw new ArgumentNullException(nameof(configOptions), "SenseNetConfig cannot be null via IOptions.Value.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("SenseNetContentService created. ServiceUrl: '{ServiceUrl}', ApiKey is {ApiKeyStatus}", 
            _config.ServiceUrl, 
            string.IsNullOrEmpty(_config.ApiKey) ? "NOT set" : "set");

        if (string.IsNullOrEmpty(_config.ServiceUrl))
        {
            _logger.LogWarning("SenseNet ServiceUrl is not configured.");
            // Consider throwing an exception if ServiceUrl is absolutely required to function,
            // or handle this state gracefully in methods.
        }
    }

    public async Task<string> GetContentDetailsAsync(string contentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetContentDetailsAsync called for contentId: {ContentId}. Using ServiceUrl: {ServiceUrl}", contentId, _config.ServiceUrl);
        
        // TODO: Implement actual SenseNet API call using _config.ServiceUrl and _config.ApiKey
        // For now, return mock data incorporating the contentId
        await Task.Delay(50, cancellationToken); // Simulate async work a bit shorter
        return $"Mock details from {_config.ServiceUrl} for content: {contentId}";
    }
}
