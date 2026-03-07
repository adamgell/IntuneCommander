#!/bin/sh
# launch-macos.sh — Build and run Intune Commander on macOS
#
# Usage:
#   ./launch-macos.sh              # Fast incremental dev build (default)
#   ./launch-macos.sh --release    # Self-contained publish + launch
#   ./launch-macos.sh --release --rebuild  # Force a clean self-contained publish
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT="$REPO_ROOT/src/Intune.Commander.Desktop/Intune.Commander.Desktop.csproj"

# Parse flags
RELEASE=0
REBUILD=0
for arg in "$@"; do
    case "$arg" in
        --release) RELEASE=1 ;;
        --rebuild) REBUILD=1 ;;
    esac
done

if [ "$RELEASE" = "1" ]; then
    # Detect CPU architecture
    ARCH="$(uname -m)"
    case "$ARCH" in
        arm64) RID="osx-arm64" ;;
        x86_64) RID="osx-x64" ;;
        *) echo "Unsupported architecture: $ARCH"; exit 1 ;;
    esac

    PUBLISH_DIR="$REPO_ROOT/publish/$RID"
    BINARY="$PUBLISH_DIR/Intune.Commander.Desktop"

    if [ ! -f "$BINARY" ] || [ "$REBUILD" = "1" ]; then
        echo "Publishing Intune Commander for $RID..."
        dotnet publish "$PROJECT" \
            --configuration Release \
            --runtime "$RID" \
            --self-contained true \
            --output "$PUBLISH_DIR" \
            -p:PublishSingleFile=true \
            -p:IncludeNativeLibrariesForSelfExtract=true
        echo "Done."
    fi

    echo "Launching Intune Commander..."
    exec "$BINARY"
else
    # Fast dev mode: incremental build via dotnet run (no publish overhead)
    echo "Building and launching Intune Commander (dev)..."
    exec dotnet run --project "$PROJECT" --configuration Debug
fi
