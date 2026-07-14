namespace PMD.App.Domain.Scanner;

public sealed record ProjectFolderScanProgress(
    int FoundFileCount,
    int ProcessedFileCount,
    int ScannedFolderCount,
    int IgnoredFolderCount,
    string CurrentFolder,
    string CurrentFile);