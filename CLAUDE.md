# CLAUDE.md

## Project Overview

CodexBarWin is a Windows 11 native app (WinUI 3) that displays AI coding tool usage limits in the system tray. It's a frontend for [Win-CodexBar CLI](https://github.com/Finesssee/Win-CodexBar).

## Tech Stack

- **Framework**: .NET 10, WinUI 3, Windows App SDK
- **Architecture**: MVVM (CommunityToolkit.Mvvm)
- **DI**: Microsoft.Extensions.DependencyInjection
- **System Tray**: H.NotifyIcon.WinUI
- **Testing**: MSTest, Moq, FluentAssertions

## Project Structure

```
CodexBarWin/
├── Models/          # Data models and DTOs
├── Services/        # Business logic (IWslService, ICodexBarService, etc.)
├── ViewModels/      # MVVM ViewModels
├── Views/           # XAML pages
├── Helpers/         # Utility classes (AsyncHelper)
├── Controls/        # Custom UI controls
└── Assets/          # Images and resources
```

## Commands

```bash
# Build
dotnet build -c Release -p:Platform=x64

# Run tests
dotnet test CodexBarWin.Tests.Unit -c Debug -p:Platform=x64

# Restore packages
dotnet restore CodexBarWin.sln
```

## Code Conventions

- **PR titles**: Use prefixes (`Add:`, `Fix:`, `Update:`, `Refactor:`, `Test:`, `Docs:`) - enforced by CI
- **Async methods**: Use `SafeFireAndForget()` for fire-and-forget, always handle exceptions
- **Provider validation**: Use `ProviderConstants.ValidateAndNormalize()` for security
- **Logging**: Use `ILogger<T>`, not `Debug.WriteLine`
- **Testing**: Services, Models, Helpers への変更時はユニットテスト必須
  - テストファイル命名: `{対象クラス名}Tests.cs`
  - テストメソッド命名: `{メソッド名}_{条件}_{期待結果}` 形式
  - UI層（Views, ViewModels）はテスト対象外

## Important Files

- `Services/CodexBarService.cs` - Core usage data fetching logic
- `Models/ProviderConfig.cs` - Provider validation (security-critical)
- `MainWindow.xaml.cs` - System tray and window animation
- `ViewModels/MainViewModel.cs` - Main UI state management

## Do Not

- Add GPL-licensed dependencies (MIT incompatible)
- Hardcode secrets or credentials
- Use `async void` except for event handlers (wrap with try-catch)
- Skip provider validation when executing WSL commands
- Merge logic changes to Services/Models/Helpers without corresponding tests

## CI/CD

- **Build**: `.github/workflows/build.yml`
- **Test**: `.github/workflows/test.yml`
- **License Check**: `.github/workflows/licenses.yml`
- **PR Title**: `.github/workflows/pr-title.yml`
- **Stale**: `.github/workflows/stale.yml` (auto-close inactive issues/PRs)

All PRs must pass CI checks before merging. Uses Squash & Merge strategy.
