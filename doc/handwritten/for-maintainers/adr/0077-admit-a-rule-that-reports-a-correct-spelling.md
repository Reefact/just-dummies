# ADR-0077 | Admit a JD rule that reports a correct spelling, bounded by an exact named equivalent

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0077-admit-a-rule-that-reports-a-correct-spelling.fr.md)

**Status:** Superseded by [ADR-0080](0080-admit-a-rule-that-names-a-resolved-ambiguity.md)
**Proposed:** 2026-08-20
**Accepted:** 2026-08-20
**Decision Makers:** Reefact

## Context

The `JustDummies` package ships thirty Roslyn rules inside itself (ADR-0023), and the ground they were
admitted on is that the type system cannot reach where the mistake lives (ADR-0038): a recipe and a drawn
value satisfy the same signatures, a seed pinned outside its scope still compiles, a constraint set that
admits no value is a well-typed chain. The rules' own README states the severity taxonomy that follows —
errors are hard defects, warnings flag likely mistakes, the information rules are conventions.

Every rule shipped so far reports something the author probably did not mean, including the three
information rules in the `Constraints` category. JD024 reports a constraint that narrows nothing, JD029 a
value written into a pool that no draw can yield, JD030 a string chain that declares no length and
therefore draws across the whole default spread. JD030 sits closest to the boundary: what it reports is
legal, deliberate on some call sites, and true — the chain does draw that spread — and it is reported
because the author is unlikely to know it.

The library ships pairs of spellings that are equivalent **by construction**, not by coincidence.
`AnyString.WithLengthBetween` is implemented as the two bounds it replaces, and its documentation says the
two forms behave identically, which is what keeps the range decomposable. The same pairing exists for the
collection generators, and nineteen `Between` overloads cover the numeric, `TimeSpan` and temporal ones.
Alongside them the library ships exact aliases of a single bound: `NonEmpty` is a minimum length of one,
`Positive` a minimum of one, `Zero` a pair of bounds.

The decomposed spelling is legal, documented and deliberately decomposable — a shared helper sets a floor
and a call site adds a ceiling — so the two forms must both stay available. Where they differ is
elsewhere: the range form records one constraint call shared by both bounds, so a conflict raised later
names the range, while the decomposed form records two and names whichever bound it collided with.

Issue #95 reports what that leaves open. A reader who writes the bounds separately never learns the range
form exists; discovering `WithLengthBetween` at all took a documentation read. It proposes a rule for it,
and its own reasoning shows where such a rule stops being sound: `GreaterThan(5).LessThan(10)` on an
integral type is the range six to nine and not five to ten, and on a floating-point type it has no range
form at all, so a rule that reported it would rewrite the numbers the author wrote or propose a constraint
that does not exist.

ADR-0046 is the rule this base already shares for questions of this shape: bound what the library
attempts, and refuse at the boundary rather than reaching for a more capable mechanism.

## Decision

A rule that reports a correct and deliberate spelling is admitted into the JD set at information severity
when, and only when, the library itself names a shorter form that is exactly equivalent by construction and
reachable without arithmetic on the author's arguments.

## Rationale

**The ground ADR-0038 states does not carry this rule, and the set's real common property does.** Nothing
at such a call site is wrong, so "the type system cannot reach it" is not the argument — there is nothing
for a type system to have caught. What the thirty rules actually share is narrower than defect-finding and
wider than that phrasing: each carries to the call site a fact the author is unlikely to hold. JD030 is the
precedent already accepted, and it reports something legal and true for exactly that reason. A shorter
equivalent the author does not know exists is the same kind of fact, and admitting it is a qualification of
ADR-0038's ground rather than a departure from it.

**Information severity is what keeps the analyzers from contradicting the API documentation.** The
decomposed form is not merely tolerated, it is blessed in prose and its decomposability is a property the
library maintains on purpose. A warning would tell the reader that the documentation is wrong. Information
says what the rules' own README says information means: a convention, a fact to weigh, never a verdict.

**Exactness by construction is what stops the rule from becoming a taste engine.** A criterion that
admitted "close enough" equivalences would put the analyzers in the business of preferring one correct
program to another, and the boundary between the two would be argued case by case forever. Requiring that
the shorter form be implemented as the longer one makes the equivalence checkable rather than debatable,
and it is the same move ADR-0046 makes everywhere else in this base: bound the surface, and let the
boundary be a thing one can point at.

**Requiring that the library named the form makes this discoverability rather than style.** The name
already exists and was already chosen; the rule only carries it to the place where it would have been
useful. A rule proposing a form the library does not ship would be a design proposal wearing an analyzer's
clothes.

