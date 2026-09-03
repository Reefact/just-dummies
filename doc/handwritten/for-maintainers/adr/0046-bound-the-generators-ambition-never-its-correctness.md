# ADR-0046 | Bound the generator's ambition, never its correctness

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0046-bound-the-generators-ambition-never-its-correctness.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-01
**Accepted:** 2026-08-01
**Decision Makers:** Reefact

## Context

`JustDummies` produces a value that is arbitrary and valid for the constraints declared at a call
site. A generator is a fluent recipe: each constraint narrows what may be drawn, contradictory
constraints fail at declaration naming both sides, and the value is built to satisfy the whole
specification rather than drawn and filtered.

Seven accepted decisions in this base each bound something, independently and for their own local
reasons:

| Decision | What it bounds |
| --- | --- |
| [ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.md) | A distinct collection uses a bounded deduplicating draw and fails explicitly when it cannot reach the requested count. |
| [ADR-0005](0005-cap-any-combine-at-arity-eight.md) | `Dummy.Combine` provides arities two through eight and stops there. |
| [ADR-0008](0008-generate-strings-from-a-home-grown-regular-subset.md) | `Dummy.StringMatching` covers the regular subset with the library's own parser and refuses a non-regular construct by name, rather than taking a regex-automaton dependency. |
| [ADR-0012](0012-meet-string-exclusions-with-a-bounded-redraw.md) | A string exclusion is met by a bounded redraw. |
| [ADR-0027](0027-guarantee-a-generated-regex-value-matches-by-bounded-redraw.md) | A generated regex value is guaranteed to match by a bounded redraw. |
| [ADR-0029](0029-let-a-size-maximum-cap-without-steering-the-draw.md) | A size the generator must actually produce is refused above one million. |
| [ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.md) | Floating-point and decimal draws stay within an ordinary magnitude rather than roaming the type's full range. |

**No decision states the rule they share.** Each argues its own bound from first principles, so every
new question about coverage is re-argued from scratch — and can be re-argued differently.

Two further facts bear on that. The library advertises and guards a **zero runtime dependency**
identity ([ADR-0003](0003-host-dummies-as-a-standalone-package.md)), checked both by an architecture
test over referenced assemblies and by an inspection of the produced `.nupkg`. And any sequential run
must replay from the seed it reports, which is the product's headline promise.

Finally, this repository is developed largely through agent sessions, whose branches make up most of
its recent history. An agent orients itself from whatever instruction file it reads, and those files
drift: `CLAUDE.md` and `AGENTS.md` still described a different product until 2026-07-31.

## Decision

`JustDummies` bounds what it will attempt — the surface it exposes and the effort it spends — and
refuses at that boundary with a named, first-class error rather than reaching for a more capable
mechanism; it never bounds the correctness of a value it does return.

## Rationale

**The name is the scope.** A dummy is a value that stands in for a real one in a test. Its worth
comes from being valid and unremarkable, not from being drawn by an ideal process. Effort spent
widening what can be generated is effort not spent on the two properties consumers actually rely on:
that a returned value satisfies every declared constraint, and that a run replays.

**Widening coverage tends to cost the zero-dependency identity.** The mechanisms that would remove
these bounds — a regex automaton, a constraint solver, an SMT backend — are not things one writes in
an afternoon. Taking one as a dependency would be the library's first, would appear in every
consumer's tree and SBOM, and would contradict a property this base already decided to guard. The
choice is therefore rarely "bounded or complete"; it is "bounded, or complete at the cost of the
identity".

**An unexplainable mechanism is a reproducibility risk.** A bounded construction can be reasoned
about from its inputs and its seed. A search that succeeds by exploration is harder to replay with
confidence, and any divergence surfaces as a test that fails for no reason a diff explains — the
worst failure this product can hand a user.

