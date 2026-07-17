using PMD.App.Application.ProjectChanges;
using PMD.App.Application.ProjectHistory;
using PMD.App.Application.ProjectStates;
using PMD.App.Application.Projects;
using PMD.App.Domain.ProjectChanges;
using PMD.App.Domain.ProjectStates;
using PMD.App.Domain.Projects;

namespace PMD.App.Tests.ProjectHistory;

public sealed class ProjectHistoryServiceTests
{
    [Fact]
    public void GetDetails_LoadsSelectedStateAndComparesItWithOlderState()
    {
        Guid projectId = Guid.NewGuid();
        Guid oldestStateId = Guid.NewGuid();
        Guid selectedStateId = Guid.NewGuid();
        Guid newestStateId = Guid.NewGuid();

        Project project = CreateProject(projectId);

        ProjectState oldestState = CreateState(
            oldestStateId,
            projectId,
            new DateTime(2026, 7, 13, 10, 0, 0),
            1);

        ProjectState selectedState = CreateState(
            selectedStateId,
            projectId,
            new DateTime(2026, 7, 14, 10, 0, 0),
            2);

        ProjectState newestState = CreateState(
            newestStateId,
            projectId,
            new DateTime(2026, 7, 15, 10, 0, 0),
            3);

        var projectStore = new FakeProjectMemoryStore(project);
        var projectStateStore = new FakeProjectStateMemoryStore(
            newestState,
            selectedState,
            oldestState);

        var service = new ProjectHistoryService(
            projectStore,
            projectStateStore,
            new ProjectChangesService());

        ProjectHistoryDetails? result = service.GetDetails(
            projectId,
            selectedStateId);

        Assert.NotNull(result);
        Assert.Equal(2, result.ProjectStateNumber);
        Assert.Equal(3, result.TotalProjectStateCount);
        Assert.Equal(newestStateId, result.NewerProjectState?.Id);
        Assert.Equal(oldestStateId, result.OlderProjectState?.Id);
        Assert.Equal(oldestStateId, result.PreviousProjectState?.Id);
        Assert.Equal(2, result.SelectedProjectState.Files.Count);
        Assert.NotNull(result.ChangesFromPreviousState);
        Assert.Equal(1, result.ChangesFromPreviousState.ModifiedFileCount);
        Assert.Equal(1, result.ChangesFromPreviousState.AddedFileCount);
    }

    [Fact]
    public void GetDetails_ReturnsNullWhenStateDoesNotBelongToProject()
    {
        Guid projectId = Guid.NewGuid();
        Project project = CreateProject(projectId);
        ProjectState projectState = CreateState(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.Now,
            1);

        var service = new ProjectHistoryService(
            new FakeProjectMemoryStore(project),
            new FakeProjectStateMemoryStore(projectState),
            new ProjectChangesService());

        ProjectHistoryDetails? result = service.GetDetails(
            projectId,
            projectState.Id);

        Assert.Null(result);
    }

    [Fact]
    public void GetDetails_ReturnsOldestStateWithoutComparison()
    {
        Guid projectId = Guid.NewGuid();
        Guid projectStateId = Guid.NewGuid();
        Project project = CreateProject(projectId);
        ProjectState projectState = CreateState(
            projectStateId,
            projectId,
            DateTime.Now,
            1);

        var service = new ProjectHistoryService(
            new FakeProjectMemoryStore(project),
            new FakeProjectStateMemoryStore(projectState),
            new ProjectChangesService());

        ProjectHistoryDetails? result = service.GetDetails(
            projectId,
            projectStateId);

        Assert.NotNull(result);
        Assert.Equal(1, result.ProjectStateNumber);
        Assert.Null(result.PreviousProjectState);
        Assert.Null(result.OlderProjectState);
        Assert.Null(result.NewerProjectState);
        Assert.Null(result.ChangesFromPreviousState);
    }

