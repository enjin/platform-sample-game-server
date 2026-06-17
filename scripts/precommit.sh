#!/usr/bin/env bash
# Run formatters, build, and tests before committing.
# Usage: ./scripts/precommit.sh
#
# Works under bash and zsh, regardless of the caller's working directory.

set -euo pipefail

# Resolve this script's directory in a shell-agnostic way.
# $BASH_SOURCE exists in bash; ${(%):-%x} is the zsh equivalent; $0 is the
# universal fallback when the script is invoked as `sh script.sh`.
if [ -n "${BASH_SOURCE:-}" ]; then
    SCRIPT_PATH="${BASH_SOURCE[0]}"
elif [ -n "${ZSH_VERSION:-}" ]; then
    # shellcheck disable=SC2296
    SCRIPT_PATH="${(%):-%x}"
else
    SCRIPT_PATH="$0"
fi

SCRIPT_DIR="$(cd "$(dirname "$SCRIPT_PATH")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT="$REPO_ROOT/PlatformSampleGameServer.csproj"
SS58_TEST_PROJECT="$REPO_ROOT/tools/Ss58SelfTest/Ss58SelfTest.csproj"
AMOUNT_TEST_PROJECT="$REPO_ROOT/tools/TransferAmountSelfTest/TransferAmountSelfTest.csproj"

cd "$REPO_ROOT"

echo "==> Restoring .NET tools"
dotnet tool restore

echo "==> Running CSharpier"
dotnet csharpier format .

echo "==> Running dotnet format (main project)"
dotnet format "$PROJECT"

echo "==> Running dotnet format (tools projects)"
dotnet format "$SS58_TEST_PROJECT"
dotnet format "$AMOUNT_TEST_PROJECT"

echo "==> Building main project"
dotnet build "$PROJECT" --configuration Release

echo "==> Building tools projects"
dotnet build "$SS58_TEST_PROJECT" --configuration Release
dotnet build "$AMOUNT_TEST_PROJECT" --configuration Release

echo "==> Running self-tests"
dotnet run --project "$SS58_TEST_PROJECT" --configuration Release
dotnet run --project "$AMOUNT_TEST_PROJECT" --configuration Release

echo "==> All checks passed"
