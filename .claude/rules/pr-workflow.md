# Pull Request Workflow

**IMPORTANT**: Use PR template (`.github/pull_request_template.md`).

## Pre-PR Validation

Before creating PR, verify:

1. **Testing**: If Services/Models/Helpers changed → unit tests exist
2. **Security**:
   - Proper `ProviderConstants.ValidateAndNormalize()` usage
   - No hardcoded secrets (see SECURITY.md)
3. **Async**: `SafeFireAndForget()` used, no bare `async void` except handlers
4. **Dependencies**: No GPL-licensed packages (check .csproj)
5. **PR title**: Follows convention (CONTRIBUTING.md#pr-title-format)
6. **Quality**: No `Debug.WriteLine`, proper error handling
7. **Changelog**: User-facing changes → CHANGELOG.md updated

**If validation fails**: STOP. Report issues. DO NOT create PR.

## PR Creation Process

1. **Analyze changes**:
   ```bash
   git log [base-branch]...HEAD      # ALL commits
   git diff [base-branch]...HEAD     # Full scope
   ```

2. **Fill template** (`.github/pull_request_template.md`):
   - Description (what + why)
   - Related issue or "N/A"
   - Type checkboxes
   - Test description
   - Checklist (mark all applicable)
   - Breaking changes or "N/A"

3. **Create PR**:
   ```bash
   gh pr create --title "Prefix: Description" --body "$(cat <<'EOF'
   [Full template content]
   EOF
   )"
   ```

## Critical Rules

- Analyze ALL commits, not just latest
- Validate BEFORE creating PR
- Preserve template structure (all sections, checkboxes)
- Use HEREDOC for multi-line formatting