**Cleverness fails silently; refusal fails usefully.** A mechanism capable enough to satisfy a set of
constraints the user did not mean to write will do exactly that, hiding a modelling error that
fail-fast conflict detection exists to surface. A named error at declaration time — the shape
ADR-0008 already chose — tells the user which construct is unsupported and what to do instead.

**Writing the rule down is what makes it hold.** Seven instances and no parent means the eighth
question is answered by whoever answers it first, and in a repository written largely by agents, that
is whoever read the most recent file. A decision record is stable in a way an instruction file is
not, and it is where a human contributor looks.

**The second half of the decision is not decoration.** "Bound the ambition" read alone is an
invitation to cut corners. Correctness is not on the table: a returned value satisfies every
constraint declared, and the analyzer set and the property suite exist to hold that line. The
boundary is about what the library *attempts*, never about what it *guarantees* once it does.

## Alternatives Considered

### Leave the principle implicit in the seven bounding decisions

They are accepted, they are consistent, and a careful reader can infer the rule. Rejected: inference
is not a decision. The rule's value is precisely that it answers a question *before* someone
re-derives it, and seven independent derivations already show it being re-derived rather than cited.

### Record it only in `CLAUDE.md` and `AGENTS.md`

Those files are read first by the agents doing most of the work, so the reach argument is real.
Rejected as sufficient: they are operational, they are mutable, and they demonstrably drift — both
described another product entirely until 2026-07-31. A human contributor reading the decision base
would never encounter the principle at all. They remain the right place for the short operative
instruction, which now cites this ADR.

### Record it as `ADR-0000`, dated before ADR-0001, so it reads first

Tempting, because the principle is foundational and one would like it read before the decisions it
governs. Rejected on two grounds. [ADR-0036](0036-keep-one-dated-line-per-state-an-adr-reached.md)
requires one dated line per state the decision actually reached *in this repository*, and this one
was reached today; backdating it would break the rule that governs dates in order to express a
reading order. And [ADR-0045](0045-renumber-the-decision-base.md) established that a number is a
stable handle, not a position — presenting this record first is the index's job, not the numbering's.

### Pursue completeness instead

Support every regex construct, solve arbitrary constraint sets, remove the caps. Rejected: it buys
marginal cases for a test-support library at the cost of the zero-dependency identity, of a draw
nobody can explain, and of a maintenance surface out of proportion to the use case.

## Consequences

### Positive

* A coverage question now has a default answer and one place to cite, instead of seven precedents to
  weigh.
* The boundary becomes a documented product property rather than an apparent gap: "refuses a
  non-regular construct by name" reads as design, not as an unfinished feature.
* The zero-dependency identity gains an argument that applies before a dependency is even evaluated.

### Negative

* Some legitimate requests are refused. A user wanting a lookahead in `Dummy.StringMatching` is told no,
  and the answer stays no until this decision is superseded.
* Contributors and agents may read the first half of the decision as licence to be careless. The
  second half is stated for that reason, and every review should hold it.

### Risks

* The principle could be invoked to refuse something genuinely cheap and worthwhile, turning a
  deliberate boundary into an excuse. Mitigation: refusing is a decision that must be argued, exactly
  like widening — this ADR raises the bar for both, not for one.
* The balance shifts with adoption. Below 1.0 with no consumers, bounded and honest is clearly right;
  a stable release with users asking for a construct may justify a different trade. That is a
  supersession, not a reinterpretation.

## Follow-up Actions

* None required. `CLAUDE.md` and `AGENTS.md` carry the operative instruction and cite this record.

## References

* [ADR-0003](0003-host-dummies-as-a-standalone-package.md) — the zero-dependency identity this
  decision protects.
* [ADR-0036](0036-keep-one-dated-line-per-state-an-adr-reached.md) — why this record is dated today
  rather than backdated.
* [ADR-0045](0045-renumber-the-decision-base.md) — why its number carries no reading order.
* The seven bounding decisions listed in *Context*, which this record consolidates rather than
  replaces. None of them is superseded: each remains the decision for its own case.