**Forbidding arithmetic on the arguments is what keeps the rule sound.** A suggestion that changes the
numbers is a different constraint wearing the same name, and on a type with no next value there is no
suggestion to make at all. This is the condition that decides the strict bounds, and it decides them the
same way for every type instead of type by type.

**The shorter form is not only shorter, which is what makes this worth a rule rather than a style guide.**
Because the range form records a single constraint call, a conflict raised later against it names the range
the author wrote rather than one of its halves. The rule therefore points at a spelling with an observable
consequence, which is the property every other information rule in this category has.

**Writing the criterion down is the decision; the first rule is only its instance.** Deciding #95 alone
would leave the next candidate to whoever argues it best. Three conditions that can be checked against a
generator's source settle JD032 and everything after it without a second reading — which is the same reason
ADR-0046 exists rather than seven separate bounding decisions.

**The criterion deliberately admits more than the case that prompted it.** `Any.String().WithMinLength(1)`
has a shorter named form implemented as exactly that constraint, so it passes all three conditions. A
criterion narrow enough to admit precisely one rule would not be a criterion, and the cost of the extra
candidates is bounded: each still has to earn an issue, an id and two documentation pages.

## Alternatives Considered

### Close the gap in the XML documentation instead

The gap #95 reports is a documentation gap, and the remedy nearest to it is prose on the two bounds
pointing at the range form, which IntelliSense would surface while the author types.

Rejected because it reaches the wrong reader. Documentation on a member is read by someone hesitating over
that member; an author writing the second bound has already decided what to write and is not hesitating.
The population this rule exists for is precisely the one that never opens the tooltip — which is why #95's
own author found the method by reading the documentation as a document, not as a tooltip.

### Ship a Roslyn refactoring rather than a diagnostic

"There is a shorter equivalent spelling" is refactoring-shaped: a light bulb offers the rewrite, nothing is
squiggled, and no build output changes. It would also cost nothing in diagnostic ids or catalogue surface.

Rejected because a refactoring is only ever discovered by someone who puts the cursor on the code and asks
what could be done to it, and nobody asks that about code they are happy with. It is a good instrument for
performing a rewrite and a useless one for announcing that a rewrite exists, and announcing is the whole
point here. The follow-up #95 already describes — a code fix beside the rule — is the right home for the
performing half.

### Report at warning severity

Consistent with the rest of the `Constraints` category by count, and it would make the suggestion harder to
ignore.

Rejected: the decomposed form is documented as correct and its decomposability is maintained on purpose, so
a warning would set the analyzers against the API documentation. The taxonomy the rules' README states puts
warnings on likely mistakes, and this is not one.

### Keep the set to defects only and decline the rule

The simplest boundary available, requiring no criterion at all and no argument about where style begins.

Rejected because the set does not currently sit on that boundary. JD030 already reports something legal,
true and deliberate on some call sites, and declining here would leave it as an anomaly no rule explains.
The choice is not whether the set reports facts as well as defects — it already does — but whether the
condition under which it may is written down or improvised.

## Consequences

### Positive

* The next candidate is decided by reading three conditions against a generator's source rather than by
  relitigating the boundary.
* The decomposed spelling stays legal, documented and decomposable; nothing about the generators changes.
* The discoverability gap closes at the call site, where the author can act on it.
* The conditions exclude the unsound cases — strict bounds, mixed pairs, floating-point types — by
  construction rather than case by case.

### Negative

* The set now holds rules of two kinds, and the framing sentence in the rules' README has to say so.
* Every admitted rule costs a diagnostic id, an English and a French page, a row in each rule table, a
  catalogue constant published on its own release train (ADR-0052), and an update to every count of the
  rules.
* An author who wrote the decomposed form on purpose sees an information diagnostic saying nothing is
  wrong, which is a small tax paid on every such call site.

### Risks

* The criterion admits more candidates than anyone has enumerated, and a wave of alias rules would be noise
  even at information severity. The mitigation is that each still goes through an issue, where the question
  is worth against cost rather than admissibility.
* A future reader may take "correct spelling" as licence for style rules generally. The three conditions
  are the answer, and they are the reason this record states a criterion rather than a verdict.

## Follow-up Actions

* Implement JD031 under this criterion (#95).
* State the criterion on the JD031 pages, so a reader who hits a silent case understands why it is silent.
* Revisit the framing sentence in the analyzers README, which currently says every rule closes a gap the
  type system cannot reach.

## References

* [ADR-0023](0023-ship-justdummies-analyzers.md) — the rules ship inside the package.
* [ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.md) — the ground this record qualifies.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — bound the surface, and make the boundary nameable.
* [ADR-0052](0052-publish-the-jd-rules-as-a-first-party-catalogue.md) — the catalogue every new id is published through.
* [Issue #95](https://github.com/Reefact/just-dummies/issues/95) — the rule this criterion admits first.
