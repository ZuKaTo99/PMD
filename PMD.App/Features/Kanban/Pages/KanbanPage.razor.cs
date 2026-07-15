using Microsoft.AspNetCore.Components;
using PMD.App.Application.Kanban;
using PMD.App.Application.Projects;
using PMD.App.Domain.Kanban;
using PMD.App.Domain.Projects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Features.Kanban.Pages;

public partial class KanbanPage : IDisposable
{
    private static readonly IReadOnlyList<KanbanColumnDefinition>
        ColumnDefinitions =
        [
            new(
                KanbanTaskStatus.Open,
                "Offen",
                "Bereit für die nächste Bearbeitung",
                "open"),
            new(
                KanbanTaskStatus.InProgress,
                "In Arbeit",
                "Aufgaben, an denen gerade gearbeitet wird",
                "in-progress"),
            new(
                KanbanTaskStatus.Blocked,
                "Blockiert",
                "Benötigt eine Entscheidung oder Voraussetzung",
                "blocked"),
            new(
                KanbanTaskStatus.Done,
                "Erledigt",
                "Abgeschlossene Arbeit",
                "done")
        ];

    [Inject]
    private IKanbanBoardService KanbanBoardService { get; set; } = default!;

    [Inject]
    private IProjectMemoryStore ProjectMemoryStore { get; set; } = default!;

    protected IReadOnlyList<KanbanColumnDefinition> Columns =>
        ColumnDefinitions;

    protected IReadOnlyList<Project> Projects =>
        ProjectMemoryStore.Projects
            .OrderBy(project => project.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    protected bool IsCreateFormOpen { get; private set; }

    protected string NewTaskTitle { get; set; } = string.Empty;

    protected string NewTaskDescription { get; set; } = string.Empty;

    protected string SelectedProjectId { get; set; } = string.Empty;

    protected KanbanTaskStatus NewTaskStatus { get; set; } =
        KanbanTaskStatus.Open;

    protected KanbanTaskPriority NewTaskPriority { get; set; } =
        KanbanTaskPriority.Normal;

    protected string CreateTaskErrorMessage { get; private set; } =
        string.Empty;

    protected bool IsCreateButtonDisabled =>
        string.IsNullOrWhiteSpace(NewTaskTitle);

    protected override void OnInitialized()
    {
        KanbanBoardService.BoardChanged += OnBoardChanged;
        ProjectMemoryStore.ProjectsChanged += OnProjectsChanged;
    }

    public void Dispose()
    {
        KanbanBoardService.BoardChanged -= OnBoardChanged;
        ProjectMemoryStore.ProjectsChanged -= OnProjectsChanged;
    }

    protected IReadOnlyList<KanbanTask> GetTasksByStatus(
        KanbanTaskStatus status)
    {
        return KanbanBoardService.Tasks
            .Where(task => task.Status == status)
            .OrderBy(task => task.SortOrder)
            .ThenByDescending(task => task.CreatedAt)
            .ToList();
    }

    protected Project? GetProject(Guid? projectId)
    {
        return projectId.HasValue
            ? ProjectMemoryStore.GetProjectById(projectId.Value)
            : null;
    }

    protected void ToggleCreateForm()
    {
        IsCreateFormOpen = !IsCreateFormOpen;
        CreateTaskErrorMessage = string.Empty;
    }

    protected void CancelCreateTask()
    {
        ResetCreateForm();
        IsCreateFormOpen = false;
    }

    protected void CreateTask()
    {
        CreateTaskErrorMessage = string.Empty;

        try
        {
            Guid? projectId = Guid.TryParse(
                SelectedProjectId,
                out Guid parsedProjectId)
                    ? parsedProjectId
                    : null;

            KanbanBoardService.CreateTask(
                NewTaskTitle,
                NewTaskDescription,
                projectId,
                NewTaskStatus,
                NewTaskPriority);

            ResetCreateForm();
            IsCreateFormOpen = false;
        }
        catch (ArgumentException exception)
        {
            CreateTaskErrorMessage = exception.Message;
        }
    }

    protected static string GetPriorityLabel(
        KanbanTaskPriority priority)
    {
        return priority switch
        {
            KanbanTaskPriority.Low => "Niedrig",
            KanbanTaskPriority.High => "Hoch",
            KanbanTaskPriority.Critical => "Kritisch",
            _ => "Normal"
        };
    }

    protected static string GetPriorityClass(
        KanbanTaskPriority priority)
    {
        return priority switch
        {
            KanbanTaskPriority.Low => "kanban-priority-low",
            KanbanTaskPriority.High => "kanban-priority-high",
            KanbanTaskPriority.Critical => "kanban-priority-critical",
            _ => "kanban-priority-normal"
        };
    }


    protected static string GetProjectAccentClass(Project? project)
    {
        return project is null
            ? string.Empty
            : $"kanban-accent-{ProjectAccentColors.Normalize(project.AccentColor)}";
    }

    protected static string FormatDate(DateTime dateTime)
    {
        return dateTime.ToString("dd.MM.yyyy");
    }

    protected static string FormatTaskCount(int taskCount)
    {
        return taskCount == 1
            ? "1 Aufgabe"
            : $"{taskCount} Aufgaben";
    }

    private void ResetCreateForm()
    {
        NewTaskTitle = string.Empty;
        NewTaskDescription = string.Empty;
        SelectedProjectId = string.Empty;
        NewTaskStatus = KanbanTaskStatus.Open;
        NewTaskPriority = KanbanTaskPriority.Normal;
        CreateTaskErrorMessage = string.Empty;
    }

    private void OnBoardChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private void OnProjectsChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    protected sealed record KanbanColumnDefinition(
        KanbanTaskStatus Status,
        string Title,
        string Description,
        string CssName);
}
