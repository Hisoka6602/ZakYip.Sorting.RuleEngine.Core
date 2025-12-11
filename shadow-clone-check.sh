#!/bin/bash

# 影分身检测脚本 / Shadow Clone Detection Script
# 用法 / Usage: ./shadow-clone-check.sh [directory] [threshold]

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DETECTOR_PATH="$SCRIPT_DIR/Tools/ShadowCloneDetector"
TARGET_DIR="${1:-.}"
THRESHOLD="${2:-0.80}"

echo "🔧 构建影分身检测工具 / Building shadow clone detector..."
cd "$DETECTOR_PATH"
dotnet build --configuration Release --nologo --verbosity quiet

echo ""
echo "🔍 运行影分身检测 / Running shadow clone detection..."
echo "目标目录 / Target directory: $TARGET_DIR"
echo "相似度阈值 / Similarity threshold: $THRESHOLD"
echo ""

dotnet run --configuration Release --no-build -- "$TARGET_DIR" --threshold "$THRESHOLD"
exit_code=$?

if [ $exit_code -eq 0 ]; then
    echo ""
    echo "✅ 影分身检测通过 / Shadow clone detection passed"
    exit 0
else
    echo ""
    echo "❌ 影分身检测失败 / Shadow clone detection failed"
    exit 1
fi