    [Fact]
    public void GetProjectStates_ReturnsAllStatesInNewestFirstOrder()
    {
        Guid projectId = Guid.NewGuid();
        Project project = CreateProject(projectId);

        ProjectState oldestState = CreateState(
            Guid.NewGuid(),
            projectId,
            new DateTime(2026, 7, 13, 10, 0, 0),
            1);

        ProjectState newestState = CreateState(
            Guid.NewGuid(),
            projectId,
            new DateTime(2026, 7, 15, 10, 0, 0),
            3);

        ProjectState middleState = CreateState(
            Guid.NewGuid(),
            projectId,
            new DateTime(2026, 7, 14, 10, 0, 0),
            2);

        var service = new ProjectHistoryService(
            new FakeProjectMemoryStore(project),
            new FakeProjectStateMemoryStore(
                oldestState,
                newestState,
                middleState),
            new ProjectChangesService());

        IReadOnlyList<ProjectState> result =
            service.GetProjectStates(projectId);

        Assert.Equal(3, result.Count);
        Assert.Equal(newestState.Id, result[0].Id);
        Assert.Equal(middleState.Id, result[1].Id);
        Assert.Equal(oldestState.Id, result[2].Id);
    }

    [Fact]
    public void GetComparisonDetails_ComparesAnyOlderStateWithNewerState()
    {
        Guid projectId = Guid.NewGuid();
        Project project = CreateProject(projectId);

        ProjectState oldestState = CreateState(
            Guid.NewGuid(),
            projectId,
            new DateTime(2026, 7, 13, 10, 0, 0),
            1);

        ProjectState middleState = CreateState(
            Guid.NewGuid(),
            projectId,
            new DateTime(2026, 7, 14, 10, 0, 0),
            2);

        ProjectState newestState = CreateState(
            Guid.NewGuid(),
            projectId,
            new DateTime(2026, 7, 15, 10, 0, 0),
            3);

        var service = new ProjectHistoryService(
            new FakeProjectMemoryStore(project),
            new FakeProjectStateMemoryStore(
                newestState,
                middleState,
                oldestState),
            new ProjectChangesService());

        ProjectHistoryComparisonDetails? result =
            service.GetComparisonDetails(
                projectId,
                oldestState.Id,
                newestState.Id);

        Assert.NotNull(result);
        Assert.Equal(1, result.OlderProjectStateNumber);
        Assert.Equal(3, result.NewerProjectStateNumber);
        Assert.Equal(oldestState.Id, result.OlderProjectState.Id);
        Assert.Equal(newestState.Id, result.NewerProjectState.Id);
        Assert.Equal(2, result.FileCountDifference);
        Assert.Equal(1, result.ChangesResult.ModifiedFileCount);
        Assert.Equal(2, result.ChangesResult.AddedFileCount);
    }

    [Fact]
    public void GetComparisonDetails_ReturnsNullForInvalidDirectionOrSameState()
    {
        Guid projectId = Guid.NewGuid();
        Project project = CreateProject(projectId);

        ProjectState olderState = CreateState(
            Guid.NewGuid(),
            projectId,
            new DateTime(2026, 7, 14, 10, 0, 0),
            1);

        ProjectState newerState = CreateState(
            Guid.NewGuid(),
            projectId,
            new DateTime(2026, 7, 15, 10, 0, 0),
            2);

        var service = new ProjectHistoryService(
            new FakeProjectMemoryStore(project),
            new FakeProjectStateMemoryStore(newerState, olderState),
            new ProjectChangesService());

        Assert.Null(service.GetComparisonDetails(
            projectId,
            newerState.Id,
            olderState.Id));

        Assert.Null(service.GetComparisonDetails(
            projectId,
            olderState.Id,
            olderState.Id));
    }

    private static Project CreateProject(Guid projectId)
    {
        return new Project
        {
            Id = projectId,
            Name = "PMD",
            RootPath = @"C:\Projects\PMD",
            LastScannedAt = DateTime.Now
        };
    }

