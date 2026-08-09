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
#               lib     -> JustDummies (the library; its analyzers ride inside it)
#               xunit   -> JustDummies.Xunit (the xUnit v3 adapter)
#               catalog -> JustDummies.DiagnosticCatalog (the JD rules as constants)
#               cli     -> dum (the scaffolder; specified, not built yet)

set -eu

if [ "$#" -lt 2 ] || [ "$#" -gt 3 ] || [ -z "$1" ] || [ -z "$2" ]; then
  echo "usage: tools/packaging/pack.sh <version> <scope:lib|xunit|cli|catalog> [--dry-run]" >&2
  exit 2
fi
version="$1"
scope="$2"

# --dry-run marks a rehearsal: the packages are built and every check still runs, but the checks that can only
# be true of a REAL release report instead of failing. Today that is one check, the xunit train's intra-product
# dependency guard; the reason it cannot hold on a rehearsal is documented where it is applied.
dry_run="no"
if [ "$#" -eq 3 ]; then
  if [ "$3" = "--dry-run" ]; then
    dry_run="yes"
  else
    echo "usage: tools/packaging/pack.sh <version> <scope:lib|xunit|cli|catalog> [--dry-run]" >&2
    exit 2
  fi
fi

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
    # The xUnit v3 adapter (ADR-0018). It rode the library's train while both lived in
    # Reefact/first-class-errors; it now versions independently, which is exactly why the
    # intra-product dependency guard below exists.
    projects='JustDummies.Xunit/JustDummies.Xunit.csproj'
    ;;
  catalog)
    # JustDummies.DiagnosticCatalog, the JD rules as compile-checked constants (ADR-0052). Its own train because
    # it versions on the RULE SET -- a rule added, retired or recategorised -- which is not when the library or
    # the adapter moves. Unlike every other package here it declares a real dependency, on the DiagnosticCatalog
    # foundation: a package that PUBLISHES a catalogue must let the foundation reach its consumers, since it
    # carries both the markers the declarations wear and the analyzers that check a consumer's suppressions.
    projects='JustDummies.DiagnosticCatalog/JustDummies.DiagnosticCatalog.csproj'
    ;;
  cli)
    # The `dum` scaffolder. JustDummies.Cli exists and packs as a .NET tool, and its command line parses in
    # full, but nothing sits behind it: `generate` is specified in
    # doc/handwritten/for-maintainers/specifications/justdummies-tool.md and not implemented, so publishing
    # this would put a tool that refuses every invocation on nuget.org. The train is
    # declared in tools/trains.sh so the tag trigger, the scope list and the release workflow are already
    # wired; this arm fails loudly rather than shipping that. Opening it means adding the project below AND
    # the assertion that the produced .nuspec declares no JustDummies dependency, which is the executable
    # form of ADR-0063.
    echo "error: the 'cli' train has nothing to publish yet -- dum parses 'generate' and does nothing" >&2
    echo "       see doc/handwritten/for-maintainers/specifications/justdummies-tool.md" >&2
    exit 2
    ;;
  *)
    echo "error: unknown scope '$scope' (expected 'lib', 'xunit', 'cli' or 'catalog')" >&2
    exit 2
    ;;
esac

# The version of JustDummies the ADAPTER declares a dependency on -- decided here, not inherited from the
# version being packed (ADR-0047). Without this, -p:Version reaches the referenced library through the project
# graph and the adapter ends up demanding its OWN version of the library, which locks the two trains together.
#
# The right answer is the newest library version this repository has actually published, and its lib-v* tags
# are the record of exactly that. Sorted by version, not lexically: lib-v0.10.0 must outrank lib-v0.9.0. If no
# lib-v* tag exists yet the variable stays empty and the default stamping applies, which the guard below then
# refuses on a real release -- the correct outcome, since there is no published library to depend on.
dependency_version=""
if [ "$scope" = "xunit" ]; then
  dependency_version="$(git tag --list 'lib-v*' | sed 's/^lib-v//' | sort -V | tail -n1)"
  if [ -n "$dependency_version" ]; then
    echo "note: the adapter will declare a dependency on JustDummies $dependency_version (latest lib-v tag)"
  fi
fi

# Intentionally unquoted: $projects is a space-separated list of project paths (no spaces in paths).
for project in $projects; do
  dotnet pack "$project" -c Release --no-build -p:Version="$version" -p:GenerateSBOM=true \
    -p:JustDummiesDependencyVersion="$dependency_version" -o artifacts
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
# (ADR-0003): architecture tests assert it at build time, and this asserts it on the shipped artifact -- a
# FirstClassErrors dependency sneaking into the nuspec must fail the pack, not surface on nuget.org. The check
# outlived the extraction on purpose: the coupling it guards against is what the extraction removed, and a
# regression would be silent without it.
for package in artifacts/*.nupkg; do
  # Fail CLOSED: an unreadable nuspec must not pass as "standalone" -- read it first (unzip fails loudly),
  # then reject any FirstClassErrors dependency found in it.
  nuspec="$(unzip -p "$package" '*.nuspec')" || { echo "error: cannot read the nuspec from $package" >&2; exit 1; }
  if printf '%s\n' "$nuspec" | grep -q '<dependency [^>]*id="FirstClassErrors'; then
    echo "error: $package declares a FirstClassErrors dependency; JustDummies is standalone (ADR-0003)" >&2
    exit 1
  fi
  echo "ok: $package is standalone (no FirstClassErrors dependency)"
done

# Analyzer-bundling proof for the lib train. The rules reach consumers only because
# _AddAnalyzerToPackage puts JustDummies.Analyzers.dll at analyzers/dotnet/cs inside the library package
# (ADR-0023). That mechanism is easy to break silently: neither `dotnet build` nor `dotnet test` inspects the
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
# The adapter must declare a dependency on a JustDummies version that EXISTS. Publishing one that demands a
# library release that never happened is NU1102 for the consumer, on an immutable artifact.
#
# Since ADR-0047 the declared version is chosen above rather than inherited, so this now verifies a decision
# instead of catching an accident -- and it stays, because the decision can still be wrong: an empty tag list,
# a hand-passed -p:JustDummiesDependencyVersion, a tag deleted between the two steps. The library versions this
# repository has published are exactly its lib-v* tags, so the declared dependency must match one. Offline by
# construction: no nuget.org round trip.
#
# On a DRY RUN it reports instead of failing, and that is not a loophole: before the first library release
# there is no lib-v* tag to point at, and the rehearsal would then be red for a reason no real release can
# have. It still prints what a real release would require.
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
    elif [ "$dry_run" = "yes" ]; then
      echo "notice: dry run -- $package depends on JustDummies $dependency_version, which no lib-v tag matches."
      echo "        A real xunit release checks this and refuses to pack when it does not hold."
    else
      echo "error: $package depends on JustDummies $dependency_version, but no lib-v${dependency_version} tag exists." >&2
      echo "       Publishing it would demand a library version that was never released (NU1102)." >&2
      echo "       Release the library first, or pin the adapter's dependency to a published version." >&2
      exit 1
    fi
  done
fi
