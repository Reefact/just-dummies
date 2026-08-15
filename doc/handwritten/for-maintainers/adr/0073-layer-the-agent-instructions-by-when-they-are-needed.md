# ADR-0073 | Layer the agent instructions by when they are needed

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0073-layer-the-agent-instructions-by-when-they-are-needed.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-15
**Accepted:** 2026-08-15
**Decision Makers:** Reefact

## Context

[ADR-0035](0035-state-the-coding-rules-where-an-agent-can-act-on-them.md) moved the coding
rules into `CLAUDE.md` because a pointer to a file the reader cannot open is not an
instruction. Its follow-up action was to add the remaining rules as they were identified.
They were: `CLAUDE.md` now carries the product scope, the language policy, the build and
test commands, the project map, the change guidelines, the coding rules, the diagnostic and
documentation conventions, the ADR procedure, the pull-request conventions and the
review-feedback procedure — 284 lines, 21 543 bytes.

Every one of those bytes is loaded at the start of every session, before the task is known.
Claude Code walks the directory tree at launch and concatenates the memory files it finds;
`@path` imports are expanded at the same moment, so splitting a file into imports moves
bytes without moving the cost. The vendor documents a target of under 200 lines per file
and states that longer files both consume more context and reduce how consistently the
instructions are followed.

The content is not uniformly needed. The ADR procedure matters when a pull request is being
finalised, the release procedure when a train is cut, the CLI conventions when
`JustDummies.Cli` or `JustDummies.GenAny` is touched, the analyzer five-in-step rule when a
`JDxxx` rule moves. A change to a single test pays for all of them.

Part of the content is not needed as prose at all, because a tool already refuses the
mistake. `IDE0008` makes an inferred type a build warning locally and an error in CI
([ADR-0034](0034-enforce-the-style-rules-the-compiler-can-express.md)). The
`DiagnosticCatalog` analyzers wired in `Directory.Build.props` raise a suppression named by
string literal, or missing its justification, as an error with no `.editorconfig`
derogation, which is [ADR-0050](0050-name-a-suppressed-rule-through-a-catalogue-constant.md)
made unbypassable. A commit header is checked by the same linter in `.githooks/commit-msg`
and in CI. `ValueObjectConventionTests` holds every `[ValueObject]` type to its identity;
`TranslationParityTests` holds every French page to its English twin.

Since ADR-0035 was accepted, Claude Code has grown two mechanisms that did not exist then.
A file under `.claude/rules/` carrying a `paths:` front-matter field is loaded **when the
agent reads a file matching the glob**, not at launch and not on request. A skill under
`.claude/skills/` preloads only its description; its body loads when the agent judges it
relevant, or when it is invoked by name. Neither requires the agent to decide to open a
document.

The repository's own history says what is at stake if this is got wrong. The explicit-type
rule drifted to 203 violations while an instruction to follow it was in place. A supplied
image was composited into three worse variants. A project's GUID has been left out of the
solution's `NestedProjects` section and fixed after the fact several times.

## Decision

Each agent instruction lives at the layer matching when it is needed — always loaded,
loaded by path, loaded on demand, or enforced by a tool instead of stated — and `CLAUDE.md`
keeps only what is worth paying for on every task.

## Rationale

The instruction that failed in ADR-0035 failed because its reader could not act on it. The
instruction that fails here fails differently: it is present, it is readable, and it is
irrelevant to the task at hand, so it dilutes the instructions that are not. Both are
failures of *delivery*, and both are answered by putting the rule where its reader meets
it. ADR-0035 answered that question in space — write the rule where the agent looks. This
answers it in time — deliver the rule when the agent needs it. The second question could not
be asked before, because the only two options were "always loaded" and "behind a pointer".

That is why this is not the dedicated document ADR-0035 rejected. That alternative was
rejected because a separate document needs a pointer from `CLAUDE.md` to be found, which
reintroduces exactly the indirection that had already failed. A path-scoped rule needs no
pointer: it arrives because the agent opened a matching file, which is the same event that
makes the rule relevant. The mechanism removes the step that made the alternative unsafe,
and the argument against it does not survive the removal.

Moving a rule out of the prose is only safe where the mechanism that replaces it is at
least as strong. Where a compiler, an analyzer, a test or a CI job already refuses the
mistake, the prose was never what protected the repository, and restating it buys adherence
to something that does not depend on adherence. Where nothing refuses it — do not reformat
what you did not change, the shape a `[SuppressMessage]` takes, a property that must not
walk a collection, an image that ships byte for byte — the prose is the only guard, and it
stays, either always loaded or scoped to the files it governs. The rule of thumb is short
enough to apply: what a tool can decide belongs to the tool, what needs judgement belongs
to an instruction, and the instruction is scoped as narrowly as its subject.

Keeping the always-loaded layer small is what makes the rest work. It carries what a change
of any kind can violate — the product's scope and the refusal it implies, the build and
test commands, the map of the repository, the platform floor, the language, and the handful
of prohibitions whose breach is cheap to commit and expensive to undo — and it carries the
routing, so the agent knows a procedure exists before it knows its content. Two rules keep
it honest: an entry earns its place only if it is reasonable to pay for it while fixing an
unrelated test, and no entry is moved out until its new home exists.

