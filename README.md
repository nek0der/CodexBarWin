# CodexBarWin

Windows native UI for AI coding tool usage monitoring.

[![CI](https://github.com/nek0der/CodexBarWin/actions/workflows/build.yml/badge.svg)](https://github.com/nek0der/CodexBarWin/actions/workflows/build.yml)
[![Test](https://github.com/nek0der/CodexBarWin/actions/workflows/test.yml/badge.svg)](https://github.com/nek0der/CodexBarWin/actions/workflows/test.yml)
[![CodeQL](https://github.com/nek0der/CodexBarWin/actions/workflows/codeql.yml/badge.svg)](https://github.com/nek0der/CodexBarWin/actions/workflows/codeql.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![WinUI](https://img.shields.io/badge/WinUI-3-blue)](https://microsoft.github.io/microsoft-ui-xaml/)

## Overview

A Windows 11 native app that displays usage limits for AI coding tools (Claude Code, Codex, Cursor, Copilot, etc.) in the system tray.

This is a WinUI 3 frontend wrapper for [Win-CodexBar CLI](https://github.com/Finesssee/Win-CodexBar).

## Screenshots

*Screenshots coming soon*

## Features

- Native Windows 11 UI (WinUI 3 + Mica)
- Multi-provider support
- System tray integration
- Dark mode support
- Auto refresh

## Supported Providers

- Claude
- Codex (OpenAI)
- Gemini

## Requirements

- Windows 10 (1809+) / Windows 11
- .NET 8.0 Runtime
- [Win-CodexBar CLI](https://github.com/Finesssee/Win-CodexBar/releases)

## Installation

### Download Release

Download the latest release from [Releases](https://github.com/nek0der/CodexBarWin/releases).

### Build from Source

```bash
git clone https://github.com/nek0der/CodexBarWin.git
cd CodexBarWin
dotnet build -c Release
```

## Usage

1. Launch CodexBarWin
2. App minimizes to system tray
3. Click tray icon to view usage

## Documentation

- [Contributing Guidelines](CONTRIBUTING.md)
- [Changelog](CHANGELOG.md)
- [Security Policy](SECURITY.md)
- [Code of Conduct](CODE_OF_CONDUCT.md)

## Contributing

Contributions are welcome! Please read our [Contributing Guidelines](CONTRIBUTING.md) before submitting a Pull Request.

## Related Projects

- [CodexBar](https://github.com/steipete/CodexBar) - Original macOS app

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

Third-party dependency licenses are automatically checked in CI. To generate a license report locally:

```bash
dotnet tool install --global nuget-license
nuget-license --input CodexBarWin.sln
```
