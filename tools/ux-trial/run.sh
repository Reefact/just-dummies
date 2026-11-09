#!/bin/sh
# Runs the developer-UX trial against the packed packages, as a consumer receives them.
#
# Usage: tools/ux-trial/run.sh [--all] [version]   (from any directory)
#
# Without --all it runs the consumer contract the release workflow blocks on: the packed `any` scaffolds the
# smoke domain, the result builds with the bundled analyzers loaded and its tests pass, and one full domain runs.
# With --all it runs every domain, as the on-demand ux-trial workflow does. Exits non-zero on any failure.

set -eu

here="$(cd "$(dirname "$0")" && pwd)"

all=false
if [ "${1:-}" = "--all" ]; then
  all=true
  shift
fi

"$here/prepare.sh" "$@"

failed=""

# The scaffolder itself, not only what it emitted once: scaffold the smoke domain's generators with the packed
# `any` into a disposable copy, then build and test that copy. The copy sits inside this folder so it builds
# under the same props and nuget.config as every domain, and .gitignore keeps it out of the working tree.
smoke="$here/.smoke"
rm -rf "$smoke"
cp -R "$here/smoke" "$smoke"
echo "== smoke (any generate, analyzers, tests)"
if dotnet restore "$smoke/Tests" \
   && (cd "$smoke/Tests" && "$here/.tools/any" generate Sku && "$here/.tools/any" generate Quantity && "$here/.tools/any" generate OrderLine) \
   && dotnet build "$smoke/Tests" > "$smoke/build.log" 2>&1 \
   && grep -q "warning JD014" "$smoke/build.log" \
   && dotnet test --project "$smoke/Tests"; then
  rm -rf "$smoke"
else
  [ -f "$smoke/build.log" ] && cat "$smoke/build.log"
  failed="$failed smoke"
fi

if [ "$all" = true ]; then
  domains="$here/domains/*/Tests $here/domains/*/Tests.*"
else
  domains="$here/domains/01-payments/Tests"
fi

for tests in $domains; do
  domain="$(basename "$(dirname "$tests")")/$(basename "$tests")"
  echo "== $domain"
  if ! dotnet test --project "$tests"; then
    failed="$failed $domain"
  fi
done

if [ -n "$failed" ]; then
  echo "UX trial failed in:$failed" >&2
  exit 1
fi

echo "UX trial: every domain run passed."
