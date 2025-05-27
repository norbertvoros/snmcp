namespace SensoryBridge.McpAdapter.Services.SenseNet;

public interface IContentService
{
    Task<string> GetContentDetailsAsync(string contentId, CancellationToken cancellationToken = default);
    // We will add more complex return types and methods later
}
