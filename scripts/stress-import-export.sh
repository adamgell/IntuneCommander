#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage:
  scripts/stress-import-export.sh [--profile NAME | --seed PATH] [options]

Options:
  --profile NAME        Saved CLI profile to use for a one-time live export.
  --seed PATH           Existing normalized export folder to reuse offline.
  --workspace PATH      Workspace for generated artifacts.
  --scale N             Total copies of each JSON export to keep in baseline/current. Default: 3
  --mutate-count N      Number of current-side files to mutate for diff testing. Default: 5
  --types VALUE         Forwarded to `ic export --types` when --profile is used. Default: all
  --ic PATH             Optional path to an existing `ic` executable.
  --help                Show this help text.

Notes:
  - Pick exactly one of --profile or --seed.
  - Phase 1 stays offline after the initial export. All dry-run and diff work happens on files only.
EOF
}

timer_now() {
  python3 - <<'PY'
import time
print(f"{time.time():.9f}")
PY
}

record_metric() {
  local label=$1
  local start=$2
  local end
  local duration

  end="$(timer_now)"
  duration="$(python3 - "$start" "$end" <<'PY'
import sys
start = float(sys.argv[1])
end = float(sys.argv[2])
print(f"{end - start:.6f}")
PY
)"

  printf "%s\t%s\n" "$label" "$duration" >> "$METRICS_TSV"
}

PROFILE=""
SEED=""
WORKSPACE=""
SCALE=3
MUTATE_COUNT=5
TYPES="all"
IC_OVERRIDE=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --profile)
      PROFILE=${2:?Missing value for --profile}
      shift 2
      ;;
    --seed)
      SEED=${2:?Missing value for --seed}
      shift 2
      ;;
    --workspace)
      WORKSPACE=${2:?Missing value for --workspace}
      shift 2
      ;;
    --scale)
      SCALE=${2:?Missing value for --scale}
      shift 2
      ;;
    --mutate-count)
      MUTATE_COUNT=${2:?Missing value for --mutate-count}
      shift 2
      ;;
    --types)
      TYPES=${2:?Missing value for --types}
      shift 2
      ;;
    --ic)
      IC_OVERRIDE=${2:?Missing value for --ic}
      shift 2
      ;;
    --help|-h)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if [[ -n "$PROFILE" && -n "$SEED" ]]; then
  echo "Choose either --profile or --seed, not both." >&2
  exit 1
fi

if [[ -z "$PROFILE" && -z "$SEED" ]]; then
  echo "You must provide either --profile or --seed." >&2
  exit 1
fi

if ! [[ "$SCALE" =~ ^[0-9]+$ ]] || [[ "$SCALE" -lt 1 ]]; then
  echo "--scale must be an integer >= 1." >&2
  exit 1
fi

if ! [[ "$MUTATE_COUNT" =~ ^[0-9]+$ ]] || [[ "$MUTATE_COUNT" -lt 0 ]]; then
  echo "--mutate-count must be an integer >= 0." >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

if [[ -z "$WORKSPACE" ]]; then
  WORKSPACE="$REPO_ROOT/artifacts/stress-$(date +%Y%m%d-%H%M%S)"
fi

SEED_EXPORT="$WORKSPACE/seed-export"
BASELINE_DIR="$WORKSPACE/baseline"
CURRENT_DIR="$WORKSPACE/current"
REPORTS_DIR="$WORKSPACE/reports"
METRICS_TSV="$REPORTS_DIR/benchmark-metrics.tsv"
BENCHMARK_JSON="$REPORTS_DIR/benchmark-summary.json"

mkdir -p "$SEED_EXPORT" "$BASELINE_DIR" "$CURRENT_DIR" "$REPORTS_DIR"
printf "step\tdurationSeconds\n" > "$METRICS_TSV"

if [[ -n "$IC_OVERRIDE" ]]; then
  IC_CMD=("$IC_OVERRIDE")
else
  echo "Building CLI once for repeatable timings..."
  start_time="$(timer_now)"
  dotnet build --nologo "$REPO_ROOT/src/Intune.Commander.CLI/Intune.Commander.CLI.csproj" > "$REPORTS_DIR/cli-build.log"
  record_metric "cli-build" "$start_time"
  IC_CMD=(dotnet "$REPO_ROOT/src/Intune.Commander.CLI/bin/Debug/net10.0/ic.dll")
