# ADR-0055 | Hold the user documentation to contracts the build checks

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0055-hold-the-user-documentation-to-contracts-the-build-checks.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-09
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

## Context

The repository now publishes a user documentation set: twenty pages, each with a French twin, under
`doc/handwritten/for-users/`, plus the root `README` pair. Together they carry well over a hundred C#
samples.

**Nothing in the build reads Markdown.** Before this decision, the only mechanism that could detect a
wrong sample was a reader noticing — and the reader who meets a wrong sample first is the newcomer
following the getting-started guide, who has no way to tell a documentation defect from a library
defect and concludes the library is broken.

The risk is not theoretical, for three reasons that are facts about this repository rather than
general observations about documentation:

* **The public surface is not frozen.** It is declared in `PublicAPI.Unshipped.txt`, and the library
  is at `1.0.0-preview`. A renamed constraint, a factory that changes its return type, a method that
  moves to a different builder — each breaks every sample naming it, and none of them breaks a build.
* **The product ships 28 analyzer rules, and its own code is held to them.** `JustDummies.UnitTests`
  loads the analyzers so a rule that misfires is met inside the repository before a consumer meets it
  ([ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.md)). The documentation was
  outside that loop, while being the place where samples are copied from.
* **French twins are required and enforced by nothing.** `CLAUDE.md` states the rule — change a page,
  change its twin — and no build step has ever checked it.

**Three defects were measured while the pages were being written**, all of which survived being read
back:

1. a sample chaining a letters-only alphabet with a suffix containing `.`, a chain the library refuses
   at declaration;
2. a claim that `Any.OneOf` mis-binds an array, which is false — `params` expansion makes that case
   correct, and it is a held `List` that does not;
3. two `[SuppressMessage]` samples written outside any type.

The tooling cost is bounded by what the repository already carries. `Microsoft.CodeAnalysis.CSharp`
is already a centrally-managed package version, and `JustDummies.Analyzers.UnitTests` already compiles
snippets in-process and runs analyzers over them.

Two properties of the corpus bear on scope. The 28 analyzer rule pages predate this documentation set
and are written to a different brief: they show `Noncompliant` code on purpose, and their samples name
symbols that exist only in the reader's imagination. And the maintainer documentation carries naming
conventions of its own — the ADR base gives its English pages no language suffix — which the
translation contract would have to accommodate before it could apply there.

## Decision

Every C# sample in the user documentation is compiled against the shipped packages and inspected by
the shipped analyzers, and every page is held to structural parity with its French twin and to links
that resolve, by a test suite whose failure is a build failure.

## Rationale

The decision converts the one class of defect that reading does not catch into the one signal this
repository already responds to.

**Compilation is the check that matches the failure mode.** A sample that names an API which no longer
exists is not a stylistic problem a careful reviewer would spot; it is a fact about the compiler, and
asking the compiler is both cheaper and more reliable than asking a human to hold the whole public
surface in mind. Against a surface that is explicitly not frozen, that check has to be mechanical or
it will not happen.

**Compiling against the packages, and nothing else, is what makes the guarantee transferable.** The
samples bind against `JustDummies`, its adapter and its catalogue as a consumer references them. A
sample that compiles in the suite therefore compiles in a reader's test project, which is the only
promise a code sample actually makes.

**Running the shipped rules over the samples closes a credibility gap that would otherwise be
structural.** A library that publishes 28 rules and teaches readers to break them is arguing against
itself, and samples are the part of documentation most likely to be copied verbatim. Since the rules
already run over the repository's own code, exempting the documentation would leave the most-copied
code the least-checked.

**Anti-patterns must stay expressible, so the contract admits them by declaration rather than by
exception.** A page that shows only correct code cannot teach a reader to recognise the mistake. A
sample therefore declares which rules it means to trip, which keeps the intent visible in the page's
source; and an allowance that stops firing fails too, because a page saying "this is what a discarded
generator looks like" beside code that no longer discards one has quietly stopped being an example.

**Structural parity is checked because it is the half that can be.** No test distinguishes a faithful
translation from a plausible one, and pretending otherwise would buy false confidence. What can be
compared is the skeleton — headings, code blocks, markers — and that is precisely the half that goes
missing: a section added in English and forgotten in French leaves a French reader with documentation
that is not wrong, only incomplete, which is the failure no reviewer notices.

