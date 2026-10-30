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

# The scaffolder itself, not only what it emitted once: scaffold the smoke domain's generators with the packed
# `any` into a disposable copy, then build and test that copy. The copy sits inside this folder so it builds
# under the same props and nuget.config as every domain, and .gitignore keeps it out of the working tree.
smoke="$here/.smoke"
rm -rf "$smoke"
cp -R "$here/smoke" "$smoke"
echo "== smoke (any generate)"
if dotnet restore "$smoke/Tests" \
   && (cd "$smoke/Tests" && "$here/.tools/any" generate Sku && "$here/.tools/any" generate Quantity && "$here/.tools/any" generate OrderLine) \
   && dotnet test --project "$smoke/Tests"; then
  rm -rf "$smoke"
else
  failed="$failed smoke"
fi

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
