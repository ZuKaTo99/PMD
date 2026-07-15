using PMD.App.Application.Kanban;
using PMD.App.Domain.Kanban;

namespace PMD.App.Tests.Kanban;

public sealed class KanbanBoardServiceTests
{
    [Fact]
    public void CreateTask_TrimsValuesAndUsesNextStatusOrder()
    {
        var repository = new InMemoryKanbanTaskRepository(
        [
            CreateTask(
                "Vorhandene Aufgabe",
                KanbanTaskStatus.Open,
                4)
        ]);

        var service = new KanbanBoardService(repository);
        Guid projectId = Guid.NewGuid();

        KanbanTask createdTask = service.CreateTask(
            "  Neue Aufgabe  ",
            "  Beschreibung  ",
            projectId,
            KanbanTaskStatus.Open,
            KanbanTaskPriority.High);

        Assert.Equal("Neue Aufgabe", createdTask.Title);
        Assert.Equal("Beschreibung", createdTask.Description);
        Assert.Equal(projectId, createdTask.ProjectId);
        Assert.Equal(KanbanTaskPriority.High, createdTask.Priority);
        Assert.Equal(5, createdTask.SortOrder);
        Assert.Same(createdTask, repository.SavedTask);
    }

    [Fact]
    public void CreateTask_RejectsEmptyTitle()
    {
        var service = new KanbanBoardService(
            new InMemoryKanbanTaskRepository());

        Assert.Throws<ArgumentException>(() =>
            service.CreateTask(
                "   ",
                string.Empty,
                null,
                KanbanTaskStatus.Open,
                KanbanTaskPriority.Normal));
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

    private sealed class InMemoryKanbanTaskRepository
        : IKanbanTaskRepository
    {
        private readonly List<KanbanTask> tasks;

        public InMemoryKanbanTaskRepository(
            IEnumerable<KanbanTask>? tasks = null)
        {
            this.tasks = tasks?.ToList() ?? [];
        }

        public KanbanTask? SavedTask { get; private set; }

        public IReadOnlyList<KanbanTask> GetAll()
        {
            return tasks.ToList();
        }

        public KanbanTask? GetById(Guid taskId)
        {
            return tasks.FirstOrDefault(task => task.Id == taskId);
        }

        public void Save(KanbanTask task)
        {
            SavedTask = task;
            tasks.RemoveAll(existingTask => existingTask.Id == task.Id);
            tasks.Add(task);
        }

        public void Delete(Guid taskId)
        {
            tasks.RemoveAll(task => task.Id == taskId);
        }
    }
}
