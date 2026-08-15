#!/bin/sh
# JustDummies — coding-rules hook.
#
# Wired from .claude/settings.json on PostToolUse for the file-editing tools, so
# an agent learns it broke a coding rule at the moment it wrote the file — not
# once CI is red, and not once a human is reading the diff.
#
# Why a hook and not just a written rule. This repository's style has always been
# recorded in JustDummies.sln.DotSettings, which Rider reads and nothing else
# can: no compiler, no CI job, and no agent. CLAUDE.md used to delegate to that
# file ("follow it"), which reads like an instruction but is not one for a reader
# that cannot open it. The explicit-type rule drifted to 203 violations under that
# arrangement. The rules an agent must follow are now written out where it reads
# them, and this hook is what makes them observable rather than merely stated
# (ADR-0035).
#
# Which rules live here. ADR-0073 layers the instructions by when they are needed
# and sends anything a tool can decide to the tool. A rule belongs in this hook
# when it is deterministic, cheap to check from the edited file alone, and would
# otherwise rest on review. Judgement — is this history messy, does this change
# embark a decision — stays in prose on purpose.
#
# It complements, and does not replace, the build-time enforcement: .editorconfig
# plus EnforceCodeStyleInBuild report the same explicit-type rule as IDE0008, which
# CI turns into an error (ADR-0055). That gate is authoritative but late — it fires
# only when someone builds. This hook fires on the edit itself.
#
# The hook never rewrites anything: it reads the file that was just written and
# reports. Fixing is the agent's job, which keeps the agent's own output the thing
# under correction rather than something a formatter quietly patched behind it.
#
# Adding a rule. Write a `rule_<name>` function that prints one line per offence
# (empty output means clean) and a matching `<name>_hint`, then add it to the rule
# list for the file types it applies to, in the dispatch below. Every rule must be
# a pure read of a file already on disk and cost milliseconds: this runs after
# every single edit. Anything needing a build or a solution-wide analysis belongs
# in CI, not here.

set -u

# Always drain stdin (the harness pipes the hook payload). Draining avoids any
# broken-pipe noise on the writer side even when we exit early.
payload="$(cat 2>/dev/null || true)"

# The file that was just written. jq when available, a raw scan otherwise, so the
# hook degrades to silence rather than to false positives.
file=''
if command -v jq >/dev/null 2>&1; then
  file="$(printf '%s' "$payload" | jq -r '.tool_input.file_path // empty' 2>/dev/null || true)"
fi
[ -n "$file" ] || exit 0
[ -f "$file" ] || exit 0                 # deleted, moved, or a path we cannot resolve

# Which rules apply to the file that was just written. A file type nobody has a
# rule for exits silently.
case "$file" in
  *.cs)           RULES='explicit_types suppression_form' ;;
  *.sln|*.csproj) RULES='sln_nesting' ;;
  *)              exit 0 ;;
esac

display="${file##*/}"

# --- rules --------------------------------------------------------------------

# Explicit types: the type is written out, never inferred. Three exclusions, all
# cases where reporting would be wrong rather than merely noisy: a comment line
# (documentation routinely shows `var` in sample code), an anonymous type (C#
# gives no other spelling), and an occurrence inside a string literal (the
# analyzer suites embed C# fixtures that deliberately use `var`).
# shellcheck disable=SC2317  # reached through the `"rule_${rule}"` dispatch in the run section below
rule_explicit_types() {
  awk -v name="$display" '
    {
      # Raw string literals ("""...""") carry code this repository does not own:
      # the generated HTML pages embed JavaScript, and the analyzer suites embed C#
      # fixtures that use `var` on purpose. Track the delimiters so the whole block,
      # and the delimiter lines themselves, are out of scope.
      fences = gsub(/"""/, "\"\"\"")
      was_raw = in_raw
      if (fences % 2 == 1) in_raw = !in_raw
      if (was_raw || in_raw) next

      body = $0
      sub(/^[ \t]*/, "", body)

      if (body ~ /^\/\//) next                                  # // or /// comment
      if (body ~ /^\*/)   next                                  # continuation of a /* */ block

      if (!match($0, /(^|[^A-Za-z0-9_.])var[ \t]+[A-Za-z_]/)) next   # no inferred declaration
      if ($0 ~ /=[ \t]*new[ \t]*\{/) next                            # anonymous type: mandatory

      # Everything up to and including the character before `var`. Counting quotes
      # in it tells us whether the occurrence sits inside a string literal; the
      # separator must be kept, since it is often the opening quote itself.
      prefix = substr($0, 1, RSTART)
      if (gsub(/"/, "\"", prefix) % 2 == 1) next                     # unclosed quote: inside a literal

      printf "  %s:%d  %s\n", name, FNR, body
    }
  ' "$file"
}

# shellcheck disable=SC2317  # reached through the `"${rule}_hint"` dispatch in the run section below
explicit_types_hint() {
  printf '%s' "Coding rule — explicit types (CLAUDE.md, \"Always-on rules\"). This file now declares
inferred types:

${1}
Write the type out. \`var\` is reserved for the declarations C# gives no other
spelling, which in practice means anonymous types. The build reports the same
thing as IDE0008 and CI turns it into an error, so this does not merge as-is.
"
}

# Suppression form: a [SuppressMessage] is written with the short name — every
# file carries `using System.Diagnostics.CodeAnalysis;` — and keeps its whole
# argument list on ONE line, however long it runs. Two suppressions on one member
# then read as two rules rather than one wrapped block. A justification long
# enough to make that unreadable moves to a SuppressionJustification constant; the
# attribute is not re-wrapped. Both halves are textual, so both are checked here
# rather than left to review. What the rule is NAMED with is not checked: DCAT0006
# and DCAT0014 already make a literal, or a missing justification, a build error.
# The same three exclusions as above apply, for the same reasons.
# shellcheck disable=SC2317  # reached through the `"rule_${rule}"` dispatch in the run section below
rule_suppression_form() {
  awk -v name="$display" '
    {
      fences = gsub(/"""/, "\"\"\"")
      was_raw = in_raw
      if (fences % 2 == 1) in_raw = !in_raw
      if (was_raw || in_raw) next

      body = $0
      sub(/^[ \t]*/, "", body)

      if (body ~ /^\/\//) next                                  # // or /// comment
      if (body ~ /^\*/)   next                                  # continuation of a /* */ block

      if (!match($0, /SuppressMessage[ \t]*\(/)) next

      # Inside a string literal (the analyzer suites embed C# fixtures): skip.
      prefix = substr($0, 1, RSTART)
      if (gsub(/"/, "\"", prefix) % 2 == 1) next

      if ($0 ~ /CodeAnalysis[ \t]*\.[ \t]*SuppressMessage/) {
        printf "  %s:%d  qualified name — write the short name\n      %s\n", name, FNR, body
        next
      }

      # A single-line attribute always closes with `)]`. Nothing else does.
      if ($0 !~ /\)[ \t]*\]/)
        printf "  %s:%d  argument list wrapped — keep it on one line\n      %s\n", name, FNR, body
    }
  ' "$file"
}

