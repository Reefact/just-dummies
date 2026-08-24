# Maintainer documentation

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./README.fr.md)

Everything needed to change this repository rather than to use it. If you are looking for how to
*write tests with* JustDummies, you want the [user documentation](../for-users/README.md) instead.

## Start here

**Never worked in this repository before?** Read these three, in order:

1. [Architecture](./architecture.en.md) — what each project is, how a draw actually flows, and where a
   change of a given kind belongs.
2. [`CONTRIBUTING.md`](../../../CONTRIBUTING.md) — commit conventions, branches, pull requests.
3. [Writing JustDummies tests](./WritingJustDummiesTests.en.md) — which of the two suites a new test
   belongs to, and why the answer is never "either".

## Architecture decisions

Every lasting decision in this repository is written down, in English and French, and stays readable
after the code that implemented it has changed.

| | |
| --- | --- |
| [Decision base](./adr/README.md) | all records, the conventions they follow, and the index |
| [Template](./adr/template.md) | the shape a new record takes |
| [Implementation reference](./specifications/adr-implementation-reference.md) | what each accepted decision actually enforces, and where |
| [`adr-check` workflow](./workflows/adr-check.en.md) | the advisory check that reads a pull request against the base |

The rule that governs the base is short: **an ADR records a significant, lasting decision — one a
future maintainer would question.** The test is whether the record would survive its own
implementation being rewritten. Most pull requests need none; the *check* is the habit, the *record*
is the exception.

You draft and propose. Accepting, superseding and deprecating are the maintainer's, exactly as no
agent merges a pull request.

## How the repository is built

| | |
| --- | --- |
| [Architecture](./architecture.en.md) | the projects, the draw pipeline, where to add a generator, an analyzer or a rule |
| [Writing JustDummies tests](./WritingJustDummiesTests.en.md) | the example suite versus the property suite |
| [Adding a release train](./AddingAReleaseTrain.en.md) | how a package gets its own versioned train |
| [The `dum` scaffolder](./specifications/justdummies-tool.md) | specified and implemented; `dum generate` runs, and the `cli` train publishes it |

## Workflows

The CI surface, one page per workflow, all indexed in [the workflows README](./workflows/README.md).

| Workflow | What it does |
| --- | --- |
| [`adr-check`](./workflows/adr-check.en.md) | reads a pull request against the decision base — advisory |
| [`analyzers`](./workflows/analyzers.en.md) | dogfoods the shipped analyzers on the Roslyn floor |
| [`justdummies-mutation`](./workflows/justdummies-mutation.en.md) | mutation-tests the diff — **reports, never blocks** |
| [`sonar`](./workflows/sonar.en.md) | the SonarCloud analysis |
| [`sonar-profile`](./workflows/sonar-profile.en.md) | how the build's rule set is derived from the quality profile |
| [`nuget-trusted-publishing`](./workflows/nuget-trusted-publishing.en.md) | how a release is cut, without a stored API key |

## Records

Material that documents a past state rather than a current rule. Useful when a decision looks
arbitrary until you know what it was reacting to.

| | |
| --- | --- |
| [Architecture and design audit](./audit/2026-07-20-dummies-architecture-and-design-audit.md) | a dated assessment, 2026-07-20 — a snapshot, not a rule |
| [Extraction record](./migration/README.md) | how this repository was split out of `Reefact/first-class-errors`, with the commit map |

## Conventions at a glance

The details live in the pages above; these are the ones that catch a newcomer out.

* **Documentation is bilingual and checked.** Every page has a French twin carrying the same
  headings, the same code blocks and the same markers, in the same order. The user documentation's
  C# samples are compiled on every build
  ([ADR-0055](./adr/0055-hold-the-user-documentation-to-contracts-the-build-checks.md)).
* **The decision base names its English pages without a language suffix** — `NNNN-slug.md` beside
  `NNNN-slug.fr.md` — while every other paired page uses `.en.md`/`.fr.md`. Both are handled; do not
  "fix" one to match the other.
* **Write the type, never `var`.** Enforced twice: by a hook on the edit, and by `IDE0008`, which CI
  turns into an error ([ADR-0034](./adr/0034-enforce-the-style-rules-the-compiler-can-express.md)).
* **A suppressed rule is named through a catalogue constant**, never a string literal
  ([ADR-0050](./adr/0050-name-a-suppressed-rule-through-a-catalogue-constant.md)).
* **Nothing enforces a mutation score.** The per-pull-request gate reports and does not block
  ([ADR-0025](./adr/0025-make-the-per-pull-request-mutation-gate-advisory.md)). Do not claim a pull
  request "passed the mutation bar" — there is none to pass.
* **Pull requests land by rebase** ([ADR-0051](./adr/0051-land-pull-requests-by-rebase.md)), so every
  commit on a branch reaches `main` on its own. Tidy the history before merging.

Agent-facing instructions live in [`AGENTS.md`](../../../AGENTS.md) and
[`CLAUDE.md`](../../../CLAUDE.md); they restate these rules where an agent will actually meet them.
`CLAUDE.md` keeps only what every task can violate — the rest is layered under `.claude/` as
path-scoped rules and on-demand skills, and some of it is enforced by a tool instead of stated
([ADR-0073](./adr/0073-layer-the-agent-instructions-by-when-they-are-needed.md)).

---

[← Repository README](../../../README.md) · [User documentation](../for-users/README.md)
