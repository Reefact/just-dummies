# ADR-0127 | Draw the empty value unless a guard refuses it

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0127-draw-the-empty-value-unless-a-guard-refuses-it.fr.md)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

> Section references (§N) point into the [`any` specification](../specifications/justdummies-tool.md).

## Context

The base table of §5.2 wrote `Any.String().NonEmpty()` for every `string` parameter and `Any.Guid().NonEmpty()`
for every `Guid`, whatever the constructor said. The specification argued it from a measurement: an unconstrained
string came back empty about one draw in seventeen when §17 measured it, one in a thousand under the wider spread
ADR-0076 set later, and a domain string was held to be overwhelmingly required non-empty. The recap tagged the
row `guard`, the same word it gives a constraint read from a guard.

Since then, a guard the engine cannot read marks its parameter and blocks the build
([ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md)), and the guard delegated to a
value-returning helper, the main silent path, is marked too (#407). A constructor that refuses the empty value is
therefore read — `IsNullOrEmpty`, a length of zero, `IsNullOrWhiteSpace` as `NotBlank()`, their throw helpers and
the two guard libraries — or marked `to verify`.

The second developer-UX trial met the default in context 35, DNA variants
([CLI-16](../../../../tools/ux-trial/REPORT-2.md#cli-16)). An allele is the sequence of bases one side of a variant
carries, and the empty allele is valid and common: the reference side of an insertion, the alternate side of a
deletion. The constructor said so in its documentation and refused nothing of the kind. The scaffold still wrote
`NonEmpty()`, an insertion or a deletion was never drawn, and one of the three users believed the constructor
refused the empty allele.

The library's first fundamental, in `CLAUDE.md`: a dummy is a value the test does not care about, and the draw
exists to put that indifference to the test. A value the constructor accepts is a valid dummy; a test that fails
on it either depended on the parameter or found a defect, and the seed replays it either way.
[ADR-0117](0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.md) applied that reasoning to the values at a
type's limit and to `null`.

## Decision

A `string` or `Guid` parameter is drawn `NonEmpty()` only where a guard of its constructor refuses the empty value;
the base table adds no refusal of its own.

## Rationale

**The default guessed a rule the constructor did not write, and guessed wrong where it mattered.** Its premise —
that a domain string is overwhelmingly required non-empty — was a statistical bet on constructors in general. The
trial produced the constructor it loses on: a type whose empty value is not a corner case but half its meaning.
A guess that is right most of the time is still a rule the developer never declared, written into a file that
claims to draw what the constructor accepts.

**The guess stopped earning its keep when the guard reader learned to say what it could not read.** The default was
protection against a silent failure: a constructor refusing the empty string, unread, drawing it and throwing. That
failure no longer happens in silence. The refusal is read into the chain when it is written in a shape the engine
knows, and the parameter is marked and the build blocked when it is not (ADR-0083). Everything the default
protected against is now either honoured or declared.

**A dummy the constructor accepts is a dummy.** Drawing the empty allele is not a risk to the suite; it is what
drawing is for. Code further on that breaks on it had a dependency the literal would have hidden, and the seed
replays the draw. Wording the empty draw as instability is the misreading the second fundamental forbids.

**The `guard` tag has to mean what it says.** A recap row tagged `guard` for a constraint no guard produced told
the developer the domain said something it did not. Once the row carries only what guards produced, the tag is
honest again, and a plain `Any.String()` reads as the neutral draw it is — with `JD030` naming the length remedy
at the call site, as it already does.

**One rule for one idea, again.** ADR-0117 decided that a value the type names — a limit, `null` — is drawn rather
than quietly avoided. The empty string and `Guid.Empty` are the same kind of value: declared, legal, and exactly
where a hidden dependency shows. Keeping a default that avoided them would have left the base table arguing
against the decision the draws had just adopted.

## Alternatives Considered

### Keep the default

Considered because it had a measurement behind it and because most constructors do refuse the empty string.

Rejected: the measurement was taken before ADR-0083, when an unread refusal was a silent failure. With the mark in
place the default protects nothing that is not already honoured or declared, and it costs the one domain where the
empty value is legal, under a tag that hides where the rule came from.

### Keep the default, and mark the parameter `to verify`

Considered as a way to keep the protection while saying it was a guess.

Rejected: the mark is for a guard the engine saw and could not read (ADR-0083), never for an opinion the engine
formed on its own. Blocking every string parameter's build to ask the developer to confirm a rule the engine
invented would turn the mark into noise on the ordinary path.

### Make the default a project option

Considered because `any.json` already carries project defaults
([ADR-0072](0072-read-project-defaults-from-a-file-the-command-line-overrides.md)).

Rejected: a switch for a guess is still a guess, applied to every string of the project at once; the place to say
"this string is never empty" is the constructor, which the engine reads, or the parameter's valid base, which the
developer owns.

## Consequences

### Positive

* An empty string or `Guid.Empty` is drawn wherever the constructor accepts it, so a dependency on content the
  test never declared shows, and the seed replays it.
* The recap's `guard` tag covers only what guards produced, and the base table no longer holds an opinion the
  guard reader had to let yield under combination.

### Negative

* A string parameter with no guard at all draws empty about one draw in a thousand, so a suite that silently
  relied on content it never asked for will see a failure it never saw. Each one is replayable from its seed and
  is either a test depending on a parameter or a defect — never a cost of the draw.
* A file scaffolded before this decision keeps its `NonEmpty()` until it is re-scaffolded (§1).

### Risks

* A developer who wants the old behaviour on one parameter adds `NonEmpty()` to its valid base; one who wants it
  on every string of a type has no switch and has to write it once per parameter. That is deliberate.
* The `Uri` row still writes `.Web()`, an opinion of the same kind on the scheme a domain accepts. It is left in
  place: no report has met a constructor accepting a non-web URI, and under
  [ADR-0085](0085-change-the-guard-reader-only-against-a-field-report.md)'s symmetry a removal needs a report too.

## Follow-up Actions

* The `cli` changelog and the release note say that files already scaffolded keep their `NonEmpty()`.

## References

* Issue [#409](https://github.com/Reefact/just-dummies/issues/409).
* [ADR-0060](0060-seed-generators-from-constructor-guards.md) — seed generators from constructor guards.
* [ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md) — block compilation on a guard the engine cannot vouch for.
* [ADR-0117](0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.md) — draw a limit value the type permits one draw in ten.
* [`any` specification](../specifications/justdummies-tool.md), §5.2 — the base table.
