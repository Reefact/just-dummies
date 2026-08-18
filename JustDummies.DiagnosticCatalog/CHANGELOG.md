# Changelog

All notable, user-facing changes to **JustDummies.DiagnosticCatalog** are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
Releases are cut from the `catalog` train (see [CONTRIBUTING.md](../CONTRIBUTING.md)).

A catalogue's version is its own, and it is not the product's: `JustDummies.DiagnosticCatalog 1.2.0`
does not describe `JustDummies 1.2.0`. It moves when the JD rule set moves — a rule added, a rule
retired, a category changed — which is not when the library or the adapter moves.

## [Unreleased]

## [1.0.0-preview.3] - 2026-08-18

### Added

- `JustDummiesRule.JD029` — *A value written into a pool that a declared constraint refuses*, category
  `JustDummies.Constraints`.
- `JustDummiesRule.JD030` — *A string dummy that declares no length*, category `JustDummies.Constraints`. The rule
  set moves from 28 identifiers, `1.0.0-preview.2`'s count, to 30.

## [1.0.0-preview.2] - 2026-08-07

First published version: **`JustDummies.DiagnosticCatalog` had never reached nuget.org before this
one.** It starts at the library's number because it describes the rule set the library's 1.0 ships, not
because the two move together afterwards — they do not, for the reason stated above.

**There is no `1.0.0-preview.1`, and there never was one on nuget.org.** A `catalog-v1.0.0-preview.1`
tag was pushed and its release run failed at version resolution, before packing or pushing anything:
`release.yml` carried an allowlist of trains that had never learned this one. The number is skipped
rather than reused because the tag it belongs to still exists and points at a commit without the fix,
and no protection was going to be relaxed to reclaim a preview number.

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

[Unreleased]: https://github.com/Reefact/just-dummies/compare/catalog-v1.0.0-preview.3...HEAD
[1.0.0-preview.3]: https://github.com/Reefact/just-dummies/compare/catalog-v1.0.0-preview.2...catalog-v1.0.0-preview.3
[1.0.0-preview.2]: https://github.com/Reefact/just-dummies/releases/tag/catalog-v1.0.0-preview.2
