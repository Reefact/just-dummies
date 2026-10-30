#!/bin/sh
# Runs the developer-UX trial end to end: packs the three packages and installs `any` (prepare.sh), then
# runs every domain's tests against them, as a consumer would. Exits non-zero if any domain fails.
#
# Usage: tools/ux-trial/run.sh [version]   (from any directory)
#
# The release workflow runs it before publishing, so what a user receives is exercised whole first.

set -eu

here="$(cd "$(dirname "$0")" && pwd)"

"$here/prepare.sh" "$@"

failed=""
for tests in "$here"/domains/*/Tests; do
  domain="$(basename "$(dirname "$tests")")"
  echo "== $domain"
  if ! dotnet test --project "$tests"; then
    failed="$failed $domain"
  fi
done

if [ -n "$failed" ]; then
  echo "UX trial failed in:$failed" >&2
  exit 1
fi

echo "UX trial: every domain passed."