Layering also bounds a cost the previous arrangement paid silently. A base of 72 decisions
cannot be read on every pull request, and the ADR check has always meant *select the
decisions this change could touch*, never *read them all*. Stated in prose among 283 other
lines, that distinction is easy to lose; stated as the procedure a skill runs, the selection
step is the first thing the procedure does.

## Alternatives Considered

### Shorten `CLAUDE.md` by deleting the reasoning behind each rule

Considered because most of the file's weight is explanation, not instruction, and the
instructions alone would fit in a third of the space.

Rejected because the explanations are what the rules are made of. "Never alter an image I
supply" is followed differently by a reader who knows three variants were once composited
out of one supplied mark; a rule whose cost is invisible is a rule that gets traded away
against a plausible-looking convenience. The reasoning is not removed by this decision, only
relocated to where it is read.

### Split `CLAUDE.md` into topic files pulled in with `@path` imports

Considered because it is the documented way to organise a large memory file, and it would
leave the content addressable and the file navigable.

Rejected because imports are expanded at launch. The context cost would be identical to
today's, so the change would be organisational only, while adding a layer of indirection to
every rule. Moving bytes without moving the cost is not the optimisation this decision is
about.

### Enforce more of the prose with hooks rather than relocating it

Considered because a check that runs is stronger than a rule that is read, and ADR-0035
already established the hook as this repository's way of making a rule observable.

Rejected as a general answer, though adopted where it applies. Most of what remains in
prose is judgement — whether a change embarks a lasting decision, whether a finding is worth
a blocking label, whether a history reads clean — and a script that ruled on those would be
making the maintainer's call with none of the maintainer's information. The rules that are
genuinely mechanical are moved to the hook by this decision; the rest cannot be, and
pretending otherwise would replace a diluted instruction with a wrong one.

### Give each subdirectory its own `CLAUDE.md` instead of using `.claude/rules/`

Considered because nested memory files also load on demand, and they need no new directory.

Rejected because the scope that matters here is rarely a directory. The documentation rule
governs `doc/**` and the repository's root pages; the C# rules govern seven projects; the
build rule governs `.csproj` files, `Directory.*.props`, `build/`, `tools/` and the
workflows. A glob states that directly, where nested files would need the same rule copied
into several trees and kept in step by hand.

## Consequences

### Positive

* A task pays for the instructions it can violate, not for every instruction in the
  repository.
* The knowledge that used to be diluted is now delivered at the moment it applies, which is
  also the moment it is most likely to be followed.
* Rules already guaranteed by a compiler, an analyzer, a test or CI stop being restated as
  requests, so the prose that remains is the prose that carries weight.
* The ADR check states its selection step explicitly, which bounds the cost of a decision
  base that will keep growing.
* Two rules that were "checked by review" — the shape of a `[SuppressMessage]`, a project
  missing from the solution's `NestedProjects` section — become observable at the edit.

### Negative

* The instructions now live in four places instead of one, and a maintainer must know which
  layer a new rule belongs to before adding it.
* A path-scoped rule is only as good as its glob. A pattern that misses a file silently
  withholds the rule that file needed.
* Rules with `paths:` are not re-injected after a compaction; they reload the next time a
  matching file is read, so a long session can run for a stretch without a rule it had
  earlier.

### Risks

* A rule moved out of the always-loaded layer can be missed by a task that needed it but
  never opened a matching file — a request phrased entirely in prose, for instance.
  The mitigation is that anything whose breach is cheap to commit keeps a one-line statement
  in `CLAUDE.md`, with the reasoning scoped.
* The layering is a judgement with nothing checking it. Nothing fails if a rule is filed at
  the wrong layer, and the symptom — an instruction quietly not followed — is the same one
  ADR-0035 was written about.
* Skills load when the agent judges them relevant. A description that does not match how a
  request is phrased leaves the procedure unloaded, and the agent proceeds without knowing
  it existed.

## Follow-up Actions

* Watch for a rule that is not applied because its glob did not match, and widen the glob
  rather than moving the rule back.
* Re-check the always-loaded layer against this decision's test whenever a rule is added to
  it, and keep the file under the vendor's 200-line target.
* Confirm each skill is actually reached by the requests it is meant to serve, and rewrite
  the description rather than duplicating the content into `CLAUDE.md` when it is not.

## References

* [ADR-0035](0035-state-the-coding-rules-where-an-agent-can-act-on-them.md) — the decision
  this one continues: it put the rules where an agent reads them, this one decides when each
  is delivered. Its third rejected alternative is revisited here on the strength of a
  mechanism that did not exist at the time.
* [ADR-0034](0034-enforce-the-style-rules-the-compiler-can-express.md) — the build-time gate
  that lets the explicit-type rule survive as a single line.
* [ADR-0050](0050-name-a-suppressed-rule-through-a-catalogue-constant.md) — enforced by
  analyzers at error severity, and therefore no longer restated as a request.
* [ADR-0002](0002-check-every-pull-request-against-the-adr-base.md) — the check whose
  selection step this decision makes explicit.
