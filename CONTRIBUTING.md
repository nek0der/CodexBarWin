# Contributing to CodexBarWin

Thank you for your interest in contributing to CodexBarWin!

## Getting Started

### Prerequisites

- Windows 10 (1809+) or Windows 11
- Visual Studio 2022 with:
  - .NET Desktop Development workload
  - Windows App SDK extension
- .NET 10.0 SDK
- Git

### Development Setup

1. Fork the repository
2. Clone your fork:
   ```bash
   git clone https://github.com/YOUR_USERNAME/CodexBarWin.git
   cd CodexBarWin
   ```
3. Open `CodexBarWin.sln` in Visual Studio 2022
4. Restore NuGet packages
5. Build and run

## How to Contribute

### Reporting Bugs

Before creating a bug report, please check existing issues to avoid duplicates.

When filing a bug report, include:
- Windows version
- .NET version (`dotnet --version`)
- Steps to reproduce
- Expected vs actual behavior
- Screenshots if applicable

Use the [bug report template](.github/ISSUE_TEMPLATE/bug_report.yml).

### Suggesting Features

Feature requests are welcome! Use the [feature request template](.github/ISSUE_TEMPLATE/feature_request.yml).

### Pull Requests

1. Create a branch from `main`:
   ```bash
   git checkout -b feature/your-feature-name
   ```
2. Make your changes
3. Test your changes thoroughly
4. Commit with a clear message:
   ```bash
   git commit -m "Add: brief description of change"
   ```
5. Push to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```
6. Open a Pull Request

#### PR Title Format (Enforced by CI)

PR titles must follow this format and are automatically checked:

```
Type: brief description
```

Valid types:
- `Add:` new features
- `Fix:` bug fixes
- `Update:` improvements to existing features
- `Remove:` removing code/features
- `Docs:` documentation changes
- `Refactor:` code refactoring
- `Test:` adding/updating tests

Examples:
- `Add: dark mode toggle in settings`
- `Fix: memory leak in main window`
- `Refactor: service layer architecture`

> **Note**: We use Squash & Merge, so your PR title becomes the final commit message.

### Code Style

- Follow existing code patterns
- Use C# naming conventions
- Use meaningful variable/method names
- Add XML documentation for public APIs
- Keep methods focused and small

### Testing

- Test on both Windows 10 and Windows 11 if possible
- Verify dark mode and light mode appearance
- Test with different DPI settings
- Ensure no memory leaks

## Project Structure

```
CodexBarWin/
├── Controls/       # Custom UI controls
├── Models/         # Data models
├── Services/       # Business logic services
├── ViewModels/     # MVVM ViewModels
├── Views/          # XAML pages
└── Assets/         # Images and resources
```

## Questions?

Feel free to open an issue for questions or reach out via GitHub Discussions.

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
