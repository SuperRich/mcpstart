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
using System.Text.RegularExpressions;

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
        public async Task<string> GenerateCodeDocumentation(
            string directoryPath, 
            string outputFilePath = "", 
            string searchTerm = "", 
            string fileExtensions = "", 
            string excludePattern = "",
            bool useRegex = false,
            bool caseSensitive = false,
            bool highlightMatches = true)
        {
            try
            {
                _logger.LogInformation($"Starting code analysis of directory: {directoryPath}");
                
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    _logger.LogInformation($"Using search filter: {searchTerm}");
                    if (useRegex)
                    {
                        _logger.LogInformation("Using regular expression search");
                    }
                    if (caseSensitive)
                    {
                        _logger.LogInformation("Search is case sensitive");
                    }
                    if (highlightMatches)
                    {
                        _logger.LogInformation("Matches will be highlighted in documentation");
                    }
                }
                
                if (!string.IsNullOrEmpty(fileExtensions))
                {
                    _logger.LogInformation($"Filtering by file extensions: {fileExtensions}");
                }
                
                if (!string.IsNullOrEmpty(excludePattern))
                {
                    _logger.LogInformation($"Excluding patterns: {excludePattern}");
                }

                if (!Directory.Exists(directoryPath))
                {
                    return $"Directory not found: {directoryPath}";
                }

                // Build the documentation
                var documentation = new StringBuilder();
                documentation.AppendLine("# Code Analysis Report");
                documentation.AppendLine($"Generated on: {DateTime.Now}");
                
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    documentation.AppendLine($"Search filter: \"{searchTerm}\"");
                    if (useRegex)
                    {
                        documentation.AppendLine("Using regular expression search");
                    }
                    if (caseSensitive)
                    {
                        documentation.AppendLine("Search is case sensitive");
                    }
                    if (highlightMatches)
                    {
                        documentation.AppendLine("Matches are highlighted in documentation");
                    }
                }
                
                if (!string.IsNullOrEmpty(fileExtensions))
                {
                    documentation.AppendLine($"File extensions: {fileExtensions}");
                }
                
                if (!string.IsNullOrEmpty(excludePattern))
                {
                    documentation.AppendLine($"Excluded patterns: {excludePattern}");
                }
                
                documentation.AppendLine();

                // Project Overview
                documentation.AppendLine("## Project Overview");
                documentation.AppendLine();
                var fileTypes = AnalyzeFileTypes(directoryPath, fileExtensions, excludePattern);
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
                    
                    var csharpAnalysis = await AnalyzeCSharpFilesAsync(
                        directoryPath, 
                        searchTerm, 
                        excludePattern, 
                        useRegex, 
                        caseSensitive,
                        highlightMatches);
                    
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

        private Dictionary<string, int> AnalyzeFileTypes(string directoryPath, string fileExtensions = "", string excludePattern = "")
        {
            var result = new Dictionary<string, int>();
            
            var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
            
            // Parse file extensions
            var extensionList = string.IsNullOrEmpty(fileExtensions) 
                ? new List<string>() 
                : fileExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(ext => ext.Trim().ToLowerInvariant())
                    .ToList();
            
            foreach (var file in files)
            {
                // Skip if file matches exclude pattern
                if (!string.IsNullOrEmpty(excludePattern) && 
                    file.Contains(excludePattern, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                var extension = Path.GetExtension(file).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension))
                {
                    extension = "(no extension)";
                }
                
                // Skip if we're filtering by extension and this extension isn't in the list
                if (extensionList.Count > 0 && !extensionList.Contains(extension))
                {
                    continue;
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

        private async Task<string> AnalyzeCSharpFilesAsync(
            string directoryPath, 
            string searchTerm = "", 
            string excludePattern = "", 
            bool useRegex = false, 
            bool caseSensitive = false,
            bool highlightMatches = true)
        {
            var documentation = new StringBuilder();
            
            // Compile regex if needed
            Regex? searchRegex = null;
            if (!string.IsNullOrEmpty(searchTerm) && useRegex)
            {
                try
                {
                    var regexOptions = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    searchRegex = new Regex(searchTerm, regexOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error compiling regular expression");
                    documentation.AppendLine($"Error compiling regular expression: {ex.Message}");
                    return documentation.ToString();
                }
            }

            // Find all C# files
            var csharpFiles = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Contains("\\obj\\") && !file.Contains("\\bin\\"))
                .Where(file => string.IsNullOrEmpty(excludePattern) || 
                               !file.Contains(excludePattern, StringComparison.OrdinalIgnoreCase))
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
                    
                    // Skip file if searchTerm is specified and not found in the file content
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        bool matchFound;
                        if (useRegex && searchRegex != null)
                        {
                            matchFound = searchRegex.IsMatch(code);
                        }
                        else
                        {
                            matchFound = caseSensitive 
                                ? code.Contains(searchTerm) 
                                : code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
                        }
                        
                        if (!matchFound)
                        {
                            continue;
                        }
                    }
                    
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
                        // Skip class if searchTerm is specified and not found in the class text
                        if (!string.IsNullOrEmpty(searchTerm))
                        {
                            string classText = classDecl.ToString();
                            bool matchFound;
                            
                            if (useRegex && searchRegex != null)
                            {
                                matchFound = searchRegex.IsMatch(classText);
                            }
                            else
                            {
                                matchFound = caseSensitive 
                                    ? classText.Contains(searchTerm) 
                                    : classText.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
                            }
                            
                            if (!matchFound)
                            {
                                continue;
                            }
                        }
                        
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
                            // Skip method if searchTerm is specified and not found in the method text
                            if (!string.IsNullOrEmpty(searchTerm))
                            {
                                string methodText = method.ToString();
                                bool matchFound;
                                
                                if (useRegex && searchRegex != null)
                                {
                                    matchFound = searchRegex.IsMatch(methodText);
                                }
                                else
                                {
                                    matchFound = caseSensitive 
                                        ? methodText.Contains(searchTerm) 
                                        : methodText.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
                                }
                                
                                if (!matchFound)
                                {
                                    continue;
                                }
                            }
                            
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
                string namespaceName = ns.Key;
                
                // Highlight namespace if it matches search term
                if (!string.IsNullOrEmpty(searchTerm) && highlightMatches)
                {
                    if (useRegex && searchRegex != null)
                    {
                        if (searchRegex.IsMatch(namespaceName))
                        {
                            namespaceName = HighlightMatches(namespaceName, searchRegex);
                        }
                    }
                    else if (!caseSensitive && namespaceName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        namespaceName = HighlightOccurrence(namespaceName, searchTerm, caseSensitive);
                    }
                    else if (caseSensitive && namespaceName.Contains(searchTerm))
                    {
                        namespaceName = HighlightOccurrence(namespaceName, searchTerm, caseSensitive);
                    }
                }
                
                documentation.AppendLine($"- **{namespaceName}**");
                
                foreach (var className in ns.Value.Distinct().OrderBy(c => c))
                {
                    string displayClassName = className;
                    
                    // Highlight class name if it matches search term
                    if (!string.IsNullOrEmpty(searchTerm) && highlightMatches)
                    {
                        if (useRegex && searchRegex != null)
                        {
                            if (searchRegex.IsMatch(displayClassName))
                            {
                                displayClassName = HighlightMatches(displayClassName, searchRegex);
                            }
                        }
                        else if (!caseSensitive && displayClassName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                        {
                            displayClassName = HighlightOccurrence(displayClassName, searchTerm, caseSensitive);
                        }
                        else if (caseSensitive && displayClassName.Contains(searchTerm))
                        {
                            displayClassName = HighlightOccurrence(displayClassName, searchTerm, caseSensitive);
                        }
                    }
                    
                    documentation.AppendLine($"  - {displayClassName}");
                }
            }
            documentation.AppendLine();

            // Generate Classes documentation
            documentation.AppendLine("### Classes");
            documentation.AppendLine();
            foreach (var classInfo in classes.OrderBy(c => c.Namespace).ThenBy(c => c.Name))
            {
                string displayClassName = classInfo.Name;
                
                // Highlight class name if it matches search term
                if (!string.IsNullOrEmpty(searchTerm) && highlightMatches)
                {
                    if (useRegex && searchRegex != null)
                    {
                        if (searchRegex.IsMatch(displayClassName))
                        {
                            displayClassName = HighlightMatches(displayClassName, searchRegex);
                        }
                    }
                    else if (!caseSensitive && displayClassName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        displayClassName = HighlightOccurrence(displayClassName, searchTerm, caseSensitive);
                    }
                    else if (caseSensitive && displayClassName.Contains(searchTerm))
                    {
                        displayClassName = HighlightOccurrence(displayClassName, searchTerm, caseSensitive);
                    }
                }
                
                documentation.AppendLine($"#### {displayClassName}");
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
                        string displayMethodName = method.Name;
                        string displayReturnType = method.ReturnType;
                        
                        // Highlight method names and return types if they match search term
                        if (!string.IsNullOrEmpty(searchTerm) && highlightMatches)
                        {
                            if (useRegex && searchRegex != null)
                            {
                                if (searchRegex.IsMatch(displayMethodName))
                                {
                                    displayMethodName = HighlightMatches(displayMethodName, searchRegex);
                                }
                                if (searchRegex.IsMatch(displayReturnType))
                                {
                                    displayReturnType = HighlightMatches(displayReturnType, searchRegex);
                                }
                            }
                            else
                            {
                                if (!caseSensitive && displayMethodName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                {
                                    displayMethodName = HighlightOccurrence(displayMethodName, searchTerm, caseSensitive);
                                }
                                else if (caseSensitive && displayMethodName.Contains(searchTerm))
                                {
                                    displayMethodName = HighlightOccurrence(displayMethodName, searchTerm, caseSensitive);
                                }
                                
                                if (!caseSensitive && displayReturnType.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                {
                                    displayReturnType = HighlightOccurrence(displayReturnType, searchTerm, caseSensitive);
                                }
                                else if (caseSensitive && displayReturnType.Contains(searchTerm))
                                {
                                    displayReturnType = HighlightOccurrence(displayReturnType, searchTerm, caseSensitive);
                                }
                            }
                        }
                        
                        var modifiers = new List<string>();
                        if (method.IsPublic) modifiers.Add("public");
                        if (method.IsStatic) modifiers.Add("static");
                        
                        var parametersString = string.Join(", ", method.Parameters);
                        
                        // Highlight parameters if they match search term
                        if (!string.IsNullOrEmpty(searchTerm) && highlightMatches)
                        {
                            if (useRegex && searchRegex != null)
                            {
                                if (searchRegex.IsMatch(parametersString))
                                {
                                    parametersString = HighlightMatches(parametersString, searchRegex);
                                }
                            }
                            else if (!caseSensitive && parametersString.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                            {
                                parametersString = HighlightOccurrence(parametersString, searchTerm, caseSensitive);
                            }
                            else if (caseSensitive && parametersString.Contains(searchTerm))
                            {
                                parametersString = HighlightOccurrence(parametersString, searchTerm, caseSensitive);
                            }
                        }
                        
                        documentation.AppendLine($"| {displayMethodName} | {displayReturnType} | {parametersString} | {string.Join(", ", modifiers)} |");
                    }
                    
                    documentation.AppendLine();
                }
            }

            return documentation.ToString();
        }

        // Helper method to highlight regex matches in markdown
        private string HighlightMatches(string text, Regex regex)
        {
            return regex.Replace(text, match => $"**`{match.Value}`**");
        }
        
        // Helper method to highlight occurrences of a string in markdown
        private string HighlightOccurrence(string text, string searchTerm, bool caseSensitive)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchTerm))
            {
                return text;
            }
            
            StringComparison comparison = caseSensitive 
                ? StringComparison.Ordinal 
                : StringComparison.OrdinalIgnoreCase;
                
            var result = new StringBuilder();
            int currentIndex = 0;
            int searchIndex;
            
            while ((searchIndex = text.IndexOf(searchTerm, currentIndex, comparison)) >= 0)
            {
                // Add the text before the match
                result.Append(text.Substring(currentIndex, searchIndex - currentIndex));
                
                // Add the highlighted match
                string match = text.Substring(searchIndex, searchTerm.Length);
                result.Append($"**`{match}`**");
                
                currentIndex = searchIndex + searchTerm.Length;
            }
            
            // Add any remaining text
            if (currentIndex < text.Length)
            {
                result.Append(text.Substring(currentIndex));
            }
            
            return result.ToString();
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