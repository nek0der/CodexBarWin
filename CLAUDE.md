# CodexBarWin

Windows 11 WinUI 3 app for AI coding tool usage monitoring. Frontend for [CodexBar CLI](https://github.com/steipete/CodexBar).

## Stack

.NET 10, WinUI 3, Windows App SDK, MVVM (CommunityToolkit.Mvvm), H.NotifyIcon.WinUI, MSTest/Moq/FluentAssertions

## Commands

```bash
dotnet build -c Release -p:Platform=x64                          # Build
dotnet test CodexBarWin.Tests.Unit -c Debug -p:Platform=x64      # Test
dotnet restore CodexBarWin.sln                                   # Restore
```

## Structure

```
Services/       - Business logic (IWslService, ICodexBarService)
Models/         - Data models, DTOs
ViewModels/     - MVVM ViewModels
Views/          - XAML pages
Helpers/        - Utility classes
```

## Code Style

- **All deliverables in English** (code, comments, commits, docs)
- **Async**: Use `SafeFireAndForget()`, no bare `async void` except event handlers
- **Logging**: `ILogger<T>`, not `Debug.WriteLine`
- **Security**: Use `ProviderConstants.ValidateAndNormalize()` for provider names

## Testing

Required for Services/, Models/, Helpers/ changes. See [.claude/rules/testing.md](.claude/rules/testing.md) for details.

## Pull Requests

**Before creating PR**: Read [.claude/rules/pr-workflow.md](.claude/rules/pr-workflow.md) for validation checklist and process.

PR titles must follow format: `Add:`, `Fix:`, `Update:`, `Refactor:`, `Test:`, `Docs:`, `Remove:`

## Security

Read [.claude/rules/security.md](.claude/rules/security.md) before modifying Services/Models.

Critical rules:
- NO GPL dependencies (MIT incompatible)
- NO hardcoded secrets
- ALWAYS validate provider input

## Important Files

- `Services/CodexBarService.cs` - Core usage data fetching
- `Models/ProviderConfig.cs` - Provider validation (security-critical)
- `MainWindow.xaml.cs` - System tray, window animation
- `ViewModels/MainViewModel.cs` - Main UI state

## CI/CD

Workflows: build, test, licenses, pr-title, stale

All PRs must pass CI. Uses Squash & Merge strategy.

## References

- Setup: [CONTRIBUTING.md](CONTRIBUTING.md#development-setup)
- Architecture: [CONTRIBUTING.md](CONTRIBUTING.md#project-structure)
- Security Policy: [SECURITY.md](SECURITY.md)
- PR Template: [.github/pull_request_template.md](.github/pull_request_template.md)
- Changelog: [CHANGELOG.md](CHANGELOG.md)
