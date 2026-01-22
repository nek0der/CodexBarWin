using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CodexBarWin.Models;
using CodexBarWin.Services;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace CodexBarWin.ViewModels;

/// <summary>
/// ViewModel for the setup page.
/// </summary>
public partial class SetupViewModel : ObservableObject
{
    private readonly ISetupChecker _setupChecker;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SetupViewModel> _logger;

    [ObservableProperty]
    private SetupStatus? _status;

    [ObservableProperty]
    private bool _isChecking;

    [ObservableProperty]
    private string _headerIcon = "\u26A0";

    [ObservableProperty]
    private string _headerText = "Setup Required";

    [ObservableProperty]
    private string _wslStatusIcon = "\u274C";

    [ObservableProperty]
    private string _wslStatusText = "Checking WSL...";

    [ObservableProperty]
    private string _codexBarStatusIcon = "\u274C";

    [ObservableProperty]
    private string _codexBarStatusText = "Checking codexbar...";

    [ObservableProperty]
    private string _instructionText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CommandItem> _commands = [];

    [ObservableProperty]
    private bool _isReady;

    public event EventHandler? SetupComplete;

    public SetupViewModel(ISetupChecker setupChecker, ISettingsService settingsService, ILogger<SetupViewModel> logger)
    {
        _setupChecker = setupChecker;
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the setup view with a status check.
    /// </summary>
    public async Task InitializeAsync()
    {
        await RecheckAsync();
    }

    [RelayCommand]
    private async Task RecheckAsync()
    {
        if (IsChecking)
        {
            return;
        }

        IsChecking = true;

        try
        {
            _logger.LogInformation("Checking setup status...");
            Status = await _setupChecker.CheckAsync();
            UpdateUI();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check setup status");
        }
        finally
        {
            IsChecking = false;
        }
    }

    [RelayCommand]
    private async Task OpenGuideAsync()
    {
        var url = _settingsService.Settings.CodexBarGuideUrl;
        await Launcher.LaunchUriAsync(new Uri(url));
    }

    [RelayCommand]
    private void Start()
    {
        if (Status?.IsReady == true)
        {
            SetupComplete?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UpdateUI()
    {
        if (Status == null)
        {
            return;
        }

        // Update WSL status
        if (Status.WslInstalled && Status.WslRunning)
        {
            WslStatusIcon = "\u2705";
            WslStatusText = Status.DefaultDistro != null
                ? $"WSL ({Status.DefaultDistro})"
                : "WSL installed";
        }
        else if (Status.WslInstalled && !Status.WslRunning)
        {
            WslStatusIcon = "\u26A0";
            WslStatusText = Status.WslError switch
            {
                Models.WslErrorType.NotRunning => "WSL is not running",
                Models.WslErrorType.Timeout => "WSL is starting up...",
                _ => "WSL has an error"
            };
        }
        else
        {
            WslStatusIcon = "\u274C";
            WslStatusText = "WSL is not installed";
        }

        // Update codexbar status
        if (Status.CodexBarInstalled)
        {
            CodexBarStatusIcon = "\u2705";
            CodexBarStatusText = Status.CodexBarVersion != null
                ? $"codexbar CLI {Status.CodexBarVersion}"
                : "codexbar CLI installed";
        }
        else if (Status.WslInstalled && Status.WslRunning && Status.Distros.Count > 0)
        {
            CodexBarStatusIcon = "\u274C";
            CodexBarStatusText = "codexbar CLI is not installed";
        }
        else
        {
            CodexBarStatusIcon = "\u2B1C"; // Gray square - not checked yet
            CodexBarStatusText = "codexbar CLI (waiting for WSL)";
        }

        // Update header and instructions
        IsReady = Status.IsReady;

        if (Status.IsReady)
        {
            HeaderIcon = "\u2705";
            HeaderText = "Setup Complete";
            InstructionText = "Make sure you're logged in to your AI tools:\n\n" +
                              "  - Claude Code: wsl -e claude login\n" +
                              "  - Codex: wsl -e codex login";
            Commands.Clear();
        }
        else
        {
            HeaderIcon = "\u26A0";
            HeaderText = "Setup Required";
            UpdateCommands();
        }
    }

    private void UpdateCommands()
    {
        Commands.Clear();

        switch (Status?.CurrentStep)
        {
            case SetupStep.InstallWsl:
                InstructionText = "CodexBarWin requires WSL to function.\n" +
                                  "Open PowerShell as Administrator and run:";
                Commands.Add(new CommandItem("wsl --install -d Ubuntu", CopyCommand));
                break;

            case SetupStep.StartWsl:
                InstructionText = "WSL is installed but not running.\n" +
                                  "Try the following commands in PowerShell:";
                Commands.Add(new CommandItem("wsl --shutdown", CopyCommand));
                Commands.Add(new CommandItem("wsl", CopyCommand));
                break;

            case SetupStep.FixWsl:
                InstructionText = "WSL has an error. Try restarting WSL:\n";
                Commands.Add(new CommandItem("wsl --shutdown", CopyCommand));
                Commands.Add(new CommandItem("wsl --update", CopyCommand));
                break;

            case SetupStep.InstallDistro:
                InstructionText = "A WSL distribution is required.\n" +
                                  "Open PowerShell and run:";
                Commands.Add(new CommandItem("wsl --install -d Ubuntu", CopyCommand));
                break;

            case SetupStep.InstallCodexBar:
                InstructionText = "Install codexbar CLI in WSL.\n\n" +
                                  "Method 1: Homebrew (recommended)";
                Commands.Add(new CommandItem("brew install steipete/tap/codexbar", CopyCommand));

                InstructionText += "\n\nMethod 2: Direct download";
                Commands.Add(new CommandItem(
                    "curl -LO https://github.com/steipete/CodexBar/releases/download/v0.17.0/CodexBarCLI-v0.17.0-linux-x86_64.tar.gz && " +
                    "tar -xzf CodexBarCLI-*.tar.gz && " +
                    "sudo mv codexbar /usr/local/bin/",
                    CopyCommand));
                break;
        }
    }

    private void CopyCommand(string command)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(command);
        Clipboard.SetContent(dataPackage);
        _logger.LogDebug("Copied command to clipboard: {Command}", command);
    }
}

/// <summary>
/// Represents a command item for the setup page.
/// </summary>
public partial class CommandItem : ObservableObject
{
    public string Command { get; }
    private readonly Action<string> _copyAction;

    public CommandItem(string command, Action<string> copyAction)
    {
        Command = command;
        _copyAction = copyAction;
    }

    [RelayCommand]
    private void Copy()
    {
        _copyAction(Command);
    }
}
