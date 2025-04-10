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
    // Result classes for structured data
    public class CodeAnalysisResult
    {
        public Dictionary<string, int> FileTypes { get; set; } = new Dictionary<string, int>();
        public DirectoryStructure DirectoryTree { get; set; } = new DirectoryStructure();
        public List<string> FailedFiles { get; set; } = new List<string>();
        
        // Language-specific analysis results
        public CSharpAnalysis CSharpResults { get; set; } = new CSharpAnalysis();
        public JsAnalysis JavaScriptResults { get; set; } = new JsAnalysis();
        public ReactAnalysis ReactResults { get; set; } = new ReactAnalysis();
        public VueAnalysis VueResults { get; set; } = new VueAnalysis();
    }

    public class DirectoryStructure
    {
        public string Name { get; set; } = "";
        public List<string> Files { get; set; } = new List<string>();
        public List<DirectoryStructure> Subdirectories { get; set; } = new List<DirectoryStructure>();
    }

    public class ClassInfo
    {
        public string Name { get; set; } = "";
        public string Namespace { get; set; } = "";
        public string FilePath { get; set; } = "";
        public List<MethodInfo> Methods { get; set; } = new List<MethodInfo>();
    }

    public class MethodInfo
    {
        public string Name { get; set; } = "";
        public string ReturnType { get; set; } = "";
        public bool IsPublic { get; set; }
        public bool IsStatic { get; set; }
        public List<string> Parameters { get; set; } = new List<string>();
    }

    public class JsFunctionInfo
    {
        public string Name { get; set; } = "";
        public List<string> Parameters { get; set; } = new List<string>();
        public string FilePath { get; set; } = "";
    }

    public class JsClassInfo
    {
        public string Name { get; set; } = "";
        public List<string> Methods { get; set; } = new List<string>();
        public string FilePath { get; set; } = "";
    }

    public class ReactComponentInfo
    {
        public string Name { get; set; } = "";
        public List<string> Props { get; set; } = new List<string>();
        public List<string> Hooks { get; set; } = new List<string>();
        public string FilePath { get; set; } = "";
    }

    public class VueComponentInfo
    {
        public string Name { get; set; } = "";
        public List<string> Props { get; set; } = new List<string>();
        public List<string> Data { get; set; } = new List<string>();
        public List<string> Methods { get; set; } = new List<string>();
        public List<string> ComputedProperties { get; set; } = new List<string>();
        public string FilePath { get; set; } = "";
        public bool IsScriptSetup { get; set; } = false;

        public VueComponentInfo()
        {
            Props = new List<string>();
            Data = new List<string>();
            Methods = new List<string>();
            ComputedProperties = new List<string>();
        }
    }

    public class CSharpAnalysis
    {
        public Dictionary<string, List<string>> Namespaces { get; set; } = new Dictionary<string, List<string>>();
        public List<ClassInfo> Classes { get; set; } = new List<ClassInfo>();
    }

    public class JsAnalysis
    {
        public List<JsFunctionInfo> Functions { get; set; } = new List<JsFunctionInfo>();
        public List<JsClassInfo> Classes { get; set; } = new List<JsClassInfo>();
    }

    public class ReactAnalysis
    {
        public List<ReactComponentInfo> Components { get; set; } = new List<ReactComponentInfo>();
    }

    public class VueAnalysis
    {
        public List<VueComponentInfo> Components { get; set; } = new List<VueComponentInfo>();
    }

    public class MatchInfo
    {
        public string Text { get; set; } = "";
        public List<int> MatchStartIndices { get; set; } = new List<int>();
        public List<int> MatchLengths { get; set; } = new List<int>();
    }

    [McpServerToolType]
    public class ProjectTool
    {
        private readonly ILogger<ProjectTool> _logger;
        private readonly List<string> _failedFiles = new List<string>(); // Store files that failed analysis

        public ProjectTool(ILogger<ProjectTool> logger)
        {
            _logger = logger;
        }

        [McpServerTool, Description("Analyzes a codebase and returns a structured representation.")]
        public async Task<CodeAnalysisResult> AnalyzeProject(
            string directoryPath,
            List<string> failedFiles,
            string searchTerm = "",
            string fileExtensions = "",
            string excludePattern = "",
            bool useRegex = false,
            bool caseSensitive = false,
            bool highlightMatches = true)
        {
            _failedFiles.Clear(); // Clear failed files list for each run
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
                        _logger.LogInformation("Matches will be highlighted in results");
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
                    throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
                }

                // Create the result object
                var result = new CodeAnalysisResult();
                
                // Analyze file types
                result.FileTypes = AnalyzeFileTypes(directoryPath, fileExtensions, excludePattern);

                // Project Structure
                result.DirectoryTree = BuildDirectoryTree(directoryPath);

                // C# Code Analysis
                if (result.FileTypes.ContainsKey(".cs") && result.FileTypes[".cs"] > 0)
                {
                    result.CSharpResults = await AnalyzeCSharpFilesAsync(
                        directoryPath, 
                        failedFiles,
                        searchTerm, 
                        excludePattern, 
                        useRegex, 
                        caseSensitive,
                        highlightMatches);
                }

                // JavaScript Code Analysis
                if (result.FileTypes.ContainsKey(".js") && result.FileTypes[".js"] > 0)
                {
                    result.JavaScriptResults = await AnalyzeJavaScriptFilesAsync(
                        directoryPath, 
                        failedFiles,
                        searchTerm, 
                        excludePattern, 
                        useRegex, 
                        caseSensitive,
                        highlightMatches);
                }

                // React Code Analysis (.jsx, .tsx)
                if ((result.FileTypes.ContainsKey(".jsx") && result.FileTypes[".jsx"] > 0) || 
                    (result.FileTypes.ContainsKey(".tsx") && result.FileTypes[".tsx"] > 0))
                {
                    result.ReactResults = await AnalyzeReactFilesAsync(
                        directoryPath, 
                        failedFiles,
                        searchTerm, 
                        excludePattern, 
                        useRegex, 
                        caseSensitive,
                        highlightMatches);
                }

                // Vue Code Analysis (.vue)
                if (result.FileTypes.ContainsKey(".vue") && result.FileTypes[".vue"] > 0)
                {
                    result.VueResults = await AnalyzeVueFilesAsync(
                        directoryPath, 
                        failedFiles,
                        searchTerm, 
                        excludePattern, 
                        useRegex, 
                        caseSensitive,
                        highlightMatches);
                }

                // Add Failed Files
                result.FailedFiles = _failedFiles.Distinct().OrderBy(f => f).ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing code");
                throw;
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
                if (IsExcluded(file, excludePattern))
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

        private DirectoryStructure BuildDirectoryTree(string directoryPath)
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            var result = new DirectoryStructure
            {
                Name = directoryInfo.Name
            };
            
            // Skip bin, obj, and hidden directories
            if (directoryInfo.Name.StartsWith(".") || 
                directoryInfo.Name == "bin" || 
                directoryInfo.Name == "obj")
            {
                return result;
            }

            // Add files
            foreach (var file in directoryInfo.GetFiles())
            {
                // Skip hidden files and excluded files
                if (file.Name.StartsWith("."))
                {
                    continue;
                }
                result.Files.Add(file.Name);
            }

            // Recursively process subdirectories
            foreach (var subDir in directoryInfo.GetDirectories()
                .Where(d => !d.Name.StartsWith(".") && d.Name != "bin" && d.Name != "obj"))
            {
                result.Subdirectories.Add(BuildDirectoryTree(subDir.FullName));
            }

            return result;
        }

        private async Task<CSharpAnalysis> AnalyzeCSharpFilesAsync(
            string directoryPath,
            List<string> failedFiles,
            string searchTerm = "",
            string excludePattern = "",
            bool useRegex = false,
            bool caseSensitive = false,
            bool highlightMatches = true)
        {
            var result = new CSharpAnalysis();
            
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
                    return result;
                }
            }

            // Find all C# files
            var csharpFiles = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Contains("\\obj\\") && !file.Contains("\\bin\\"))
                .Where(file => !IsExcluded(file, excludePattern))
                .ToList();

            if (csharpFiles.Count == 0)
            {
                return result;
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
                    failedFiles.Add(file); // Add to failed list
                }
            }

            result.Namespaces = namespaces;
            result.Classes = classes;
            return result;
        }

        private async Task<JsAnalysis> AnalyzeJavaScriptFilesAsync(
            string directoryPath,
            List<string> failedFiles,
            string searchTerm = "",
            string excludePattern = "",
            bool useRegex = false,
            bool caseSensitive = false,
            bool highlightMatches = true)
        {
            var allFunctions = new List<JsFunctionInfo>();
            var allClasses = new List<JsClassInfo>();
            Regex? searchRegex = null;
            StringComparison comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            // Compile search regex once if needed
            if (!string.IsNullOrEmpty(searchTerm) && useRegex)
            {
                try
                {
                    var regexOptions = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    searchRegex = new Regex(searchTerm, regexOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error compiling search regular expression for JS");
                    return new JsAnalysis(); 
                }
            }

            var jsFiles = Directory.GetFiles(directoryPath, "*.js", SearchOption.AllDirectories)
                .Where(file => !IsExcluded(file, excludePattern))
                .ToList();

            _logger.LogInformation($"Processing {jsFiles.Count} JavaScript files...");

            foreach (var file in jsFiles)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    // Extract all functions and classes regardless of search term first
                    ExtractJavaScriptFunctions(file, content, allFunctions);
                    ExtractJavaScriptClasses(file, content, allClasses);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error analyzing JavaScript file: {file}");
                    failedFiles.Add(file); // Add to failed list
                }
            }

            // --- Filtering Stage ---
            var filteredFunctions = new List<JsFunctionInfo>();
            var filteredClasses = new List<JsClassInfo>();
            
            if (!string.IsNullOrEmpty(searchTerm))
            {
                // Apply search filters if search term was provided
                foreach (var func in allFunctions)
                {
                    bool isMatch = false;
                    
                    // Match on function name
                    if (useRegex && searchRegex != null)
                    {
                        isMatch = searchRegex.IsMatch(func.Name);
                    }
                    else
                    {
                        isMatch = func.Name.Contains(searchTerm, comparison);
                    }
                    
                    if (isMatch)
                    {
                        filteredFunctions.Add(func);
                    }
                }
                
                foreach (var cls in allClasses)
                {
                    bool isMatch = false;
                    
                    // Match on class name
                    if (useRegex && searchRegex != null)
                    {
                        isMatch = searchRegex.IsMatch(cls.Name);
                    }
                    else
                    {
                        isMatch = cls.Name.Contains(searchTerm, comparison);
                    }
                    
                    // Also match on method names
                    if (!isMatch)
                    {
                        foreach (var method in cls.Methods)
                        {
                            if (useRegex && searchRegex != null)
                            {
                                if (searchRegex.IsMatch(method))
                                {
                                    isMatch = true;
                                    break;
                                }
                            }
                            else if (method.Contains(searchTerm, comparison))
                            {
                                isMatch = true;
                                break;
                            }
                        }
                    }
                    
                    if (isMatch)
                    {
                        filteredClasses.Add(cls);
                    }
                }
                
                // Check if we filtered everything out
                if (!filteredFunctions.Any() && !filteredClasses.Any())
                {
                    _logger.LogInformation("No JavaScript functions or classes matched the search criteria.");
                    return new JsAnalysis(); 
                }
            }
            else if (!allFunctions.Any() && !allClasses.Any())
            {
                _logger.LogInformation("No JavaScript functions or classes were found in the analyzed files.");
                return new JsAnalysis();
            }
            else
            {
                // Without a search term, we simply include everything
                filteredFunctions = allFunctions;
                filteredClasses = allClasses;
            }

            return new JsAnalysis { 
                Functions = filteredFunctions, 
                Classes = filteredClasses 
            };
        }
        
        private void ExtractJavaScriptFunctions(string filePath, string content, List<JsFunctionInfo> functions)
        {
            // More robust regex attempts (still not perfect)
            // Function declarations: function name(...params) { ... } OR function(...params) { ... } (anonymous)
             var functionRegex = new Regex(@"function\s+([a-zA-Z_$][a-zA-Z0-9_$]*\s*)?\(([^)]*)\)", RegexOptions.Multiline);
            // Arrow functions: (const|let|var) name = (...params) => { ... } OR (...params) => { ... }
             var arrowFunctionRegex = new Regex(@"(?:const|let|var)\s+([a-zA-Z_$][a-zA-Z0-9_$]*)\s*=\s*\(?([^)]*)\)?\s*=>", RegexOptions.Multiline);
            // Object methods: methodName(...) { ... } OR methodName: function(...) { ... }
             var methodRegex = new Regex(@"([a-zA-Z_$][a-zA-Z0-9_$]*)\s*\(([^)]*)\)\s*{", RegexOptions.Multiline); // Simplified, might catch non-methods
             var methodClassicRegex = new Regex(@"([a-zA-Z_$][a-zA-Z0-9_$]*)\s*:\s*function\s*\(([^)]*)\)", RegexOptions.Multiline);


            foreach (Match match in functionRegex.Matches(content))
            {
                var functionName = match.Groups[1].Value;
                var parameters = match.Groups[2].Value.Split(',')
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList();
                
                functions.Add(new JsFunctionInfo
                {
                    Name = functionName,
                    Parameters = parameters,
                    FilePath = filePath
                });
            }
            
            foreach (Match match in arrowFunctionRegex.Matches(content))
            {
                var functionName = match.Groups[2].Value;
                var parameters = match.Groups[3].Value.Split(',')
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList();
                
                functions.Add(new JsFunctionInfo
                {
                    Name = functionName,
                    Parameters = parameters,
                    FilePath = filePath
                });
            }
        }
        
        private void ExtractJavaScriptClasses(string filePath, string content, List<JsClassInfo> classes)
        {
            // Class declarations: class Name { ... } (handles potential export default)
            var classRegex = new Regex(@"(?:export\s+default\s+)?class\s+([a-zA-Z_$][a-zA-Z0-9_$]*)", RegexOptions.Multiline);

            foreach (Match match in classRegex.Matches(content))
            {
                var className = match.Groups[1].Value;
                var classMethods = new List<string>();
                
                // Find methods in the class - Improved slightly
                var methodRegex = new Regex(@"(?:static\s+|get\s+|set\s+|async\s+)*([a-zA-Z_$][a-zA-Z0-9_$]*)\s*\(([^)]*)\)\s*{", RegexOptions.Multiline);
                 var constructorRegex = new Regex(@"constructor\s*\(([^)]*)\)\s*{", RegexOptions.Multiline);

                // Get the class content
                var startIndex = match.Index + match.Length;
                var braceLevel = 0;
                var endIndex = startIndex;
                var foundOpenBrace = false;
                
                for (int i = startIndex; i < content.Length; i++)
                {
                    if (content[i] == '{')
                    {
                        braceLevel++;
                        foundOpenBrace = true;
                    }
                    else if (content[i] == '}')
                    {
                        braceLevel--;
                        if (foundOpenBrace && braceLevel == 0)
                        {
                            endIndex = i + 1;
                            break;
                        }
                    }
                }
                
                if (endIndex > startIndex)
                {
                    var classContent = content.Substring(startIndex, endIndex - startIndex);
                    
                    foreach (Match methodMatch in methodRegex.Matches(classContent))
                    {
                        var methodName = methodMatch.Groups[1].Value;
                        if(!string.IsNullOrWhiteSpace(methodName) && methodName != "constructor") // Avoid adding 'constructor' itself multiple times
                        {
                            classMethods.Add(methodName.Trim());
                        }
                    }
                     // Check for constructor separately
                     if (constructorRegex.IsMatch(classContent)) {
                         classMethods.Add("constructor");
                     }
                }
                
                classes.Add(new JsClassInfo
                {
                    Name = className,
                    Methods = classMethods,
                    FilePath = filePath
                });
            }
        }

        private async Task<ReactAnalysis> AnalyzeReactFilesAsync(
            string directoryPath,
            List<string> failedFiles,
            string searchTerm = "",
            string excludePattern = "",
            bool useRegex = false,
            bool caseSensitive = false,
            bool highlightMatches = true)
        {
            var allComponents = new List<ReactComponentInfo>();
            Regex? searchRegex = null;
            StringComparison comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            // Compile search regex once if needed
            if (!string.IsNullOrEmpty(searchTerm) && useRegex)
            {
                try
                {
                    var regexOptions = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    searchRegex = new Regex(searchTerm, regexOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error compiling search regular expression for React");
                    return new ReactAnalysis();
                }
            }

            // Get all React files (both JSX and TSX)
            var reactFiles = Directory.GetFiles(directoryPath, "*.jsx", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(directoryPath, "*.tsx", SearchOption.AllDirectories))
                .Where(file => !IsExcluded(file, excludePattern))
                .ToList();

            _logger.LogInformation($"Processing {reactFiles.Count} React files...");

            foreach (var file in reactFiles)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    ExtractReactComponents(file, content, allComponents);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error analyzing React file: {file}");
                    failedFiles.Add(file);
                }
            }

            // Filtering Stage
            var filteredComponents = new List<ReactComponentInfo>();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                // Apply search filters if a search term was provided
                foreach (var component in allComponents)
                {
                    bool isMatch = false;
                    
                    // Match component name
                    if (useRegex && searchRegex != null)
                    {
                        isMatch = searchRegex.IsMatch(component.Name);
                    }
                    else
                    {
                        isMatch = component.Name.Contains(searchTerm, comparison);
                    }
                    
                    // Match props
                    if (!isMatch)
                    {
                        foreach (var prop in component.Props)
                        {
                            if (useRegex && searchRegex != null)
                            {
                                if (searchRegex.IsMatch(prop))
                                {
                                    isMatch = true;
                                    break;
                                }
                            }
                            else if (prop.Contains(searchTerm, comparison))
                            {
                                isMatch = true;
                                break;
                            }
                        }
                    }
                    
                    // Match hooks
                    if (!isMatch)
                    {
                        foreach (var hook in component.Hooks)
                        {
                            if (useRegex && searchRegex != null)
                            {
                                if (searchRegex.IsMatch(hook))
                                {
                                    isMatch = true;
                                    break;
                                }
                            }
                            else if (hook.Contains(searchTerm, comparison))
                            {
                                isMatch = true;
                                break;
                            }
                        }
                    }
                    
                    if (isMatch)
                    {
                        filteredComponents.Add(component);
                    }
                }
                
                // Check if we filtered everything out
                if (!filteredComponents.Any())
                {
                    _logger.LogInformation("No React components matched the search criteria.");
                    return new ReactAnalysis { Components = new List<ReactComponentInfo>() };
                }
            }
            else if (!allComponents.Any())
            {
                _logger.LogInformation("No React components were found in the analyzed files.");
                return new ReactAnalysis { Components = new List<ReactComponentInfo>() };
            }
            else
            {
                // Without a search term, we include everything
                filteredComponents = allComponents;
            }

            return new ReactAnalysis { Components = filteredComponents };
        }
        
        private void ExtractReactComponents(string filePath, string content, List<ReactComponentInfo> components)
        {
            // Functional components: function ComponentName(...) { ... } (Improved slightly for generics/types)
            var functionComponentRegex = new Regex(@"function\s+([A-Z][a-zA-Z0-9_$]*)(?:<[^>]*>)?\s*\(([^)]*)\)", RegexOptions.Multiline);
            // Arrow function components: const ComponentName = (...) => { ... } OR const ComponentName: React.FC<...> = (...) => { ... }
             var arrowComponentRegex = new Regex(@"(?:const|let|var)\s+([A-Z][a-zA-Z0-9_$]*)(?:\s*:\s*React\.(?:FC|FunctionComponent)<[^>]*>)?\s*=\s*\(?([^)]*)\)?\s*=>", RegexOptions.Multiline);
            // Class components: class ComponentName extends React.Component { ... }
             var classComponentRegex = new Regex(@"class\s+([A-Z][a-zA-Z0-9_$]*)\s+extends\s+(?:React\.)?(?:Component|PureComponent)", RegexOptions.Multiline);

            // React hooks: useState, useEffect, useContext, useReducer, useCallback, useMemo, useRef, useImperativeHandle, useLayoutEffect, useDebugValue, or custom hooks use[A-Z]...
            var hooksRegex = new Regex(@"use(?:State|Effect|Context|Reducer|Callback|Memo|Ref|ImperativeHandle|LayoutEffect|DebugValue|[A-Z][a-zA-Z0-9_$]*)\(", RegexOptions.Multiline);

            // Process function components
            ProcessReactMatches(content, functionComponentRegex, hooksRegex, components, filePath, isArrow: false, isClass: false);

            // Process arrow function components
            ProcessReactMatches(content, arrowComponentRegex, hooksRegex, components, filePath, isArrow: true, isClass: false);

             // Process class components (simpler extraction, less focus on props/hooks here)
             foreach (Match match in classComponentRegex.Matches(content))
             {
                 var componentName = match.Groups[1].Value;
                  if (!components.Any(c => c.Name == componentName && c.FilePath == filePath)) // Avoid duplicates if other regex matched
                 {
                     components.Add(new ReactComponentInfo
                     {
                         Name = componentName,
                         FilePath = filePath,
                         // Props/Hooks extraction for class components is more complex and not added here
                         Props = new List<string>(), // Indicate class component nature maybe?
                         Hooks = new List<string>()
                     });
                 }
             }
        }

         // Helper to reduce redundancy in React component extraction
         private void ProcessReactMatches(string content, Regex componentRegex, Regex hooksRegex, List<ReactComponentInfo> components, string filePath, bool isArrow, bool isClass)
         {
             foreach (Match match in componentRegex.Matches(content))
             {
                 var componentName = match.Groups[isArrow ? 2 : 1].Value;
                 var paramsGroup = match.Groups[isArrow ? 3 : 2].Value;

                 // Basic check if it's a valid component name
                 if (string.IsNullOrEmpty(componentName) || !char.IsUpper(componentName[0])) continue;

                 // Avoid adding duplicates if already found by another pattern
                 if (components.Any(c => c.Name == componentName && c.FilePath == filePath)) continue;

                 var propsParam = paramsGroup.Trim();
                 var props = new List<string>();
                 var hooks = new HashSet<string>();

                 // Find component body (heuristic, might fail on complex structures)
                 int bodyStartIndex = match.Index + match.Length;
                 int bodyEndIndex = FindMatchingBraceOrParen(content, bodyStartIndex); // Find end of function body or arrow expression

                 if (bodyEndIndex > bodyStartIndex)
                 {
                     var componentBody = content.Substring(bodyStartIndex, bodyEndIndex - bodyStartIndex);

                     // Extract hooks
                     foreach (Match hookMatch in hooksRegex.Matches(componentBody))
                     {
                         hooks.Add(hookMatch.Value.TrimEnd('('));
                     }

                     // --- Extract Props (Simplified) ---
                     // 1. Destructuring: ({ prop1, prop2, ...rest })
                     var destructureRegex = new Regex(@"{([^}]+)}");
                     var destructureMatch = destructureRegex.Match(propsParam);
                     if (destructureMatch.Success)
                     {
                         var propsContent = destructureMatch.Groups[1].Value;
                         props.AddRange(propsContent.Split(',')
                             .Select(p => p.Split(':')[0].Trim()) // Handle { prop: alias }
                             .Where(p => !string.IsNullOrWhiteSpace(p) && !p.Contains("...")) // Exclude rest syntax for now
                             .Select(p => p.Split('=')[0].Trim()) // Handle default values { prop = defaultValue }
                             );
                     }
                     // 2. Standard 'props' object access: props.propName (only if param is 'props')
                     else if (propsParam == "props")
                     {
                         var propsAccessRegex = new Regex(@"props\.([a-zA-Z_$][a-zA-Z0-9_$]*)", RegexOptions.Multiline);
                         foreach (Match propMatch in propsAccessRegex.Matches(componentBody))
                         {
                             props.Add(propMatch.Groups[1].Value);
                         }
                     }
                     // 3. TypeScript type annotation (basic detection)
                     var tsPropsRegex = new Regex(@"\(\s*props\s*:\s*(\w+)\s*\)"); // Catches (props: PropsType)
                     var tsMatch = tsPropsRegex.Match(propsParam);
                     if(tsMatch.Success) {
                         props.Add($"Type: {tsMatch.Groups[1].Value}"); // Indicate TS prop type found
                     }
                 }

                 components.Add(new ReactComponentInfo
                 {
                     Name = componentName,
                     Props = props.Distinct().ToList(),
                     Hooks = hooks.OrderBy(h => h).ToList(),
                     FilePath = filePath
                 });
             }
         }

         // Helper to find the end of a code block (limited)
        private int FindMatchingBraceOrParen(string content, int startIndex)
        {
            int braceLevel = 0;
            int parenLevel = 0;
            char openChar = '\0';
            char closeChar = '\0';

            // Find the first opening brace or paren
            for (int i = startIndex; i < content.Length; i++) {
                if (content[i] == '{') {
                    openChar = '{';
                    closeChar = '}';
                    startIndex = i;
                    braceLevel = 1;
                    break;
                }
                 if (content[i] == '(') {
                    // Handle case like `() => (...)` vs `() => { ... }`
                    // Look ahead slightly to see if it's likely an expression body `=> (` or block `=> {`
                    int nextNonWs = i + 1;
                    while(nextNonWs < content.Length && char.IsWhiteSpace(content[nextNonWs])) nextNonWs++;
                     if(nextNonWs < content.Length && content[nextNonWs] == '{') {
                         // Likely a block after `=>`, wait for `{`
                     } else {
                         openChar = '(';
                         closeChar = ')';
                         startIndex = i;
                         parenLevel = 1;
                         break;
                     }
                }
                 // If we hit a semicolon or line break before an opening brace/paren, assume simple arrow expression
                 if (content[i] == ';' || content[i] == '\n') return i;
            }

            if (openChar == '\0') return startIndex; // No block found

            for (int i = startIndex + 1; i < content.Length; i++)
            {
                if (content[i] == openChar) {
                    if (openChar == '{') braceLevel++; else parenLevel++;
                }
                else if (content[i] == closeChar)
                {
                     if (openChar == '{') braceLevel--; else parenLevel--;

                    if ((openChar == '{' && braceLevel == 0) || (openChar == '(' && parenLevel == 0))
                    {
                        return i + 1;
                    }
                }
                // Basic handling for comments and strings (incomplete)
                else if (content[i] == '/' && i + 1 < content.Length) {
                    if (content[i+1] == '/') i = content.IndexOf('\n', i); // Skip single line comment
                    else if (content[i+1] == '*') i = content.IndexOf("*/", i+2) + 1; // Skip multi-line comment
                    if (i < startIndex) return content.Length; // Comment ended before start index somehow? Bail out
                }
                 else if (content[i] == '"' || content[i] == '\'' || content[i] == '`') { // Skip strings
                     char quote = content[i];
                     i++;
                     while(i < content.Length && (content[i] != quote || content[i-1] == '\\')) i++;
                 }
            }

            return content.Length; // Fallback: return end of content if no match found
        }

        private async Task<VueAnalysis> AnalyzeVueFilesAsync(
            string directoryPath,
            List<string> failedFiles,
            string searchTerm = "",
            string excludePattern = "",
            bool useRegex = false,
            bool caseSensitive = false,
            bool highlightMatches = true)
        {
            var allComponents = new List<VueComponentInfo>();
            Regex? searchRegex = null;
            StringComparison comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            // Compile search regex once if needed
            if (!string.IsNullOrEmpty(searchTerm) && useRegex)
            {
                try
                {
                    var regexOptions = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    searchRegex = new Regex(searchTerm, regexOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error compiling search regular expression for Vue");
                    return new VueAnalysis();
                }
            }

            var vueFiles = Directory.GetFiles(directoryPath, "*.vue", SearchOption.AllDirectories)
                .Where(file => !IsExcluded(file, excludePattern))
                .ToList();

            _logger.LogInformation($"Processing {vueFiles.Count} Vue component files...");

            foreach (var file in vueFiles)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    ExtractVueComponentInfo(file, content, allComponents);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error analyzing Vue file: {file}");
                    failedFiles.Add(file);
                }
            }

            // Filtering Stage
            var filteredComponents = new List<VueComponentInfo>();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                // Apply search filters if a search term was provided
                foreach (var component in allComponents)
                {
                    bool isMatch = false;
                    
                    // Match component name
                    if (useRegex && searchRegex != null)
                    {
                        isMatch = searchRegex.IsMatch(component.Name);
                    }
                    else
                    {
                        isMatch = component.Name.Contains(searchTerm, comparison);
                    }
                    
                    // Match props
                    if (!isMatch)
                    {
                        foreach (var prop in component.Props)
                        {
                            if (useRegex && searchRegex != null)
                            {
                                if (searchRegex.IsMatch(prop))
                                {
                                    isMatch = true;
                                    break;
                                }
                            }
                            else if (prop.Contains(searchTerm, comparison))
                            {
                                isMatch = true;
                                break;
                            }
                        }
                    }
                    
                    // Match data
                    if (!isMatch)
                    {
                        foreach (var data in component.Data)
                        {
                            if (useRegex && searchRegex != null)
                            {
                                if (searchRegex.IsMatch(data))
                                {
                                    isMatch = true;
                                    break;
                                }
                            }
                            else if (data.Contains(searchTerm, comparison))
                            {
                                isMatch = true;
                                break;
                            }
                        }
                    }
                    
                    // Match methods
                    if (!isMatch)
                    {
                        foreach (var method in component.Methods)
                        {
                            if (useRegex && searchRegex != null)
                            {
                                if (searchRegex.IsMatch(method))
                                {
                                    isMatch = true;
                                    break;
                                }
                            }
                            else if (method.Contains(searchTerm, comparison))
                            {
                                isMatch = true;
                                break;
                            }
                        }
                    }
                    
                    // Match computed properties
                    if (!isMatch)
                    {
                        foreach (var computed in component.ComputedProperties)
                        {
                            if (useRegex && searchRegex != null)
                            {
                                if (searchRegex.IsMatch(computed))
                                {
                                    isMatch = true;
                                    break;
                                }
                            }
                            else if (computed.Contains(searchTerm, comparison))
                            {
                                isMatch = true;
                                break;
                            }
                        }
                    }
                    
                    if (isMatch)
                    {
                        filteredComponents.Add(component);
                    }
                }
                
                // Check if we filtered everything out
                if (!filteredComponents.Any())
                {
                    _logger.LogInformation("No Vue components matched the search criteria.");
                    return new VueAnalysis { Components = new List<VueComponentInfo>() };
                }
            }
            else if (!allComponents.Any())
            {
                _logger.LogInformation("No Vue components were found in the analyzed files.");
                return new VueAnalysis { Components = new List<VueComponentInfo>() };
            }
            else
            {
                // Without a search term, we include everything
                filteredComponents = allComponents;
            }

            return new VueAnalysis { Components = filteredComponents };
        }

         // NOTE: Regex for Vue SFC is complex. This handles basic Options API and detects <script setup>.
        private void ExtractVueComponentInfo(string filePath, string content, List<VueComponentInfo> components)
        {
            var component = new VueComponentInfo { FilePath = filePath };

            // Check for <script setup>
            var scriptSetupRegex = new Regex(@"<script[^>]*\ssetup[^>]*>", RegexOptions.IgnoreCase);
             component.IsScriptSetup = scriptSetupRegex.IsMatch(content);

            // Extract standard script section (non-setup)
            var scriptRegex = new Regex(@"<script(?!\s*setup)[^>]*>(.*?)</script>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var scriptMatch = scriptRegex.Match(content);

            if (scriptMatch.Success)
            {
                var scriptContent = scriptMatch.Groups[1].Value;
                
                // Extract component name
                var nameRegex = new Regex(@"name:\s*['""]([^'""]+)['""]", RegexOptions.Multiline);
                var nameMatch = nameRegex.Match(scriptContent);
                
                if (nameMatch.Success)
                {
                    component.Name = nameMatch.Groups[1].Value;
                }
                
                // Extract props
                var propsRegex = new Regex(@"props:\s*{([^{}]*?(?:{(?:[^{}]*?{[^{}]*?})*?[^{}]*?})*?[^{}]*?)}", RegexOptions.Singleline);
                var propsMatch = propsRegex.Match(scriptContent);
                
                if (propsMatch.Success)
                {
                    var propsContent = propsMatch.Groups[1].Value;
                    var propNameRegex = new Regex(@"^\s*([a-zA-Z_$][a-zA-Z0-9_$]*)\s*:", RegexOptions.Multiline);
                    
                    foreach (Match propMatch in propNameRegex.Matches(propsContent))
                    {
                        component.Props.Add(propMatch.Groups[1].Value);
                    }
                }
                
                // Extract props (array syntax - improved for quotes/spacing)
                var propsArrayRegex = new Regex(@"props:\s*\[([^\]]*)\]", RegexOptions.Singleline);
                var propsArrayMatch = propsArrayRegex.Match(scriptContent);
                
                if (propsArrayMatch.Success)
                {
                    var propsContent = propsArrayMatch.Groups[1].Value;
                    var propValues = propsContent.Split(',')
                        .Select(p => p.Trim().Trim('\'', '"'))
                        .Where(p => !string.IsNullOrEmpty(p))
                        .ToList();
                        
                    component.Props.AddRange(propValues);
                }
                
                // Extract data properties (improved for spacing)
                var dataRegex = new Regex(@"data\s*\(\s*\)\s*{\s*return\s*{([^{}]*?(?:{(?:[^{}]*?{[^{}]*?})*?[^{}]*?})*?[^{}]*?)}", RegexOptions.Singleline);
                var dataMatch = dataRegex.Match(scriptContent);
                
                if (dataMatch.Success)
                {
                    var dataContent = dataMatch.Groups[1].Value;
                    var dataPropertyRegex = new Regex(@"^\s*([a-zA-Z_$][a-zA-Z0-9_$]*)\s*:", RegexOptions.Multiline);
                    
                    foreach (Match propMatch in dataPropertyRegex.Matches(dataContent))
                    {
                        component.Data.Add(propMatch.Groups[1].Value);
                    }
                }
                
                // Extract methods (improved for async/spacing)
                var methodsRegex = new Regex(@"methods:\s*{([^{}]*?(?:{(?:[^{}]*?{[^{}]*?})*?[^{}]*?})*?[^{}]*?)}", RegexOptions.Singleline);
                var methodsMatch = methodsRegex.Match(scriptContent);
                
                if (methodsMatch.Success)
                {
                    var methodsContent = methodsMatch.Groups[1].Value;
                    var methodNameRegex = new Regex(@"^\s*(?:async\s+)?([a-zA-Z_$][a-zA-Z0-9_$]*)\s*(?:\([^)]*\)\s*{|:\s*function\s*\([^)]*\))", RegexOptions.Multiline);
                    
                    foreach (Match methodMatch in methodNameRegex.Matches(methodsContent))
                    {
                        component.Methods.Add(methodMatch.Groups[1].Value);
                    }
                }
                
                // Extract computed properties (improved for spacing)
                var computedRegex = new Regex(@"computed:\s*{([^{}]*?(?:{(?:[^{}]*?{[^{}]*?})*?[^{}]*?})*?[^{}]*?)}", RegexOptions.Singleline);
                var computedMatch = computedRegex.Match(scriptContent);
                
                if (computedMatch.Success)
                {
                    var computedContent = computedMatch.Groups[1].Value;
                    var computedNameRegex = new Regex(@"^\s*([a-zA-Z_$][a-zA-Z0-9_$]*)\s*(?:\([^)]*\)\s*{|:\s*(?:function\s*\(|{\s*get\s*\())", RegexOptions.Multiline);
                    
                    foreach (Match compMatch in computedNameRegex.Matches(computedContent))
                    {
                        component.ComputedProperties.Add(compMatch.Groups[1].Value);
                    }
                }
            }
             else if (component.IsScriptSetup) {
                 // Basic info for script setup components
                 component.Name = Path.GetFileNameWithoutExtension(filePath); // Use filename as name
                 // Could add regex here to find `defineProps`, `defineEmits`, top-level functions/consts
                 // Example (very basic):
                  var definePropsRegex = new Regex(@"defineProps\s*<\s*([^>]+)\s*>\s*\(\s*\)"); // Find defineProps<Interface>()
                  var propsTypeMatch = definePropsRegex.Match(content);
                  if(propsTypeMatch.Success) {
                      component.Props.Add($"Type: {propsTypeMatch.Groups[1].Value}");
                  }

                  var functionSetupRegex = new Regex(@"(?:function|const)\s+([a-zA-Z_$][a-zA-Z0-9_$]*)\s*=", RegexOptions.Multiline);
                 foreach(Match funcMatch in functionSetupRegex.Matches(content)) {
                      // Check if it's inside the <script setup> block if needed
                      component.Methods.Add(funcMatch.Groups[1].Value + " (setup)"); // Mark as setup function/const
                 }
             }

             // Add component if it has a name or specific features, or if script setup
             if (!string.IsNullOrEmpty(component.Name) || component.IsScriptSetup || component.Props.Any() || component.Data.Any() || component.Methods.Any() || component.ComputedProperties.Any())
             {
                 components.Add(component);
             }
        }

        // Helper method to check if a file path matches a simple exclusion pattern
        private bool IsExcluded(string filePath, string excludePattern)
        {
            if (string.IsNullOrEmpty(excludePattern))
            {
                return false;
            }

            // Normalize path separators for comparison
            string normalizedPath = filePath.Replace('\\', '/');
            string normalizedPattern = excludePattern.Replace('\\', '/');


            // Simple check first
             if (normalizedPath.Contains(normalizedPattern, StringComparison.OrdinalIgnoreCase)) {
                 return true;
             }


             // Convert simple glob (*, ?) to regex for more flexibility
            // Escape regex special characters except for * and ?
            string regexPattern = Regex.Escape(normalizedPattern)
                                     .Replace("\\*", ".*") // Replace * with .* (any character, zero or more times)
                                     .Replace("\\?", ".");  // Replace ? with . (any single character)

            // Match against the full normalized path, case-insensitive
            try
            {
                 // Anchor the pattern to match anywhere within the path string segments implicitly
                 // We check parts of the path against the pattern
                 string[] pathSegments = normalizedPath.Split('/');
                 string[] patternSegments = normalizedPattern.Split('/');

                 // This is a simplified check: if any part of the pattern matches any part of the path.
                 // A more robust glob implementation would handle directory separators and **.
                 foreach(var pSeg in patternSegments) {
                     string currentRegexPattern = $"^{Regex.Escape(pSeg).Replace("\\*", ".*").Replace("\\?", ".")}$"; // Anchor each segment
                     Regex regex = new Regex(currentRegexPattern, RegexOptions.IgnoreCase);
                     foreach(var pathSeg in pathSegments) {
                         if(regex.IsMatch(pathSeg)) return true;
                     }
                 }

                 // Fallback: Check the full path against the full pattern regex
                 Regex fullRegex = new Regex(regexPattern, RegexOptions.IgnoreCase);
                 if(fullRegex.IsMatch(normalizedPath)) {
                     return true;
                 }

            }
            catch (RegexParseException ex)
            {
                _logger.LogWarning($"Invalid regex generated from exclude pattern '{excludePattern}': {ex.Message}");
                // Fallback to simple contains check if regex fails
                return normalizedPath.Contains(normalizedPattern, StringComparison.OrdinalIgnoreCase);
            }


            return false; // Default to not excluded
        }
    }

    // Add QueryCodeStructure tool after the ProjectTool class
    [McpServerToolType]
    public class CodeQueryTool
    {
        private readonly ILogger<CodeQueryTool> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private static CodeAnalysisResult? _lastAnalysisResult;

        public CodeQueryTool(ILogger<CodeQueryTool> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
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
            // Implementation to generate Markdown documentation
            // For now, this can be a placeholder
            var documentationBuilder = new StringBuilder();
            documentationBuilder.AppendLine("# Code Documentation");
            documentationBuilder.AppendLine();
            documentationBuilder.AppendLine("This documentation is generated automatically.");
            documentationBuilder.AppendLine();

            // Store result for future queries
            var projectTool = new ProjectTool(_loggerFactory.CreateLogger<ProjectTool>());
            _lastAnalysisResult = await projectTool.AnalyzeProject(
                directoryPath, 
                new List<string>(), 
                searchTerm, 
                fileExtensions, 
                excludePattern, 
                useRegex, 
                caseSensitive, 
                highlightMatches);

            // Generate documentation based on the analysis result
            documentationBuilder.AppendLine("## Project Overview");
            documentationBuilder.AppendLine();
            
            // File types
            documentationBuilder.AppendLine("### File Types");
            documentationBuilder.AppendLine();
            documentationBuilder.AppendLine("| Extension | Count |");
            documentationBuilder.AppendLine("|-----------|-------|");
            
            foreach (var fileType in _lastAnalysisResult.FileTypes.OrderByDescending(ft => ft.Value))
            {
                documentationBuilder.AppendLine($"| {fileType.Key} | {fileType.Value} |");
            }
            
            documentationBuilder.AppendLine();
            
            // Directory structure section
            documentationBuilder.AppendLine("### Directory Structure");
            documentationBuilder.AppendLine();
            documentationBuilder.AppendLine("```");
            AppendDirectoryStructure(documentationBuilder, _lastAnalysisResult.DirectoryTree, 0);
            documentationBuilder.AppendLine("```");
            documentationBuilder.AppendLine();
            
            // Write to file if path is provided
            if (!string.IsNullOrEmpty(outputFilePath))
            {
                await File.WriteAllTextAsync(outputFilePath, documentationBuilder.ToString());
                return $"Documentation generated and saved to {outputFilePath}";
            }
            
            return documentationBuilder.ToString();
        }

        private void AppendDirectoryStructure(StringBuilder sb, DirectoryStructure dir, int level)
        {
            string indent = new string(' ', level * 2);
            sb.AppendLine($"{indent}📂 {dir.Name}");
            
            foreach (var file in dir.Files.OrderBy(f => f))
            {
                sb.AppendLine($"{indent}  📄 {file}");
            }
            
            foreach (var subdir in dir.Subdirectories.OrderBy(d => d.Name))
            {
                AppendDirectoryStructure(sb, subdir, level + 1);
            }
        }

        [McpServerTool, Description("Allow the AI to ask questions about the code structure and get answers directly from the analysis tool.")]
        public async Task<string> QueryCodeStructure(string query, string directoryPath = "")
        {
            // If directoryPath is provided or no previous analysis exists, analyze the code first
            if (!string.IsNullOrEmpty(directoryPath) || _lastAnalysisResult == null)
            {
                if (string.IsNullOrEmpty(directoryPath))
                {
                    return "Please provide a directory path for initial analysis.";
                }
                
                _logger.LogInformation($"Analyzing code structure for directory: {directoryPath}");
                var projectTool = new ProjectTool(_loggerFactory.CreateLogger<ProjectTool>());
                _lastAnalysisResult = await projectTool.AnalyzeProject(directoryPath, new List<string>());
            }
            
            if (_lastAnalysisResult == null)
            {
                return "No code analysis result available. Please analyze a codebase first.";
            }

            // Process different types of queries
            _logger.LogInformation($"Processing query: {query}");
            
            // Simple query parser - in a real implementation, you might use NLP or a more sophisticated approach
            query = query.ToLowerInvariant().Trim();
            
            if (query.Contains("file type") || query.Contains("extension"))
            {
                return ProcessFileTypeQuery(_lastAnalysisResult);
            }
            else if (query.Contains("class") || query.Contains("classes"))
            {
                return ProcessClassQuery(_lastAnalysisResult, query);
            }
            else if (query.Contains("method") || query.Contains("function"))
            {
                return ProcessMethodQuery(_lastAnalysisResult, query);
            }
            else if (query.Contains("component"))
            {
                return ProcessComponentQuery(_lastAnalysisResult, query);
            }
            else if (query.Contains("structure") || query.Contains("directory") || query.Contains("folder"))
            {
                return ProcessDirectoryQuery(_lastAnalysisResult);
            }
            else if (query.Contains("namespace"))
            {
                return ProcessNamespaceQuery(_lastAnalysisResult);
            }
            else if (query.Contains("largest") || query.Contains("biggest"))
            {
                return ProcessSizeQuery(_lastAnalysisResult, query);
            }
            else if (query.Contains("count"))
            {
                return ProcessCountQuery(_lastAnalysisResult, query);
            }
            else if (query.Contains("summary") || query.Contains("overview"))
            {
                return ProcessSummaryQuery(_lastAnalysisResult);
            }
            else
            {
                // Default response for unrecognized queries
                return "I couldn't understand that query. Try asking about classes, methods, components, file types, directory structure, or namespaces.";
            }
        }

        private string ProcessFileTypeQuery(CodeAnalysisResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# File Types in the Codebase");
            sb.AppendLine();
            
            if (result.FileTypes.Count == 0)
            {
                return "No file types found in the analysis.";
            }
            
            sb.AppendLine("| Extension | Count |");
            sb.AppendLine("|-----------|-------|");
            
            foreach (var fileType in result.FileTypes.OrderByDescending(ft => ft.Value))
            {
                sb.AppendLine($"| {fileType.Key} | {fileType.Value} |");
            }
            
            return sb.ToString();
        }

        private string ProcessClassQuery(CodeAnalysisResult result, string query)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Classes in the Codebase");
            sb.AppendLine();
            
            // C# classes
            if (result.CSharpResults.Classes.Count > 0)
            {
                sb.AppendLine("## C# Classes");
                sb.AppendLine();
                
                // Check if query contains a specific class name
                string className = ExtractNameFromQuery(query, "class");
                
                if (!string.IsNullOrEmpty(className))
                {
                    // Search for specific class
                    var matchingClasses = result.CSharpResults.Classes
                        .Where(c => c.Name.ToLowerInvariant().Contains(className))
                        .ToList();
                        
                    if (matchingClasses.Count == 0)
                    {
                        sb.AppendLine($"No C# classes found matching '{className}'.");
                    }
                    else
                    {
                        foreach (var cls in matchingClasses)
                        {
                            sb.AppendLine($"### {cls.Name}");
                            sb.AppendLine($"- Namespace: {cls.Namespace}");
                            sb.AppendLine($"- File: {cls.FilePath}");
                            sb.AppendLine($"- Methods: {cls.Methods.Count}");
                            sb.AppendLine();
                            
                            if (cls.Methods.Count > 0)
                            {
                                sb.AppendLine("| Method | Return Type | Public | Static | Parameters |");
                                sb.AppendLine("|--------|-------------|--------|--------|------------|");
                                
                                foreach (var method in cls.Methods)
                                {
                                    string parameters = string.Join(", ", method.Parameters);
                                    sb.AppendLine($"| {method.Name} | {method.ReturnType} | {method.IsPublic} | {method.IsStatic} | {parameters} |");
                                }
                                
                                sb.AppendLine();
                            }
                        }
                    }
                }
                else
                {
                    // List all classes
                    sb.AppendLine("| Class | Namespace | Methods |");
                    sb.AppendLine("|-------|-----------|---------|");
                    
                    foreach (var cls in result.CSharpResults.Classes.OrderBy(c => c.Name))
                    {
                        sb.AppendLine($"| {cls.Name} | {cls.Namespace} | {cls.Methods.Count} |");
                    }
                    
                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("No C# classes found in the analysis.");
            }
            
            // JavaScript classes
            if (result.JavaScriptResults.Classes.Count > 0)
            {
                sb.AppendLine("## JavaScript Classes");
                sb.AppendLine();
                
                string className = ExtractNameFromQuery(query, "class");
                
                if (!string.IsNullOrEmpty(className))
                {
                    // Search for specific class
                    var matchingClasses = result.JavaScriptResults.Classes
                        .Where(c => c.Name.ToLowerInvariant().Contains(className))
                        .ToList();
                        
                    if (matchingClasses.Count == 0)
                    {
                        sb.AppendLine($"No JavaScript classes found matching '{className}'.");
                    }
                    else
                    {
                        foreach (var cls in matchingClasses)
                        {
                            sb.AppendLine($"### {cls.Name}");
                            sb.AppendLine($"- File: {cls.FilePath}");
                            sb.AppendLine($"- Methods: {string.Join(", ", cls.Methods)}");
                            sb.AppendLine();
                        }
                    }
                }
                else
                {
                    // List all classes
                    sb.AppendLine("| Class | Methods |");
                    sb.AppendLine("|-------|---------|");
                    
                    foreach (var cls in result.JavaScriptResults.Classes.OrderBy(c => c.Name))
                    {
                        sb.AppendLine($"| {cls.Name} | {cls.Methods.Count} |");
                    }
                    
                    sb.AppendLine();
                }
            }
            
            return sb.ToString();
        }

        private string ProcessMethodQuery(CodeAnalysisResult result, string query)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Methods in the Codebase");
            sb.AppendLine();
            
            string methodName = ExtractNameFromQuery(query, "method");
            
            if (!string.IsNullOrEmpty(methodName))
            {
                // Search for specific method
                sb.AppendLine($"## Methods matching '{methodName}'");
                sb.AppendLine();
                
                bool foundAny = false;
                
                // C# methods
                var csharpMatches = result.CSharpResults.Classes
                    .SelectMany(c => c.Methods.Select(m => new { Class = c, Method = m }))
                    .Where(x => x.Method.Name.ToLowerInvariant().Contains(methodName))
                    .ToList();
                    
                if (csharpMatches.Count > 0)
                {
                    sb.AppendLine("### C# Methods");
                    sb.AppendLine();
                    sb.AppendLine("| Method | Class | Namespace | Return Type | Public | Static | Parameters |");
                    sb.AppendLine("|--------|-------|-----------|-------------|--------|--------|------------|");
                    
                    foreach (var match in csharpMatches)
                    {
                        string parameters = string.Join(", ", match.Method.Parameters);
                        sb.AppendLine($"| {match.Method.Name} | {match.Class.Name} | {match.Class.Namespace} | {match.Method.ReturnType} | {match.Method.IsPublic} | {match.Method.IsStatic} | {parameters} |");
                    }
                    
                    sb.AppendLine();
                    foundAny = true;
                }
                
                // JavaScript functions
                var jsMatches = result.JavaScriptResults.Functions
                    .Where(f => f.Name.ToLowerInvariant().Contains(methodName))
                    .ToList();
                    
                if (jsMatches.Count > 0)
                {
                    sb.AppendLine("### JavaScript Functions");
                    sb.AppendLine();
                    sb.AppendLine("| Function | Parameters | File |");
                    sb.AppendLine("|----------|------------|------|");
                    
                    foreach (var func in jsMatches)
                    {
                        string parameters = string.Join(", ", func.Parameters);
                        sb.AppendLine($"| {func.Name} | {parameters} | {func.FilePath} |");
                    }
                    
                    sb.AppendLine();
                    foundAny = true;
                }
                
                if (!foundAny)
                {
                    sb.AppendLine($"No methods or functions found matching '{methodName}'.");
                }
            }
            else
            {
                // Count methods by type
                int csharpMethodCount = result.CSharpResults.Classes
                    .SelectMany(c => c.Methods)
                    .Count();
                    
                int jsMethodCount = result.JavaScriptResults.Functions.Count;
                
                sb.AppendLine("## Method Counts");
                sb.AppendLine();
                sb.AppendLine("| Language | Method Count |");
                sb.AppendLine("|----------|--------------|");
                sb.AppendLine($"| C# | {csharpMethodCount} |");
                sb.AppendLine($"| JavaScript | {jsMethodCount} |");
                sb.AppendLine($"| Total | {csharpMethodCount + jsMethodCount} |");
            }
            
            return sb.ToString();
        }

        private string ProcessComponentQuery(CodeAnalysisResult result, string query)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Components in the Codebase");
            sb.AppendLine();
            
            string componentName = ExtractNameFromQuery(query, "component");
            
            // React components
            if (result.ReactResults.Components.Count > 0)
            {
                sb.AppendLine("## React Components");
                sb.AppendLine();
                
                if (!string.IsNullOrEmpty(componentName))
                {
                    var matchingComponents = result.ReactResults.Components
                        .Where(c => c.Name.ToLowerInvariant().Contains(componentName))
                        .ToList();
                        
                    if (matchingComponents.Count == 0)
                    {
                        sb.AppendLine($"No React components found matching '{componentName}'.");
                    }
                    else
                    {
                        foreach (var component in matchingComponents)
                        {
                            sb.AppendLine($"### {component.Name}");
                            sb.AppendLine($"- File: {component.FilePath}");
                            sb.AppendLine($"- Props: {string.Join(", ", component.Props)}");
                            sb.AppendLine($"- Hooks: {string.Join(", ", component.Hooks)}");
                            sb.AppendLine();
                        }
                    }
                }
                else
                {
                    sb.AppendLine("| Component | Props | Hooks |");
                    sb.AppendLine("|-----------|-------|-------|");
                    
                    foreach (var component in result.ReactResults.Components.OrderBy(c => c.Name))
                    {
                        sb.AppendLine($"| {component.Name} | {component.Props.Count} | {component.Hooks.Count} |");
                    }
                    
                    sb.AppendLine();
                }
            }
            
            // Vue components
            if (result.VueResults.Components.Count > 0)
            {
                sb.AppendLine("## Vue Components");
                sb.AppendLine();
                
                if (!string.IsNullOrEmpty(componentName))
                {
                    var matchingComponents = result.VueResults.Components
                        .Where(c => c.Name.ToLowerInvariant().Contains(componentName))
                        .ToList();
                        
                    if (matchingComponents.Count == 0)
                    {
                        sb.AppendLine($"No Vue components found matching '{componentName}'.");
                    }
                    else
                    {
                        foreach (var component in matchingComponents)
                        {
                            sb.AppendLine($"### {component.Name}");
                            sb.AppendLine($"- File: {component.FilePath}");
                            sb.AppendLine($"- Script Setup: {component.IsScriptSetup}");
                            sb.AppendLine($"- Props: {string.Join(", ", component.Props)}");
                            sb.AppendLine($"- Data: {string.Join(", ", component.Data)}");
                            sb.AppendLine($"- Methods: {string.Join(", ", component.Methods)}");
                            sb.AppendLine($"- Computed: {string.Join(", ", component.ComputedProperties)}");
                            sb.AppendLine();
                        }
                    }
                }
                else
                {
                    sb.AppendLine("| Component | Props | Data | Methods | Computed |");
                    sb.AppendLine("|-----------|-------|------|---------|----------|");
                    
                    foreach (var component in result.VueResults.Components.OrderBy(c => c.Name))
                    {
                        sb.AppendLine($"| {component.Name} | {component.Props.Count} | {component.Data.Count} | {component.Methods.Count} | {component.ComputedProperties.Count} |");
                    }
                    
                    sb.AppendLine();
                }
            }
            
            if (result.ReactResults.Components.Count == 0 && result.VueResults.Components.Count == 0)
            {
                sb.AppendLine("No components found in the analysis.");
            }
            
            return sb.ToString();
        }

        private string ProcessDirectoryQuery(CodeAnalysisResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Directory Structure");
            sb.AppendLine();
            sb.AppendLine("```");
            AppendDirectoryStructure(sb, result.DirectoryTree, 0);
            sb.AppendLine("```");
            return sb.ToString();
        }

        private string ProcessNamespaceQuery(CodeAnalysisResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Namespaces in the Codebase");
            sb.AppendLine();
            
            if (result.CSharpResults.Namespaces.Count == 0)
            {
                return "No namespaces found in the analysis.";
            }
            
            foreach (var ns in result.CSharpResults.Namespaces.OrderBy(n => n.Key))
            {
                sb.AppendLine($"## {ns.Key}");
                
                // Count classes in this namespace
                int classCount = result.CSharpResults.Classes.Count(c => c.Namespace == ns.Key);
                sb.AppendLine($"Contains {classCount} classes.");
                sb.AppendLine();
            }
            
            return sb.ToString();
        }

        private string ProcessSizeQuery(CodeAnalysisResult result, string query)
        {
            var sb = new StringBuilder();
            
            if (query.Contains("class"))
            {
                // Find class with the most methods
                var largestClass = result.CSharpResults.Classes
                    .OrderByDescending(c => c.Methods.Count)
                    .FirstOrDefault();
                    
                if (largestClass != null)
                {
                    sb.AppendLine($"The largest class is '{largestClass.Name}' with {largestClass.Methods.Count} methods.");
                    sb.AppendLine($"It is in the namespace '{largestClass.Namespace}' and defined in '{largestClass.FilePath}'.");
                }
                else
                {
                    sb.AppendLine("No classes found in the analysis.");
                }
            }
            else if (query.Contains("component"))
            {
                // Find component with the most props, methods, etc.
                var largestReactComponent = result.ReactResults.Components
                    .OrderByDescending(c => c.Props.Count + c.Hooks.Count)
                    .FirstOrDefault();
                    
                var largestVueComponent = result.VueResults.Components
                    .OrderByDescending(c => c.Props.Count + c.Data.Count + c.Methods.Count + c.ComputedProperties.Count)
                    .FirstOrDefault();
                    
                if (largestReactComponent != null)
                {
                    sb.AppendLine($"The largest React component is '{largestReactComponent.Name}' with {largestReactComponent.Props.Count} props and {largestReactComponent.Hooks.Count} hooks.");
                    sb.AppendLine($"It is defined in '{largestReactComponent.FilePath}'.");
                    sb.AppendLine();
                }
                
                if (largestVueComponent != null)
                {
                    sb.AppendLine($"The largest Vue component is '{largestVueComponent.Name}' with:");
                    sb.AppendLine($"- {largestVueComponent.Props.Count} props");
                    sb.AppendLine($"- {largestVueComponent.Data.Count} data properties");
                    sb.AppendLine($"- {largestVueComponent.Methods.Count} methods");
                    sb.AppendLine($"- {largestVueComponent.ComputedProperties.Count} computed properties");
                    sb.AppendLine($"It is defined in '{largestVueComponent.FilePath}'.");
                }
                
                if (largestReactComponent == null && largestVueComponent == null)
                {
                    sb.AppendLine("No components found in the analysis.");
                }
            }
            else if (query.Contains("file") || query.Contains("extension"))
            {
                // Find most common file type
                var mostCommonFileType = result.FileTypes
                    .OrderByDescending(ft => ft.Value)
                    .FirstOrDefault();
                    
                if (mostCommonFileType.Key != null)
                {
                    sb.AppendLine($"The most common file type is '{mostCommonFileType.Key}' with {mostCommonFileType.Value} files.");
                }
                else
                {
                    sb.AppendLine("No file types found in the analysis.");
                }
            }
            else
            {
                sb.AppendLine("Please specify what you want to know about (largest class, component, or file type).");
            }
            
            return sb.ToString();
        }

        private string ProcessCountQuery(CodeAnalysisResult result, string query)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Counts in the Codebase");
            sb.AppendLine();
            
            sb.AppendLine("| Element | Count |");
            sb.AppendLine("|---------|-------|");
            
            int totalFiles = result.FileTypes.Sum(ft => ft.Value);
            sb.AppendLine($"| Files | {totalFiles} |");
            
            int totalCSharpClasses = result.CSharpResults.Classes.Count;
            sb.AppendLine($"| C# Classes | {totalCSharpClasses} |");
            
            int totalCSharpMethods = result.CSharpResults.Classes.Sum(c => c.Methods.Count);
            sb.AppendLine($"| C# Methods | {totalCSharpMethods} |");
            
            int totalJsClasses = result.JavaScriptResults.Classes.Count;
            sb.AppendLine($"| JavaScript Classes | {totalJsClasses} |");
            
            int totalJsFunctions = result.JavaScriptResults.Functions.Count;
            sb.AppendLine($"| JavaScript Functions | {totalJsFunctions} |");
            
            int totalReactComponents = result.ReactResults.Components.Count;
            sb.AppendLine($"| React Components | {totalReactComponents} |");
            
            int totalVueComponents = result.VueResults.Components.Count;
            sb.AppendLine($"| Vue Components | {totalVueComponents} |");
            
            int totalNamespaces = result.CSharpResults.Namespaces.Count;
            sb.AppendLine($"| Namespaces | {totalNamespaces} |");
            
            return sb.ToString();
        }

        private string ProcessSummaryQuery(CodeAnalysisResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Codebase Summary");
            sb.AppendLine();
            
            // Total file count by type
            int totalFiles = result.FileTypes.Sum(ft => ft.Value);
            sb.AppendLine($"This codebase contains {totalFiles} files across {result.FileTypes.Count} different file types.");
            sb.AppendLine();
            
            // Top 3 file types
            var top3FileTypes = result.FileTypes
                .OrderByDescending(ft => ft.Value)
                .Take(3)
                .ToList();
                
            sb.AppendLine("Top file types:");
            foreach (var ft in top3FileTypes)
            {
                sb.AppendLine($"- {ft.Key}: {ft.Value} files");
            }
            sb.AppendLine();
            
            // Code structure
            int totalCSharpClasses = result.CSharpResults.Classes.Count;
            int totalCSharpMethods = result.CSharpResults.Classes.Sum(c => c.Methods.Count);
            int totalJsClasses = result.JavaScriptResults.Classes.Count;
            int totalJsFunctions = result.JavaScriptResults.Functions.Count;
            int totalReactComponents = result.ReactResults.Components.Count;
            int totalVueComponents = result.VueResults.Components.Count;
            
            sb.AppendLine("Code structure:");
            if (totalCSharpClasses > 0)
            {
                sb.AppendLine($"- {totalCSharpClasses} C# classes with {totalCSharpMethods} methods");
            }
            if (totalJsClasses > 0 || totalJsFunctions > 0)
            {
                sb.AppendLine($"- {totalJsClasses} JavaScript classes and {totalJsFunctions} functions");
            }
            if (totalReactComponents > 0)
            {
                sb.AppendLine($"- {totalReactComponents} React components");
            }
            if (totalVueComponents > 0)
            {
                sb.AppendLine($"- {totalVueComponents} Vue components");
            }
            sb.AppendLine();
            
            // Failed files
            if (result.FailedFiles.Count > 0)
            {
                sb.AppendLine($"Note: {result.FailedFiles.Count} files could not be analyzed properly.");
            }
            
            return sb.ToString();
        }

        private string ExtractNameFromQuery(string query, string element)
        {
            // Try to extract a name after patterns like "find class named X" or "show method X"
            string[] patterns = {
                $"{element} named (\\w+)",
                $"{element} (\\w+)",
                $"find {element} (\\w+)",
                $"show {element} (\\w+)",
                $"describe {element} (\\w+)"
            };
            
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(query, pattern);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value.ToLowerInvariant();
                }
            }
            
            return string.Empty;
        }
    }
} 