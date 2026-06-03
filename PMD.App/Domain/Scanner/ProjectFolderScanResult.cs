using System;
using System.Collections.Generic;
using System.Text;

namespace PMD.App.Domain.Scanner;

public sealed class ProjectFolderScanResult
{
    public string RootPath { get; init; } = string.Empty;

    public DateTime ScannedAt { get; init; } = DateTime.Now;

    public List<ProjectFileEntry> Files { get; init; } = new();

    public int FileCount => Files.Count;

    public long TotalSizeInBytes => Files.Sum(file => file.SizeInBytes);
}