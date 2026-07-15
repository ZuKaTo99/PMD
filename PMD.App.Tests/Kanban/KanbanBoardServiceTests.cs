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
    public void UpdateTask_UpdatesContentWithinSameStatus()
    {
        KanbanTask existingTask = CreateTask(
            "Alte Aufgabe",
            KanbanTaskStatus.Open,
            0);

        var repository = new InMemoryKanbanTaskRepository([existingTask]);
        var service = new KanbanBoardService(repository);
        Guid projectId = Guid.NewGuid();

        KanbanTask updatedTask = service.UpdateTask(
            existingTask.Id,
            "  Neuer Titel  ",
            "  Neue Beschreibung  ",
            projectId,
            KanbanTaskStatus.Open,
            KanbanTaskPriority.Critical);

        Assert.Equal("Neuer Titel", updatedTask.Title);
        Assert.Equal("Neue Beschreibung", updatedTask.Description);
        Assert.Equal(projectId, updatedTask.ProjectId);
        Assert.Equal(KanbanTaskPriority.Critical, updatedTask.Priority);
        Assert.Equal(KanbanTaskStatus.Open, updatedTask.Status);
        Assert.Equal(0, updatedTask.SortOrder);
        Assert.Equal(updatedTask.Id, repository.SavedTask?.Id);
    }

    [Fact]
    public void UpdateTask_ChangingStatusMovesToEndAndReindexesSource()
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

        KanbanTask updatedTask = service.UpdateTask(
            firstOpenTask.Id,
            "Jetzt in Arbeit",
            "Bearbeitung begonnen",
            null,
            KanbanTaskStatus.InProgress,
            KanbanTaskPriority.High);

        Assert.Collection(
            service.Tasks,
            task =>
            {
                Assert.Equal(secondOpenTask.Id, task.Id);
                Assert.Equal(KanbanTaskStatus.Open, task.Status);
                Assert.Equal(0, task.SortOrder);
            },
            task =>
            {
                Assert.Equal(inProgressTask.Id, task.Id);
                Assert.Equal(KanbanTaskStatus.InProgress, task.Status);
                Assert.Equal(0, task.SortOrder);
            },
            task =>
            {
                Assert.Equal(updatedTask.Id, task.Id);
                Assert.Equal("Jetzt in Arbeit", task.Title);
                Assert.Equal(KanbanTaskStatus.InProgress, task.Status);
                Assert.Equal(1, task.SortOrder);
            });

        Assert.Equal(2, repository.SavedTasks.Count);
    }

    [Fact]
    public void UpdateTask_RejectsEmptyTitle()
    {
        KanbanTask existingTask = CreateTask(
            "Aufgabe",
            KanbanTaskStatus.Open,
            0);

        var service = new KanbanBoardService(
            new InMemoryKanbanTaskRepository([existingTask]));

        Assert.Throws<ArgumentException>(() =>
            service.UpdateTask(
                existingTask.Id,
                "   ",
                string.Empty,
                null,
                KanbanTaskStatus.Open,
                KanbanTaskPriority.Normal));
    }

    [Fact]
    public void DeleteTask_RemovesTaskAndReindexesColumn()
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

        service.DeleteTask(secondTask.Id);

        Assert.Collection(
            service.Tasks,
            task =>
            {
                Assert.Equal(firstTask.Id, task.Id);
                Assert.Equal(0, task.SortOrder);
            },
            task =>
            {
                Assert.Equal(thirdTask.Id, task.Id);
                Assert.Equal(1, task.SortOrder);
            });

        Assert.Equal(secondTask.Id, repository.DeletedTaskId);
        Assert.Single(repository.SavedTasks);
        Assert.Equal(thirdTask.Id, repository.SavedTasks[0].Id);
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

        public Guid? DeletedTaskId { get; private set; }

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
            DeletedTaskId = taskId;
            tasks.RemoveAll(task => task.Id == taskId);
        }

        public void DeleteAndSaveAll(
            Guid taskId,
            IReadOnlyList<KanbanTask> tasksToSave)
        {
            Delete(taskId);
            SaveAll(tasksToSave);
        }
    }
}
