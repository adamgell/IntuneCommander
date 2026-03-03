# Drift detection (CLI)

Intune Commander includes CLI commands for file-based drift detection workflows:

```bash
ic export --output ./baseline --normalize

ic diff \
  --baseline ./baseline \
  --current ./current \
  --format json \
  --output drift-report.json \
  --min-severity medium \
  --fail-on-drift

ic alert teams --webhook "$TEAMS_WEBHOOK" --report drift-report.json
```

## Commands

- `ic export --normalize` normalizes exported JSON in the target folder (volatile fields removed, keys/arrays sorted).
- `ic diff` compares two export folders and produces a drift report (json, text, or markdown).
- `ic alert` supports `teams`, `slack`, `github`, and `email` providers.
