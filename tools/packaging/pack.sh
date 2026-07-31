#!/bin/sh
# Single source of truth for producing the published NuGet packages.
#
# Both the real release (.github/workflows/release.yml) and the automatic
# rehearsal (.github/workflows/release-dryrun.yml) call this, so the dry run can
# never silently drift from the release it is meant to mirror: the set of packed
# projects, the pack flags, the embedded SBOM and the "is the SBOM actually
# there?" check all live here, once.
#
# It assumes the solution has already been built in Release (it packs with
# --no-build). It writes the .nupkg / .snupkg into ./artifacts.
#
# Usage: tools/packaging/pack.sh <version> <scope:lib|xunit|cli>
#   <version> is any valid SemVer (a real release passes the tag version; the
#             dry run passes a throwaway like 0.0.0-dryrun).
#   <scope>   selects which release train to pack, since the trains are versioned
#             and released independently (see tools/trains.sh):
#               lib   -> JustDummies (the library; its analyzers ride inside it)
#               xunit -> JustDummies.Xunit (the xUnit v3 adapter)
#               cli   -> dum (the scaffolder; specified, not built yet)

set -eu

if [ "$#" -ne 2 ] || [ -z "$1" ] || [ -z "$2" ]; then
  echo "usage: tools/packaging/pack.sh <version> <scope:lib|xunit|cli>" >&2
  exit 2
fi
version="$1"
scope="$2"

# The projects that carry NuGet identity, selected by release train. GenerateSBOM embeds an SPDX SBOM
# (_manifest/spdx_2.2/manifest.spdx.json) inside each package; it is passed here, not hardcoded in the
# csproj, so local and smoke-check packs stay SBOM-free.
case "$scope" in
  lib)
    # JustDummies, the standalone arbitrary-test-value library. JustDummies.Analyzers is NOT packed on its
    # own (IsPackable=false): the library's _AddAnalyzerToPackage target carries the analyzer DLL inside this
    # package under analyzers/dotnet/cs, so a consumer gets the rules by restoring the library -- asserted
    # after the pack, below.
    projects='JustDummies/JustDummies.csproj'
    ;;
  xunit)
    # The xUnit v3 adapter (ADR-0039). It rode the library's train while both lived in
    # Reefact/first-class-errors; it now versions independently, which is exactly why the
    # intra-product dependency guard below exists.
    projects='JustDummies.Xunit/JustDummies.Xunit.csproj'
    ;;
  cli)
    # The `dum` scaffolder. Specified in doc/handwritten/for-maintainers/specifications/justdummies-tool.md
    # ("Status: specification, ready to implement. Nothing is built yet"), so there is no project to pack.
    # The train is declared in tools/trains.sh so the tag trigger, the scope list and the release workflow are
    # already wired; this arm fails loudly rather than packing nothing and reporting success.
    echo "error: the 'cli' train has no packable project yet -- the dum scaffolder is specified but not built" >&2
    echo "       see doc/handwritten/for-maintainers/specifications/justdummies-tool.md" >&2
    exit 2
    ;;
  *)
    echo "error: unknown scope '$scope' (expected 'lib', 'xunit' or 'cli')" >&2
    exit 2
    ;;
esac

# Intentionally unquoted: $projects is a space-separated list of project paths (no spaces in paths).
for project in $projects; do
  dotnet pack "$project" -c Release --no-build -p:Version="$version" -p:GenerateSBOM=true -o artifacts
done

