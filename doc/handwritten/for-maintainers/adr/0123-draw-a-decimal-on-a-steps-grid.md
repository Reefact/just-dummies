# ADR-0123 | Draw a decimal on a step's grid

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0123-draw-a-decimal-on-a-steps-grid.fr.md)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

## Context

[ADR-0015](0015-draw-lattice-constrained-scalars-on-the-grid.md) gave `decimal` one lattice, `WithScale(n)`: the
multiples of `10⁻ⁿ`. It set the number of places, not the step. The first developer-UX trial met two grids that are
not a number of places — a tick size of `0.25` (domain 02) and half-point grades (domain 18) — and participants wrote
either a 41-value `OneOf` or `Any.Int32().Between(0, 80).As(k => k * 0.25m)`, the projection ADR-0015 exists to
replace: the declared range no longer means what it says, and the value leaves the constraint algebra. ADR-0015 kept a
general step for "if real usage shows" it is needed; issue #359 is that usage.

A general step differs from a scale in one way that matters. Every value the type holds at a magnitude too large for
`n` places has fewer places, so it is still on `WithScale(n)`'s grid: that grid reaches the whole domain. A step
`p × 10⁻ⁿ` with `p` other than one does not: near `decimal.MaxValue`, `k × 0.25` needs more significant digits than
the 96-bit mantissa carries, and the type rounds it — off the grid. The multiples of `0.25` that the type holds
exactly stop at about `decimal.MaxValue × 10⁻²`.

## Decision

`Any.Decimal().MultipleOf(step)` restricts the value to the multiples of a strictly positive step, drawn by
construction as a whole number of steps, never rounded or filtered onto them.

* It is declared once, like the integer `MultipleOf`: a second, different step conflicts with the first, and the same
  step again is a repetition.
* It holds beside `WithScale(n)` when the step lies on that scale's grid — `0.25` on `WithScale(2)`'s — and the grid
  drawn from is then the step's. A step off the scale's grid conflicts, naming both, in either order:
  `MultipleOf(0.25m).WithScale(1)`.
* The grid ends where its points stop being exact. A step written in `n` places reaches values up to about
  `decimal.MaxValue × 10⁻ⁿ`; a declared interval lying wholly past that is refused naming the step, and a draw with an
  open side — the one in ten [ADR-0117](0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.md) spreads over the
  declared domain included — is spread over the part the grid reaches. A step that is itself `10⁻ⁿ` is
  `WithScale(n)`'s grid and reaches the whole domain.

This amends ADR-0015 on `decimal` only. Binary floating point keeps no step, for the reason ADR-0015 gave.

## Rationale

**By construction, as ADR-0015 decided for every lattice.** A point is `k × step` with `k` a whole number, and both
the multiplication and the division that finds `k` stay inside the range where the type is exact, so a drawn value is
on the grid because of how it was built, not because a check let it through. No redraw, no filter.

**The scale and the step agree or conflict; they do not intersect.** The multiples of `0.25` that have one place are the
multiples of `0.5`: an intersection exists, and computing it is the least-common-multiple combination ADR-0015
rejected as disproportionate. When the step lies on the scale's grid, every point of the step's grid satisfies both,
so keeping the step's grid is exact and needs no arithmetic. When it does not, the caller wrote two grids that do not
describe the same values, and naming both says so.

**End the grid rather than round onto it.** Past the exact range, a multiple of the step is either not a value the
type holds or one it holds only by rounding off the grid. Reaching further would need either an exact
arbitrary-precision search for the nearest representable multiple — a mechanism nobody could reason about from the call
site — or a value that violates the constraint it was drawn under. [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md)
asks for the bound, stated: every drawn value is a multiple of the step, and the region where the library will not go
is named. It lies past `10²⁶` for a step of two places, so a test that meets it declares a range there on purpose.

**A power of ten is the scale's grid.** The multiples of `10⁻ⁿ` are exactly the values of `WithScale(n)`, whose grid
reaches the whole domain. Drawing `MultipleOf(0.01m)` from it keeps two spellings of one grid from behaving
differently at the top of the range.

## Alternatives Considered

### Keep `WithScale` the only decimal lattice

Considered because it needs nothing new. Rejected because the field report shows the grid people need is a step, and
the workarounds it leaves — a long `OneOf`, or the projection ADR-0015 replaced — are what that decision exists to
retire.

### Draw over the whole domain and round onto the step

Considered because it would reach `decimal.MaxValue` as `WithScale` does. Rejected because past the exact range the
rounded value is off the grid: a value that violates the declared constraint, which the library never returns.

### Intersect a step and a scale that disagree

Considered because the intersection of two grids is often non-empty. Rejected for the reason ADR-0015 gave against
combining lattices: a least-common-multiple corner the demand does not show, where "the step must lie on the scale's
grid" is a rule a reader checks in their head.

## Consequences

### Positive

* A tick size, a half-point grade or any regular decimal grid is one call, drawn on the grid with the declared range
  keeping its meaning, exclusions, value sets and the cardinality hint all applying.
* A step and a scale that disagree fail at the arrange line, naming both, and JD034 reports them at build time when
  both arguments are constants.

### Negative

* A step other than a power of ten does not reach the top of the decimal domain: values past about
  `decimal.MaxValue × 10⁻ⁿ` are never drawn, and an interval wholly past it is refused although the type holds some
  multiples there.
* `decimal` now carries two lattices, and their agreement rule is one more thing to learn.

### Risks

* A reader may expect `MultipleOf(0.25m).WithScale(1)` to yield the multiples of `0.5`. Mitigation: the refusal names
  both constraints and says the step is not a multiple of `0.1`, and the numbers page shows the composing case.

## Follow-up Actions

* None. A least-common-multiple combination of a step and a scale is revisited only if a field report asks for it.

## References

* Issue [#359](https://github.com/Reefact/just-dummies/issues/359) — the decision this record carries.
* [ADR-0015](0015-draw-lattice-constrained-scalars-on-the-grid.md) — the lattices this record amends for `decimal`.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — bound the ambition, never the correctness.
* [ADR-0117](0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.md) — the draw with an open side that reaches the
  declared domain.
* The numbers page — [`numbers.en.md`](../../for-users/generators/numbers.en.md).
