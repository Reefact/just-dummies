# ADR-0052 | Publish the JD rules as a first-party catalogue, and read the descriptors from it

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0052-publish-the-jd-rules-as-a-first-party-catalogue.fr.md)

**Status:** Proposed
**Proposed:** 2026-08-02
**Decision Makers:** Reefact

## Context

[ADR-0050](0050-name-a-suppressed-rule-through-a-catalogue-constant.md) converted every suppression in
this repository to a catalogue constant — except seven. Those name `JD` rules, and no catalogue
described them, so they stayed literals. Whoever consumes JustDummies is in the same position, worse:
they suppress rules **this product publishes**, with strings nothing verifies, and they have no way to
do otherwise.

The exposure is not hypothetical. `JD001`–`JD028` are a public contract — the repository already treats
a renamed diagnostic id as a breaking change — and every one of them is reachable from a consumer's
`[SuppressMessage]`.

Inside the product the same strings are transcribed twice already. `DiagnosticIds` holds the id,
`DiagnosticCategories` holds the category, and `Descriptors` assembles them into the
`DiagnosticDescriptor` the analyzer reports with. A consumer's suppression is a third transcription,
in their assembly, and nothing on the platform compares any of the three.

`DiagnosticCatalog` supports exactly this case, and distinguishes it from mirroring somebody else's
analyzer: when the same project owns the analyzer **and** the catalogue, the descriptor can read the
catalogue's constants, and the two stop being independent copies of one string.

## Decision

The `JD` rules are published as `JustDummies.DiagnosticCatalog`, on its own release train, and
`JustDummies.Analyzers` reads its descriptors' id, category, title and help link from that catalogue.

## Rationale

**A consumer's suppression is the case the product cannot check any other way.** The seven literals
here could have been left alone. The ones in a consumer's codebase could not: they silence rules
JustDummies ships, and when a rule is retired or recategorised their attribute keeps compiling and
silences nothing. Publishing the catalogue is the only way that failure becomes visible to the person
it happens to.

**The loop is what a first-party catalogue is for.** With the descriptor reading the catalogue, the
rule the analyzer *reports* and the rule a consumer *silences* are the same value by construction. The
category especially: it is a string only this product publishes, nothing verifies it, and "by
diligence" is precisely what fails.

**The direction is decided by which artifact is written by hand.** These declarations are
hand-written, so the descriptor is fed from them. The reverse — generating the catalogue from the
descriptors — is what a repository generating its catalogues must do instead, and it needs a
regeneration check to replace the loop. Feeding is available here, and is stronger: nothing to run,
nothing to check, the compiler enforces it.

**It costs the shipped analyzer nothing.** The members are `const`, so the compiler substitutes their
values and the built analyzer carries no reference to resolve at load time — which matters, because it
is loaded from inside the library package by each consumer's compiler, on a pinned Roslyn floor
(ADR-0001).

**Its own train, because it versions on something else.** The catalogue moves when the rule set moves —
a rule added, retired, recategorised. That is not when the library moves, nor when the adapter does.
Tying it to either would mean cutting a content-free release of one to publish the other, which is the
coupling [ADR-0047](0047-declare-the-adapters-library-dependency-independently.md) removed.

## Alternatives Considered

### Leave the `JD` suppressions as literals

The status quo, and the smallest change. Rejected: it accepts for this product's own rules exactly the
silent failure ADR-0050 rejected for everyone else's, and it leaves consumers with no option at all.

### Publish the catalogue from `Reefact/diagnostic-catalog`, beside the other thirteen

It would inherit that repository's generator and publishing chain. Rejected: it would have to read the
descriptors from a **published** `JustDummies.Analyzers`, so the catalogue could never be generated
before the release it describes and would always trail it by a version — a catalogue describing rules
its users may not have, which is the defect this record exists to remove.

### Generate the catalogue from the descriptors, in this repository

The direction the foundation's own repository uses. Rejected here because the loop is available: the
declarations are hand-written, so the descriptor can read them and no regeneration check is needed.
Generating would also put the rule text behind a tool for a rule set that changes a few times a year.

### Ship `Id` and `Category` only

Enough for a suppression. Rejected: `Title` is what a consumer's IDE shows when hovering the constant,
which is where the rule's prose goes once the suppression stops carrying it, and `HelpLinkUri`
composes from the id at compile time for nothing. `MessageFormat` and `Description` are deliberately
**not** published: the message format's placeholders are coupled to the analyzer's own call sites, and
neither is anything a suppression names.

## Consequences

### Positive

* A consumer's suppression of a `JD` rule is compile-checked, and the DCAT analyzers report and fix
  the literal form.
* The id, the category and the title exist once for the whole product, so the analyzer and a
  suppression cannot disagree.
* The last seven literal suppressions in this repository are converted: there are now none.

### Negative

* A fourth package to release, with its own changelog, train and nuget.org policy.
* The first package here to declare a **real** dependency. `JustDummies` and `JustDummies.Xunit` keep
  `PrivateAssets="all"` on the catalogues they consume; this one must not, because a package that
  publishes a catalogue has to let the foundation reach its consumers — it carries the markers the
  declarations wear and the analyzers that check a consumer's suppressions. ADR-0003 is unaffected: it
  is about the library, and the library's `.nuspec` still declares nothing.
* `JustDummies.DiagnosticCatalog` is the one project here that cannot run the JustDummies analyzers on
  itself: they read their descriptors from it, so referencing them back is a build cycle. It declares
  constants and no executable statement, so there is nothing for those rules to fire on.

### Risks

* A rule added to the analyzer and not to the catalogue is invisible to a compiler. That is why
  `CatalogueCoverageTests` compares the two by reflection, in both directions, from the shipped
  artifacts rather than from a list — a list would be a fourth transcription needing the same guard.
* The catalogue's stability rules are stricter than an ordinary package's: a member is `const`, so it
  is inlined into a consumer's assembly at *their* compile time, and deleting one breaks their build
  with a message naming nothing they wrote. A rule withdrawn from the product is carried forward as
  `[Obsolete]`, never deleted. Nothing enforces that; the changelog and this record are where it is
  written down.

## Follow-up Actions

* Decide the package icon. The foundation's convention badges a catalogue's icon with the **prefix of
  the rules it holds** (`JD`), on the DiagnosticCatalog family mark — which is that project's identity,
  not this product's. The package currently ships the JustDummies mark, which says whose product it is
  and not which rules it holds. Left open deliberately.
* Create the nuget.org trusted-publishing policy for the new package id before its first release.

## References

* [ADR-0050](0050-name-a-suppressed-rule-through-a-catalogue-constant.md) — the decision this one
  completes, and the seven suppressions it could not convert.
* [ADR-0003](0003-host-dummies-as-a-standalone-package.md) — the standalone requirement, and why a
  declared dependency here does not touch it.
* [ADR-0047](0047-declare-the-adapters-library-dependency-independently.md) — why a package that
  versions on something else gets its own train.
* `JustDummies.DiagnosticCatalog/DiagnosticCatalogOptIn.props` — the file without which the catalogue
  checks nobody, and says nothing about it.
