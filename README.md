# HelloRichardMCP - C# MCP Tool Playground

This repository serves as a starter playground for developing C# tools using the Model Context Protocol (MCP).

## About MCP

Model Context Protocol (MCP) is a framework that enables the development of tools that can be utilized by AI models directly. This allows AI assistants to interact with your codebase, perform analyses, and execute tasks through well-defined interfaces.

## Included Tools

### ProjectTool

The repository includes a sample `ProjectTool` that demonstrates MCP integration by:

- Analyzing C# codebases and frontend technologies (JavaScript, React, Vue)
- Generating Markdown documentation
- Identifying namespaces, classes, and methods
- Supporting search functionality with regex capabilities
- Creating file type statistics and directory structure visualizations

#### Frontend Analysis Capabilities

The tool provides in-depth analysis for popular frontend technologies:

- **JavaScript**: Detects functions, arrow functions, classes, and methods
- **React**: Identifies components, props, and React hooks usage
- **Vue**: Extracts component structure, props, data properties, methods, and computed properties

#### Available Methods

- `AnalyzeProject`: Analyzes a codebase and generates comprehensive documentation

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

## Setting Up with Cursor IDE

To use these tools with Cursor IDE:

1. Build the project first:
   ```bash
   cd HelloRichardMCP/HelloRichardMCP
   dotnet build
   ```

2. Configure Cursor to use the MCP tools by adding this to your `~/.cursor/mcp.json`:
   ```json
   {
     "HelloRichardMCP": {
       "type": "stdio",
       "command": "dotnet",
       "args": [
         "run",
         "--project",
         "PATH_TO_YOUR_PROJECT/HelloRichardMCP/HelloRichardMCP/HelloRichardMCP.csproj"
       ]
     }
   }
   ```
   Replace `PATH_TO_YOUR_PROJECT` with the actual path to your project.

3. Restart Cursor IDE for the changes to take effect.

### Available Tool Commands

Once set up, the following tools will be available:

- `AnalyzeProject`: Analyze code structure and generate documentation
- `QueryCodeStructure`: Ask questions about the codebase (supports queries about file types, classes, methods, components, etc.)
- `GenerateCodeDocumentation`: Generate Markdown documentation for the codebase

Example queries you can try:
- "Show me all classes in the project"
- "What file types are in the codebase?"
- "List all React components"
- "Show me the directory structure"
- "Count all methods and functions"