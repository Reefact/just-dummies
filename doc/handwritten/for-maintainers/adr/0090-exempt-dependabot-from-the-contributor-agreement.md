# ADR-0090 | Exempt Dependabot from the contributor agreement, only at its own signed commit

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0090-exempt-dependabot-from-the-contributor-agreement.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-31
**Accepted:** 2026-08-31
**Decision Makers:** Reefact

## Context

JustDummies is owned by Sylvain Aurat in his personal capacity. Anyone other than the Project Owner
must accept the [Contributor Agreement](../../../../CONTRIBUTOR_AGREEMENT.md) before a contribution
is accepted; a pull request opened directly by the Project Owner does not need the acknowledgement,
and one produced through the Project Owner's Claude Code workflow keeps and checks it regardless. A
CI check enforces this by reading an acknowledgement checkbox in the pull request's body.

The agreement defines a Contribution as material the contributor **intentionally submits**. It asks
that contributor to represent that they are legally entitled to submit it and hold the rights needed
to enter into the agreement, and it takes an assignment of the transferable economic rights in what
they submitted.

Dependabot is a GitHub app, not a legal person. It opens a pull request because this repository's own
dependency configuration tells it which ecosystems to watch, and what it submits is a version number
answering an upstream release.

Dependabot does not fill this repository's pull request template, so the acknowledgement checkbox is
never present in the body it writes. The check fails on every Dependabot pull request. Both pull
requests opened since the model landed show it: on the first, the failure was cleared by editing the
body by hand; the second is open and failing on nothing else.

Patch and minor Dependabot updates are merged by a workflow once the required checks pass, with no
human acting on them.

Anyone with push access can append commits to a Dependabot branch. Opening a pull request does not
fix what that pull request later carries. The repository's two other Dependabot workflows already
settle identity against this: an author check **plus** GitHub's own signature on the branch tip
before arming an action, and a weaker signal to withdraw one.

The autofix workflow repairs a Dependabot pull request by amending or rebasing it, which keeps
Dependabot as the commit author and drops GitHub's signature from the tip.

## Decision

A pull request opened by Dependabot requires no Contributor Agreement acknowledgement while its head
is Dependabot's own GitHub-signed commit, and requires one like any other pull request at any other
head.

## Rationale

The gate collects an assignment of economic rights and a set of representations about the work.
Dependabot can give neither: it is not a legal person, and the version number it wrote at this
repository's own instruction carries no third party's rights for it to assign. Demanding the box on
its pull requests asks for a consent nobody is in a position to give, and obtains one only because a
human ticks it on the bot's behalf — a signature with no signatory behind it. Naming who the gate
never could bind takes nothing away from what it collects from those it can.

The cost of leaving it as it stands falls on the gate rather than on Dependabot. Every dependency
pull request arrives carrying a failing governance check that a human clears by editing the body. A
gate whose normal operating procedure is a manual bypass stops being read as a gate; the routine
bypass is the harm here, not the red check. It also breaks the lane it sits across, since patch and
minor updates are meant to land without a human and a check they can never pass puts one back in
front of every one of them.

Waiving an acknowledgement is the direction in which a mistake costs something, so it takes the
strong proof rather than the convenient one. An exemption keyed on who opened the pull request would
rest on a fact that says nothing about what the branch carries now: appending to a Dependabot branch
is open to anyone with push access, and what they append is a Contribution in the agreement's own
terms. Requiring that the head still be Dependabot's own signed commit ties the exemption to the work
rather than to the label on it — a commit author name is a local setting and forges freely, GitHub's
signature does not.

That same condition answers the Claude Code case without a second rule. A repaired Dependabot pull
request carries a change the Project Owner's model workflow wrote, and the model already holds that
such a change keeps the acknowledgement. The repair drops the signature, so the requirement returns
by construction rather than through an exception someone has to remember to write.

## Alternatives Considered

### Keep clearing the check by hand

It needs no change at all and leaves the gate exactly as written.

Rejected because it makes a manual bypass of a governance check the routine way dependency updates
land, which erodes the check faster than any exemption written down would, and because it stations a
human in the one lane whose whole value is that none is needed.

### Exempt every bot author

Reading the author account's type needs no list to keep up to date, and covers any future automation
in one stroke.

Rejected because it hands the waiver to every GitHub App ever installed on the repository, present or
future, on the strength of a type flag — a far wider concession than the case that prompted it, and
one that would widen again silently every time an app is added.

### Exempt on the author alone, without the signed head

It is simpler, needs no second read, and survives a repair by the autofix workflow.

Rejected because it waives the acknowledgement for whatever the branch carries rather than for what
Dependabot wrote. It is exactly the check the repository's two other Dependabot workflows already
found insufficient, for exactly the same reason.

### Make the check advisory on bot pull requests

The repository already chose to make its mutation gate advisory rather than remove it
([ADR-0025](0025-make-the-per-pull-request-mutation-gate-advisory.md)) when it began producing
failures that said nothing about the pull request.

Rejected because the two gates do different work. The mutation gate measures, and a measurement is
still worth reporting once it can no longer block. This one collects a consent, and an advisory
consent collects nothing: a check that reports "not accepted" and lets the merge happen anyway is
worse than one that does not run, because it looks like protection.

## Consequences

### Positive

* A dependency pull request goes green on its own, and the patch and minor lane closes without a
  human being stationed in it.
* The exemption opens no path for an appended contribution to ride in behind Dependabot's name: the
  head check withdraws it the moment the branch stops being Dependabot's own work.
* The exemption is stated in the agreement's own terms — who is in a position to make its
  representations — rather than as a convenience granted to CI.

### Negative

* A Dependabot pull request repaired by the autofix workflow asks for the acknowledgement, including
  after a trivial repair whose auto-merge that workflow deliberately keeps. The Project Owner then
  ticks the box or lands the pull request by hand.
* One additional API read per event on a Dependabot pull request.

### Risks

* The verdict describes the head the event reported. A later push raises its own event and is checked
  on its own terms, so the window is the ordinary one, but the check answers for what it read.
* Were GitHub to stop signing Dependabot's commits, every Dependabot pull request would ask for the
  box again. Noisy, never unsafe: the failure falls in the direction that asks for consent.

## Follow-up Actions

* If the acknowledgement on a trivially repaired Dependabot pull request turns into routine friction,
  decide whether the autofix workflow should preserve a verifiable signature, or whether the trivial
  class of repair deserves an exemption of its own — as a decision, not as a patch.

## References

* `.github/workflows/contributor-agreement.yml` — the gate and this exemption.
* `.github/workflows/dependabot-automerge.yml` and `.github/workflows/dependabot-autofix.yml` — the
  identity checks this exemption mirrors.
* [`CONTRIBUTOR_AGREEMENT.md`](../../../../CONTRIBUTOR_AGREEMENT.md) — §1 "Contribution" and
  §2 "Ownership and authority".
* [ADR-0025](0025-make-the-per-pull-request-mutation-gate-advisory.md) — the advisory gate this one is
  deliberately not.
