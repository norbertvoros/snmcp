using Xunit;
using SensoryBridge.SenseNetClient.Services; // The class we're testing
using SensoryBridge.McpAdapter.Services.SenseNet; // The interface
using System.Threading.Tasks;
using System.Threading;

namespace SensoryBridge.SenseNetClient.Tests;

public class SenseNetContentServiceTests
{
    private readonly IContentService _contentService;

    public SenseNetContentServiceTests()
    {
        // In the future, if SenseNetContentService takes dependencies (like a SenseNet SDK client or IOptions),
        // those would be mocked here. For now, it has no constructor dependencies.
        _contentService = new SenseNetContentService();
    }

    [Fact]
    public async Task GetContentDetailsAsync_ValidId_ReturnsMockData()
    {
        // Arrange
        var contentId = "test-content-123";
        var expectedMockResponseSubstring = "Mock details for content: test-content-123";

        // Act
        var result = await _contentService.GetContentDetailsAsync(contentId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(expectedMockResponseSubstring, result);
    }

    [Fact]
    public async Task GetContentDetailsAsync_DifferentId_ReturnsCorrectMockData()
    {
        // Arrange
        var contentId = "another-id-456";
        var expectedMockResponseSubstring = "Mock details for content: another-id-456";

        // Act
        var result = await _contentService.GetContentDetailsAsync(contentId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(expectedMockResponseSubstring, result);
    }

    [Fact]
    public async Task GetContentDetailsAsync_SimulatesAsyncWork()
    {
        // Arrange
        var contentId = "test-async";
        var task = _contentService.GetContentDetailsAsync(contentId, CancellationToken.None);
        
        // Assert that the task is not completed synchronously if it involves Task.Delay
        Assert.False(task.IsCompletedSuccessfully, "Task should not complete synchronously due to Task.Delay.");

        // Act
        var result = await task;

        // Assert
        Assert.NotNull(result);
    }
}
