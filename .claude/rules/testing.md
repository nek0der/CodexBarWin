# Testing Requirements

## Scope

Unit tests REQUIRED for changes to:
- Services/
- Models/
- Helpers/

UI layer (Views, ViewModels) is EXEMPT.

## Naming Conventions

**File**: `{ClassName}Tests.cs`

Example: `CodexBarService.cs` → `CodexBarServiceTests.cs`

**Method**: `{MethodName}_{Condition}_{ExpectedResult}`

Examples:
- `GetUsageDataAsync_WhenValidProvider_ReturnsUsageData`
- `ValidateAndNormalize_WhenInvalidProvider_ThrowsException`

## Tech Stack

- MSTest (test framework)
- Moq (mocking)
- FluentAssertions (assertions)

## Running Tests

```bash
dotnet test CodexBarWin.Tests.Unit -c Debug -p:Platform=x64
```
