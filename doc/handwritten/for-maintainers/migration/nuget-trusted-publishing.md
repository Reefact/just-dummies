# nuget.org trusted publishing — manual setup

`release.yml` publishes with **trusted publishing**: the job exchanges its GitHub OIDC token for a
short-lived, single-use NuGet API key. No long-lived key is stored anywhere. The exchange only works once
nuget.org holds a policy that names this repository and this workflow.

**The policy exists and works.** `JustDummies` has been published from it, so the OIDC exchange in
`release.yml` is proven end to end rather than assumed. Until a policy exists, every run of `release.yml` —
including a dry run — fails at the `NuGet login (OIDC)` step, by design: the dry run rehearses the exchange
precisely so a misconfiguration surfaces before a real release rather than during one.

None of this can be automated from CI: creating a trusted-publishing policy requires an authenticated session
on nuget.org as the package owner.

## What to configure on nuget.org

Sign in as the account that will own the packages and create a trusted-publishing policy under
*Account settings → Trusted Publishing*.

| Field | Value |
| --- | --- |
| Package owner | `Reefact` |
| Repository owner | `Reefact` |
| Repository | `just-dummies` |
| Workflow file | `release.yml` |
| Environment | *(leave empty — `release.yml` declares no environment)* |

**The policy is scoped to the repository, not to a package id** — confirmed by the maintainer, who holds the
account. One policy therefore covers every package this repository publishes, and a new package id needs no
new policy:

* `JustDummies` — the `lib-v*` train
* `JustDummies.Xunit` — the `xunit-v*` train
* `JustDummies.DiagnosticCatalog` — the `catalog-v*` train
* `dum` — the `cli-v*` train, once it is built and its package id is decided; `tools/packaging/pack.sh`
  fails that train loudly until then

An id nobody owns yet is reserved to the account on the first successful push, so a package can be published
before it exists. Verify the id is not already taken by somebody else before relying on it.

This page previously said one policy was needed per package id. It was wrong, and it was written before any
package had been published — which is why nothing had contradicted it. The first `JustDummies.Xunit` release
is what will demonstrate the repository scope on a second id rather than on the maintainer's word.

## What to configure on GitHub

One repository **variable** — *Settings → Secrets and variables → Actions → Variables*:

| Variable | Value |
| --- | --- |
| `NUGET_USER` | the nuget.org account **username** (the profile name, not the email address) |

A variable rather than a secret, deliberately: the username is public on the nuget.org profile and is an
identifier, not a credential. The only credential in this path is the short-lived key the OIDC exchange
mints, and it never leaves the runner. Storing the username as a secret would mask it in the logs and make a
failed login harder to diagnose, protecting nothing.

`release.yml` reads it at `vars.NUGET_USER` and passes it to `NuGet/login`. Nothing else in the release path
needs a secret: the OIDC token is minted by GitHub, and `GITHUB_TOKEN` covers the GitHub Release. Setting it
as a *secret* instead leaves `vars.NUGET_USER` empty and the login fails with
`Input required and not supplied: user`.

No branch protection, environment or approval gate is required. If one is added later, its name must be
declared both in `release.yml` (`environment:`) and in the nuget.org policy — the exchange matches on it.

## Verifying without publishing

Once the policies and the variable exist:

```
gh workflow run release.yml -f component=lib -f version=0.0.0-dry.1 -f dry_run=true
```

A green run proves the whole pipeline end to end — restore, build, test, pack, SBOM, attestation and the OIDC
exchange — while publishing nothing: the push and the GitHub Release are the only steps a dry run skips.

## What has been published

Two versions of `JustDummies`, and nothing else:

* `0.1.0-preview.1` — the first release, 2026-07-31.
* `0.0.0-rulesetcheck` — **published by mistake**, 2026-08-01. A `lib-v0.0.0-rulesetcheck` tag was pushed to
  test a repository tag-protection setting, and pushing a `lib-v*` tag is what triggers `release.yml`. A
  published version is immutable, so it can only be unlisted and deprecated. **Pushing a `<train>-v*` tag
  publishes.** To exercise tag protection without publishing, use a ref the release trigger does not match,
  or `workflow_dispatch` with `dry_run=true` as above.

`JustDummies.Xunit` and `JustDummies.DiagnosticCatalog` have never been published.

Publishing `JustDummies` was also the prerequisite for the FirstClassErrors cleanup: until a restorable
package exists, `FirstClassErrors.Testing` must keep embedding `JustDummies.dll` (see ADR-0044 and, in
`Reefact/first-class-errors`, issue #229).
