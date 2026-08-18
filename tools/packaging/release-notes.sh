#!/bin/sh
# Emit ONE release's product-facing release note, read verbatim from the train's
# RELEASE_NOTES-<major>.x.en.md file — a product-facing rewrite of the matching CHANGELOG.md
# section, drafted by hand (see the release-notes skill) before the tag is pushed.
#
# This deliberately does NOT derive anything from `git log`: a commit subject is a record for
# maintainers, not an announcement for a consumer deciding whether to upgrade, and mixing the
# two produced exactly that — see git history before this script's rewrite for what it used to
# emit.
#
# Usage: tools/packaging/release-notes.sh <scope:lib|xunit|cli|catalog> <current-tag> [<end-ref>]
#   Emits Markdown on stdout: the "## <version> ..." section of that train's release-notes file
#   matching <current-tag>'s version. <end-ref> is accepted for compatibility with callers that
#   still pass the release commit as a third argument, but is not used — the source is a
#   committed file, not commit history, so there is no range to resolve.
#
#   Refuses (exit 1) rather than emitting a fallback when the release-notes file, or the section
#   for this version, does not exist: an untagged release is the wrong moment to discover that
#   nobody wrote what the release actually contains.

set -eu

if [ "$#" -lt 2 ] || [ "$#" -gt 3 ] || [ -z "$1" ] || [ -z "$2" ]; then
  echo "usage: tools/packaging/release-notes.sh <scope:lib|xunit|cli|catalog> <current-tag> [<end-ref>]" >&2
  exit 2
fi
scope="$1"
current_tag="$2"

# Tag prefix and changelog location come from tools/trains.sh (the single source of truth
# shared with the changelog tooling), so the release-notes file location can never drift from it.
# shellcheck source=tools/trains.sh
. "$(dirname "$0")/../trains.sh"
prefix="$(prefix_of "$scope")"
if [ -z "$prefix" ]; then
  echo "error: unknown scope '$scope' (expected one of: $(train_ids | tr '\n' ' ' | sed 's/ *$//'))" >&2
  exit 2
fi

case "$current_tag" in
  "$prefix"*) version="${current_tag#"$prefix"}" ;;
  *)
    echo "error: tag '$current_tag' does not start with the '$scope' train's prefix '$prefix'" >&2
    exit 2
    ;;
esac

# The release-notes file lives beside the train's changelog, one per major version, so a train
# past 1.x opens RELEASE_NOTES-2.x.en.md rather than growing the first file forever.
major="${version%%.*}"
changelog="$(changelog_of "$scope")"
notes_file="$(dirname "$changelog")/RELEASE_NOTES-${major}.x.en.md"

if [ ! -f "$notes_file" ]; then
  echo "error: $notes_file does not exist — write this major version's release notes (see the release-notes skill) before tagging" >&2
  exit 1
fi

# Escape the version for use as a literal in an ERE: it is data (from a git tag), not a pattern,
# and both '.' and '-' are meaningful in a regex.
version_pattern="$(printf '%s' "$version" | sed 's/[.[\*^$/-]/\\&/g')"

# The version's own section: from its "## <version>" heading (word-boundary after it, so
# "1.1.0" cannot also match a heading for "1.1.0-beta.1") up to the next "## " heading or EOF.
notes="$(awk -v heading="^## ${version_pattern}([[:space:]]|\$)" '
  $0 ~ heading { in_section = 1; print; next }
  in_section && /^## / { exit }
  in_section { print }
' "$notes_file")"

if [ -z "$notes" ]; then
  echo "error: $notes_file has no '## $version' section — write it (see the release-notes skill) before tagging" >&2
  exit 1
fi

printf '%s\n' "$notes"