fi

run_ic() {
  "${IC_CMD[@]}" "$@"
}

copy_tree() {
  local source=$1
  local destination=$2
  rm -rf "$destination"
  mkdir -p "$destination"
  cp -R "$source"/. "$destination"/
}

scale_directory() {
  local target_dir=$1
  local scale_factor=$2

  python3 - "$target_dir" "$scale_factor" <<'PY'
import copy
import json
import pathlib
import sys

root = pathlib.Path(sys.argv[1])
scale = int(sys.argv[2])

if scale <= 1:
    raise SystemExit(0)

def rewrite(node, label):
    if isinstance(node, dict):
        updated = {}
        for key, value in node.items():
            child = rewrite(value, label)
            lowered = key.lower()
            if isinstance(child, str):
                if lowered == "id":
                    child = f"{child}-{label}"
                elif lowered in {"displayname", "name"} and child:
                    child = f"{child} [{label}]"
                elif lowered == "description" and child:
                    child = f"{child} [{label}]"
            updated[key] = child
        return updated

    if isinstance(node, list):
        return [rewrite(item, label) for item in node]

    return node

files = sorted(path for path in root.rglob("*.json") if path.name.lower() != "migration-table.json")

for path in files:
    payload = json.loads(path.read_text())
    for index in range(2, scale + 1):
        label = f"clone-{index:03d}"
        clone = rewrite(copy.deepcopy(payload), label)
        clone_path = path.with_name(f"{path.stem}__{label}{path.suffix}")
        clone_path.write_text(json.dumps(clone, indent=2) + "\n")
PY
}

mutate_directory() {
  local target_dir=$1
  local mutate_count=$2

  python3 - "$target_dir" "$mutate_count" <<'PY'
import json
import pathlib
import sys

root = pathlib.Path(sys.argv[1])
mutate_count = int(sys.argv[2])

def mutate_low(node):
    if isinstance(node, dict):
        for key, value in node.items():
            lowered = key.lower()
            if lowered in {"displayname", "name", "description"} and isinstance(value, str) and value:
                node[key] = f"{value} [offline-mutated]"
                return True
        for value in node.values():
            if mutate_low(value):
                return True
    elif isinstance(node, list):
        for item in node:
            if mutate_low(item):
                return True
    return False

def mutate_assignment(node):
    def mutate_existing_string(value):
        if isinstance(value, dict):
            for nested_key, nested_value in value.items():
                if isinstance(nested_value, str) and nested_value:
                    value[nested_key] = f"{nested_value}-offline-mutated"
                    return True
                if mutate_existing_string(nested_value):
                    return True
        elif isinstance(value, list):
            for index, item in enumerate(value):
                if isinstance(item, str) and item:
                    value[index] = f"{item}-offline-mutated"
                    return True
                if mutate_existing_string(item):
                    return True
        return False

    if isinstance(node, dict):
        for key, value in node.items():
            lowered = key.lower()
            if "assignment" in lowered:
                if mutate_existing_string(value):
                    return True
            if mutate_assignment(value):
                return True
    elif isinstance(node, list):
        for item in node:
            if mutate_assignment(item):
                return True
    return False

def mutate_security(node):
    security_keys = ("password", "mfa", "encryption", "bitlocker", "isenabled", "state")

    if isinstance(node, dict):
        for key, value in node.items():
            lowered = key.lower()
            if any(token in lowered for token in security_keys):
                if lowered.endswith("state") and isinstance(value, str):
                    node[key] = "reportOnly"
                    return True
                if lowered == "isenabled" and isinstance(value, bool):
                    node[key] = False
                    return True
                if isinstance(value, bool):
                    node[key] = not value
                    return True
                if isinstance(value, int):
                    node[key] = value - 1 if value > 0 else 1
                    return True
                if isinstance(value, str) and value:
                    node[key] = f"{value}-offline-mutated"
                    return True
            if mutate_security(value):
                return True
    elif isinstance(node, list):
        for item in node:
            if mutate_security(item):
                return True
    return False

files = sorted(path for path in root.rglob("*.json") if path.name.lower() != "migration-table.json")
selected = files[: min(mutate_count, len(files))]

for index, path in enumerate(selected):
    payload = json.loads(path.read_text())
    if index % 3 == 0:
        changed = mutate_low(payload)
    elif index % 3 == 1:
        changed = mutate_assignment(payload) or mutate_low(payload)
    else:
        changed = mutate_security(payload) or mutate_low(payload)

    if not changed:
        if isinstance(payload, dict):
            payload["description"] = "offline-mutated"
        else:
            payload = {"value": payload, "description": "offline-mutated"}

    path.write_text(json.dumps(payload, indent=2) + "\n")
PY
}