# Positive proof, not just a green pack: a pack that silently stopped embedding
# the manifest (a GenerateSBOM / Microsoft.Sbom.Targets regression) would
# otherwise pass unnoticed. Assert the SPDX file is present in every package.
for package in artifacts/*.nupkg; do
  if unzip -l "$package" | grep -q '_manifest/spdx_2.2/manifest.spdx.json'; then
    echo "ok: SBOM present in $package"
  else
    echo "error: SBOM manifest missing from $package" >&2
    exit 1
  fi
done

# Standalone guard, on every train. JustDummies' whole identity is that it depends on nothing outside itself
# (ADR-0011): architecture tests assert it at build time, and this asserts it on the shipped artifact -- a
# FirstClassErrors dependency sneaking into the nuspec must fail the pack, not surface on nuget.org. The check
# outlived the extraction on purpose: the coupling it guards against is what the extraction removed, and a
# regression would be silent without it.
for package in artifacts/*.nupkg; do
  # Fail CLOSED: an unreadable nuspec must not pass as "standalone" -- read it first (unzip fails loudly),
  # then reject any FirstClassErrors dependency found in it.
  nuspec="$(unzip -p "$package" '*.nuspec')" || { echo "error: cannot read the nuspec from $package" >&2; exit 1; }
  if printf '%s\n' "$nuspec" | grep -q '<dependency [^>]*id="FirstClassErrors'; then
    echo "error: $package declares a FirstClassErrors dependency; JustDummies is standalone (ADR-0011)" >&2
    exit 1
  fi
  echo "ok: $package is standalone (no FirstClassErrors dependency)"
done

# Analyzer-bundling proof for the lib train. The rules reach consumers only because
# _AddAnalyzerToPackage puts JustDummies.Analyzers.dll at analyzers/dotnet/cs inside the library package
# (ADR-0044). That mechanism is easy to break silently: neither `dotnet build` nor `dotnet test` inspects the
# .nupkg content, so losing the target would pass every local check and only surface as "the JD rules stopped
# firing" in a consumer's IDE, with no error anywhere.
if [ "$scope" = "lib" ]; then
  for package in artifacts/JustDummies.[0-9]*.nupkg; do
    if unzip -l "$package" | grep -q 'analyzers/dotnet/cs/JustDummies\.Analyzers\.dll'; then
      echo "ok: analyzers bundled in $package"
    else
      echo "error: JustDummies.Analyzers.dll missing from analyzers/dotnet/cs in $package" >&2
      exit 1
    fi
  done
fi

# Intra-product dependency guard for the xunit train. This is the cost of giving the adapter its own train.
# JustDummies.Xunit carries a ProjectReference on JustDummies, so `dotnet pack` stamps
# <dependency id="JustDummies" version="$version" /> -- the version being packed HERE, on the xunit train.
# Publishing xunit-v0.2.0 while the library sits at lib-v0.1.0 would therefore ship an adapter demanding a
# JustDummies 0.2.0 that was never published: NU1102 for the consumer, on an immutable artifact. The library
# versions this repository has actually published are exactly its lib-v* tags, so require the stamped
# dependency to match one. Offline by construction -- no nuget.org round trip, and it works on a dry run.
if [ "$scope" = "xunit" ]; then
  for package in artifacts/JustDummies.Xunit.*.nupkg; do
    nuspec="$(unzip -p "$package" '*.nuspec')" || { echo "error: cannot read the nuspec from $package" >&2; exit 1; }
    dependency_version="$(printf '%s\n' "$nuspec" \
      | grep -o '<dependency [^>]*id="JustDummies"[^>]*>' \
      | sed -n 's/.*version="\([^"]*\)".*/\1/p' \
      | head -n1)"
    if [ -z "$dependency_version" ]; then
      echo "error: $package declares no JustDummies dependency; the adapter cannot work without the library" >&2
      exit 1
    fi
    if git rev-parse --verify --quiet "refs/tags/lib-v${dependency_version}" >/dev/null; then
      echo "ok: $package depends on JustDummies $dependency_version, published as lib-v${dependency_version}"
    else
      echo "error: $package depends on JustDummies $dependency_version, but no lib-v${dependency_version} tag exists." >&2
      echo "       Publishing it would demand a library version that was never released (NU1102)." >&2
      echo "       Release the library first, or pin the adapter's dependency to a published version." >&2
      exit 1
    fi
  done
fi