    private static ProjectState CreateState(
        Guid projectStateId,
        Guid projectId,
        DateTime scannedAt,
        int version)
    {
        List<ProjectStateFile> files = new()
        {
            CreateFile(
                projectStateId,
                "src/App.cs",
                version == 1 ? "hash-a" : "hash-b",
                version == 1 ? "class App { }" : "class App { int Version; }")
        };

        if (version >= 2)
        {
            files.Add(CreateFile(
                projectStateId,
                "README.md",
                "hash-readme",
                "# PMD"));
        }

        if (version >= 3)
        {
            files.Add(CreateFile(
                projectStateId,
                "src/NewFeature.cs",
                "hash-feature",
                "class NewFeature { }"));
        }

        return new ProjectState
        {
            Id = projectStateId,
            ProjectId = projectId,
            ProjectName = "PMD",
            RootPath = @"C:\Projects\PMD",
            CreatedAt = scannedAt,
            ScannedAt = scannedAt,
            FileCount = files.Count,
            TotalSizeInBytes = files.Sum(file => file.SizeInBytes),
            Files = files
        };
    }

    private static ProjectStateFile CreateFile(
        Guid projectStateId,
        string relativePath,
        string hash,
        string content)
    {
        return new ProjectStateFile
        {
            ProjectStateId = projectStateId,
            RelativePath = relativePath,
            FileName = Path.GetFileName(relativePath),
            Extension = Path.GetExtension(relativePath),
            SizeInBytes = content.Length,
            LastChangedAt = new DateTime(2026, 7, 15, 10, 0, 0),
            ContentHashSha256 = hash,
            TextSnapshotContent = content,
            TextSnapshotLineCount = 1
        };
    }

    private sealed class FakeProjectMemoryStore : IProjectMemoryStore
    {
        private readonly Project project;

        public FakeProjectMemoryStore(Project project)
        {
            this.project = project;
        }

        public event Action? ProjectsChanged;

        public IReadOnlyList<Project> Projects => new[] { project };

        public Project? GetProjectById(Guid projectId)
        {
            return project.Id == projectId ? project : null;
        }

        public Project? GetProjectByRootPath(string rootPath)
        {
            return string.Equals(
                project.RootPath,
                rootPath,
                StringComparison.OrdinalIgnoreCase)
                ? project
                : null;
        }

        public Project RememberScannedProject(
            string projectName,
            string rootPath,
            DateTime scannedAt)
        {
            return project;
        }

        public bool UpdateProjectDetails(
            Guid projectId,
            string newName,
            string accentColor)
        {
            return false;
        }

        public bool RemoveProject(Guid projectId)
        {
            return false;
        }

        public void Clear()
        {
            ProjectsChanged?.Invoke();
        }
    }

    private sealed class FakeProjectStateMemoryStore : IProjectStateMemoryStore
    {
        private readonly IReadOnlyList<ProjectState> projectStates;

        public FakeProjectStateMemoryStore(params ProjectState[] projectStates)
        {
            this.projectStates = projectStates;
        }

        public event Action? ProjectStatesChanged;

        public IReadOnlyList<ProjectState> ProjectStates => projectStates;

        public ProjectState? GetLatestByProjectId(Guid projectId)
        {
            return projectStates
                .Where(projectState => projectState.ProjectId == projectId)
                .OrderByDescending(projectState => projectState.ScannedAt)
                .FirstOrDefault();
        }

        public IReadOnlyList<ProjectState> GetByProjectId(
            Guid projectId,
            int maxCount)
        {
            return projectStates
                .Where(projectState => projectState.ProjectId == projectId)
                .OrderByDescending(projectState => projectState.ScannedAt)
                .Take(maxCount)
                .ToList();
        }

        public IReadOnlyList<ProjectStateFile> GetFilesByProjectStateId(
            Guid projectStateId)
        {
            return projectStates
                .First(projectState => projectState.Id == projectStateId)
                .Files;
        }

        public ProjectState LoadFiles(ProjectState projectState)
        {
            return projectState;
        }

        public bool Remember(ProjectState projectState)
        {
            return false;
        }

        public void RemoveByProjectId(Guid projectId)
        {
            ProjectStatesChanged?.Invoke();
        }

        public void Clear()
        {
            ProjectStatesChanged?.Invoke();
        }
    }
}
