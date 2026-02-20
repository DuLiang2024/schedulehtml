using System.Text.Json;
using System.Text.Json.Serialization;
using ScheduleHtml.Core.Models;
using ScheduleHtml.Core.Validation;

namespace ScheduleHtml.Core.Serialization;

public static class TimelineDocumentSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static TimelineDocument? Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<TimelineDocument>(json, Options);
    }

    public static string Serialize(TimelineDocument document) =>
        JsonSerializer.Serialize(document, Options);

    public static TimelineDocument LoadAndValidate(string json)
    {
        var document = Deserialize(json);
        var result = TimelineDocumentValidator.Validate(document);
        result.EnsureValid();
        return TimelineDocumentValidator.Normalize(document!);
    }
}
