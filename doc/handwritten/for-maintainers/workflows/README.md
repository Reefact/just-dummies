# CI/CD workflow reference

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](README.fr.md)

> Maintainer documentation. This describes the GitHub Actions workflows that
> build, check, and publish JustDummies. It is **not** part of the library's
> user documentation under `doc/handwritten/for-users/`.

## What this is

Each workflow under [`.github/workflows/`](../../../../.github/workflows/) carries a
fair amount of intent that is easy to break by "cleaning it up": a permission
that is narrow on purpose, a step ordering that guards a specific failure, a
version that is frozen for a product reason. The workflow files themselves hold
the line-by-line rationale in comments — those comments are the source of truth
closest to the code. **These pages are the pedagogical layer above them:** what
each workflow is *for*, when and how it runs, and the handful of things you must
not change without understanding why.

Read the page for a workflow before you touch it. When the page and the YAML
disagree, the YAML wins — and the page should be corrected.

**Seven workflows have a page so far**, and the table below says which. The rest
are listed anyway: an index that omits them would read as though they did not
exist. Their YAML comments are, for now, all the documentation they have.

## The cross-cutting conventions

A few decisions are shared by every workflow. They are documented once here
instead of being repeated on every page. Each was checked against the workflows
as they stand, not carried over on faith:

- **Actions are pinned by commit SHA, not by tag.** A tag like `@v4` can be moved
  by its owner to point at new code; a 40-hex SHA cannot. Every `uses:` therefore
  pins a SHA with the human-readable tag in a trailing comment (`# v4`). When you
  bump an action, change **both**. Counted: 47 SHA-pinned `uses:`, and one that is not —
  `contributor-agreement` pins `actions/github-script@v9` by tag.
- **`permissions:` start read-only and widen per job.** The workflow-level block
  is the least privilege the workflow needs (usually `contents: read`); a job that
  must write something (upload SARIF, publish a release, enable auto-merge)
  re-declares a `permissions:` block that adds *only* that scope. Never widen the
  top-level block to satisfy one job. A job that needs *nothing* does the reverse:
  it declares `permissions: {}` — the explicit empty mapping, since a bare
  `permissions:` is a null and not an empty map.
- **Every job sets `timeout-minutes`.** The GitHub default is six hours; a hung
  step would otherwise hold a runner for that long. Counted: 24 jobs, 23 with a
  cap — every job but `contributor-agreement`'s, which sets none. Each is set a few
  times the observed run time, noted in a comment beside it.
- **`concurrency` cancels superseded runs.** Pushing twice to the same branch or
  PR cancels the in-flight run. The one exception is `release`, which sets
  `cancel-in-progress: false` — you never want to cancel a half-finished publish.
- **Security scanners also run weekly on a `schedule`.** `codeql` and `scorecard`
  re-run against unchanged code so newly shipped queries and checks are applied
  even when nothing was pushed.
- **Forks cannot read secrets.** Workflows that need one (`sonar`) detect a fork
  PR and skip rather than fail; GitHub does not expose repository secrets to a PR
  raised from a fork.
- **Required checks are the real gate.** Several workflows (`dependency-review`,
  `dependabot-automerge`) only *signal* or *enable* — they merge nothing on their
  own. What actually blocks a bad merge is the branch-protection configuration on
  `main` marking these checks as **required**. That is a repository setting, not
  something a workflow can enforce for itself.

## The workflows

### Build & quality

| Workflow | Purpose |
| --- | --- |
| `ci` | Build and test the solution on Linux and Windows, with coverage, plus the .NET Framework 4.7.2 floor leg. The primary gate. |
| `justdummies` | Prove the packaged `netstandard2.0` and `net8.0` assets behave on the runtimes that actually load them — the leg the net10 test project cannot exercise. |
| [`justdummies-mutation`](justdummies-mutation.en.md) | Mutation testing of the three components with Stryker.NET — an advisory check on what a PR changed, plus the weekly full sweep. Publishes counts by status, never a score (ADR-0093). |
| [`gendummy-sweep`](gendummy-sweep.en.md) | Weekly: the generative sweep over the scaffolding engine — ~3600 guarded domains from a declared axis product, each scaffolded, compiled, analyzed and drawn from. The instrument that finds defects; a covering slice of it runs on every build. |
| `stryker-xunit-v3-watch` | Weekly: flags the moment Stryker.NET fixes its xUnit v3 test-discovery bug, which nothing else here would ever notice. Reports on PR #148; merges and reopens nothing. |
| [`analyzers`](analyzers.en.md) | Load the bundled analyzers out of the packed artifact under the oldest supported compiler (Roslyn 4.8), the one thing an ordinary build never does. |
| [`sonar`](sonar.en.md) | SonarQube Cloud analysis — quality gate and coverage reporting. |
| [`sonar-profile`](sonar-profile.en.md) | Weekly: fails when the committed Sonar C# rule list has drifted from the SonarCloud quality profile. Reports, never repairs. |
| `sonar-gate` | Nightly: reads the SonarCloud Quality Gate verdict and fails when it is red. |
| `commit-lint` | Enforce the Conventional Commits convention on every PR commit, using the same script as the local hook. |
| `lint` | shellcheck and actionlint over the files the C# compiler never sees — the POSIX scripts and the workflow definitions. |
| [`adr-check`](adr-check.en.md) | Advisory, manual dispatch: check a branch against the ADR base (new decision / supersede / conflict). Never blocks. |

### Security & supply chain

| Workflow | Purpose |
| --- | --- |
| `codeql` | GitHub CodeQL static analysis for C#, results on the code-scanning dashboard. |
| `dependency-review` | Block a PR that introduces a known-vulnerable dependency. Requires the repository's dependency graph to be enabled. |
| `scorecard` | OpenSSF Scorecard — scores the repository's security posture. |

### Release

| Workflow | Purpose |
| --- | --- |
| [`release`](nuget-trusted-publishing.en.md) | Build, attest, and publish the NuGet packages on a version tag of one of the four trains (`lib-v*`, `xunit-v*`, `catalog-v*`, `cli-v*`). **Pushing such a tag publishes**, and a published version is immutable — the linked page covers the nuget.org trusted-publishing setup it needs and how to rehearse without publishing. |
| `release-dryrun` | Continuously rehearse the side-effect-free part of the release (pack + SBOM) on every PR and push. |
| `changelog` | Draft the `[Unreleased]` section of a train's changelog from merged PRs, on manual dispatch, and open a review PR. |

### Dependency maintenance

| Workflow | Purpose |
| --- | --- |
| `dependabot-automerge` | Enable auto-merge on Dependabot patch/minor updates; leave majors for a human. |
| `dependabot-autofix` | Claude triages a failing Dependabot PR and pushes a low-risk fix. Never merges. |

## Related maintainer docs

- [`tools/trains.sh`](../../../../tools/trains.sh) — the single source of truth for
  the release trains that `release`, `release-dryrun` and `changelog` all read.
- [Writing JustDummies tests](../WritingJustDummiesTests.en.md) — which of the two
  suites a new test belongs to.
- [`CONTRIBUTING.md`](../../../../CONTRIBUTING.md) — the commit and PR conventions the
  `commit-lint` workflow checks.
