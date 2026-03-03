# CLI

Intune Commander includes a headless CLI app (`ic`) for automation, CI/CD, and scheduled backups.

## Authentication

You can authenticate with either a saved profile or explicit tenant credentials.

### Saved profile

```bash
ic export --profile Contoso-Prod --output ./export
```

### Environment variables (CI-friendly)

```bash
export IC_TENANT_ID="<tenant-guid>"
export IC_CLIENT_ID="<client-guid>"
export IC_CLIENT_SECRET="<secret>"
export IC_CLOUD="Commercial"

ic export --output ./export --types all
```

## Commands

### Export

```bash
ic export --profile Contoso-Prod --output ./export --types all
```

### Import

```bash
ic import --folder ./export --profile Contoso-Dev
ic import --folder ./export --profile Contoso-Dev --dry-run
```

### List

```bash
ic list compliance --profile Contoso-Prod --format json
ic list configurations --profile Contoso-Prod --format table
```

### Profile

```bash
ic profile list
ic profile test --name Contoso-Prod
```

## Output behavior

- `stdout`: command output (JSON/table)
- `stderr`: progress and auth prompts (for example, Device Code messages)
