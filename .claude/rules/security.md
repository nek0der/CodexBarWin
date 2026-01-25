# Security Rules

## Provider Validation

**ALWAYS** use `ProviderConstants.ValidateAndNormalize()` before executing WSL commands with provider names.

See `Models/ProviderConfig.cs` for validation logic.

## Secrets Management

- NO hardcoded credentials
- NO API keys in code
- NO secrets in CLAUDE.md or documentation

## Dependencies

- NO GPL-licensed packages (MIT incompatible)
- Check `.github/workflows/licenses.yml` for enforcement

## Async Safety

- NO bare `async void` except event handlers
- ALL event handlers: wrap with try-catch
- Fire-and-forget: use `SafeFireAndForget()`

See SECURITY.md for full policy.
