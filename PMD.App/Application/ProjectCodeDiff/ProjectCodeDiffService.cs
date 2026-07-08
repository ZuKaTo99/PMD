using PMD.App.Domain.ProjectChanges;
using PMD.App.Domain.ProjectCodeDiff;
using PMD.App.Domain.ProjectStates;

namespace PMD.App.Application.ProjectCodeDiff;

public sealed class ProjectCodeDiffService : IProjectCodeDiffService
{
    private const int ContextLineCount = 2;
    private const int MaxUnchangedLinesBetweenChanges = 2;
    private const int MaxComparableLineCount = 600;
    private const int MaxComparableLineProduct = 250_000;

    public ProjectCodeDiffResult BuildDiff(ProjectFileChange fileChange)
    {
        ArgumentNullException.ThrowIfNull(fileChange);

        try
        {
            return fileChange.ChangeKind switch
            {
                ProjectFileChangeKind.Added => BuildAddedFileDiff(fileChange),
                ProjectFileChangeKind.Removed => BuildRemovedFileDiff(fileChange),
                ProjectFileChangeKind.Modified => BuildModifiedFileDiff(fileChange),
                ProjectFileChangeKind.Unchanged => BuildUnchangedFileDiff(fileChange),
                _ => BuildUnsupportedDiff(fileChange)
            };
        }
        catch
        {
            return BuildMessageResult(
                fileChange,
                "Der Code-Vergleich konnte für diese Datei nicht erstellt werden. PMD zeigt die Datei deshalb nur als geändert an.");
        }
    }

    private static ProjectCodeDiffResult BuildAddedFileDiff(ProjectFileChange fileChange)
    {
        if (!HasTextSnapshot(fileChange.LatestFile))
        {
            return BuildMessageResult(
                fileChange,
                "Für diese neue Datei ist kein gespeicherter Textauszug vorhanden.");
        }

        IReadOnlyList<string> latestLines = SplitLines(
            fileChange.LatestFile!.TextSnapshotContent);

        return new ProjectCodeDiffResult
        {
            RelativePath = fileChange.RelativePath,
            LatestSnapshotWasTruncated = fileChange.LatestFile.TextSnapshotWasTruncated,
            Sections =
            [
                new ProjectCodeChangeSection
                {
                    ChangeKind = ProjectCodeChangeKind.Added,
                    LatestStartLineNumber = 1,
                    LatestLines = latestLines
                }
            ]
        };
    }

    private static ProjectCodeDiffResult BuildRemovedFileDiff(ProjectFileChange fileChange)
    {
        if (!HasTextSnapshot(fileChange.PreviousFile))
        {
            return BuildMessageResult(
                fileChange,
                "Für diese entfernte Datei ist kein gespeicherter Textauszug vorhanden.");
        }

        IReadOnlyList<string> previousLines = SplitLines(
            fileChange.PreviousFile!.TextSnapshotContent);

        return new ProjectCodeDiffResult
        {
            RelativePath = fileChange.RelativePath,
            PreviousSnapshotWasTruncated = fileChange.PreviousFile.TextSnapshotWasTruncated,
            Sections =
            [
                new ProjectCodeChangeSection
                {
                    ChangeKind = ProjectCodeChangeKind.Removed,
                    PreviousStartLineNumber = 1,
                    PreviousLines = previousLines
                }
            ]
        };
    }

    private static ProjectCodeDiffResult BuildModifiedFileDiff(ProjectFileChange fileChange)
    {
        if (!HasTextSnapshot(fileChange.PreviousFile) ||
            !HasTextSnapshot(fileChange.LatestFile))
        {
            return BuildMessageResult(
                fileChange,
                "Für diese Datei gibt es noch keinen gespeicherten Textauszug aus beiden Prüfungen.");
        }

        IReadOnlyList<string> previousLines = SplitLines(
            fileChange.PreviousFile!.TextSnapshotContent);

        IReadOnlyList<string> latestLines = SplitLines(
            fileChange.LatestFile!.TextSnapshotContent);

        if (!CanBuildLineDiff(previousLines, latestLines))
        {
            return new ProjectCodeDiffResult
            {
                RelativePath = fileChange.RelativePath,
                Message = "Der gespeicherte Textauszug ist für einen direkten Abschnittsvergleich zu groß. PMD zeigt diese Datei deshalb nur als geändert an.",
                PreviousSnapshotWasTruncated = fileChange.PreviousFile.TextSnapshotWasTruncated,
                LatestSnapshotWasTruncated = fileChange.LatestFile.TextSnapshotWasTruncated
            };
        }

        IReadOnlyList<DiffOperation> operations = BuildDiffOperations(
            previousLines,
            latestLines);

        IReadOnlyList<ProjectCodeChangeSection> sections =
            BuildChangeSections(operations);

        string? message = sections.Count == 0
            ? "Im gespeicherten Textauszug wurde kein Abschnittsunterschied gefunden. Die Änderung kann außerhalb des gespeicherten Ausschnitts liegen."
            : null;

        return new ProjectCodeDiffResult
        {
            RelativePath = fileChange.RelativePath,
            Message = message,
            PreviousSnapshotWasTruncated = fileChange.PreviousFile.TextSnapshotWasTruncated,
            LatestSnapshotWasTruncated = fileChange.LatestFile.TextSnapshotWasTruncated,
            Sections = sections
        };
    }

