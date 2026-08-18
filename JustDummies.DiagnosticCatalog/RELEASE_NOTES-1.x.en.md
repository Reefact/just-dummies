# Release notes — JustDummies.DiagnosticCatalog, 1.x

What changed for you, release by release, in the `catalog` train. A catalogue's version is its own — it does not describe `JustDummies` at the same number. For the full technical record, see [CHANGELOG.md](https://github.com/Reefact/just-dummies/blob/main/JustDummies.DiagnosticCatalog/CHANGELOG.md).

## 1.0.0-preview.2 — August 7, 2026

_First published version — the catalogue reaches nuget.org for the first time, at the number of the rule set JustDummies 1.0 ships. There is no `1.0.0-preview.1`: that tag's release run failed before publishing anything, and the number is skipped rather than reused._

### ✨ New

- **`JustDummiesRule`** — the 28 analyzer rules, `JD001` to `JD028`, each carrying `Id`, `Category`, `Title` and `HelpLinkUri` as compile-time constants a `[SuppressMessage]` can name.
- **`JustDummiesCategory`** — the four categories those rules are grouped under.
- **Opt-in scoped to your own project** — `build/JustDummies.DiagnosticCatalog.props` switches the checks on only for the project that references this catalogue, never for one that merely depends on it.

### 🙌 Improvements

- **A rule is never removed and a member is never renamed.** A rule retired from the product is carried forward as `[Obsolete]` instead, so upgrading never breaks a build over a diagnostic id.
