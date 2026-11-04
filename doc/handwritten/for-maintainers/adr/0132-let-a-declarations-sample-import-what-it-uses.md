# ADR-0132 | Let a declarations sample import what it uses

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0132-let-a-declarations-sample-import-what-it-uses.fr.md)

**Status:** Accepted
**Proposed:** 2026-11-04
**Accepted:** 2026-11-04
**Decision Makers:** Reefact

## Context

[ADR-0055](0055-hold-the-user-documentation-to-contracts-the-build-checks.md) holds every C# sample of the user
documentation to a compile contract the build checks. Its consequences record the contract a sample is written to:
the declared modes, the shared illustrative domain, and no import directives inside a sample. The harness kept that
last term by compiling every sample under the same imports, `JustDummies` and xUnit among them, and inside a namespace
under `JustDummies`.

A sample comes in two modes. A statements sample is a fragment of a method body, which C# gives nowhere to write an
import. A declarations sample is whole types, which is what a reader copies into a file of their own.

The third developer-UX trial met the consequence of the injected imports in five domains (DOCS-11): a reader who
copied a declarations sample into a test file met `CS0103` on `Any`, or a missing `[Fact]`, because the page compiled
with imports the reader's file did not have. The harness hid the gap from the build: under a namespace inside
`JustDummies`, the library's types resolved through the enclosing namespace even without an import. The first
recommendation of the same trial, removing the injection, had already been carried out for the xUnit adapter's page
alone.

## Decision

A declarations sample states the imports it uses, and the harness compiles it with only what a test project supplies
unwritten — the SDK's implicit imports and the shared illustrative domain — in a namespace outside `JustDummies`; a
statements sample keeps the shared imports.

## Rationale

**A sample a reader copies has to compile where it is copied.** ADR-0055 exists so that the documentation cannot
drift from what a reader can run. An import the harness supplies and the reader's file does not is exactly such a
drift, and the build reported every such sample as correct.

**The term of the contract was the cause, not a style choice.** "No import directives inside a sample" was kept by
supplying the imports, so it could only ever hold by hiding the one thing a reader needs to write first. Reversing it
for declarations samples makes the page show that line instead of assuming it.

**Statements samples keep the old term because nothing else is possible.** A method body has nowhere to write an
import; there, the shared imports are the only way to compile at all, and the reader copies a fragment into a method
that already has its file's imports.

**What a test project supplies unwritten is the honest baseline.** The SDK's implicit imports are in every reader's
project without a line of theirs, and the illustrative domain is the one thing ADR-0055 lets a page assume. Anything
beyond that is a line the reader must write, so the sample writes it too.

## Alternatives Considered

### Keep the term and document the imports in prose

Considered because it leaves the samples and the harness untouched. Rejected because the build would still not see a
missing import, which is how DOCS-11 went unnoticed; a sentence cannot be held to a contract the way a sample can.

### Remove the shared imports from every sample

Considered for a single rule. Rejected because a statements sample cannot carry an import, so it would stop
compiling at all, and the compile contract of ADR-0055 is the point.

## Consequences

### Positive

* A declarations sample is copyable as it stands: the page shows the imports the reader's file needs.
* A sample that forgets an import fails the build, and a contract test keeps the harness from supplying it again.

### Negative

* A declarations sample is longer by its import lines, and every new one has to state them.
* The contract of ADR-0055 now differs by mode, which a writer has to know.

### Risks

* A reader of ADR-0055 alone still finds "no import directives inside a sample". Mitigation: this record amends it
  and is listed beside it in the index.

## Follow-up Actions

* None: the harness change, the imports of the existing samples and the contract test landed with this decision
  (#516).

## References

* [ADR-0055](0055-hold-the-user-documentation-to-contracts-the-build-checks.md) — the decision this amends. It keeps
  its status; only the import term of its sample contract changes, for declarations samples.
* [ADR-0113](0113-check-a-distinct-collections-cardinality-when-the-specification-is-whole.md) — a worked amendment
  that leaves the amended record in place.
* [UX trial 3, DOCS-11](../../../../tools/ux-trial/REPORT-3.md#docs-11).
* Issue [#443](https://github.com/Reefact/just-dummies/issues/443), pull request
  [#516](https://github.com/Reefact/just-dummies/pull/516).
