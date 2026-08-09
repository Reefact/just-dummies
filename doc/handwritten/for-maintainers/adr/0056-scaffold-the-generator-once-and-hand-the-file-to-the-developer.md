# ADR-0056 | Scaffold the generator once and hand the file to the developer

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md), the document this record was extracted from.

## Context

The tool writes a C# file, containing a generator for a type of the developer's own code, into the
developer's own project. Three shapes exist for such a tool, all of them in use by real tooling: a
Roslyn source generator producing the file into the build's intermediate output; a file written
once into the source tree; and a file written into the source tree together with a verification
command that fails when it no longer matches what the tool would produce today.

A file in the source tree can fall out of step, silently, with the type it was derived from when
that type's constructor changes.

The library the tool serves states the absence of magic as part of its positioning: no reflection,
no object-graph filling, and its own description is "small, deterministic, explicit".

The tool cannot infer every constructor parameter. Some parameters carry invariants expressed in
ways no closed rule set can read (§9), so a scaffolded file is expected to be incomplete for some
types.

A source generator's output is not editable by the developer and does not appear in code review. A
file in the source tree is both.

## Decision

The tool writes each generator file once and transfers ownership of it to the developer, who may
edit it freely and is never asked to regenerate it.

## Rationale

Drift is the only serious objection to writing into the source tree, and it exists only while the
tool claims ownership of the file. Once ownership is transferred, "the file no longer matches what
the tool would produce" stops being a defect and becomes the expected state of a file the developer
has edited — which is precisely what the tool asks them to do. The objection dissolves rather than
being mitigated.

That transfer is also what makes an incomplete file acceptable. A tool that owns its output must
produce something complete or fail; a tool that hands over a skeleton may stop where its knowledge
stops and say so, which is the honest position given that some invariants are unreadable. [ADR-0060](0060-seed-generators-from-constructor-guards.md) and
[ADR-0060](0060-seed-generators-from-constructor-guards.md) depend on this being settled first.

Editability and review visibility serve a library whose selling point is that nothing happens
behind the developer's back. A generator they can read, step through in a debugger and modify is
consistent with that positioning; one materialised by the compiler is not.

Removing ownership removes an entire class of machinery with it: no verification verb, no
regeneration protocol, no drift detection, no rules about which regions may be hand-edited. For a
tool whose first design rule is that it must be trivial to adopt, the machinery not built is worth
more than the guarantees it would have offered.

## Alternatives Considered

##### A Roslyn source generator

Considered because it makes drift structurally impossible: it re-runs on every build, so its output
cannot lag the type.

Rejected because it forfeits everything that the file being real buys. The developer cannot edit
it, cannot complete the parameters the tool failed to infer, and reviewers never see it. It also
has no useful way to leave work unfinished, so the unresolved-parameter case would have to fail the
build with no place for the developer to act.

##### A written file plus a verification verb

Considered because it is the standard answer to drift for committed generated artefacts, and
integrates cleanly into continuous integration.

Rejected because verification and editing are mutually exclusive. A command that fails whenever the
file differs from a fresh generation forbids the very editing this tool exists to invite. Keeping
both would mean encoding which regions belong to the tool and which to the developer — more
machinery than the whole feature is worth.

## Consequences

**Positive.** The tool has one verb and no protocol. The scaffolded file is ordinary code:
reviewable, debuggable, editable. The unresolved-parameter path of [ADR-0060](0060-seed-generators-from-constructor-guards.md) becomes available.

**Negative.** A generator can fall behind its type. Adding a constructor parameter breaks the
generator's compilation, which surfaces the problem; changing a parameter's invariant does not — the
generator keeps producing values the constructor now rejects, and only a failing test reveals it.

**Risks.** A developer may expect regeneration to preserve their edits. Mitigated by the emitted
header, which states that regeneration overwrites and that the type is `partial` so neighbouring
files survive, and by `--force` being required to overwrite at all.

## Follow-up Actions

* State the "this file is yours" position prominently in the tool's user documentation: it inverts
  the expectation set by most scaffolding tools.

## References

* §1, §3, §4.3 of this specification.

---
