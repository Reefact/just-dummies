# ADR-0115 | Allow an editorial correction to an accepted ADR, never a change of substance

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0115-allow-an-editorial-correction-to-an-accepted-adr.fr.md)

**Status:** Accepted
**Proposed:** 2026-10-29
**Accepted:** 2026-10-30
**Decision Makers:** Reefact

## Context

The decision base treats an accepted record as a piece of history rather than a draft. Its README says that a
superseded record's body is never edited, and that a citation of a superseded record is not repinned unless what
it claims stopped being true. It names no exception: read literally, fixing a typo or a dead link in an accepted
record is forbidden too.

A record goes on being read long after it is accepted, and parts of it that carry no decision decay: a link whose
target moved, a typo, a French twin that drifted from the English, a citation of a record that has since been
superseded. ADR-0080 records the cost of the last one as a negative consequence: "a reader following a link from
JD031 lands on a record whose successor holds the operative text … the detour is real."

The same question reaches the user documentation. Issue #329 found a user page citing ADR-0029 for a rule ADR-0076
had since replaced. Five user pages cite superseded records, for claims that are still true, and repinning them
would contradict the README's rule; leaving them sends a user, who reads for the behaviour in force, through the
detour ADR-0080 regrets.

What a record says is fixed by the date it was accepted: its context describes the world at that date, and its
decision and rationale answer that world.

## Decision

An accepted record may receive an editorial correction — one after which the record, read at the date it was
accepted, says exactly what it said before — and never a change of substance, which goes through a new record.

## Rationale

**The criterion protects what history is for.** A record is kept to explain why a decision was right when it was
taken. A typo fixed, a moved link repaired or a French sentence realigned on the canonical English leaves that
explanation intact; a sentence of context made "more accurate today" rewrites it. Asking whether the record says
the same thing at its own date separates the two without a list to maintain.

**A citation is kept and annotated, never replaced.** Replacing a citation of a superseded record with its
successor would put into the context a record that did not exist at that date, which changes what the context
says. Writing the successor beside it adds a pointer and changes nothing the record claims, so the detour ADR-0080
names disappears without touching history.

**User pages follow the same rule for the same reason.** A user page that cites a superseded record keeps the
citation, which may carry a part of the claim the successor does not restate, and names the successor on the same
line, so a user reaches the operative text in one step. The pairing is mechanical, so the documentation suite can
hold it.

**The git history keeps every edit visible.** An editorial correction is a commit like any other, so nothing about
a record changes unseen.

## Alternatives Considered

### Keep the record untouched, without exception

Considered because it is the current rule and the simplest to state. Rejected because it forbids repairs that
change nothing a record says, and leaves the detour ADR-0080 regrets in place for every reader.

### Repin a citation to the successor

Considered because it sends the reader straight to the operative text. Rejected because a successor may restate
only part of what the original decided, and because in a record's context it would describe a world that did not
yet exist.

### List the permitted edits

Considered because a list is easy to apply. Rejected as the rule itself: a list ages and invites arguing a new case
into it. The criterion decides; the README gives examples of it.

## Consequences

### Positive

* A dead link, a typo or a drifted twin in an accepted record can be fixed without a new record.
* A reader of a user page, or of a record citing a superseded one, sees the record in force in one step.

### Negative

* "Says the same thing" is a judgement, made by whoever edits and checked at review.

### Risks

* An edit presented as editorial may shift a meaning. Mitigation: the criterion is stated with the rule, the commit
  shows the edit, and the maintainer reviews every change to the decision base.

## Follow-up Actions

* State the criterion and the citation rule in the decision base's README (EN and FR).
* Name the successor beside every citation of a superseded record in the user documentation, and hold that in the
  documentation suite.
* Annotating citations of superseded records inside other records is now permitted; it is left to a later change.

## References

* [ADR-0036](0036-keep-one-dated-line-per-state-an-adr-reached.md) — one dated line per state, never overwritten.
* [ADR-0080](0080-admit-a-rule-that-names-a-resolved-ambiguity.md) — the detour a superseded citation costs.
* Issue [#329](https://github.com/Reefact/just-dummies/issues/329).
