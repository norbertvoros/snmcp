using Xunit;
using Moq;
using SensoryBridge.McpAdapter.Services.SenseNet;
using SensoryBridge.McpAdapter.McpTools;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel; // For DescriptionAttribute if needed in assertions, though unlikely
using ModelContextProtocol.Server; // Required for McpServerToolTypeAttribute and McpServerToolAttribute
using System.Reflection; // Required for IsDefined
using System.Linq; // Required for FirstOrDefault

namespace SensoryBridge.McpAdapter.Tests;

public class ContentToolsTests
{
    private readonly Mock<IContentService> _mockContentService;
    private readonly ContentTools _contentTools;

    public ContentToolsTests()
    {
        _mockContentService = new Mock<IContentService>();
        _contentTools = new ContentTools(_mockContentService.Object);
    }

    [Fact]
    public async Task GetSenseNetContentDetails_ValidId_CallsServiceAndReturnsContent()
    {
        // Arrange
        var contentId = "test-id";
        var expectedDetails = "Details for test-id";
        var cancellationToken = CancellationToken.None;

        _mockContentService.Setup(s => s.GetContentDetailsAsync(contentId, cancellationToken))
            .ReturnsAsync(expectedDetails);

        // Act
        var result = await _contentTools.GetSenseNetContentDetails(contentId, cancellationToken);

        // Assert
        Assert.Equal(expectedDetails, result);
        _mockContentService.Verify(s => s.GetContentDetailsAsync(contentId, cancellationToken), Times.Once);
    }

    [Fact]
    public void GetSenseNetContentDetails_HasCorrectAttributes()
    {
        // Arrange
        var methodInfo = typeof(ContentTools).GetMethod(nameof(ContentTools.GetSenseNetContentDetails));
        var classInfo = typeof(ContentTools);

        // Assert
        Assert.NotNull(methodInfo);
        Assert.True(classInfo.IsDefined(typeof(McpServerToolTypeAttribute), false)); // Ensure this attribute is from the correct namespace
        Assert.True(methodInfo.IsDefined(typeof(McpServerToolAttribute), false)); // Ensure this attribute is from the correct namespace
        
        var descriptionAttribute = methodInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
        Assert.NotNull(descriptionAttribute);
        Assert.Equal("Retrieves details for a specific content item from SenseNet.", descriptionAttribute.Description);

        var parameters = methodInfo.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal("contentId", parameters[0].Name);
        var paramDescAttr = parameters[0].GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
        Assert.NotNull(paramDescAttr);
        Assert.Equal("The ID or path of the content item.", paramDescAttr.Description);
        
        Assert.Equal("cancellationToken", parameters[1].Name);
    }
}
