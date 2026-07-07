namespace PMD.App.Application.Scanner;

public static class ProjectTextFileRules
{
    private static readonly HashSet<string> SupportedTextFileExtensions = new(
        StringComparer.OrdinalIgnoreCase)
    {
        // .NET / C# / XAML
        ".cs",
        ".csx",
        ".razor",
        ".razor.css",
        ".xaml",
        ".csproj",
        ".vbproj",
        ".fsproj",
        ".sln",
        ".props",
        ".targets",
        ".config",
        ".resx",

        // Web
        ".html",
        ".htm",
        ".css",
        ".scss",
        ".sass",
        ".less",
        ".js",
        ".jsx",
        ".ts",
        ".tsx",
        ".vue",
        ".svelte",
        ".astro",
        ".mjs",
        ".cjs",

        // Data / config
        ".json",
        ".jsonc",
        ".xml",
        ".yml",
        ".yaml",
        ".toml",
        ".ini",
        ".env",
        ".properties",
        ".conf",
        ".cfg",
        ".lock",

        // Documentation / text
        ".md",
        ".mdx",
        ".txt",
        ".rst",
        ".adoc",
        ".csv",
        ".tsv",
        ".log",

        // Python
        ".py",
        ".pyw",
        ".ipynb",

        // Java / Kotlin / JVM
        ".java",
        ".kt",
        ".kts",
        ".groovy",
        ".gradle",

        // C / C++ / Objective-C
        ".c",
        ".h",
        ".cpp",
        ".cxx",
        ".cc",
        ".hpp",
        ".hh",
        ".hxx",
        ".m",
        ".mm",

        // Rust / Go
        ".rs",
        ".go",

        // PHP / Ruby
        ".php",
        ".phtml",
        ".rb",
        ".erb",

        // Swift / Apple
        ".swift",
        ".plist",

        // Shell / scripts
        ".sh",
        ".bash",
        ".zsh",
        ".fish",
        ".bat",
        ".cmd",
        ".ps1",
        ".psm1",

        // SQL / database scripts
        ".sql",

        // Mobile / cross-platform
        ".dart",
        ".lua",

        // Functional / other languages
        ".fs",
        ".fsi",
        ".fsx",
        ".scala",
        ".clj",
        ".cljs",
        ".hs",
        ".elm",
        ".ex",
        ".exs",
        ".erl",
        ".hrl",

        // Game / engine / tools
        ".gd",
        ".shader",
        ".hlsl",
        ".glsl",
        ".cginc",
        ".asmdef",

        // Infrastructure / DevOps
        ".dockerfile",
        ".tf",
        ".tfvars",
        ".hcl",
        ".bicep",
        ".ps1xml",

        // Templates
        ".tpl",
        ".mustache",
        ".handlebars",
        ".hbs",
        ".liquid"
    };

    private static readonly HashSet<string> SupportedTextFileNames = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ".editorconfig",
        ".env",
        ".env.local",
        ".env.development",
        ".env.production",
        ".gitattributes",
        ".gitignore",
        ".dockerignore",
        ".npmrc",
        ".nvmrc",
        ".prettierrc",
        ".eslintrc",
        ".babelrc",
        ".stylelintrc",
        "Dockerfile",
        "Makefile",
        "CMakeLists.txt",
        "README",
        "LICENSE",
        "CHANGELOG"
    };

    public static bool IsSupportedTextSnapshotFile(string fileName, string extension)
    {
        if (SupportedTextFileNames.Contains(fileName))
        {
            return true;
        }

        if (SupportedTextFileExtensions.Contains(extension))
        {
            return true;
        }

        return HasKnownCompoundExtension(fileName);
    }

    private static bool HasKnownCompoundExtension(string fileName)
    {
        return SupportedTextFileExtensions.Any(extension =>
            fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
    }
}