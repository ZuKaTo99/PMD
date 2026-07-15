using PMD.App.Application.Kanban;
using PMD.App.Domain.Kanban;

namespace PMD.App.Tests.Kanban;

public sealed class KanbanTaskFilterTests
{
    [Fact]
    public void Apply_SearchesTitleAndDescriptionCaseInsensitively()
    {
        IReadOnlyList<KanbanTask> tasks =
        [
            CreateTask(
                "Dashboard überarbeiten",
                "Sprachdiagramm erweitern"),
            CreateTask(
                "Kanban testen",
                "Drag-and-drop prüfen")
        ];

        IReadOnlyList<KanbanTask> result = KanbanTaskFilter.Apply(
            tasks,
            KanbanTaskFilterCriteria.Empty with
            {
                SearchText = "SPRACHDIAGRAMM"
            });

        KanbanTask matchingTask = Assert.Single(result);
        Assert.Equal("Dashboard überarbeiten", matchingTask.Title);
    }

    [Fact]
    public void Apply_FiltersByProjectPriorityAndStatus()
    {
        Guid matchingProjectId = Guid.NewGuid();

        KanbanTask matchingTask = CreateTask(
            "Passende Aufgabe",
            projectId: matchingProjectId,
            priority: KanbanTaskPriority.High,
            status: KanbanTaskStatus.InProgress);

        IReadOnlyList<KanbanTask> result = KanbanTaskFilter.Apply(
            [
                matchingTask,
                CreateTask(
                    "Falsches Projekt",
                    projectId: Guid.NewGuid(),
                    priority: KanbanTaskPriority.High,
                    status: KanbanTaskStatus.InProgress),
                CreateTask(
                    "Falsche Priorität",
                    projectId: matchingProjectId,
                    priority: KanbanTaskPriority.Normal,
                    status: KanbanTaskStatus.InProgress),
                CreateTask(
                    "Falscher Status",
                    projectId: matchingProjectId,
                    priority: KanbanTaskPriority.High,
                    status: KanbanTaskStatus.Open)
            ],
            new KanbanTaskFilterCriteria(
                string.Empty,
                matchingProjectId,
                false,
                KanbanTaskPriority.High,
                KanbanTaskStatus.InProgress));

        Assert.Same(matchingTask, Assert.Single(result));
    }

    [Fact]
    public void Apply_OnlyUnassignedProject_ExcludesAssignedTasks()
    {
        KanbanTask unassignedTask = CreateTask("Ohne Projekt");

        IReadOnlyList<KanbanTask> result = KanbanTaskFilter.Apply(
            [
                unassignedTask,
                CreateTask(
                    "Mit Projekt",
                    projectId: Guid.NewGuid())
            ],
            KanbanTaskFilterCriteria.Empty with
            {
                OnlyUnassignedProject = true
            });

        Assert.Same(unassignedTask, Assert.Single(result));
    }

    [Fact]
    public void Apply_EmptyCriteria_PreservesBoardOrder()
    {
        KanbanTask laterOpenTask = CreateTask(
            "Zweite offene Aufgabe",
            status: KanbanTaskStatus.Open,
            sortOrder: 1);

        KanbanTask firstOpenTask = CreateTask(
            "Erste offene Aufgabe",
            status: KanbanTaskStatus.Open,
            sortOrder: 0);

        KanbanTask inProgressTask = CreateTask(
            "In Arbeit",
            status: KanbanTaskStatus.InProgress,
            sortOrder: 0);

        IReadOnlyList<KanbanTask> result = KanbanTaskFilter.Apply(
            [laterOpenTask, inProgressTask, firstOpenTask],
            KanbanTaskFilterCriteria.Empty);

        Assert.Collection(
            result,
            task => Assert.Same(firstOpenTask, task),
            task => Assert.Same(laterOpenTask, task),
            task => Assert.Same(inProgressTask, task));
    }

    private static KanbanTask CreateTask(
        string title,
        string description = "",
        Guid? projectId = null,
        KanbanTaskPriority priority = KanbanTaskPriority.Normal,
        KanbanTaskStatus status = KanbanTaskStatus.Open,
        int sortOrder = 0)
    {
        DateTime now = DateTime.Now;

        return new KanbanTask
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            ProjectId = projectId,
            Priority = priority,
            Status = status,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
