using System.Text.Json.Serialization;

namespace CodexBarWin.Models;

/// <summary>
/// JSON serializer context for AOT-compatible serialization.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(CacheData))]
[JsonSerializable(typeof(UsageDataDto))]
[JsonSerializable(typeof(List<UsageDataDto>))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}

/// <summary>
/// Cache data for disk persistence.
/// </summary>
public record CacheData
{
    public DateTime Timestamp { get; init; }
    public Dictionary<string, UsageData> Items { get; init; } = [];
}

/// <summary>
/// DTO for deserializing codexbar JSON output.
/// </summary>
public record UsageDataDto
{
    public string Provider { get; init; } = string.Empty;
    public string? Source { get; init; }
    public UsageDto? Usage { get; init; }
    public string? Error { get; init; }

    public UsageData ToUsageData() => new()
    {
        Provider = Provider,
        Plan = Usage?.LoginMethod,
        Session = Usage?.Primary?.ToUsageWindow(),
        Weekly = Usage?.Secondary?.ToUsageWindow(),
        Tertiary = Usage?.Tertiary?.ToUsageWindow(),
        Status = Source,
        Error = Error,
        FetchedAt = DateTime.UtcNow
    };
}

/// <summary>
/// DTO for usage information.
/// </summary>
public record UsageDto
{
    public string? LoginMethod { get; init; }
    public UsageWindowDto? Primary { get; init; }
    public UsageWindowDto? Secondary { get; init; }
    public UsageWindowDto? Tertiary { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for usage window information.
/// </summary>
public record UsageWindowDto
{
    public double UsedPercent { get; init; }
    public int WindowMinutes { get; init; }
    public DateTime? ResetsAt { get; init; }
    public string? ResetDescription { get; init; }

    public UsageWindow ToUsageWindow() => new()
    {
        Used = (int)UsedPercent,
        Limit = 100,
        ResetAt = ResetsAt,
        ResetIn = ResetDescription
    };
}
