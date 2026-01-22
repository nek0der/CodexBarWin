using CodexBarWin.Models;
using CodexBarWin.Services;

namespace CodexBarWin.Tests.Unit.Mocks;

/// <summary>
/// Mock implementation of IWslService for testing.
/// </summary>
public class MockWslService : IWslService
{
    private readonly Dictionary<string, WslResult> _commandResults = new();
    private WslStatusResult _statusResult = new() { IsInstalled = true, IsRunning = true };
    private List<string> _distros = ["Ubuntu"];

    public List<string> ExecutedCommands { get; } = [];

    public void SetCommandResult(string command, WslResult result)
    {
        _commandResults[command] = result;
    }

    public void SetStatusResult(WslStatusResult result)
    {
        _statusResult = result;
    }

    public void SetDistros(List<string> distros)
    {
        _distros = distros;
    }

    public Task<WslResult> ExecuteAsync(string command, CancellationToken ct = default)
    {
        ExecutedCommands.Add(command);

        if (_commandResults.TryGetValue(command, out var result))
        {
            return Task.FromResult(result);
        }

        // Default success result
        return Task.FromResult(new WslResult
        {
            Success = true,
            Output = "[]",
            ExitCode = 0
        });
    }

    public Task<bool> IsWslInstalledAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_statusResult.IsInstalled);
    }

    public Task<WslStatusResult> CheckWslStatusAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_statusResult);
    }

    public Task<List<string>> GetDistrosAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_distros);
    }
}
