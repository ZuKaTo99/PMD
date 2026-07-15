using PMD.App.Application.Database;
using PMD.App.Domain.Kanban;
using PMD.App.Infrastructure.Database;
using PMD.App.Infrastructure.Kanban;

namespace PMD.App.Tests.Kanban;

public sealed class SqliteKanbanTaskRepositoryTests : IDisposable
{
    private readonly string testDirectoryPath;
    private readonly SqliteKanbanTaskRepository repository;

    public SqliteKanbanTaskRepositoryTests()
    {
        testDirectoryPath = Path.Combine(
            Path.GetTempPath(),
            "PMD.App.Tests",
            "Kanban",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(testDirectoryPath);

        string databasePath = Path.Combine(
            testDirectoryPath,
            "pmd-kanban-tests.db");

        IPmdDatabasePathProvider pathProvider =
            new TestDatabasePathProvider(databasePath);

        var connectionFactory =
            new PmdDatabaseConnectionFactory(pathProvider);

        new PmdDatabaseInitializer(connectionFactory).Initialize();

        repository = new SqliteKanbanTaskRepository(connectionFactory);
    }

    [Fact]
    public void Save_PersistsKanbanTaskWithProjectAndPriority()
    {
        Guid projectId = Guid.NewGuid();
        var task = new KanbanTask
        {
            Id = Guid.NewGuid(),
            Title = "Kanban-Grundlage testen",
            Description = "Die Aufgabe muss nach einem Neustart erhalten bleiben.",
            ProjectId = projectId,
            Status = KanbanTaskStatus.InProgress,
            Priority = KanbanTaskPriority.High,
            SortOrder = 3,
            CreatedAt = new DateTime(2026, 7, 15, 1, 30, 0),
            UpdatedAt = new DateTime(2026, 7, 15, 1, 30, 0)
        };

        repository.Save(task);

        KanbanTask? loadedTask = repository.GetById(task.Id);

        Assert.NotNull(loadedTask);
        Assert.Equal(task.Title, loadedTask!.Title);
        Assert.Equal(task.Description, loadedTask.Description);
        Assert.Equal(projectId, loadedTask.ProjectId);
        Assert.Equal(KanbanTaskStatus.InProgress, loadedTask.Status);
        Assert.Equal(KanbanTaskPriority.High, loadedTask.Priority);
        Assert.Equal(3, loadedTask.SortOrder);
    }

    [Fact]
    public void GetAll_OrdersTasksByStatusAndSortOrder()
    {
        repository.Save(CreateTask(
            "Zweite offene Aufgabe",
            KanbanTaskStatus.Open,
            1));

        repository.Save(CreateTask(
            "Erste offene Aufgabe",
            KanbanTaskStatus.Open,
            0));

        repository.Save(CreateTask(
            "Aufgabe in Arbeit",
            KanbanTaskStatus.InProgress,
            0));

        IReadOnlyList<KanbanTask> tasks = repository.GetAll();

        Assert.Collection(
            tasks,
            task => Assert.Equal("Erste offene Aufgabe", task.Title),
            task => Assert.Equal("Zweite offene Aufgabe", task.Title),
            task => Assert.Equal("Aufgabe in Arbeit", task.Title));
    }

    [Fact]
    public void SaveAll_PersistsMovedTaskStatusAndSortOrder()
    {
        KanbanTask openTask = CreateTask(
            "Offene Aufgabe",
            KanbanTaskStatus.Open,
            0);

        KanbanTask inProgressTask = CreateTask(
            "Aufgabe in Arbeit",
            KanbanTaskStatus.InProgress,
            0);

        repository.Save(openTask);
        repository.Save(inProgressTask);

        DateTime updatedAt = DateTime.Now.AddMinutes(1);

        repository.SaveAll(
        [
            CopyTask(
                openTask,
                KanbanTaskStatus.InProgress,
                0,
                updatedAt),
            CopyTask(
                inProgressTask,
                KanbanTaskStatus.InProgress,
                1,
                updatedAt)
        ]);

        IReadOnlyList<KanbanTask> tasks = repository.GetAll();

        Assert.Collection(
            tasks,
            task =>
            {
                Assert.Equal(openTask.Id, task.Id);
                Assert.Equal(KanbanTaskStatus.InProgress, task.Status);
                Assert.Equal(0, task.SortOrder);
            },
            task =>
            {
                Assert.Equal(inProgressTask.Id, task.Id);
                Assert.Equal(KanbanTaskStatus.InProgress, task.Status);
                Assert.Equal(1, task.SortOrder);
            });
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(testDirectoryPath))
            {
                Directory.Delete(testDirectoryPath, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static KanbanTask CreateTask(
        string title,
        KanbanTaskStatus status,
        int sortOrder)
    {
        DateTime now = DateTime.Now;

        return new KanbanTask
        {
            Id = Guid.NewGuid(),
            Title = title,
            Status = status,
            Priority = KanbanTaskPriority.Normal,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static KanbanTask CopyTask(
        KanbanTask task,
        KanbanTaskStatus status,
        int sortOrder,
        DateTime updatedAt)
    {
        return new KanbanTask
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            ProjectId = task.ProjectId,
            Status = status,
            Priority = task.Priority,
            SortOrder = sortOrder,
            CreatedAt = task.CreatedAt,
            UpdatedAt = updatedAt
        };
    }

    private sealed class TestDatabasePathProvider : IPmdDatabasePathProvider
    {
        private readonly string databasePath;

        public TestDatabasePathProvider(string databasePath)
        {
            this.databasePath = databasePath;
        }

        public string GetDatabasePath()
        {
            return databasePath;
        }
    }
}
