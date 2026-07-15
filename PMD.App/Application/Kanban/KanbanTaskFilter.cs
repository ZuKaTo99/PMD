using PMD.App.Domain.Kanban;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Application.Kanban;

public static class KanbanTaskFilter
{
    public static IReadOnlyList<KanbanTask> Apply(
        IEnumerable<KanbanTask> tasks,
        KanbanTaskFilterCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(criteria);

        string normalizedSearchText = criteria.SearchText?.Trim() ?? string.Empty;

        return tasks
            .Where(task => MatchesSearch(task, normalizedSearchText))
            .Where(task => MatchesProject(task, criteria))
            .Where(task => !criteria.Priority.HasValue ||
                task.Priority == criteria.Priority.Value)
            .Where(task => !criteria.Status.HasValue ||
                task.Status == criteria.Status.Value)
            .OrderBy(task => task.Status)
            .ThenBy(task => task.SortOrder)
            .ThenByDescending(task => task.CreatedAt)
            .ToList();
    }

    private static bool MatchesSearch(
        KanbanTask task,
        string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        return task.Title.Contains(
                searchText,
                StringComparison.OrdinalIgnoreCase) ||
            task.Description.Contains(
                searchText,
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesProject(
        KanbanTask task,
        KanbanTaskFilterCriteria criteria)
    {
        if (criteria.OnlyUnassignedProject)
        {
            return !task.ProjectId.HasValue;
        }

        return !criteria.ProjectId.HasValue ||
            task.ProjectId == criteria.ProjectId.Value;
    }
}
