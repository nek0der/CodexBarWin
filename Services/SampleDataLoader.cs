using CodexBarWin.Models;
using Microsoft.Extensions.Logging;

namespace CodexBarWin.Services;

/// <summary>
/// Service for loading sample data files for development mode.
/// </summary>
public class SampleDataLoader : ISampleDataLoader
{
    private readonly ILogger<SampleDataLoader> _logger;
    private readonly string _samplesDirectory;

    public SampleDataLoader(ILogger<SampleDataLoader> logger)
    {
        _logger = logger;
        
        // Get the application directory
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _samplesDirectory = Path.Combine(appDirectory, "Samples");
    }

    /// <inheritdoc />
    public string? LoadSampleJson(string provider)
    {
        try
        {
            // Validate and normalize provider name
            var normalizedProvider = ProviderConstants.ValidateAndNormalize(provider);
            
            // Construct file path (e.g., Samples/claude.json)
            var fileName = $"{normalizedProvider}.json";
            var filePath = Path.Combine(_samplesDirectory, fileName);

            // Check if file exists
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Sample data file not found: {FilePath}", filePath);
                return null;
            }

            // Read file contents
            var json = File.ReadAllText(filePath);
            _logger.LogInformation("Loaded sample data for {Provider} from {FilePath}", normalizedProvider, filePath);
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load sample data for {Provider}", provider);
            return null;
        }
    }
}
