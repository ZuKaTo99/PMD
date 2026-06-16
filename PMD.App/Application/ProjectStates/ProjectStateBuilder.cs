using System;
using System.Linq;
using PMD.App.Domain.ProjectStates;
using PMD.App.Domain.Scanner;

namespace PMD.App.Application.ProjectStates;

public static class ProjectStateBuilder
{
    public static ProjectState CreateFromScanResult(
        string projectName,
        ProjectFolderScanResult scanResult)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);
        ArgumentNullException.ThrowIfNull(scanResult);

        var projectStateId = Guid.NewGuid();

        return new ProjectState
        {
            Id = projectStateId,
            ProjectName = projectName.Trim(),
            RootPath = scanResult.RootPath,
            CreatedAt = DateTime.Now,
            ScannedAt = scanResult.ScannedAt,
            ScanDuration = TimeSpan.Zero,
            FileCount = scanResult.FileCount,
            ScannedFolderCount = 0,
            IgnoredFolderCount = 0,
            WarningCount = 0,
            TotalSizeInBytes = scanResult.TotalSizeInBytes,

            Files = scanResult.Files
                .Select(file => new ProjectStateFile
                {
                    ProjectStateId = projectStateId,
                    RelativePath = file.RelativePath,
                    FileName = file.FileName,
                    Extension = file.Extension,
                    SizeInBytes = file.SizeInBytes,
                    LastChangedAt = file.LastChangedAt
                })
                .ToList()
        };
    }
}