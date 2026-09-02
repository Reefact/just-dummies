# Release notes — JustDummies.DiagnosticCatalog, 1.x

What changed for you, release by release, in the `catalog` train. A catalogue's version is its own — it does not describe `JustDummies` at the same number. For the full technical record, see [CHANGELOG.md](https://github.com/Reefact/just-dummies/blob/main/JustDummies.DiagnosticCatalog/CHANGELOG.md).

## 1.0.0-preview.5 — September 2, 2026

_A license change every consumer should read._

### ⚠️ Breaking changes

- **JustDummies.DiagnosticCatalog is now licensed under [PolyForm Internal Use 1.0.0](https://github.com/Reefact/just-dummies/blob/main/LICENSE), not Apache 2.0 — source-available, not open source.** You may read, build, modify and run the package for your own or your company's internal business operations; you may not distribute the software. Versions already published on NuGet are untouched and keep the license they shipped with. Contributions are now governed by a [Contributor Agreement](https://github.com/Reefact/just-dummies/blob/main/CONTRIBUTOR_AGREEMENT.md).

## 1.0.0-preview.4 — August 24, 2026

_The catalogue catches up with the rule set `JustDummies 1.0.0-preview.3` shipped: three rules join the constants, `JD031`, `JD032` and `JD033`._

### ✨ New

- **`JustDummiesRule.JD031`** — *Two inclusive bounds the library also names as one range*, category `JustDummies.Constraints`.
- **`JustDummiesRule.JD032`** — *A bound declared twice, where only the tighter one survives*, category `JustDummies.Constraints`.
- **`JustDummiesRule.JD033`** — *An anchored literal the declared characters cannot draw*, category `JustDummies.Constraints`.

## 1.0.0-preview.3 — August 18, 2026

_The catalogue catches up with the rule set `JustDummies 1.0.0-preview.2` shipped: two rules join the constants, `JD029` and `JD030`._

### ✨ New

- **`JustDummiesRule.JD029`** — *A value written into a pool that a declared constraint refuses*, category `JustDummies.Constraints`.
- **`JustDummiesRule.JD030`** — *A string dummy that declares no length*, category `JustDummies.Constraints`.

## 1.0.0-preview.2 — August 7, 2026

_First published version — the catalogue reaches nuget.org for the first time, at the number of the rule set JustDummies 1.0 ships. There is no `1.0.0-preview.1`: that tag's release run failed before publishing anything, and the number is skipped rather than reused._

### ✨ New

- **`JustDummiesRule`** — the 28 analyzer rules, `JD001` to `JD028`, each carrying `Id`, `Category`, `Title` and `HelpLinkUri` as compile-time constants a `[SuppressMessage]` can name.
- **`JustDummiesCategory`** — the four categories those rules are grouped under.
- **Opt-in scoped to your own project** — `build/JustDummies.DiagnosticCatalog.props` switches the checks on only for the project that references this catalogue, never for one that merely depends on it.

### 🙌 Improvements

- **A rule is never removed and a member is never renamed.** A rule retired from the product is carried forward as `[Obsolete]` instead, so upgrading never breaks a build over a diagnostic id.
