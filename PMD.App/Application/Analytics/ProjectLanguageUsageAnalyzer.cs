using PMD.App.Domain.ProjectStates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PMD.App.Application.Analytics;

public static class ProjectLanguageUsageAnalyzer
{
    private static readonly IReadOnlyDictionary<string, LanguageDefinition>
        LanguagesByExtension =
            new Dictionary<string, LanguageDefinition>(
                StringComparer.OrdinalIgnoreCase)
            {
                [".cs"] = new("C#", "#178600"),
                [".csx"] = new("C#", "#178600"),
                [".razor"] = new("HTML", "#e34c26"),
                [".html"] = new("HTML", "#e34c26"),
                [".htm"] = new("HTML", "#e34c26"),
                [".css"] = new("CSS", "#663399"),
                [".scss"] = new("SCSS", "#c6538c"),
                [".sass"] = new("Sass", "#a53b70"),
                [".less"] = new("Less", "#1d365d"),
                [".js"] = new("JavaScript", "#f1e05a"),
                [".jsx"] = new("JavaScript", "#f1e05a"),
                [".mjs"] = new("JavaScript", "#f1e05a"),
                [".cjs"] = new("JavaScript", "#f1e05a"),
                [".ts"] = new("TypeScript", "#3178c6"),
                [".tsx"] = new("TypeScript", "#3178c6"),
                [".vue"] = new("Vue", "#41b883"),
                [".svelte"] = new("Svelte", "#ff3e00"),
                [".astro"] = new("Astro", "#ff5a03"),
                [".xaml"] = new("XAML", "#0c54c2"),
                [".py"] = new("Python", "#3572a5"),
                [".pyw"] = new("Python", "#3572a5"),
                [".ipynb"] = new("Jupyter Notebook", "#da5b0b"),
                [".java"] = new("Java", "#b07219"),
                [".kt"] = new("Kotlin", "#a97bff"),
                [".kts"] = new("Kotlin", "#a97bff"),
                [".groovy"] = new("Groovy", "#4298b8"),
                [".c"] = new("C", "#555555"),
                [".h"] = new("C", "#555555"),
                [".cpp"] = new("C++", "#f34b7d"),
                [".cxx"] = new("C++", "#f34b7d"),
                [".cc"] = new("C++", "#f34b7d"),
                [".hpp"] = new("C++", "#f34b7d"),
                [".hh"] = new("C++", "#f34b7d"),
                [".hxx"] = new("C++", "#f34b7d"),
                [".m"] = new("Objective-C", "#438eff"),
                [".mm"] = new("Objective-C++", "#6866fb"),
                [".rs"] = new("Rust", "#dea584"),
                [".go"] = new("Go", "#00add8"),
                [".php"] = new("PHP", "#4f5d95"),
                [".phtml"] = new("PHP", "#4f5d95"),
                [".rb"] = new("Ruby", "#701516"),
                [".erb"] = new("Ruby", "#701516"),
                [".swift"] = new("Swift", "#f05138"),
                [".sh"] = new("Shell", "#89e051"),
                [".bash"] = new("Shell", "#89e051"),
                [".zsh"] = new("Shell", "#89e051"),
                [".fish"] = new("Shell", "#89e051"),
                [".ps1"] = new("PowerShell", "#012456"),
                [".psm1"] = new("PowerShell", "#012456"),
                [".bat"] = new("Batchfile", "#c1f12e"),
                [".cmd"] = new("Batchfile", "#c1f12e"),
                [".sql"] = new("SQL", "#e38c00"),
                [".dart"] = new("Dart", "#00b4ab"),
                [".lua"] = new("Lua", "#000080"),
                [".fs"] = new("F#", "#b845fc"),
                [".fsi"] = new("F#", "#b845fc"),
                [".fsx"] = new("F#", "#b845fc"),
                [".vb"] = new("Visual Basic", "#945db7"),
                [".scala"] = new("Scala", "#c22d40"),
                [".clj"] = new("Clojure", "#db5855"),
                [".cljs"] = new("Clojure", "#db5855"),
                [".hs"] = new("Haskell", "#5e5086"),
                [".elm"] = new("Elm", "#60b5cc"),
                [".ex"] = new("Elixir", "#6e4a7e"),
                [".exs"] = new("Elixir", "#6e4a7e"),
                [".erl"] = new("Erlang", "#b83998"),
                [".hrl"] = new("Erlang", "#b83998"),
                [".gd"] = new("GDScript", "#355570"),
                [".shader"] = new("ShaderLab", "#8e7cc3"),
                [".hlsl"] = new("HLSL", "#aace60"),
                [".glsl"] = new("GLSL", "#5686a5"),
                [".cginc"] = new("ShaderLab", "#8e7cc3"),
                [".tf"] = new("HCL", "#844fba"),
                [".tfvars"] = new("HCL", "#844fba"),
                [".hcl"] = new("HCL", "#844fba"),
                [".bicep"] = new("Bicep", "#519aba")
            };

