using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloRichardMCP
{
    [McpServerToolType]
    public class CodeAnalysisTool
    {
        private readonly ILogger<CodeAnalysisTool> _logger;

        public CodeAnalysisTool(ILogger<CodeAnalysisTool> logger)
        {
            _logger = logger;
        }

        [McpServerTool, Description("Analyzes a codebase and generates a Markdown documentation guide.")]
        public async Task<string> GenerateCodeDocumentation(string directoryPath, string outputFilePath = "")
        {
            try
            {
                _logger.LogInformation($"Starting code analysis of directory: {directoryPath}");

                if (!Directory.Exists(directoryPath))
                {
                    return $"Directory not found: {directoryPath}";
                }

                // Build the documentation
                var documentation = new StringBuilder();
                documentation.AppendLine("# Code Analysis Report");
                documentation.AppendLine($"Generated on: {DateTime.Now}");
                documentation.AppendLine();

                // Project Overview
                documentation.AppendLine("## Project Overview");
                documentation.AppendLine();
                var fileTypes = AnalyzeFileTypes(directoryPath);
                documentation.AppendLine("### File Types");
                documentation.AppendLine();
                foreach (var fileType in fileTypes.OrderByDescending(f => f.Value))
                {
                    documentation.AppendLine($"- {fileType.Key}: {fileType.Value} files");
                }
                documentation.AppendLine();

                // Project Structure
                documentation.AppendLine("## Project Structure");
                documentation.AppendLine();
                documentation.AppendLine("```");
                documentation.AppendLine(GenerateDirectoryTree(directoryPath, 0));
                documentation.AppendLine("```");
                documentation.AppendLine();

                // C# Code Analysis
                if (fileTypes.ContainsKey(".cs") && fileTypes[".cs"] > 0)
                {
                    documentation.AppendLine("## C# Code Analysis");
                    documentation.AppendLine();
                    
                    var csharpAnalysis = await AnalyzeCSharpFilesAsync(directoryPath);
                    documentation.Append(csharpAnalysis);
                }

                // Save to file if specified, or use default name
                if (string.IsNullOrEmpty(outputFilePath))
                {
                    // Use default filename in current directory
                    outputFilePath = "code_documentation.md";
                }

                // Ensure path is absolute
                if (!Path.IsPathRooted(outputFilePath))
                {
                    outputFilePath = Path.GetFullPath(outputFilePath);
                }

                File.WriteAllText(outputFilePath, documentation.ToString());
                return $"Documentation generated and saved to {outputFilePath}\n\n{documentation.ToString()}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating code documentation");
                return $"Error: {ex.Message}";
            }
        }

        private Dictionary<string, int> AnalyzeFileTypes(string directoryPath)
        {
            var result = new Dictionary<string, int>();
            
            var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension))
                {
                    extension = "(no extension)";
                }

                if (result.ContainsKey(extension))
                {
                    result[extension]++;
                }
                else
                {
                    result[extension] = 1;
                }
            }

            return result;
        }

        private string GenerateDirectoryTree(string directoryPath, int indentLevel)
        {
            var builder = new StringBuilder();
            var directoryInfo = new DirectoryInfo(directoryPath);
            
            // Skip bin, obj, and hidden directories
            if (directoryInfo.Name.StartsWith(".") || 
                directoryInfo.Name == "bin" || 
                directoryInfo.Name == "obj")
            {
                return builder.ToString();
            }

            var indent = new string(' ', indentLevel * 2);
            
            // Add directory name
            if (indentLevel > 0)
            {
                builder.AppendLine($"{indent}|-- {directoryInfo.Name}/");
            }
            else
            {
                builder.AppendLine($"{directoryInfo.Name}/");
            }

            // Add files
            foreach (var file in directoryInfo.GetFiles())
            {
                // Skip hidden files
                if (file.Name.StartsWith("."))
                {
                    continue;
                }
                builder.AppendLine($"{indent}    |-- {file.Name}");
            }

            // Recursively process subdirectories
            foreach (var subDir in directoryInfo.GetDirectories()
                .Where(d => !d.Name.StartsWith(".") && d.Name != "bin" && d.Name != "obj"))
            {
                builder.Append(GenerateDirectoryTree(subDir.FullName, indentLevel + 1));
            }

            return builder.ToString();
        }

        private async Task<string> AnalyzeCSharpFilesAsync(string directoryPath)
        {
            var documentation = new StringBuilder();
            
            // Find all C# files
            var csharpFiles = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Contains("\\obj\\") && !file.Contains("\\bin\\"))
                .ToList();

            if (csharpFiles.Count == 0)
            {
                return "No C# files found for analysis.";
            }

            // Collect namespaces, classes, and methods
            var namespaces = new Dictionary<string, List<string>>();
            var classes = new List<ClassInfo>();
            
            foreach (var file in csharpFiles)
            {
                try
                {
                    var code = await File.ReadAllTextAsync(file);
                    var syntaxTree = CSharpSyntaxTree.ParseText(code);
                    var root = await syntaxTree.GetRootAsync();

                    // Extract namespaces
                    var namespaceDeclarations = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>();
                    foreach (var ns in namespaceDeclarations)
                    {
                        var namespaceName = ns.Name.ToString();
                        if (!namespaces.ContainsKey(namespaceName))
                        {
                            namespaces[namespaceName] = new List<string>();
                        }
                    }

                    // Extract classes
                    var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
                    foreach (var classDecl in classDeclarations)
                    {
                        var parentNamespace = classDecl.Parent as NamespaceDeclarationSyntax;
                        var namespaceName = parentNamespace?.Name.ToString() ?? "Global";
                        
                        var classInfo = new ClassInfo
                        {
                            Name = classDecl.Identifier.Text,
                            Namespace = namespaceName,
                            FilePath = file,
                            Methods = new List<MethodInfo>()
                        };

                        // Get methods
                        var methodDeclarations = classDecl.DescendantNodes().OfType<MethodDeclarationSyntax>();
                        foreach (var method in methodDeclarations)
                        {
                            var methodInfo = new MethodInfo
                            {
                                Name = method.Identifier.Text,
                                ReturnType = method.ReturnType.ToString(),
                                IsPublic = method.Modifiers.Any(m => m.Text == "public"),
                                IsStatic = method.Modifiers.Any(m => m.Text == "static"),
                                Parameters = method.ParameterList.Parameters
                                    .Select(p => new { Type = p.Type?.ToString(), Name = p.Identifier.Text })
                                    .Select(p => $"{p.Type} {p.Name}")
                                    .ToList()
                            };
                            
                            classInfo.Methods.Add(methodInfo);
                        }

                        classes.Add(classInfo);

                        // Add class to namespace list
                        if (namespaces.ContainsKey(namespaceName))
                        {
                            namespaces[namespaceName].Add(classInfo.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error analyzing file: {file}");
                }
            }

            // Generate Namespaces documentation
            documentation.AppendLine("### Namespaces");
            documentation.AppendLine();
            foreach (var ns in namespaces.OrderBy(n => n.Key))
            {
                documentation.AppendLine($"- **{ns.Key}**");
                foreach (var className in ns.Value.Distinct().OrderBy(c => c))
                {
                    documentation.AppendLine($"  - {className}");
                }
            }
            documentation.AppendLine();

            // Generate Classes documentation
            documentation.AppendLine("### Classes");
            documentation.AppendLine();
            foreach (var classInfo in classes.OrderBy(c => c.Namespace).ThenBy(c => c.Name))
            {
                documentation.AppendLine($"#### {classInfo.Name}");
                documentation.AppendLine();
                documentation.AppendLine($"**Namespace:** {classInfo.Namespace}");
                documentation.AppendLine($"**File:** {Path.GetFileName(classInfo.FilePath)}");
                documentation.AppendLine();
                
                if (classInfo.Methods.Count > 0)
                {
                    documentation.AppendLine("**Methods:**");
                    documentation.AppendLine();
                    documentation.AppendLine("| Name | Return Type | Parameters | Modifiers |");
                    documentation.AppendLine("|------|-------------|------------|-----------|");
                    
                    foreach (var method in classInfo.Methods.OrderBy(m => m.Name))
                    {
                        var modifiers = new List<string>();
                        if (method.IsPublic) modifiers.Add("public");
                        if (method.IsStatic) modifiers.Add("static");
                        
                        var parametersString = string.Join(", ", method.Parameters);
                        documentation.AppendLine($"| {method.Name} | {method.ReturnType} | {parametersString} | {string.Join(", ", modifiers)} |");
                    }
                    
                    documentation.AppendLine();
                }
            }

            return documentation.ToString();
        }

        private class ClassInfo
        {
            public string Name { get; set; } = "";
            public string Namespace { get; set; } = "";
            public string FilePath { get; set; } = "";
            public List<MethodInfo> Methods { get; set; } = new List<MethodInfo>();
        }

        private class MethodInfo
        {
            public string Name { get; set; } = "";
            public string ReturnType { get; set; } = "";
            public bool IsPublic { get; set; }
            public bool IsStatic { get; set; }
            public List<string> Parameters { get; set; } = new List<string>();
        }
    }
} 