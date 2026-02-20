using ScheduleHtml.Core.Models;
using ScheduleHtml.Core.Serialization;
using ScheduleHtml.Core.Validation;

namespace ScheduleHtml.Core.Tests;

public class TimelineDocumentValidatorTests
{
    [Fact]
    public void DefaultDocument_ShouldValidate()
    {
        var json = File.ReadAllText(GetFixturePath());
        var document = TimelineDocumentSerializer.Deserialize(json);
        var result = TimelineDocumentValidator.Validate(document);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.NotNull(document);
        Assert.Equal("day", document!.View.Mode);
        Assert.True(document.Items.Count > 0);
    }

    [Fact]
    public void DuplicateLane_IsRejected()
    {
        var now = DateTimeOffset.UtcNow;
        var document = new TimelineDocument
        {
            View = new TimelineView
            {
                Mode = TimelineViewModes.Day,
                Range = new TimelineRange { Start = now, End = now.AddDays(2) }
            },
            Lanes = new List<TimelineLane>
            {
                new() { Id = "alpha", Label = "Alpha" },
                new() { Id = "alpha", Label = "Duplicate" }
            },
            Items = new List<TimelineItem>
            {
                new()
                {
                    Id = "item-1",
                    LaneId = "alpha",
                    Start = now,
                    End = now.AddHours(2)
                }
            }
        };

        var result = TimelineDocumentValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Duplicate lane id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Normalize_ComputesEndFromDuration()
    {
        var now = DateTimeOffset.UtcNow;
        var document = new TimelineDocument
        {
            View = new TimelineView
            {
                Mode = TimelineViewModes.Day,
                Range = new TimelineRange { Start = now, End = now.AddDays(2) }
            },
            Lanes = new List<TimelineLane>
            {
                new() { Id = "qa", Label = "QA" }
            },
            Items = new List<TimelineItem>
            {
                new()
                {
                    Id = "duration",
                    LaneId = "qa",
                    Start = now,
                    DurationDays = 0.5
                }
            }
        };

        var normalized = TimelineDocumentValidator.Normalize(document);
        var item = Assert.Single(normalized.Items);

        Assert.NotNull(item.End);
        Assert.Equal(item.Start!.Value.AddDays(0.5), item.End);
    }

    private static string GetFixturePath()
    {
        var relative = Path.Combine("src", "Web", "wwwroot", "data", "default-schedule.json");
        var directory = AppContext.BaseDirectory;

        for (var i = 0; i < 8 && directory is not null; i++)
        {
            var candidate = Path.Combine(directory, relative);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new FileNotFoundException("Unable to locate default timeline JSON fixture.", relative);
    }
}
