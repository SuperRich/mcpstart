# HelloRichardMCP - C# MCP Tool Playground

This repository serves as a starter playground for developing C# tools using the Model Context Protocol (MCP).

## About MCP

Model Context Protocol (MCP) is a framework that enables the development of tools that can be utilized by AI models directly. This allows AI assistants to interact with your codebase, perform analyses, and execute tasks through well-defined interfaces.

## Included Tools

### CodeAnalysisTool

The repository includes a sample `CodeAnalysisTool` that demonstrates MCP integration by:

- Analyzing C# codebases
- Generating Markdown documentation
- Identifying namespaces, classes, and methods
- Supporting search functionality with regex capabilities
- Creating file type statistics and directory structure visualizations

## Getting Started

1. Clone this repository
2. Open the solution in Visual Studio or your preferred C# IDE
3. Build the solution to restore dependencies
4. Run the project to start the MCP server

## Extending the Playground

This playground is designed to be extended with your own MCP tools:

1. Create new tool classes and decorate them with `[McpServerToolType]`
2. Add methods with the `[McpServerTool]` attribute
3. Provide descriptive names and parameters for your tools
4. Implement your tool's functionality

## Requirements

- .NET SDK (6.0 or later recommended)
- Microsoft.CodeAnalysis packages for code analysis capabilities
- ModelContextProtocol.Server package

## License

MIT