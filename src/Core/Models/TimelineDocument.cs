using System.Text.Json.Serialization;

namespace ScheduleHtml.Core.Models;

/// <summary>
/// Root container that mirrors the JSON document edited in the UI.
/// </summary>
public sealed record class TimelineDocument
{
    [JsonPropertyName("view")]
    public TimelineView View { get; init; } = new();

    [JsonPropertyName("lanes")]
    public List<TimelineLane> Lanes { get; init; } = [];

    [JsonPropertyName("items")]
    public List<TimelineItem> Items { get; init; } = [];
}

/// <summary>
/// Describes how the timeline should be rendered (day/hour granularity and range).
/// </summary>
public sealed record class TimelineView
{
    [JsonPropertyName("mode")]
    public string Mode { get; init; } = TimelineViewModes.Day;

    [JsonPropertyName("range")]
    public TimelineRange Range { get; init; } = new();
}

/// <summary>
/// Start/end range for the entire timeline.
/// </summary>
public record struct TimelineRange
{
    [JsonPropertyName("start")]
    public DateTimeOffset Start { get; init; }

    [JsonPropertyName("end")]
    public DateTimeOffset End { get; init; }
}

/// <summary>
/// Represents a swim lane.
/// </summary>
public sealed record class TimelineLane
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("color")]
    public string? Color { get; init; }
}

/// <summary>
/// Represents an item plotted on a lane.
/// </summary>
public sealed record class TimelineItem
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("laneId")]
    public string LaneId { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("start")]
    public DateTimeOffset? Start { get; init; }

    [JsonPropertyName("end")]
    public DateTimeOffset? End { get; init; }

    [JsonPropertyName("durationDays")]
    public double? DurationDays { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

/// <summary>
/// View mode constants to keep JSON aligned with UI features.
/// </summary>
public static class TimelineViewModes
{
    public const string Day = "day";
    public const string Hour = "hour";

    public static bool IsSupported(string? mode) =>
        mode is Day or Hour;
}
