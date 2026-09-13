using PMD.App.Domain.ProjectVersioning;

namespace PMD.App.Application.ProjectVersioning;

public interface IProjectVersionStore
{
    Task<ProjectVersionSnapshot> CreateSnapshotAsync(
        Guid projectId,
        string projectRootPath,
        IReadOnlyList<ProjectVersionFileSource> files,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default);

    Task<string?> GetLatestCommitIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<bool> ContainsObjectAsync(
        Guid projectId,
        string objectId,
        CancellationToken cancellationToken = default);

    Task<byte[]> ReadObjectAsync(
        Guid projectId,
        string objectId,
        CancellationToken cancellationToken = default);
}