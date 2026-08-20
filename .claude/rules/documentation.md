---
paths:
  - "doc/**/*.md"
  - "*.md"
  - "**/CHANGELOG.md"
  - "**/README.nuget.md"
---

# Documentation

**The English page is canonical. French is an intentional translation, and it is not one
file.** Every maintainer page, every ADR and every analyzer page comes as a pair; the user
documentation, `CONTRIBUTING.fr.md` and `SECURITY.fr.md` are paired too. **Change a page,
change its twin** — same headings, same code blocks, same markers, in the same order.

`JustDummies.Documentation.UnitTests` checks parity, resolves the links and **compiles the
C# samples in the user documentation on every build** (ADR-0055). A sample that does not
compile fails the build, so keep samples real.

## Two naming conventions, both correct

* The decision base names its English pages **without a language suffix** — `NNNN-slug.md`
  beside `NNNN-slug.fr.md`.
* Every other paired page uses **`.en.md` / `.fr.md`**.

Both are handled. Do not "fix" one to match the other.

## Language

The repository language is **English**: source, comments, commit messages, branch names,
pull-request titles and descriptions, issues, review comments. Never write repository
content in French unless you are updating the French documentation. (Replying in French in
the chat is fine.)

## When user-facing behaviour changes

Update the English page and its French twin in the same change. For an analyzer rule, four
more things move with it — see the `analyzers` rule.

A generated value's relationship to its seed is **not** a versioned contract while the
library is below 1.0: changing a draw sequence is allowed, and the changelog says so when it
happens.

## Where things live

| | |
|---|---|
| `doc/handwritten/for-users/` | how to write tests **with** JustDummies; analyzer pages; guides |
| `doc/handwritten/for-maintainers/` | how to change this repository — start at its `README.md` |
| `doc/handwritten/for-maintainers/adr/` | the 73 decision records and their index |
| `doc/handwritten/for-maintainers/specifications/` | the `dum` specification, the ADR implementation reference |
| `doc/handwritten/for-maintainers/workflows/` | one page per CI workflow |
| `doc/handwritten/for-maintainers/audit/`, `migration/` | dated records of a past state, not current rules |

An ADR records a **decision and its reasoning**, never how it is implemented; mechanics live
in the code and in the reference documentation the ADR links to.

Changelogs are per release train, and an entry is written **by hand in the change that
produces it** — a user-facing change announces itself in the same commit. The `changelog`
workflow drafts a section from merged pull requests as a net before a cut, not as the normal
route. See the `release-train` skill before cutting a section.
