# Sample Data for Developer Mode

This directory contains sample JSON files used when Developer Mode is enabled in CodexBarWin.

## Files

- `claude.json` - Sample data for Claude AI
- `gemini.json` - Sample data for Google Gemini
- `codex.json` - Sample data for OpenAI Codex

## Usage

When Developer Mode is enabled in Settings:
1. The app will load sample data from these JSON files instead of calling the CodexBar CLI
2. Each file should contain a JSON array with usage data matching the structure returned by `codexbar usage --format json`

## JSON Structure

The sample files follow the structure returned by `codexbar usage --format json`. The structure is based on [CodexBar CLI documentation](https://github.com/steipete/CodexBar/blob/main/docs/cli.md):

```json
[
  {
    "provider": "claude|gemini|codex",
    "source": "oauth|cli|openai-web|api",
    "usage": {
      "loginMethod": "OAuth|API|plus|free",
      "primary": {
        "usedPercent": 45.0,
        "windowMinutes": 300,
        "resetsAt": "2026-01-24T20:00:00Z",
        "resetDescription": "in 5 hours"
      },
      "secondary": {
        "usedPercent": 68.0,
        "windowMinutes": 10080,
        "resetsAt": "2026-01-28T00:00:00Z",
        "resetDescription": "in 4 days"
      },
      "tertiary": {
        "usedPercent": 12.0,
        "windowMinutes": 10080,
        "resetsAt": "2026-01-28T00:00:00Z",
        "resetDescription": "in 4 days (Sonnet)"
      },
      "updatedAt": "2026-01-24T15:00:00Z"
    },
    "error": null
  }
]
```

### Field Descriptions

- **provider**: Provider identifier (`claude`, `gemini`, `codex`)
- **source**: Data source method (`oauth`, `cli`, `openai-web`, `api`)
- **usage.loginMethod**: Authentication method used
- **usage.primary**: Session usage (e.g., "Session" for Claude/Codex, "Pro" for Gemini)
- **usage.secondary**: Weekly/longer window usage (e.g., "Weekly" for Claude/Codex, "Flash" for Gemini)
- **usage.tertiary**: Additional usage window (optional, e.g., "Sonnet Weekly" for Claude)
- **usedPercent**: Percentage of quota used (0-100)
- **windowMinutes**: Duration of the usage window in minutes
- **resetsAt**: ISO 8601 timestamp when the quota resets
- **resetDescription**: Human-readable description of when quota resets
- **updatedAt**: ISO 8601 timestamp when the data was last updated
- **error**: Error message if data fetch failed (null if successful)

## Updating Sample Data

To update sample data with real data from CodexBar CLI:

```bash
# In WSL
codexbar usage --provider claude --format json --pretty > claude.json
codexbar usage --provider gemini --format json --pretty > gemini.json
codexbar usage --provider codex --format json --pretty > codex.json
```

## Notes

- These files are intended for development and testing purposes only
- The data values are fictional and do not represent actual API usage
- Keep these files in sync with the latest CodexBar JSON output format
