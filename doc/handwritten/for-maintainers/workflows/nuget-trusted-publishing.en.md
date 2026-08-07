# nuget.org trusted publishing

`release.yml` publishes with **trusted publishing**: the job exchanges its GitHub OIDC token for a
short-lived, single-use NuGet API key. No long-lived key is stored anywhere. The exchange works only
while nuget.org holds a policy naming this repository and this workflow.

Setting it up cannot be automated: creating the policy requires an authenticated session on nuget.org
as the package owner.

## What this page does not tell you

**Which packages and versions have been published.** nuget.org and this repository's `<train>-v*` tags
already answer that, authoritatively and without anyone remembering to update them. A list here would be
a third copy, wrong the day after a release nobody thought to document — which is exactly what happened
to the version of this page that tried.

To find out, read the source:

```
curl -s https://api.nuget.org/v3-flatcontainer/justdummies/index.json
git tag --list 'lib-v*' 'xunit-v*' 'catalog-v*'
```

## ⚠️ Pushing a train tag publishes

`release.yml` triggers on `lib-v*`, `xunit-v*`, `catalog-v*` and `cli-v*`. Pushing such a tag packs the
tagged commit and pushes it to nuget.org, and **a published version is immutable** — it can be unlisted
and deprecated, never removed or replaced.

This is not hypothetical: a `lib-v0.0.0-rulesetcheck` tag was once pushed to test a tag-protection
setting, and it published. To exercise anything about release tags without publishing, use a ref the
trigger does not match, or the dry run below.

## What to configure on nuget.org

Sign in as the account that owns the packages, then create a policy under
*Account settings → Trusted Publishing*.

| Field | Value |
| --- | --- |
| Package owner | `Reefact` |
| Repository owner | `Reefact` |
| Repository | `just-dummies` |
| Workflow file | `release.yml` |
| Environment | *(leave empty — `release.yml` declares no environment)* |

**The policy is scoped to the repository, not to a package id.** One policy covers every package this
repository publishes, so a new package id — a new release train — needs no new policy.

An id nobody owns yet is reserved to the account on the first successful push, so a package can be
published before it exists on nuget.org. Verify the id is not already taken by somebody else before
relying on it.

## What to configure on GitHub

One repository **variable** — *Settings → Secrets and variables → Actions → Variables*:

| Variable | Value |
| --- | --- |
| `NUGET_USER` | the nuget.org account **username** (the profile name, not the email address) |

A variable rather than a secret, deliberately: the username is public on the nuget.org profile and is an
identifier, not a credential. The only credential in this path is the short-lived key the OIDC exchange
mints, and it never leaves the runner. Storing the username as a secret would mask it in the logs and
make a failed login harder to diagnose, protecting nothing.

`release.yml` reads it at `vars.NUGET_USER` and passes it to `NuGet/login`. Nothing else in the release
path needs a secret: the OIDC token is minted by GitHub, and `GITHUB_TOKEN` covers the GitHub Release.
Setting it as a *secret* instead leaves `vars.NUGET_USER` empty and the login fails with
`Input required and not supplied: user`.

No branch protection, environment or approval gate is required. If one is added later, its name must be
declared both in `release.yml` (`environment:`) and in the nuget.org policy — the exchange matches on it.

## Verifying without publishing

```
gh workflow run release.yml -f component=lib -f version=0.0.0-dry.1 -f dry_run=true
```

A green run proves the whole pipeline end to end — restore, build, test, pack, SBOM, attestation and the
OIDC exchange — while publishing nothing: the push and the GitHub Release are the only steps a dry run
skips. Until a policy exists, every run including this one fails at `NuGet login (OIDC)`, by design: the
rehearsal exists so a misconfiguration surfaces before a real release rather than during one.
