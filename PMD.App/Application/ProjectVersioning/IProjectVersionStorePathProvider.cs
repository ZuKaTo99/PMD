namespace PMD.App.Application.ProjectVersioning;

public interface IProjectVersionStorePathProvider
{
    string GetProjectVersionStorePath(Guid projectId);
}