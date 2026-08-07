# Changelog

All notable, user-facing changes to **JustDummies.DiagnosticCatalog** are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
Releases are cut from the `catalog` train (see [CONTRIBUTING.md](../CONTRIBUTING.md)).

A catalogue's version is its own, and it is not the product's: `JustDummies.DiagnosticCatalog 1.2.0`
does not describe `JustDummies 1.2.0`. It moves when the JD rule set moves — a rule added, a rule
retired, a category changed — which is not when the library or the adapter moves.

## [Unreleased]

Nothing is published yet: **`JustDummies.DiagnosticCatalog` has never been released to nuget.org**, so
everything below belongs to its first version, whatever number and date that version ends up carrying.

### Added

- **`JustDummiesRule`** — the 28 JustDummies analyzer rules, `JD001` to `JD028`, each a marked static
  class carrying `Id`, `Category`, `Title` and `HelpLinkUri` as `const string`, so a
  `[SuppressMessage]` can name a rule the compiler resolves.
- **`JustDummiesCategory`** — the four categories those rules are grouped under, declared once each and
  reached only through the rule that carries them.
- The `DCAT` opt-in, packed as `build/JustDummies.DiagnosticCatalog.props`, which is what switches the
  checks on for a project that references this catalogue — and stops them at that project, so an
  application referencing a library that took this catalogue is not analysed by a catalogue it never
  chose.

### Notes

**This is a first-party catalogue, not a mirror.** `JustDummies.Analyzers` reads its
`DiagnosticDescriptor` arguments from these constants rather than the other way round, so the rule the
analyzer reports and the rule a consumer silences are the same value by construction. That is why
there is no `CatalogSource` attribute here: nothing upstream is being snapshotted.

**A rule is never removed and a member is never renamed.** These are `const`, so they are inlined into
a consumer's assembly at their compile time; removing one breaks their build with a message that names
nothing they wrote. A rule retired from the product is carried forward as `[Obsolete]` instead.
