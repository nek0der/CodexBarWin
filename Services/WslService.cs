using System.Diagnostics;
using System.Text;
using CodexBarWin.Models;
using Microsoft.Extensions.Logging;

namespace CodexBarWin.Services;

/// <summary>
/// Service for executing commands in WSL.
/// </summary>
public class WslService : IWslService
{
    private readonly ILogger<WslService> _logger;
    private readonly ISettingsService? _settingsService;
    private readonly string? _distro;

    private TimeSpan Timeout => _settingsService != null
        ? TimeSpan.FromSeconds(_settingsService.Settings.Timeouts.WslCommandTimeoutSeconds)
        : TimeSpan.FromSeconds(30);

    private TimeSpan StatusCheckTimeout => _settingsService != null
        ? TimeSpan.FromSeconds(_settingsService.Settings.Timeouts.WslStatusCheckTimeoutSeconds)
        : TimeSpan.FromSeconds(10);

    public WslService(ILogger<WslService> logger, ISettingsService? settingsService = null, string? distro = null)
    {
        _logger = logger;
        _settingsService = settingsService;
        _distro = distro;
    }

    public async Task<WslResult> ExecuteAsync(string command, CancellationToken ct = default)
    {
        _logger.LogDebug("Executing WSL command: {Command}", command);

        // bash -ic loads .bashrc which sets PATH
        var shellScript = command;

        var psi = new ProcessStartInfo
        {
            FileName = "wsl",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        // Use ArgumentList for proper escaping
        if (_distro != null)
        {
            psi.ArgumentList.Add("-d");
            psi.ArgumentList.Add(_distro);
        }
        psi.ArgumentList.Add("--");
        psi.ArgumentList.Add("bash");
        psi.ArgumentList.Add("-ic");  // -i for interactive (loads .bashrc for Codex CLI)
        psi.ArgumentList.Add(shellScript);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(Timeout);

        Process? process = null;
        try
        {
            process = new Process { StartInfo = psi };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cts.Token);
            var errorTask = process.StandardError.ReadToEndAsync(cts.Token);

            await process.WaitForExitAsync(cts.Token);

            var output = await outputTask;
            var error = await errorTask;

            _logger.LogDebug("WSL command completed: {Command}, ExitCode={ExitCode}",
                command, process.ExitCode);

            return new WslResult
            {
                Success = process.ExitCode == 0,
                Output = output,
                Error = error,
                ExitCode = process.ExitCode
            };
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            _logger.LogWarning("WSL command timed out after {Timeout}s: {Command}", Timeout.TotalSeconds, command);
            return new WslResult
            {
                Success = false,
                Error = $"Command timed out after {Timeout.TotalSeconds} seconds",
                ExitCode = -1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute WSL command: {Command}", command);
            return new WslResult
            {
                Success = false,
                Error = ex.Message,
                ExitCode = -1
            };
        }
        finally
        {
            // Always kill the process if still running to prevent zombie wslhost.exe
            KillProcessSafely(process);
            process?.Dispose();
        }
    }

    public async Task<bool> IsWslInstalledAsync(CancellationToken ct = default)
    {
        var result = await CheckWslStatusAsync(ct);
        return result.IsInstalled;
    }

    public async Task<WslStatusResult> CheckWslStatusAsync(CancellationToken ct = default)
    {
        Process? process = null;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "wsl",
                Arguments = "--status",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process = Process.Start(psi);
            if (process == null)
            {
                return new WslStatusResult { IsInstalled = false, Error = Models.WslErrorType.NotInstalled };
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(StatusCheckTimeout);

            var outputTask = process.StandardOutput.ReadToEndAsync(cts.Token);
            var errorTask = process.StandardError.ReadToEndAsync(cts.Token);

            await process.WaitForExitAsync(cts.Token);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode == 0)
            {
                return new WslStatusResult { IsInstalled = true, IsRunning = true };
            }

            // Check for specific error conditions
            var combinedOutput = (output + error).ToLowerInvariant();
            if (combinedOutput.Contains("not running") || combinedOutput.Contains("起動していません"))
            {
                _logger.LogWarning("WSL is installed but not running");
                return new WslStatusResult
                {
                    IsInstalled = true,
                    IsRunning = false,
                    Error = Models.WslErrorType.NotRunning,
                    ErrorMessage = "WSL is not running. Please restart WSL."
                };
            }

            return new WslStatusResult
            {
                IsInstalled = true,
                IsRunning = false,
                Error = Models.WslErrorType.Other,
                ErrorMessage = error
            };
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 2)
        {
            // File not found - WSL not installed
            _logger.LogDebug("WSL command not found");
            return new WslStatusResult { IsInstalled = false, Error = Models.WslErrorType.NotInstalled };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("WSL status check timed out");
            return new WslStatusResult
            {
                IsInstalled = true,
                IsRunning = false,
                Error = Models.WslErrorType.Timeout,
                ErrorMessage = "WSL status check timed out. WSL may be starting up."
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "WSL is not installed or not accessible");
            return new WslStatusResult { IsInstalled = false, Error = Models.WslErrorType.NotInstalled };
        }
        finally
        {
            KillProcessSafely(process);
            process?.Dispose();
        }
    }

    public async Task<List<string>> GetDistrosAsync(CancellationToken ct = default)
    {
        Process? process = null;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "wsl",
                Arguments = "-l -q",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.Unicode
            };

            process = Process.Start(psi);
            if (process == null) return [];

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(StatusCheckTimeout);

            var output = await process.StandardOutput.ReadToEndAsync(cts.Token);
            await process.WaitForExitAsync(cts.Token);

            return output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim('\0', ' ', '\r'))
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get WSL distros");
            return [];
        }
        finally
        {
            KillProcessSafely(process);
            process?.Dispose();
        }
    }

    /// <summary>
    /// Safely kills a process and its entire process tree if still running.
    /// </summary>
    private void KillProcessSafely(Process? process)
    {
        if (process == null) return;

        try
        {
            if (!process.HasExited)
            {
                _logger.LogDebug("Killing WSL process {ProcessId}", process.Id);
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to kill WSL process");
        }
    }
}
