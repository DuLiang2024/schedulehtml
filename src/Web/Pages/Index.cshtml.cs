using Microsoft.AspNetCore.Mvc.RazorPages;
using ScheduleHtml.Core.Serialization;
using ScheduleHtml.Core.Validation;

namespace ScheduleHtml.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IWebHostEnvironment _hostEnvironment;

    public IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment hostEnvironment)
    {
        _logger = logger;
        _hostEnvironment = hostEnvironment;
    }

    public string DefaultJson { get; private set; } = "{}";

    public IReadOnlyList<string> ValidationErrors { get; private set; } = Array.Empty<string>();

    public string DocumentPath => Path.Combine(_hostEnvironment.WebRootPath, "data", "default-schedule.json");

    public async Task OnGet()
    {
        if (!System.IO.File.Exists(DocumentPath))
        {
            _logger.LogWarning("Timeline document not found at {DocumentPath}", DocumentPath);
            return;
        }

        try
        {
            DefaultJson = await System.IO.File.ReadAllTextAsync(DocumentPath);
            var result = TimelineDocumentValidator.Validate(TimelineDocumentSerializer.Deserialize(DefaultJson));
            ValidationErrors = result.IsValid ? Array.Empty<string>() : result.Errors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read timeline document from {DocumentPath}", DocumentPath);
            ValidationErrors = new[] { "Failed to load default JSON. Check server logs for details." };
        }
    }
}
