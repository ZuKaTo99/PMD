using System;
using System.Collections.Generic;
using System.Text;

namespace PMD.App.Domain.Scanner;

public sealed class ProjectFileEntry
{
    public string FullPath { get; init; } = string.Empty;

    public string RelativePath { get; init; } = string.Empty;

    public string FileName {  get; init; } = string.Empty;

    public string Extension {  get; init; } = string.Empty;

    public long SizeInBytes {  get; init; }

    public DateTime LastChangedAt {  get; init; }

}