    private static readonly IReadOnlyDictionary<string, LanguageDefinition>
        LanguagesByFileName =
            new Dictionary<string, LanguageDefinition>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["Dockerfile"] = new("Dockerfile", "#384d54"),
                ["Makefile"] = new("Makefile", "#427819")
            };

    private static readonly string[] IgnoredPathSegments =
    [
        "/wwwroot/lib/",
        "/vendor/",
        "/vendors/",
        "/third_party/",
        "/third-party/",
        "/packages/",
        "/node_modules/",
        "/dist/",
        "/coverage/"
    ];

    public static IReadOnlyList<ProjectLanguageUsage> Analyze(
        IEnumerable<ProjectStateFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        Dictionary<string, MutableLanguageUsage> languageUsages =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (ProjectStateFile file in files)
        {
            if (ShouldIgnore(file) ||
                !TryGetLanguage(file, out LanguageDefinition language))
            {
                continue;
            }

            if (!languageUsages.TryGetValue(
                    language.Name,
                    out MutableLanguageUsage? usage))
            {
                usage = new MutableLanguageUsage(language);
                languageUsages.Add(language.Name, usage);
            }

            usage.SizeInBytes += Math.Max(0, file.SizeInBytes);
            usage.FileCount++;
        }

        return CreateResult(languageUsages.Values);
    }

    public static IReadOnlyList<ProjectLanguageUsage> Combine(
        IEnumerable<IReadOnlyList<ProjectLanguageUsage>> languageUsageSets)
    {
        ArgumentNullException.ThrowIfNull(languageUsageSets);

        Dictionary<string, MutableLanguageUsage> combinedUsages =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (IReadOnlyList<ProjectLanguageUsage> languageUsageSet
                 in languageUsageSets)
        {
            foreach (ProjectLanguageUsage languageUsage in languageUsageSet)
            {
                LanguageDefinition language = new(
                    languageUsage.Name,
                    languageUsage.Color);

                if (!combinedUsages.TryGetValue(
                        language.Name,
                        out MutableLanguageUsage? combinedUsage))
                {
                    combinedUsage = new MutableLanguageUsage(language);
                    combinedUsages.Add(language.Name, combinedUsage);
                }

                combinedUsage.SizeInBytes +=
                    Math.Max(0, languageUsage.SizeInBytes);
                combinedUsage.FileCount +=
                    Math.Max(0, languageUsage.FileCount);
            }
        }

        return CreateResult(combinedUsages.Values);
    }

    private static IReadOnlyList<ProjectLanguageUsage> CreateResult(
        IEnumerable<MutableLanguageUsage> mutableUsages)
    {
        List<MutableLanguageUsage> usages = mutableUsages.ToList();
        long totalSizeInBytes = usages.Sum(usage => usage.SizeInBytes);
        int totalFileCount = usages.Sum(usage => usage.FileCount);
        bool useFileCount = totalSizeInBytes <= 0;

        double totalWeight = useFileCount
            ? totalFileCount
            : totalSizeInBytes;

        if (totalWeight <= 0)
        {
            return Array.Empty<ProjectLanguageUsage>();
        }

        return usages
            .Select(usage => new ProjectLanguageUsage
            {
                Name = usage.Language.Name,
                Color = usage.Language.Color,
                SizeInBytes = usage.SizeInBytes,
                FileCount = usage.FileCount,
                Percentage = (useFileCount
                        ? usage.FileCount
                        : usage.SizeInBytes) /
                    totalWeight * 100d
            })
            .OrderByDescending(usage => usage.Percentage)
            .ThenBy(usage => usage.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool TryGetLanguage(
        ProjectStateFile file,
        out LanguageDefinition language)
    {
        if (LanguagesByFileName.TryGetValue(
                file.FileName,
                out LanguageDefinition? fileNameLanguage))
        {
            language = fileNameLanguage;
            return true;
        }

        string extension = file.Extension?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = Path.GetExtension(file.FileName);
        }

        if (!extension.StartsWith('.'))
        {
            extension = $".{extension}";
        }

        if (LanguagesByExtension.TryGetValue(
                extension,
                out LanguageDefinition? extensionLanguage))
        {
            language = extensionLanguage;
            return true;
        }

        language = default!;
        return false;
    }

    private static bool ShouldIgnore(ProjectStateFile file)
    {
        string relativePath = "/" + (file.RelativePath ?? string.Empty)
            .Replace('\\', '/')
            .TrimStart('/')
            .ToLowerInvariant();

        if (IgnoredPathSegments.Any(segment =>
                relativePath.Contains(segment, StringComparison.Ordinal)))
        {
            return true;
        }

        string fileName = file.FileName ?? string.Empty;

        return fileName.EndsWith(".min.js", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".min.css", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record LanguageDefinition(
        string Name,
        string Color);

    private sealed class MutableLanguageUsage
    {
        public MutableLanguageUsage(LanguageDefinition language)
        {
            Language = language;
        }

        public LanguageDefinition Language { get; }

        public long SizeInBytes { get; set; }

        public int FileCount { get; set; }
    }
}
