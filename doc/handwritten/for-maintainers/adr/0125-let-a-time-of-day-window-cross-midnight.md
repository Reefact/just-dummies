# ADR-0125 | Let a time-of-day window cross midnight

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0125-let-a-time-of-day-window-cross-midnight.fr.md)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

## Context

`Any.TimeOnly().Between(start, end)` refused a start after the end with an `ArgumentException`, as every ordered type
does: on a line, that order is a transposition. Eight domains of twenty in the first developer-UX trial — night
shifts, curfews, trading sessions, overnight windows — wrote `Between(new TimeOnly(22, 0), new TimeOnly(6, 0))` and met
that refusal. A time of day is not a line, though. It wraps at midnight, and `TimeOnly.IsBetween(22:00, 06:00)` already
reads that pair as the night. On a cyclic domain, a start after the end is the only way to name a window that crosses
midnight; the refusal turned a documented meaning into an error.

Issue #357 decided the behaviour, which this record carries because a documented refusal becomes a behaviour.

## Decision

On `TimeOnly`, `Between(start, end)` with the start after the end is the window from `start`, forward through
midnight, to `end`, both included.

* It is drawn uniformly over the window: from its two segments in proportion to their length.
* `Between(t, t)` stays the single value `t`, as on every other type. The whole day is `Any.TimeOnly()`.
* Once a window crosses midnight, every bound is read along its direction: `After` and `AfterOrEqualTo` move its
  start, `Before` and `BeforeOrEqualTo` its end, a further `Between` narrows both. The order the calls are written in
  does not matter: `After(23:00).Between(22:00, 06:00)` is `Between(22:00, 06:00).After(23:00)`.
* A time outside the window — `After(12:00)` on the night — admits no value. It is refused with a
  `ConflictingAnyConstraintException` naming both declarations, in either order.
* `Except`, `OneOf` and `WithGranularity` apply to both segments. A granularity that does not divide a day has no single
  grid across midnight — its grid restarts there — and is refused on such a window, naming both.
* A start after the end stays an argument error on every other temporal type, and a window that does not cross
  midnight behaves exactly as before: same draws, same messages.

## Rationale

**The type's own reading.** `TimeOnly.IsBetween` defines the pair this way, and the trial shows it is what people
write. Refusing it made the library disagree with the type it generates.

**One interval in a frame that starts the day at the window's start.** Measured from the window's start, the window
is one interval, so the engine that draws every other ordered type draws it uniformly by construction, and every
existing mechanism — exclusions, value sets, the grid, the cardinality hint, the conflict messages — applies unchanged.
The declarations made before the window are applied again in that frame, which is what makes their order irrelevant:
a bound means the same thing whether it was written before the window or after it. A generator whose windows never
cross midnight never leaves the ordinary frame, so its draws and seeds are untouched.

**Along the direction, or not at all.** A bound inside the window has one reading: it moves the start or the end. A
time outside it has none that is not a guess — `Before(12:00)` on the night could mean the whole night or nothing — so
it is refused rather than silently given one. Two windows crossing midnight whose intersection is two pieces are
refused for the same reason: the result is not a window.

**A grid that does not divide a day is refused, not bent.** A granularity of seven minutes on the night places
23:55 and then 00:00: a gap of five minutes at midnight. No single step describes that set, and drawing it would need
a second grid the caller never declared. [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) asks
for the refusal, stated; every granularity that divides a day, which is every round one, is accepted.

## Alternatives Considered

### Keep the refusal, and document an offset recipe

Considered because it changes nothing. Rejected because the pair is the type's own reading of a night, and a recipe
— draw an offset from the start, add it modulo a day — is what eight domains had to rediscover.

### Read `Between(t, t)` as the whole day

Considered because, on a cycle, a window from `t` round to `t` is the day. Rejected because a test that wrote
`Between(9:00, 9:00)` for nine o'clock would silently draw any time of day, and `Between` would mean on `TimeOnly`
what it means on no other type.

### Intersect bounds linearly, whatever the window

Considered because it needs no frame. Rejected because `Between(22:00, 06:00).After(23:00)` would lose the hours after
midnight, and the order of the calls would change what they mean.

### Draw two segments as two specifications

Considered because each segment is an ordinary interval. Rejected because every bound would still need reading along
the window, and the conflict messages, the cardinality hint and the pool inspection would each need a two-segment
version of what the single interval already provides.

## Consequences

### Positive

* A night, a curfew or an overnight session is one call, drawn uniformly, with every other constraint applying to it.
* The order of the declarations does not matter, and a time outside the window fails at the arrange line naming both.
* Windows that do not cross midnight are unchanged, draws included.

### Negative

* On `TimeOnly`, `Between` no longer catches a start and an end written the wrong way round: it draws the other part
  of the day. A test that meant 06:00 to 22:00 and wrote it backwards gets the night.
* A granularity that does not divide a day cannot be used on a window across midnight.

### Risks

* JD014 never read `TimeOnly` arguments, which are not compile-time constants, so it reports nothing either way; JD032
  does not yet fold a `Between` with a bound on the same side, and no rule yet reports a time outside the window at
  build time. Mitigation: those rules are tracked by their own issues (#355, #356); the run-time refusal names both
  declarations meanwhile.

## Follow-up Actions

* JD032 folding a `Between` with a bound on the same side, along the window (#355).
* A build-time report of a time outside a window across midnight, and of `Between(t, t)` (#356).

## References

* Issue [#357](https://github.com/Reefact/just-dummies/issues/357) — the decision this record carries.
* [ADR-0015](0015-draw-lattice-constrained-scalars-on-the-grid.md) — the granularity grid.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — refuse at the edge rather than guess.
* The dates and times page — [`dates-and-times.en.md`](../../for-users/generators/dates-and-times.en.md).