# shellcheck disable=SC2317  # reached through the `"${rule}_hint"` dispatch in the run section below
suppression_form_hint() {
  printf '%s' "Coding rule — the shape of a [SuppressMessage] (.claude/rules/csharp.md).
This file now declares:

${1}
Spell it with the short name (the file carries \`using System.Diagnostics.CodeAnalysis;\`)
and keep the whole argument list on a single line, however long. Two suppressions on
one member must read as two rules, not one wrapped block. If the justification makes
the line unreadable, move the text to a \`SuppressionJustification.<RuleId>\` constant
— do not re-wrap the attribute.
"
}

# Solution nesting: every project in JustDummies.sln must also appear in
# GlobalSection(NestedProjects), under the `src`, `tests` or `doc` solution folder
# like its siblings. A project left out sits loose at the solution root in Visual
# Studio and Rider instead of grouped with the rest. This has been missed and fixed
# after the fact several times, and it is entirely mechanical — which is what makes
# it a hook's job (ADR-0073).
#
# Read from the solution rather than from the edited file: a new .csproj says
# nothing about the solution, and the omission is only visible there.
# shellcheck disable=SC2317  # reached through the `"rule_${rule}"` dispatch in the run section below
rule_sln_nesting() {
  root="${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel 2>/dev/null || true)}"
  [ -n "$root" ] || return 0
  sln="$root/JustDummies.sln"
  [ -f "$sln" ] || return 0

  awk '
    BEGIN { FS = "\"" }

    # Project("{type}") = "Name", "Path\Name.csproj", "{guid}". A solution folder
    # repeats its name as its path, so the project-file extension tells the two
    # apart without hard-coding the folder type GUID.
    /^Project\(/ {
      if ($6 ~ /\.[A-Za-z]+proj$/) { name[$8] = $4; order[++n] = $8 }
      next
    }

    /GlobalSection\(NestedProjects\)/          { nesting = 1; next }
    nesting && /EndGlobalSection/              { nesting = 0; next }
    nesting {
      if (match($0, /\{[0-9A-Fa-f-]+\}/)) nested[substr($0, RSTART, RLENGTH)] = 1
      next
    }

    END {
      for (i = 1; i <= n; i++)
        if (!(order[i] in nested)) printf "  %s  %s\n", name[order[i]], order[i]
    }
  ' "$sln"
}

# shellcheck disable=SC2317  # reached through the `"${rule}_hint"` dispatch in the run section below
sln_nesting_hint() {
  printf '%s' "Repository rule — solution folders (.claude/rules/build-and-ci.md).
These projects are in JustDummies.sln but not in GlobalSection(NestedProjects):

${1}
Add one line per project to that section, nesting it under the same solution folder
as its siblings — \`src\` for shipping code, \`tests\` for a test project, \`doc\` for the
documentation project. A project left out sits loose at the solution root in Visual
Studio and Rider instead of grouped with the rest.
"
}

# --- run ----------------------------------------------------------------------

report=''
for rule in $RULES; do
  offences="$("rule_${rule}" 2>/dev/null || true)"
  [ -n "$offences" ] || continue
  report="${report}$("${rule}_hint" "$offences")
"
done

[ -n "$report" ] || exit 0               # clean: stay silent

printf '%s' "$report" >&2
exit 2                                   # advisory; PostToolUse surfaces it to the agent
