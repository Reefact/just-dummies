#!/bin/sh
# Read the SonarCloud Quality Gate verdict for this project and fail when it is not green.
#
# Usage:
#   tools/sonar-profile/check-gate.sh
#
# Why this exists. `dotnet-sonarscanner end` uploads the analysis and returns; it neither waits
# for the gate nor reads it, and no GitHub check carries the verdict. The gate was therefore
# computed and enforced by nothing, while the workflow that produced it stayed green as long as
# the upload succeeded.
#
# Why here and not in the sonar workflow. Making the scanner wait would couple MERGE
# AVAILABILITY to a third-party service: the sonar job is a required check, so a SonarCloud
# outage would stop every merge while a red gate still would not. Reading the verdict from a
# SCHEDULED job separates the two — the verdict is enforced, and an outage costs a red nightly
# instead of a frozen repository (decision: ADR-0039, follow-up on the gate questions).
#
# What this catches that the build cannot. build/sonar-profile.globalconfig enforces the C# rules
# the NuGet analyzer implements, and that is a strict subset of what the gate measures:
#   - symbolic-execution rules (S2583 and its family) that SonarCloud's engine runs and the
#     analyzer package does not — measured: a violation of S2583 sat in this repository with the
#     rule enforced at `warning` and the local build reported nothing;
#   - every non-C# rule family: githubactions, shell, secrets, xml, json, yaml;
#   - coverage, duplication and security-hotspot review, which no analyzer can answer.
# Those are the classes that actually turn this gate red, so the build hardening and this check
# are complements, not alternatives.
#
# The project is public, so no token is required; SONAR_TOKEN is honoured when set.

set -eu

PROJECT="${SONAR_PROJECT_KEY:-reefact_just-dummies}"
API="${SONAR_API_BASE:-https://sonarcloud.io/api}"

fail() { printf 'check-gate: %s\n' "$1" >&2; exit "${2:-1}"; }

command -v curl >/dev/null || fail "curl is required"
command -v jq   >/dev/null || fail "jq is required"

# --proto '=https' --proto-redir '=https' refuse every non-HTTPS hop, including on a redirect,
# which matters because the token below would otherwise be sent in clear (Sonar
# githubactions:S6506 flagged exactly this shape elsewhere in the repository). The token is
# prepended to this function's own positional parameters rather than duplicating the curl call, so
# those restrictions are written ONCE and cannot drift between the authenticated and anonymous
# paths — the authenticated one being exactly where a drift would leak the credential. Same shape
# as sync-profile.sh, deliberately.
fetch() {
  if [ -n "${SONAR_TOKEN:-}" ]; then set -- --user "${SONAR_TOKEN}:" "$@"; fi

  curl --proto '=https' --proto-redir '=https' -sSfL --retry 3 --retry-delay 2 --max-time 60 "$@"
}

body="$(fetch "${API}/qualitygates/project_status?projectKey=${PROJECT}")" \
  || fail "could not reach ${API}"

status="$(printf '%s' "$body" | jq -r '.projectStatus.status // empty')"
[ -n "$status" ] || fail "no gate status in the response; the project key may be wrong: ${PROJECT}"

# A rating is reported as 1..5 where the dashboard shows A..E, so the raw number is translated:
# "3" tells a reader nothing, "C" tells them where they are.
render() {
  printf '%s' "$body" | jq -r '
    def letter: {"1":"A","2":"B","3":"C","4":"D","5":"E"};
    .projectStatus.conditions[]?
    | select(.status != "OK")
    | . as $c
    | ($c.metricKey | test("_rating$")) as $isRating
    | "  \($c.metricKey): \(if $isRating then (letter[$c.actualValue] // $c.actualValue) else $c.actualValue end)"
      + " (needs \(if $c.comparator == "GT" then "at most" else "at least" end)"
      + " \(if $isRating then (letter[$c.errorThreshold] // $c.errorThreshold) else $c.errorThreshold end))"
  '
}

if [ "$status" = "OK" ]; then
  printf 'check-gate: the SonarCloud quality gate is green for %s.\n' "$PROJECT"
  exit 0
fi

condition_count="$(printf '%s' "$body" | jq -r '.projectStatus.conditions | length')"

# NONE is not a verdict, and this script must not report it as one.
#
# SonarCloud answers NONE with an empty condition list when the gate assigned to the project evaluated
# nothing. The usual cause is that the gate's conditions are scoped to NEW CODE while the project has no
# new-code period defined, so there is no "new" for them to measure -- which is the state a freshly
# created project sits in until someone sets one.
#
# It is treated as a FAILURE on purpose. "The gate measured nothing" and "the gate found nothing wrong"
# are different facts with the same green colour, and this job exists precisely because a Sonar upload
# that reported nothing had let two VULNERABILITY-typed findings reach main behind a permanently green
# check. Passing on NONE would rebuild that hole one level up. The message says what to fix instead of
# printing an empty list of failing conditions.
if [ "$status" = "NONE" ]; then
  printf 'check-gate: the SonarCloud quality gate returned NONE for %s -- it evaluated nothing.\n' "$PROJECT" >&2
  printf '\n' >&2
  printf 'This is not a pass. The gate is assigned but measured no condition (%s reported), so no\n' "$condition_count" >&2
  printf 'verdict exists to enforce. The usual cause is a missing NEW CODE period: the default "Sonar\n' >&2
  printf 'way" gate scopes its conditions to new code, and a project without a baseline has none.\n' >&2
  printf '\n' >&2
  printf 'Set one at https://sonarcloud.io/project/new_code?id=%s, then re-run this job.\n' "$PROJECT" >&2
  printf 'Verify with: curl -s "%s/qualitygates/project_status?projectKey=%s"\n' "$API" "$PROJECT" >&2
  exit 1
fi

printf 'check-gate: the SonarCloud quality gate is %s for %s.\n' "$status" "$PROJECT" >&2
printf '\n' >&2
# A non-OK status with nothing to render would otherwise print a bare heading and leave the reader with
# no idea whether the gate found something unnameable or the response was shaped unexpectedly.
if [ "$condition_count" -eq 0 ]; then
  printf 'The gate is not green, yet it reports no condition at all. That combination is unexpected;\n' >&2
  printf 'read the raw response before trusting either half of it:\n' >&2
  printf '  curl -s "%s/qualitygates/project_status?projectKey=%s"\n' "$API" "$PROJECT" >&2
  exit 1
fi
printf 'Failing conditions:\n' >&2
render >&2
printf '\n' >&2
printf 'A rating worse than A means the new-code period carries at least one issue of that kind:\n' >&2
printf '  reliability -> a Bug, security -> a Vulnerability, maintainability -> a Code Smell.\n' >&2
printf 'Open https://sonarcloud.io/project/issues?id=%s&resolved=false&inNewCodePeriod=true\n' "$PROJECT" >&2
exit 1
