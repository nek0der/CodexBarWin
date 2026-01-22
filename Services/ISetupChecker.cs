using CodexBarWin.Models;

namespace CodexBarWin.Services;

/// <summary>
/// Interface for checking application setup status.
/// </summary>
public interface ISetupChecker
{
    /// <summary>
    /// Checks the setup status of the application.
    /// </summary>
    Task<SetupStatus> CheckAsync(CancellationToken ct = default);
}
