#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT="$REPO_ROOT/src/Intune.Commander.CLI"

# Detect architecture
ARCH=$(uname -m)
if [ "$ARCH" = "arm64" ]; then
    RID="osx-arm64"
else
    RID="osx-x64"
fi

OUTPUT_DIR="$REPO_ROOT/artifacts/cli/$RID"

echo "Building ic CLI for $RID..."

dotnet publish "$PROJECT" \
    -r "$RID" \
    -c Release \
    --self-contained \
    -p:PublishSingleFile=true \
    -p:EnableCompressionInSingleFile=true \
    -o "$OUTPUT_DIR"

echo ""
echo "Binary output: $OUTPUT_DIR/ic"
echo ""
echo "To install globally, run:"
echo "  sudo cp \"$OUTPUT_DIR/ic\" /usr/local/bin/ic"
echo "  sudo chmod +x /usr/local/bin/ic"
