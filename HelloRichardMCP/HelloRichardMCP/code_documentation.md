# Code Analysis Report
Generated on: 10/04/2025 23:48:29

## Project Overview

### File Types

- .dll: 72 files
- .json: 6 files
- .cache: 6 files
- .cs: 5 files
- .info: 2 files
- .exe: 2 files
- .pdb: 2 files
- .csproj: 1 files
- .props: 1 files
- .targets: 1 files
- .up2date: 1 files
- .txt: 1 files
- .editorconfig: 1 files

## Project Structure

```
HelloRichardMCP/
    |-- CodeAnalysisTool.cs
    |-- HelloRichardMCP.csproj
    |-- Program.cs

```

## C# Code Analysis

### Namespaces

- **HelloRichardMCP**
  - CodeAnalysisTool

### Classes

#### ClassInfo

**Namespace:** Global
**File:** CodeAnalysisTool.cs

#### GreetingTool

**Namespace:** Global
**File:** Program.cs

**Methods:**

| Name | Return Type | Parameters | Modifiers |
|------|-------------|------------|-----------|
| GreetRichard | string |  | public, static |

#### MethodInfo

**Namespace:** Global
**File:** CodeAnalysisTool.cs

#### CodeAnalysisTool

**Namespace:** HelloRichardMCP
**File:** CodeAnalysisTool.cs

**Methods:**

| Name | Return Type | Parameters | Modifiers |
|------|-------------|------------|-----------|
| AnalyzeCSharpFilesAsync | Task<string> | string directoryPath, string searchTerm, string excludePattern, bool useRegex, bool caseSensitive, bool highlightMatches |  |
| AnalyzeFileTypes | Dictionary<string, int> | string directoryPath, string fileExtensions, string excludePattern |  |
| GenerateCodeDocumentation | Task<string> | string directoryPath, string outputFilePath, string searchTerm, string fileExtensions, string excludePattern, bool useRegex, bool caseSensitive, bool highlightMatches | public |
| GenerateDirectoryTree | string | string directoryPath, int indentLevel |  |
| HighlightMatches | string | string text, Regex regex |  |
| HighlightOccurrence | string | string text, string searchTerm, bool caseSensitive |  |

