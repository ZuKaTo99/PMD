using LibGit2Sharp;
using PMD.App.Application.ProjectVersioning;
using PMD.App.Domain.ProjectVersioning;

namespace PMD.App.Infrastructure.ProjectVersioning;

public sealed class LibGitProjectVersionStore
    : IProjectVersionStore
{
    private const string SnapshotReferenceName =
        "refs/heads/pmd-snapshots";

    private readonly IProjectVersionStorePathProvider pathProvider;

    private readonly SemaphoreSlim operationSemaphore =
        new(1, 1);

    public LibGitProjectVersionStore(
        IProjectVersionStorePathProvider pathProvider)
    {
        ArgumentNullException.ThrowIfNull(pathProvider);

        this.pathProvider = pathProvider;
    }

    public async Task<ProjectVersionSnapshot> CreateSnapshotAsync(
        Guid projectId,
        string projectRootPath,
        IReadOnlyList<ProjectVersionFileSource> files,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        ValidateProjectId(projectId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            projectRootPath);

        ArgumentNullException.ThrowIfNull(files);

        await operationSemaphore
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            return await Task.Run(
                    () => CreateSnapshot(
                        projectId,
                        projectRootPath,
                        files,
                        createdAt,
                        cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            operationSemaphore.Release();
        }
    }

    public async Task<string?> GetLatestCommitIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        ValidateProjectId(projectId);

        await operationSemaphore
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            return await Task.Run(
                    () => GetLatestCommitId(projectId),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            operationSemaphore.Release();
        }
    }

    public async Task<bool> ContainsObjectAsync(
        Guid projectId,
        string objectId,
        CancellationToken cancellationToken = default)
    {
        ValidateProjectId(projectId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            objectId);

        await operationSemaphore
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            return await Task.Run(
                    () => ContainsObject(
                        projectId,
                        objectId),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            operationSemaphore.Release();
        }
    }

    public async Task<byte[]> ReadObjectAsync(
        Guid projectId,
        string objectId,
        CancellationToken cancellationToken = default)
    {
        ValidateProjectId(projectId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            objectId);

        await operationSemaphore
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            return await Task.Run(
                    () => ReadObject(
                        projectId,
                        objectId,
                        cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            operationSemaphore.Release();
        }
    }

    private ProjectVersionSnapshot CreateSnapshot(
        Guid projectId,
        string projectRootPath,
        IReadOnlyList<ProjectVersionFileSource> files,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        string normalizedProjectRootPath =
            Path.GetFullPath(projectRootPath);

        if (!Directory.Exists(
                normalizedProjectRootPath))
        {
            throw new DirectoryNotFoundException(
                "Der Projektordner wurde nicht gefunden: " +
                normalizedProjectRootPath);
        }

        string repositoryPath =
            GetRepositoryPath(projectId);

        if (IsSameOrChildPath(
                normalizedProjectRootPath,
                repositoryPath))
        {
            throw new InvalidOperationException(
                "Der interne PMD-Versionsspeicher darf nicht im " +
                "Benutzerprojekt liegen.");
        }

        IReadOnlyList<NormalizedVersionFile>
            normalizedFiles =
                NormalizeFiles(files);

        EnsureRepository(repositoryPath);

        using var repository =
            new Repository(repositoryPath);

        Commit? parentCommit =
            GetLatestCommit(repository);

        var treeDefinition =
            new TreeDefinition();

        var storedFiles =
            new List<ProjectVersionFileObject>(
                normalizedFiles.Count);

        foreach (NormalizedVersionFile file
                 in normalizedFiles)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            Blob blob = GetOrCreateBlob(
                repository,
                normalizedProjectRootPath,
                file);

            Mode mode = file.IsExecutable
                ? Mode.ExecutableFile
                : Mode.NonExecutableFile;

            treeDefinition.Add(
                file.RelativePath,
                blob,
                mode);

            storedFiles.Add(
                new ProjectVersionFileObject
                {
                    RelativePath =
                        file.RelativePath,
                    ObjectId =
                        blob.Id.Sha,
                    IsExecutable =
                        file.IsExecutable
                });
        }

        cancellationToken
            .ThrowIfCancellationRequested();

        Tree tree =
            repository.ObjectDatabase.CreateTree(
                treeDefinition);

        var signature =
            new Signature(
                "PMD",
                "snapshot@pmd.local",
                createdAt);

        IEnumerable<Commit> parents =
            parentCommit is null
                ? Array.Empty<Commit>()
                : new[] { parentCommit };

        Commit commit =
            repository.ObjectDatabase.CreateCommit(
                signature,
                signature,
                $"PMD snapshot {createdAt:O}",
                tree,
                parents,
                prettifyMessage: true);

        repository.Refs.Add(
            SnapshotReferenceName,
            commit.Id,
            "PMD snapshot gespeichert",
            allowOverwrite: true);

        return new ProjectVersionSnapshot
        {
            ProjectId = projectId,
            CommitId = commit.Id.Sha,
            TreeId = tree.Id.Sha,
            ParentCommitId =
                parentCommit?.Id.Sha ??
                string.Empty,
            CreatedAt = createdAt,
            Files = storedFiles
        };
    }

    private string? GetLatestCommitId(
        Guid projectId)
    {
        string repositoryPath =
            GetRepositoryPath(projectId);

        if (!Repository.IsValid(
                repositoryPath))
        {
            return null;
        }

        using var repository =
            new Repository(repositoryPath);

        return GetLatestCommit(repository)?
            .Id
            .Sha;
    }

    private bool ContainsObject(
        Guid projectId,
        string objectId)
    {
        string repositoryPath =
            GetRepositoryPath(projectId);

        if (!Repository.IsValid(
                repositoryPath) ||
            !TryParseObjectId(
                objectId,
                out ObjectId parsedObjectId))
        {
            return false;
        }

        using var repository =
            new Repository(repositoryPath);

        return repository.Lookup<Blob>(
            parsedObjectId) is not null;
    }

    private byte[] ReadObject(
        Guid projectId,
        string objectId,
        CancellationToken cancellationToken)
    {
        string repositoryPath =
            GetRepositoryPath(projectId);

        if (!Repository.IsValid(
                repositoryPath))
        {
            throw new DirectoryNotFoundException(
                "Für das Projekt wurde noch kein interner " +
                "PMD-Versionsspeicher angelegt.");
        }

        if (!TryParseObjectId(
                objectId,
                out ObjectId parsedObjectId))
        {
            throw new ArgumentException(
                "Die angegebene Git-Objekt-ID ist ungültig.",
                nameof(objectId));
        }

        using var repository =
            new Repository(repositoryPath);

        Blob? blob = repository.Lookup<Blob>(
            parsedObjectId);

        if (blob is null)
        {
            throw new KeyNotFoundException(
                "Das Git-Objekt wurde nicht gefunden: " +
                objectId);
        }

        using Stream contentStream =
            blob.GetContentStream();

        using var memoryStream =
            new MemoryStream();

        contentStream.CopyTo(memoryStream);

        cancellationToken
            .ThrowIfCancellationRequested();

        return memoryStream.ToArray();
    }

    private static Blob GetOrCreateBlob(
        Repository repository,
        string projectRootPath,
        NormalizedVersionFile file)
    {
        if (!string.IsNullOrWhiteSpace(
                file.ExistingObjectId))
        {
            if (!TryParseObjectId(
                    file.ExistingObjectId,
                    out ObjectId existingObjectId))
            {
                throw new ArgumentException(
                    "Die Git-Objekt-ID für '" +
                    file.RelativePath +
                    "' ist ungültig.");
            }

            Blob? existingBlob =
                repository.Lookup<Blob>(
                    existingObjectId);

            if (existingBlob is null)
            {
                throw new InvalidOperationException(
                    "Das wiederzuverwendende Git-Objekt für '" +
                    file.RelativePath +
                    "' wurde nicht gefunden.");
            }

            return existingBlob;
        }

        string sourcePath = Path.GetFullPath(
            Path.Combine(
                projectRootPath,
                file.RelativePath.Replace(
                    '/',
                    Path.DirectorySeparatorChar)));

        if (!IsSameOrChildPath(
                projectRootPath,
                sourcePath))
        {
            throw new InvalidOperationException(
                "Die Datei liegt außerhalb des Projektordners: " +
                file.RelativePath);
        }

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException(
                "Die Projektdatei wurde nicht gefunden.",
                sourcePath);
        }

        return repository.ObjectDatabase.CreateBlob(
            sourcePath);
    }

    private static IReadOnlyList<NormalizedVersionFile>
        NormalizeFiles(
            IReadOnlyList<ProjectVersionFileSource> files)
    {
        var normalizedFiles =
            new List<NormalizedVersionFile>(
                files.Count);

        var knownPaths =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (ProjectVersionFileSource file
                 in files)
        {
            ArgumentNullException.ThrowIfNull(file);

            string normalizedPath =
                NormalizeRelativePath(
                    file.RelativePath);

            if (!knownPaths.Add(normalizedPath))
            {
                throw new InvalidOperationException(
                    "Der Dateipfad ist im Projektstand doppelt " +
                    "vorhanden: " +
                    normalizedPath);
            }

            normalizedFiles.Add(
                new NormalizedVersionFile(
                    normalizedPath,
                    (file.ExistingObjectId ??
                     string.Empty).Trim(),
                    file.IsExecutable));
        }

        return normalizedFiles
            .OrderBy(
                file => file.RelativePath,
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeRelativePath(
        string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            relativePath);

        if (Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException(
                "Der Dateipfad muss relativ zum Projektordner sein.",
                nameof(relativePath));
        }

        string normalizedPath =
            relativePath.Replace('\\', '/');

        string[] pathSegments =
            normalizedPath.Split('/');

        if (pathSegments.Any(segment =>
                string.IsNullOrWhiteSpace(segment) ||
                segment == "." ||
                segment == ".."))
        {
            throw new ArgumentException(
                "Der relative Dateipfad enthält einen ungültigen " +
                "Pfadabschnitt.",
                nameof(relativePath));
        }

        return string.Join(
            '/',
            pathSegments);
    }

    private string GetRepositoryPath(
        Guid projectId)
    {
        return Path.GetFullPath(
            pathProvider.GetProjectVersionStorePath(
                projectId));
    }

    private static void EnsureRepository(
        string repositoryPath)
    {
        if (Repository.IsValid(repositoryPath))
        {
            return;
        }

        if (Directory.Exists(repositoryPath) &&
            Directory
                .EnumerateFileSystemEntries(
                    repositoryPath)
                .Any())
        {
            throw new InvalidOperationException(
                "Der vorgesehene PMD-Versionsspeicher enthält " +
                "bereits Dateien, ist aber kein gültiges " +
                "Git-Repository.");
        }

        string? parentDirectory =
            Path.GetDirectoryName(repositoryPath);

        if (string.IsNullOrWhiteSpace(
                parentDirectory))
        {
            throw new InvalidOperationException(
                "Der Ordner für den PMD-Versionsspeicher " +
                "konnte nicht bestimmt werden.");
        }

        Directory.CreateDirectory(
            parentDirectory);

        Repository.Init(
            repositoryPath,
            isBare: true);
    }

    private static Commit? GetLatestCommit(
        Repository repository)
    {
        Reference? snapshotReference =
            repository.Refs[
                SnapshotReferenceName];

        if (snapshotReference is null)
        {
            return null;
        }

        DirectReference directReference =
            snapshotReference
                .ResolveToDirectReference();

        return repository.Lookup<Commit>(
            directReference.TargetIdentifier);
    }

    private static bool TryParseObjectId(
        string objectId,
        out ObjectId parsedObjectId)
    {
        if (string.IsNullOrWhiteSpace(objectId))
        {
            parsedObjectId = null!;
            return false;
        }

        return ObjectId.TryParse(
            objectId.Trim(),
            out parsedObjectId);
    }

    private static bool IsSameOrChildPath(
        string parentPath,
        string candidatePath)
    {
        string relativePath =
            Path.GetRelativePath(
                Path.GetFullPath(parentPath),
                Path.GetFullPath(candidatePath));

        if (relativePath == ".")
        {
            return true;
        }

        return !Path.IsPathRooted(relativePath) &&
            relativePath != ".." &&
            !relativePath.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal) &&
            !relativePath.StartsWith(
                $"..{Path.AltDirectorySeparatorChar}",
                StringComparison.Ordinal);
    }

    private static void ValidateProjectId(
        Guid projectId)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException(
                "Die Projekt-ID darf nicht leer sein.",
                nameof(projectId));
        }
    }

    private sealed record NormalizedVersionFile(
        string RelativePath,
        string ExistingObjectId,
        bool IsExecutable);
}