    private static ProjectCodeDiffResult BuildUnchangedFileDiff(ProjectFileChange fileChange)
    {
        return BuildMessageResult(
            fileChange,
            "Diese Datei ist unverändert.");
    }

    private static ProjectCodeDiffResult BuildUnsupportedDiff(ProjectFileChange fileChange)
    {
        return BuildMessageResult(
            fileChange,
            "Für diese Änderung kann kein Code-Vergleich erstellt werden.");
    }

    private static ProjectCodeDiffResult BuildMessageResult(
        ProjectFileChange fileChange,
        string message)
    {
        return new ProjectCodeDiffResult
        {
            RelativePath = fileChange.RelativePath,
            Message = message,
            PreviousSnapshotWasTruncated =
                fileChange.PreviousFile?.TextSnapshotWasTruncated ?? false,
            LatestSnapshotWasTruncated =
                fileChange.LatestFile?.TextSnapshotWasTruncated ?? false
        };
    }

    private static bool HasTextSnapshot(ProjectStateFile? file)
    {
        return !string.IsNullOrWhiteSpace(file?.TextSnapshotContent);
    }

    private static IReadOnlyList<string> SplitLines(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return Array.Empty<string>();
        }

        return content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
    }

    private static bool CanBuildLineDiff(
        IReadOnlyList<string> previousLines,
        IReadOnlyList<string> latestLines)
    {
        if (previousLines.Count > MaxComparableLineCount ||
            latestLines.Count > MaxComparableLineCount)
        {
            return false;
        }

        long lineProduct =
            (long)previousLines.Count * latestLines.Count;

        return lineProduct <= MaxComparableLineProduct;
    }

    private static IReadOnlyList<DiffOperation> BuildDiffOperations(
        IReadOnlyList<string> previousLines,
        IReadOnlyList<string> latestLines)
    {
        int[,] lcs = BuildLongestCommonSubsequenceTable(
            previousLines,
            latestLines);

        List<DiffOperation> operations = [];

        int previousIndex = 0;
        int latestIndex = 0;

        while (previousIndex < previousLines.Count &&
               latestIndex < latestLines.Count)
        {
            if (string.Equals(
                    previousLines[previousIndex],
                    latestLines[latestIndex],
                    StringComparison.Ordinal))
            {
                operations.Add(DiffOperation.Unchanged(
                    previousLines[previousIndex],
                    previousIndex + 1,
                    latestIndex + 1));

                previousIndex++;
                latestIndex++;
            }
            else if (lcs[previousIndex + 1, latestIndex] >=
                     lcs[previousIndex, latestIndex + 1])
            {
                operations.Add(DiffOperation.Removed(
                    previousLines[previousIndex],
                    previousIndex + 1));

                previousIndex++;
            }
            else
            {
                operations.Add(DiffOperation.Added(
                    latestLines[latestIndex],
                    latestIndex + 1));

                latestIndex++;
            }
        }

        while (previousIndex < previousLines.Count)
        {
            operations.Add(DiffOperation.Removed(
                previousLines[previousIndex],
                previousIndex + 1));

            previousIndex++;
        }

        while (latestIndex < latestLines.Count)
        {
            operations.Add(DiffOperation.Added(
                latestLines[latestIndex],
                latestIndex + 1));

            latestIndex++;
        }

        return operations;
    }

    private static int[,] BuildLongestCommonSubsequenceTable(
        IReadOnlyList<string> previousLines,
        IReadOnlyList<string> latestLines)
    {
        int[,] table = new int[previousLines.Count + 1, latestLines.Count + 1];

        for (int previousIndex = previousLines.Count - 1;
             previousIndex >= 0;
             previousIndex--)
        {
            for (int latestIndex = latestLines.Count - 1;
                 latestIndex >= 0;
                 latestIndex--)
            {
                if (string.Equals(
                        previousLines[previousIndex],
                        latestLines[latestIndex],
                        StringComparison.Ordinal))
                {
                    table[previousIndex, latestIndex] =
                        table[previousIndex + 1, latestIndex + 1] + 1;
                }
                else
                {
                    table[previousIndex, latestIndex] = Math.Max(
                        table[previousIndex + 1, latestIndex],
                        table[previousIndex, latestIndex + 1]);
                }
            }
        }

        return table;
    }

    private static IReadOnlyList<ProjectCodeChangeSection> BuildChangeSections(
        IReadOnlyList<DiffOperation> operations)
    {
        List<ProjectCodeChangeSection> sections = [];

        int index = 0;

        while (index < operations.Count)
        {
            if (operations[index].Kind == DiffOperationKind.Unchanged)
            {
                index++;
                continue;
            }

            int changeStartIndex = index;
            int changeEndIndex = FindGroupedChangeEndIndex(
                operations,
                changeStartIndex);

            sections.Add(BuildChangeSection(
                operations,
                changeStartIndex,
                changeEndIndex));

            index = changeEndIndex + 1;
        }

        return sections;
    }

    private static int FindGroupedChangeEndIndex(
        IReadOnlyList<DiffOperation> operations,
        int changeStartIndex)
    {
        int index = changeStartIndex;
        int latestChangeIndex = changeStartIndex;
        int unchangedLineCount = 0;

        while (index < operations.Count)
        {
            if (operations[index].Kind == DiffOperationKind.Unchanged)
            {
                unchangedLineCount++;

                if (unchangedLineCount > MaxUnchangedLinesBetweenChanges)
                {
                    break;
                }
            }
            else
            {
                latestChangeIndex = index;
                unchangedLineCount = 0;
            }

            index++;
        }

        return latestChangeIndex;
    }

    private static ProjectCodeChangeSection BuildChangeSection(
        IReadOnlyList<DiffOperation> operations,
        int changeStartIndex,
        int changeEndIndex)
    {
        IReadOnlyList<DiffOperation> changedOperations = operations
            .Skip(changeStartIndex)
            .Take(changeEndIndex - changeStartIndex + 1)
            .ToList();

        bool hasRemovedLines = changedOperations.Any(
            operation => operation.Kind == DiffOperationKind.Removed);

        bool hasAddedLines = changedOperations.Any(
            operation => operation.Kind == DiffOperationKind.Added);

        ProjectCodeChangeKind changeKind = GetSectionChangeKind(
            hasRemovedLines,
            hasAddedLines);

        return new ProjectCodeChangeSection
        {
            ChangeKind = changeKind,
            PreviousStartLineNumber = changedOperations
                .FirstOrDefault(operation => operation.PreviousLineNumber.HasValue)
                ?.PreviousLineNumber,
            LatestStartLineNumber = changedOperations
                .FirstOrDefault(operation => operation.LatestLineNumber.HasValue)
                ?.LatestLineNumber,
            ContextBefore = GetContextBefore(operations, changeStartIndex),
            PreviousLines = changedOperations
                .Where(operation => operation.Kind == DiffOperationKind.Removed)
                .Select(operation => operation.Text)
                .ToList(),
            LatestLines = changedOperations
                .Where(operation => operation.Kind == DiffOperationKind.Added)
                .Select(operation => operation.Text)
                .ToList(),
            ContextAfter = GetContextAfter(operations, changeEndIndex)
        };
    }

    private static ProjectCodeChangeKind GetSectionChangeKind(
        bool hasRemovedLines,
        bool hasAddedLines)
    {
        if (hasRemovedLines && hasAddedLines)
        {
            return ProjectCodeChangeKind.Modified;
        }

        if (hasAddedLines)
        {
            return ProjectCodeChangeKind.Added;
        }

        return ProjectCodeChangeKind.Removed;
    }

    private static IReadOnlyList<string> GetContextBefore(
        IReadOnlyList<DiffOperation> operations,
        int changeStartIndex)
    {
        return operations
            .Take(changeStartIndex)
            .Reverse()
            .Where(operation => operation.Kind == DiffOperationKind.Unchanged)
            .Take(ContextLineCount)
            .Reverse()
            .Select(operation => operation.Text)
            .ToList();
    }

    private static IReadOnlyList<string> GetContextAfter(
        IReadOnlyList<DiffOperation> operations,
        int changeEndIndex)
    {
        return operations
            .Skip(changeEndIndex + 1)
            .Where(operation => operation.Kind == DiffOperationKind.Unchanged)
            .Take(ContextLineCount)
            .Select(operation => operation.Text)
            .ToList();
    }

    private sealed class DiffOperation
    {
        private DiffOperation(
            DiffOperationKind kind,
            string text,
            int? previousLineNumber,
            int? latestLineNumber)
        {
            Kind = kind;
            Text = text;
            PreviousLineNumber = previousLineNumber;
            LatestLineNumber = latestLineNumber;
        }

        public DiffOperationKind Kind { get; }

        public string Text { get; }

        public int? PreviousLineNumber { get; }

        public int? LatestLineNumber { get; }

        public static DiffOperation Unchanged(
            string text,
            int previousLineNumber,
            int latestLineNumber)
        {
            return new DiffOperation(
                DiffOperationKind.Unchanged,
                text,
                previousLineNumber,
                latestLineNumber);
        }

        public static DiffOperation Removed(
            string text,
            int previousLineNumber)
        {
            return new DiffOperation(
                DiffOperationKind.Removed,
                text,
                previousLineNumber,
                null);
        }

        public static DiffOperation Added(
            string text,
            int latestLineNumber)
        {
            return new DiffOperation(
                DiffOperationKind.Added,
                text,
                null,
                latestLineNumber);
        }
    }

    private enum DiffOperationKind
    {
        Unchanged,
        Removed,
        Added
    }
}