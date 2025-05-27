# SensoryBridge: MCP Server for SenseNet

SensoryBridge is a .NET application that acts as a Model Context Protocol (MCP) server for a SenseNet Content Management System instance. It allows AI agents and other MCP-compatible clients to interact with SenseNet content and functionalities through a standardized protocol.

## Projects

The solution consists of the following projects:

*   **`SensoryBridge.Api`**:
    *   An ASP.NET Core Web API project.
    *   Hosts the MCP server logic and exposes HTTP endpoints for MCP interaction.
    *   Handles incoming requests from MCP clients, translates them into SenseNet operations, and returns results.
*   **`SensoryBridge.McpAdapter`**:
    *   A .NET class library.
    *   Defines the MCP "Tools" that map to SenseNet functionalities (e.g., content retrieval, user management).
    *   Contains interfaces for SenseNet services that these tools will use.
*   **`SensoryBridge.SenseNetClient`**:
    *   A .NET class library.
    *   Provides the concrete implementation for the SenseNet service interfaces defined in `SensoryBridge.McpAdapter`.
    *   Handles the actual communication with the SenseNet instance using its client SDK.
    *   Currently, it includes a mock implementation for `IContentService` for demonstration and testing purposes.

## Setup & Configuration

### Prerequisites

*   .NET 8.0 SDK (or as specified in global.json/project files)
*   A running SenseNet instance (version compatible with `SenseNet.Client` SDK).

### Configuration

The SensoryBridge API needs to be configured to connect to your SenseNet instance. This is done through the `appsettings.json` (and `appsettings.Development.json`) file in the `SensoryBridge.Api` project.

1.  **Open `SensoryBridge.Api/appsettings.json` (or `appsettings.Development.json` for development overrides).**
2.  **Locate or add the `SenseNet` section:**

    ```json
    {
      // ... other settings ...
      "SenseNet": {
        "ServiceUrl": "YOUR_SENSENET_INSTANCE_URL_HERE", // e.g., "https://my.sensenet.site/api/"
        "ApiKey": "YOUR_SENSENET_API_KEY_HERE_IF_NEEDED"  // Or other auth details
      }
    }
    ```

3.  **Update `ServiceUrl`**: Set this to the base URL of your SenseNet service API.
4.  **Update `ApiKey`**: If your SenseNet instance requires an API key or other specific credentials for authentication, configure them here. The `SenseNetConfig.cs` model and `SenseNetContentService` might need adjustments based on the exact authentication mechanism used by your SenseNet instance and its SDK.

### Running the Application

1.  **Navigate to the API project directory:**
    ```bash
    cd SensoryBridge.Api
    ```
2.  **Run the application:**
    ```bash
    dotnet run
    ```
3.  The API will typically start on a local port (e.g., `http://localhost:5000` or `https://localhost:5001`). Check the console output for the exact URL.

## API Endpoints

The SensoryBridge API exposes the following MCP-related endpoints:

*   **`GET /mcp/tools`**
    *   Lists all available MCP tools that can be invoked. Each tool entry includes its name, description, and input schema.
    *   **Response Body Example:**
        ```json
        [
          {
            "name": "GetSenseNetContentDetails",
            "description": "Retrieves details for a specific content item from SenseNet.",
            "inputSchema": {
              "type": "object",
              "properties": {
                "contentId": {
                  "type": "string",
                  "description": "The ID or path of the content item."
                }
              },
              "required": ["contentId"]
            }
            // ... other tools ...
          }
        ]
        ```

*   **`POST /mcp/tools/{toolName}/call`**
    *   Invokes a specific MCP tool by its name.
    *   **Path Parameter:** `{toolName}` - The name of the tool to call (e.g., `GetSenseNetContentDetails`).
    *   **Request Body:** A JSON object containing the arguments required by the tool, matching its input schema.
        *   **Example for `GetSenseNetContentDetails`:**
            ```json
            {
              "contentId": "your-content-id-or-path"
            }
            ```
    *   **Response Body:** A JSON array of "Content" objects, representing the output from the tool.
        *   **Example for `GetSenseNetContentDetails` (current mock):**
            ```json
            [
              {
                "type": "text",
                "text": "Mock details from YOUR_CONFIGURED_SENSENET_URL for content: your-content-id-or-path"
              }
            ]
            ```

## How to Add New Tools

1.  **Define the Service Method:**
    *   If interacting with SenseNet, add a method to an appropriate service interface in `SensoryBridge.McpAdapter/Services/SenseNet/` (e.g., `IContentService.cs`).
    *   Implement this method in the corresponding service implementation in `SensoryBridge.SenseNetClient/Services/` (e.g., `SenseNetContentService.cs`). This implementation will use the SenseNet client SDK.
2.  **Create the MCP Tool Method:**
    *   In a class within `SensoryBridge.McpAdapter/McpTools/` (e.g., `ContentTools.cs`), create a public method that will serve as the MCP tool.
    *   This method should typically call the service method created in step 1.
    *   Decorate the method with `[McpServerTool]` and provide a `[Description]`. Tool parameters should also have `[Description]` attributes.
    *   Ensure the class containing the tool method is decorated with `[McpServerToolType]`.
3.  **Register the Tool Type:**
    *   In `SensoryBridge.Api/Program.cs`, ensure the class containing your new tool is registered with the MCP server. If you used an existing `[McpServerToolType]` class (like `ContentTools`), it should be picked up automatically if that class is already registered (e.g., via `WithTools<ContentTools>()`). If it's a new class, add it:
        ```csharp
        builder.Services.AddMcpServer()
            .WithTools<ContentTools>()  // Existing
            .WithTools<YourNewToolClass>(); // Add this for your new tool class
        ```
4.  **Testing:**
    *   Add unit tests for your new service method and MCP tool method.
    *   Run the API and test the new tool via the `/mcp/tools` and `/mcp/tools/{newToolName}/call` endpoints.
```
