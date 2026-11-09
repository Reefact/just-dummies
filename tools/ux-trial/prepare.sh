#!/bin/sh
# Prepares the developer-UX trial: packs JustDummies, JustDummies.Xunit and JustDummies.Cli into
# ./local-feed and installs the `any` tool into ./.tools, exactly as a consumer would receive them.
#
# Usage: tools/ux-trial/prepare.sh [version]   (from any directory)
#
# The local feed, the extracted packages and the tool are wiped first: NuGet treats a version as
# immutable, so a re-pack under the same version would otherwise be served stale.

set -eu

version="${1:-1.0.0-uxtrial.1}"
here="$(cd "$(dirname "$0")" && pwd)"
root="$(cd "$here/../.." && pwd)"

rm -rf "$here/local-feed" "$here/packages" "$here/.tools"

dotnet pack "$root/JustDummies/JustDummies.csproj" -c Release -p:Version="$version" -o "$here/local-feed"
dotnet pack "$root/JustDummies.Xunit/JustDummies.Xunit.csproj" -c Release -p:Version="$version" \
  -p:JustDummiesDependencyVersion="$version" -o "$here/local-feed"
dotnet pack "$root/JustDummies.Cli/JustDummies.Cli.csproj" -c Release -p:Version="$version" -o "$here/local-feed"

dotnet tool install JustDummies.Cli --version "$version" --tool-path "$here/.tools" \
  --add-source "$here/local-feed" --configfile "$here/nuget.config"

echo "Ready: $here/.tools/any, packages $version in $here/local-feed"
