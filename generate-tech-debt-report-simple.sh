#!/bin/bash
# Simple Technical Debt Report Generator

OUTPUT_DIR="${1:-.}"
REPORT_DIR="$OUTPUT_DIR/tech-debt-reports"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
REPORT_FILE="$REPORT_DIR/tech-debt-report-$TIMESTAMP.md"

mkdir -p "$REPORT_DIR"

echo "📊 Generating Technical Debt Report..."
echo "Report: $REPORT_FILE"

# Generate report header
cat > "$REPORT_FILE" << 'EOF'
# Technical Debt Report / 技术债务报告

> **Generated:** {TIMESTAMP}
> **Project:** ZakYip.Sorting.RuleEngine.Core

## 1. Code Duplication Detection (jscpd)

EOF

# Run jscpd
echo "Running jscpd..."
if command -v jscpd &> /dev/null; then
    jscpd . >> "$REPORT_FILE" 2>&1 || true
else
    echo "jscpd not installed" >> "$REPORT_FILE"
fi

# Run shadow clone detection
echo ""  >> "$REPORT_FILE"
echo "## 2. Shadow Clone Semantic Detection" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

echo "Running shadow clone detection..."
if [ -f "./shadow-clone-check.sh" ]; then
    ./shadow-clone-check.sh . >> "$REPORT_FILE" 2>&1 || true
else
    echo "shadow-clone-check.sh not found" >> "$REPORT_FILE"
fi

# Replace timestamp
# macOS (BSD sed) requires a backup extension for -i; Linux (GNU sed) does not.
if [[ "$(uname)" == "Darwin" ]]; then
    sed -i .bak "s/{TIMESTAMP}/$(date '+%Y-%m-%d %H:%M:%S')/g" "$REPORT_FILE"
    rm -f "$REPORT_FILE.bak"
else
    sed -i "s/{TIMESTAMP}/$(date '+%Y-%m-%d %H:%M:%S')/g" "$REPORT_FILE"
fi

# Create symlink to latest
ln -sf "tech-debt-report-$TIMESTAMP.md" "$REPORT_DIR/latest.md"

echo "✅ Report generated: $REPORT_FILE"
echo "📌 Latest: $REPORT_DIR/latest.md"

