---
paths:
  - "**/*.cs"
---

# Writing C# in this repository

Rules that judgement has to apply. What a tool already refuses is listed at the end so it
is not mistaken for something you must watch.

## Value objects are `class`, never `struct`

A type whose instances are values declares itself `[ValueObject]` (ADR-0043) — today
`ConstraintClaim`, `ConstraintCall` and `Replay`. Such a type **must** be a `class`: a
`struct` always exposes an unsuppressable default constructor (`default(T)`, `new T[]`,
uninitialized fields) yielding a zero-initialized instance that bypasses every validating
constructor, and nullable reference types only warn at compile time. Do not convert one to
`struct`/`readonly struct` for allocation reasons: these sit on the constraint-declaration
path, not in a hot loop, and invariant correctness takes precedence. Enums are the
legitimate value-type case — they carry no invariant to bypass.

A reflection convention in `JustDummies.UnitTests/ValueObjectConventionTests.cs` holds every
marked type to a full value identity and to rendering itself for a reader.

**A declared constraint is carried as a value object, never as the text it renders to**
(ADR-0042).

## A property protects a field; a computation is a method

A getter is read as an access, not as a call: callers put one in a loop, in a debugger
watch, in a condition evaluated twice, on the assumption that reading it again costs what
reading it once did. So a member that allocates, copies a collection or walks one — however
short — is a **method**, named for the work it does, while a member that returns a field, or
a field plus an O(1) test, stays a property.

The surface already reads this way and must keep doing so: on `ICardinalityHint<T>`,
`DistinctCardinality` is a property because it is a field's `Count`, and `Contains(T)` is a
method because it searches. Watch the case that looks free: handing out a `List<T>` field as
`IReadOnlyList<T>` leaks a handle a caller can cast back and mutate, so it has to be wrapped
— and the wrapper is an allocation per call, which makes the member a method.

`S2365` (*properties should not make collection or array copies*) catches part of this; it
does not see a scalar property doing O(n) work, so that half rests on review.

## The shape of a `[SuppressMessage]`

The attribute is spelled with the **short name** — the file carries
`using System.Diagnostics.CodeAnalysis;` — and its whole argument list stays on a **single
line**, however long that line runs. Both halves serve the same reading: when two
suppressions sit on one member, one line each shows at a glance that they are two different
rules rather than one wrapped block, and the qualifier repeated 79 times said nothing the
using does not.

A justification used at a **single** site stays inline, next to what it justifies. The
moment the **same text** serves a second site, it moves to a `const` in a nested static
class named after the rule id — `SuppressionJustification.S3267.AccumulatorAdvancesInLoop`
— whose `///<summary>` carries the detailed reasoning while the constant's value stays one
crisp sentence; both sites then reference the constant, so the reasoning has one home and
cannot drift into diverging copies (a copy-paste of one of these blocks is also how a
duplicated attribute once slipped in). The same value/summary split is **allowed** for a
single-site justification whose author wants the attribute short and the detail documented.
An attribute whose justification makes its line unreadable takes that remedy — it is never
re-wrapped.

## Platform floor

Preserve compatibility with the **netstandard2.0** floor: a net8.0-only API belongs behind
the existing `#if NET8_0_OR_GREATER` additive branch, never in the common surface. net8.0
additionally carries the generators for the types absent downlevel — `DateOnly`, `TimeOnly`,
`Int128`, `UInt128`, `Half`. The supported .NET Framework floor is 4.7.2 and CI runs the
suites on it (ADR-0007).

## Already enforced — do not restate as a request

* **`var`** → `IDE0008`, a build warning locally and an error in CI (ADR-0034), plus the
  edit-time hook.
* **A suppression named by string literal, or missing its justification** → `DCAT0006` /
  `DCAT0014`, errors by default with no `.editorconfig` derogation. The catalogue constants
  are `SonarRule`, `NetAnalyzersRule`, `JustDummiesRule` (ADR-0050); if the rule you need
  belongs to a catalogue this repository does not reference yet, say so and stop.
* **Every Sonar rule** in `build/sonar-profile.globalconfig` at `warning`, promoted to an
  error by the CI ratchet.
* **The public surface of `JustDummies` and `JustDummies.Xunit`** → `RS0016`/`RS0017`
  against the committed `PublicAPI/<tfm>/` baselines. Accepting an intended change means
  updating the baseline in the same commit; the procedure is in `CONTRIBUTING.md`,
  "Public API baseline".

## Reference

* `doc/handwritten/for-maintainers/architecture.en.md` — the projects, the draw pipeline,
  and where a change of a given kind belongs.