summarize_workspace() {
  python3 - "$BASELINE_DIR" "$CURRENT_DIR" "$METRICS_TSV" "$BENCHMARK_JSON" <<'PY'
import json
import pathlib
import sys

baseline = pathlib.Path(sys.argv[1])
current = pathlib.Path(sys.argv[2])
metrics_path = pathlib.Path(sys.argv[3])
benchmark_json = pathlib.Path(sys.argv[4])

def count_json(root):
    return sum(1 for path in root.rglob("*.json") if path.name.lower() != "migration-table.json")

metrics = []
lines = metrics_path.read_text().splitlines()[1:]
for line in lines:
    if not line.strip():
        continue
    step, duration = line.split("\t", 1)
    metrics.append({"step": step, "durationSeconds": float(duration)})

benchmark_json.write_text(json.dumps({
    "dataset": {
        "baselineJsonFiles": count_json(baseline),
        "currentJsonFiles": count_json(current)
    },
    "metrics": metrics
}, indent=2) + "\n")
PY
}

if [[ -n "$PROFILE" ]]; then
  echo "Creating normalized seed export with profile '$PROFILE'..."
  start_time="$(timer_now)"
  run_ic export --profile "$PROFILE" --output "$SEED_EXPORT" --types "$TYPES" --normalize > "$REPORTS_DIR/export-command.json"
  record_metric "export-normalized" "$start_time"
else
  if [[ ! -d "$SEED" ]]; then
    echo "Seed export folder not found: $SEED" >&2
    exit 1
  fi

  echo "Reusing seed export from '$SEED'..."
  copy_tree "$SEED" "$SEED_EXPORT"
fi

echo "Building baseline dataset..."
copy_tree "$SEED_EXPORT" "$BASELINE_DIR"
scale_directory "$BASELINE_DIR" "$SCALE"

echo "Building current dataset..."
copy_tree "$BASELINE_DIR" "$CURRENT_DIR"
mutate_directory "$CURRENT_DIR" "$MUTATE_COUNT"

echo "Running offline import validation..."
start_time="$(timer_now)"
run_ic import --folder "$BASELINE_DIR" --dry-run > "$REPORTS_DIR/import-baseline.json"
record_metric "import-dry-run-baseline" "$start_time"

start_time="$(timer_now)"
run_ic import --folder "$CURRENT_DIR" --dry-run > "$REPORTS_DIR/import-current.json"
record_metric "import-dry-run-current" "$start_time"

echo "Running drift reports..."
start_time="$(timer_now)"
run_ic diff --baseline "$BASELINE_DIR" --current "$CURRENT_DIR" --format json --output "$REPORTS_DIR/diff.json" > "$REPORTS_DIR/diff-json.stdout"
record_metric "diff-json" "$start_time"

start_time="$(timer_now)"
run_ic diff --baseline "$BASELINE_DIR" --current "$CURRENT_DIR" --format markdown --output "$REPORTS_DIR/diff.md" > "$REPORTS_DIR/diff-markdown.stdout"
record_metric "diff-markdown" "$start_time"

summarize_workspace

cat <<EOF
Stress workflow complete.

Workspace:           $WORKSPACE
Seed export:         $SEED_EXPORT
Baseline dataset:    $BASELINE_DIR
Current dataset:     $CURRENT_DIR
Import dry-run JSON: $REPORTS_DIR/import-baseline.json
Current dry-run JSON:$REPORTS_DIR/import-current.json
Diff JSON report:    $REPORTS_DIR/diff.json
Diff markdown report:$REPORTS_DIR/diff.md
Benchmark summary:   $BENCHMARK_JSON
EOF
