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

    [Fact]
    public void MoveTask_ToAnotherStatus_ReordersBothColumns()
    {
        KanbanTask firstOpenTask = CreateTask(
            "Erste offene Aufgabe",
            KanbanTaskStatus.Open,
            0);

        KanbanTask secondOpenTask = CreateTask(
            "Zweite offene Aufgabe",
            KanbanTaskStatus.Open,
            1);

        KanbanTask inProgressTask = CreateTask(
            "Bereits in Arbeit",
            KanbanTaskStatus.InProgress,
            0);

        var repository = new InMemoryKanbanTaskRepository(
        [
            firstOpenTask,
            secondOpenTask,
            inProgressTask
        ]);

        var service = new KanbanBoardService(repository);

        service.MoveTask(
            secondOpenTask.Id,
            KanbanTaskStatus.InProgress,
            0);

        Assert.Collection(
            service.Tasks,
            task =>
            {
                Assert.Equal(firstOpenTask.Id, task.Id);
                Assert.Equal(KanbanTaskStatus.Open, task.Status);
                Assert.Equal(0, task.SortOrder);
            },
            task =>
            {
                Assert.Equal(secondOpenTask.Id, task.Id);
                Assert.Equal(KanbanTaskStatus.InProgress, task.Status);
                Assert.Equal(0, task.SortOrder);
            },
            task =>
            {
                Assert.Equal(inProgressTask.Id, task.Id);
                Assert.Equal(KanbanTaskStatus.InProgress, task.Status);
                Assert.Equal(1, task.SortOrder);
            });

        Assert.Equal(2, repository.SavedTasks.Count);
    }

    [Fact]
    public void MoveTask_WithinStatus_ReordersColumn()
    {
        KanbanTask firstTask = CreateTask(
            "Erste Aufgabe",
            KanbanTaskStatus.Open,
            0);

        KanbanTask secondTask = CreateTask(
            "Zweite Aufgabe",
            KanbanTaskStatus.Open,
            1);

        KanbanTask thirdTask = CreateTask(
            "Dritte Aufgabe",
            KanbanTaskStatus.Open,
            2);

        var repository = new InMemoryKanbanTaskRepository(
        [
            firstTask,
            secondTask,
            thirdTask
        ]);

        var service = new KanbanBoardService(repository);

        service.MoveTask(
            thirdTask.Id,
            KanbanTaskStatus.Open,
            0);

        Assert.Collection(
            service.Tasks,
            task =>
            {
                Assert.Equal(thirdTask.Id, task.Id);
                Assert.Equal(0, task.SortOrder);
            },
            task =>
            {
                Assert.Equal(firstTask.Id, task.Id);
                Assert.Equal(1, task.SortOrder);
            },
            task =>
            {
                Assert.Equal(secondTask.Id, task.Id);
                Assert.Equal(2, task.SortOrder);
            });

        Assert.Equal(3, repository.SavedTasks.Count);
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

        public IReadOnlyList<KanbanTask> SavedTasks { get; private set; } =
            Array.Empty<KanbanTask>();

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
            SaveAll([task]);
        }

        public void SaveAll(IReadOnlyList<KanbanTask> tasksToSave)
        {
            SavedTasks = tasksToSave.ToList();

            foreach (KanbanTask task in tasksToSave)
            {
                tasks.RemoveAll(existingTask => existingTask.Id == task.Id);
                tasks.Add(task);
            }
        }

        public void Delete(Guid taskId)
        {
            tasks.RemoveAll(task => task.Id == taskId);
        }
    }
}
