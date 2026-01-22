namespace CodexBarWin.Models;

/// <summary>
/// Represents the setup status of the application.
/// </summary>
public record SetupStatus
{
    public bool WslInstalled { get; init; }
    public bool WslRunning { get; init; }
    public WslErrorType? WslError { get; init; }
    public List<string> Distros { get; init; } = [];
    public string? DefaultDistro => Distros.FirstOrDefault();
    public bool CodexBarInstalled { get; init; }
    public string? CodexBarVersion { get; init; }
    public string? CodexBarError { get; init; }
    public bool IsReady { get; init; }

    public SetupStep CurrentStep
    {
        get
        {
            if (WslError == WslErrorType.NotInstalled) return SetupStep.InstallWsl;
            if (WslError == WslErrorType.NotRunning) return SetupStep.StartWsl;
            if (WslError != null) return SetupStep.FixWsl;
            if (!WslInstalled) return SetupStep.InstallWsl;
            if (Distros.Count == 0) return SetupStep.InstallDistro;
            if (!CodexBarInstalled) return SetupStep.InstallCodexBar;
            return SetupStep.Ready;
        }
    }
}

/// <summary>
/// Represents the current setup step.
/// </summary>
public enum SetupStep
{
    InstallWsl,
    StartWsl,
    FixWsl,
    InstallDistro,
    InstallCodexBar,
    Ready
}

/// <summary>
/// Represents WSL-related errors.
/// </summary>
public enum WslErrorType
{
    /// <summary>WSL command not found (not installed).</summary>
    NotInstalled,
    /// <summary>WSL is installed but not running (needs restart or initialization).</summary>
    NotRunning,
    /// <summary>WSL command timed out.</summary>
    Timeout,
    /// <summary>Other WSL error.</summary>
    Other
}