**The measured defects settle the cost-benefit.** Three real errors in one authoring pass, each of
which read as correct prose, is evidence that review does not catch this class. The suite caught all
three before publication, at the cost of a harness the repository already had the parts for.

**Scoping out the analyzer rule pages keeps this decision about the contract rather than about them.**
Holding them to the compile contract would mean rewriting fifty-six pages to invent the symbols their
samples name — a change to the analyzer documentation, argued on its own merits, not a consequence of
deciding how the user documentation is checked. They are held to the translation and link contracts,
which they already satisfy.

## Alternatives Considered

### Leave the samples to review

Cheapest, and it is the status quo everywhere else in the industry.

Rejected because it is exactly what was measured to fail: three defects survived review during the
very pass that wrote the pages, by the author who had the API open. Review is a poor detector for
"this identifier no longer exists", and it degrades further as the surface moves.

### Extract every sample into a compiled sample project, and include the files into the pages

The samples would compile by construction, and an IDE would refactor them along with the API.

Rejected because it inverts the authoring flow and moves the text away from the prose that explains
it: a page becomes a sequence of includes, and the sentence introducing a sample is written against a
file the author is not looking at. It also solves a narrower problem than it costs — the reader reads
the page, so the page is where the code must be right — and it does nothing for the rules, the
translation parity or the links.

### Compile everything, including the 28 analyzer rule pages

Uniform, with no scope for a reader to misunderstand.

Rejected for now because those pages deliberately show noncompliant code naming imagined symbols, so
the contract could only be met by rewriting them. That is a decision about the analyzer documentation
and deserves its own argument; folding it in here would have made this ADR carry a change nobody had
asked for.

### Check the translation's meaning, not only its structure

The strongest form of the parity guarantee.

Rejected because it is not achievable: no mechanical check tells a good translation from a plausible
one, and a check that appeared to do so would license less careful review rather than more. Claiming
only the skeleton is what keeps the contract honest about what it verifies.

## Consequences

### Positive

* A sample that binds in the suite binds in a consumer's test project; the promise a code sample makes
  is the promise it keeps.
* The documentation cannot silently drift from the API, which is what makes it safe to keep a
  documentation set this size against an unfrozen surface.
* The most-copied code in the repository is now held to the rules the product ships.
* An anti-pattern is declared rather than incidental, and cannot decay into a stale example.
* A French twin cannot lose a section, a code block or an opt-out without the build saying so.

### Negative

* A documentation change can break the build. That is the mechanism working, and it is still a cost:
  a page is no longer a file anyone can edit without running the suite.
* Samples must be written to a contract — the declared modes, the shared illustrative domain, no
  import directives inside a sample — which is a constraint on every future page.
* A sample that is valid C# but that the harness cannot wrap needs an explicit opt-out, so a reader of
  the page's source meets an escape hatch where no defect exists.

### Risks

* **The escape hatch becomes the habit.** If opting out is easier than writing a bindable sample, the
  contract hollows out. Mitigated by a ceiling on how many samples may opt out, which fails the suite
  rather than warning.
* **The illustrative domain grows into a second product.** Fixtures that demonstrate patterns of their
  own would compete with the pages for the reader's attention. Mitigated by keeping them deliberately
  ordinary.
* **The scope is read as broader than it is.** "The documentation is checked" is not true of the
  analyzer rule pages' samples, and a future maintainer who assumes otherwise would trust a guarantee
  that is not there.

## Follow-up Actions

* Consider extending the compile contract to the 28 analyzer rule pages, which needs a convention for
  the symbols their samples name.
* Consider extending the translation and link contracts to the maintainer documentation, which first
  needs the ADR base's English-page naming settled.

## References

* [ADR-0019](0019-split-the-justdummies-test-bed-between-example-and-property-suites.md) — which suite
  a new test belongs to.
* [ADR-0034](0034-enforce-the-style-rules-the-compiler-can-express.md) — the precedent for moving a
  rule from attention to the build.
* [ADR-0035](0035-state-the-coding-rules-where-an-agent-can-act-on-them.md) — why a rule nothing
  checks drifts.
* [ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.md) — the analyzers this
  contract runs over the samples.
* Pull request [#40](https://github.com/Reefact/just-dummies/pull/40) — the documentation set and the
  suite this decision records.
