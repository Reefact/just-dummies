# nuget.org trusted publishing — manual setup

`release.yml` publishes with **trusted publishing**: the job exchanges its GitHub OIDC token for a
short-lived, single-use NuGet API key. No long-lived key is stored anywhere. The exchange only works once
nuget.org holds a policy that names this repository and this workflow.

**Neither `JustDummies` nor `JustDummies.Xunit` has ever been published**, so no policy exists yet. Until the
steps below are done, every run of `release.yml` — including a dry run — fails at the `NuGet login (OIDC)`
step. That is by design: the dry run rehearses the exchange precisely so a misconfiguration surfaces before a
real release rather than during one.

None of this can be automated from CI: creating a trusted-publishing policy requires an authenticated session
on nuget.org as the package owner.

## What to configure on nuget.org

Sign in as the account that will own the packages, then for **each** package ID create a trusted-publishing
policy under *Account settings → Trusted Publishing*.

| Field | Value |
| --- | --- |
| Package owner | `Reefact` |
| Repository owner | `Reefact` |
| Repository | `just-dummies` |
| Workflow file | `release.yml` |
| Environment | *(leave empty — `release.yml` declares no environment)* |

Create one policy per package ID:

* `JustDummies` — published by the `lib-v*` train
* `JustDummies.Xunit` — published by the `xunit-v*` train

Both packages are **new IDs on nuget.org**. A trusted-publishing policy can be created for an ID the account
does not own yet: nuget.org reserves it to the account on the first successful push. Verify the ID is not
already taken by someone else before relying on it.

The future `dum` scaffolder (`cli-v*` train) will need a third policy when it is built and its package ID is
decided; `tools/packaging/pack.sh` fails that train loudly until then.

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

## What is deliberately not done

No package is published by this migration, and no version has been decided. Publishing the first
`JustDummies` version is also the prerequisite for the FirstClassErrors cleanup: until a restorable package
exists, `FirstClassErrors.Testing` must keep embedding `JustDummies.dll` (see ADR-0044 and, in
`Reefact/first-class-errors`, issue #229).
