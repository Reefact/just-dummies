# `sonar` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](sonar.fr.md)

> Maintainer documentation — part of the [workflow reference](README.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/sonar.yml`](../../../../.github/workflows/sonar.yml)

## What it is for

`sonar` runs the SonarQube Cloud analysis: it feeds the **Quality Gate** and the
**coverage** metric shown on the two SonarCloud badges in the README. It is the
static-analysis-plus-coverage view of the codebase, hosted off GitHub.

## When it runs

- On every **push to `main`**.
- On every **pull request targeting `main`** — **except PRs from forks and runs
  triggered by Dependabot** (see below).
- On demand via **`workflow_dispatch`**.

## How it runs

One job, `analyze`, on Linux:

1. Checkout with **`fetch-depth: 0`** — full history, so Sonar can attribute
   issues via `git blame` and distinguish new code from old.
2. Set up .NET **and Java 17** — the SonarScanner for .NET runs on the JVM.
3. `dotnet-sonarscanner begin` → **build** → test with coverage →
   `dotnet-sonarscanner end`.

## Permissions & security

`contents: read` only. PR decoration (the inline Sonar comments) is delivered by
the **SonarQube Cloud GitHub App**, not by this workflow's token, so no
`pull-requests: write` is needed here. The analysis authenticates with the
`SONAR_TOKEN` secret.

## Handle with care

- **The build must sit *between* `begin` and `end`.** The scanner hooks MSBuild
  to observe the compilation; it cannot analyse a pre-built or `--no-build`
  output. Do not reorder these steps or add `--no-build` to the analysis build.
- **The analysis build disables the warning ratchet on purpose.** It passes
  `-p:TreatWarningsAsErrors=false -p:MSBuildTreatWarningsAsErrors=false`. The
  scanner needs the compilation to **complete** so it can collect the
  `SonarAnalyzer` diagnostics and upload them in `end`; a Sonar-rule warning
  promoted to an error would fail the build before results are reported. The
  ratchet stays enforced by [`ci`](../../../../.github/workflows/ci.yml) on both OS legs — that is the gate,
  this analysis leg is not.
- **The guard on unreadable secrets is required, not optional.** The job's `if`
  skips the analysis for the two runs that cannot read `SONAR_TOKEN`, because
  each would fail on a missing secret rather than on a real problem:
  - **PRs from forks** — `… head.repo.full_name == github.repository`. A fork PR
    never receives this repository's secrets.
  - **Runs triggered by Dependabot** — `github.actor != 'dependabot[bot]'`.
    GitHub treats them like fork runs: they read the separate **Dependabot
    secrets** store, so `secrets.SONAR_TOKEN` arrives as the empty string and
    `dotnet-sonarscanner begin` stops on *"The format of the analysis property
    sonar.token= is invalid"*. Mirroring the token into the Dependabot store is
    the other available fix and is **declined** — it would hand the analysis
    token to the least-trusted runs here in order to analyse a version bump. The
    condition keys on `github.actor` rather than the PR's author because the
    withheld secret follows whoever *triggered* the run: a human pushing to a
    Dependabot branch gets the secrets back and that run analyses normally.

  Branches inside this repository (the normal contributor flow) run normally.
- **`fetch-depth: 0` matters.** A shallow checkout would break Sonar's new-code
  detection and blame attribution.

## Related

- [`ci`](../../../../.github/workflows/ci.yml) — produces the same OpenCover coverage shape via the shared
  `coverage.runsettings`, and is where the warning ratchet is actually enforced.
