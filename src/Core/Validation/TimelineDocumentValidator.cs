using System.Collections.Immutable;
using ScheduleHtml.Core.Models;

namespace ScheduleHtml.Core.Validation;

public static class TimelineDocumentValidator
{
    public static TimelineValidationResult Validate(TimelineDocument? document)
    {
        var errors = new List<string>();

        if (document is null)
        {
            errors.Add("Document is null or could not be parsed.");
            return new TimelineValidationResult(false, errors.ToImmutableArray());
        }

        if (document.View is null)
        {
            errors.Add("view section is required.");
        }
        else
        {
            if (!TimelineViewModes.IsSupported(document.View.Mode))
            {
                errors.Add($"view.mode must be one of [{TimelineViewModes.Day}, {TimelineViewModes.Hour}].");
            }

            var range = document.View.Range;
            if (range.Start == default || range.End == default)
            {
                errors.Add("view.range.start and view.range.end are required ISO-8601 date strings.");
            }
            else if (range.End <= range.Start)
            {
                errors.Add("view.range.end must be after view.range.start.");
            }
        }

        if (document.Lanes == null || document.Lanes.Count == 0)
        {
            errors.Add("lanes must include at least one entry.");
        }

        var laneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (document.Lanes is not null)
        {
            foreach (var lane in document.Lanes)
            {
                if (string.IsNullOrWhiteSpace(lane.Id))
                {
                    errors.Add("lanes[].id cannot be empty.");
                    continue;
                }

                if (!laneIds.Add(lane.Id))
                {
                    errors.Add($"Duplicate lane id detected: {lane.Id}");
                }

                if (string.IsNullOrWhiteSpace(lane.Label))
                {
                    errors.Add($"Lane '{lane.Id}' is missing a label.");
                }
            }
        }

        if (document.Items is null)
        {
            errors.Add("items must be provided (can be empty array).");
        }
        else
        {
            foreach (var item in document.Items)
            {
                if (string.IsNullOrWhiteSpace(item.LaneId))
                {
                    errors.Add($"Item '{item.Id}' must specify a laneId.");
                }
                else if (!laneIds.Contains(item.LaneId))
                {
                    errors.Add($"Item '{item.Id}' references unknown lane '{item.LaneId}'.");
                }

                if (item.Start is null && item.End is null)
                {
                    errors.Add($"Item '{item.Id}' needs start/end or start + durationDays.");
                }
                else if (item.Start is not null && item.End is not null && item.End <= item.Start)
                {
                    errors.Add($"Item '{item.Id}' must have end after start.");
                }
                else if (item.Start is null && item.End is not null && item.DurationDays is null)
                {
                    errors.Add($"Item '{item.Id}' is missing start date.");
                }
            }
        }

        return new TimelineValidationResult(errors.Count == 0, errors.ToImmutableArray());
    }

    public static TimelineDocument Normalize(TimelineDocument document)
    {
        var normalized = document with
        {
            Items = document.Items?
                .Select(item =>
                {
                    var start = item.Start;
                    var end = item.End;

                    if (start is not null && end is null && item.DurationDays is not null)
                    {
                        end = start.Value.AddDays(item.DurationDays.Value);
                    }

                    return item with { End = end };
                })
                .OrderBy(item => item.Start ?? item.End ?? DateTimeOffset.MaxValue)
                .ToList() ?? []
        };

        return normalized;
    }
}

public sealed record class TimelineValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public void EnsureValid()
    {
        if (!IsValid)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, Errors));
        }
    }
}
