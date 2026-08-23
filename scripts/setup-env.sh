#!/usr/bin/env bash

# SupplyChainX Development Environment Helper Script
set -e

echo "=========================================="
echo "   SupplyChainX v0.1.0 Environment Check  "
echo "=========================================="

# Check .NET SDK
if command -v dotnet >/dev/null 2>&1; then
    echo "[OK] .NET SDK: $(dotnet --version)"
else
    echo "[WARN] .NET SDK not found in system PATH."
fi

# Check Node.js
if command -v node >/dev/null 2>&1; then
    echo "[OK] Node.js: $(node --version)"
else
    echo "[WARN] Node.js not found."
fi

# Check Docker
if command -v docker >/dev/null 2>&1; then
    echo "[OK] Docker: $(docker --version)"
else
    echo "[WARN] Docker not found."
fi

# Copy environment template if missing
if [ ! -f "infrastructure/.env" ] && [ -f "infrastructure/.env.example" ]; then
    cp infrastructure/.env.example infrastructure/.env
    echo "[OK] Created infrastructure/.env from template."
fi

echo "=========================================="
echo "Environment setup check complete."
