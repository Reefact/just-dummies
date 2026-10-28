# 19 — Cybersecurity / IAM

Access tokens, OAuth clients and role grants: GUID ids, scopes, expiry instants, redirect URIs,
duplicate-free sets. Built against the packed `1.0.0-uxtrial.1` packages; learned from the user
documentation only. Implementation read once, afterwards, to confirm one diagnosis (noted below).

## Domain model (`Domain/`)

| Type | Shape | Guards |
|---|---|---|
| `ClientId` | value object over `Guid` | `if (value == Guid.Empty) throw` |
| `Scope` | value object, `resource:action` | `ThrowIfNullOrWhiteSpace`, then a `[GeneratedRegex]` check |
| `RedirectUri` | value object over `Uri` | delegated to a private `Validate`: absolute, https (http only on loopback), no fragment |
| `Role`, `Permission` | enum, `[Flags]` enum; `RolePermissions.Of` is a switch expression | |
| `RoleAssignment` | grant over a `DateOnly` range, `DateOnly?` end | non-empty subject, `Enum.IsDefined`, `validUntil >= validFrom` |
| `TokenPolicy` | max lifetime, max scopes | `ThrowIfLessThanOrEqual(TimeSpan, TimeSpan.Zero)`, `ThrowIfGreaterThan(TimeSpan, static field)`, `ThrowIfNegativeOrZero(int)`, `ThrowIfGreaterThan(int, 20)` |
| `ClientApplication` | aggregate | name not blank and ≤ 80 chars, non-empty URI and scope lists, **distinct URIs checked on the field copy** |
| `AccessToken` | aggregate | combined Guid guard, non-empty scopes, `expiresAt > issuedAt`, lifetime ≤ 24 h |
| `TokenIssuer` | policy: caps the lifetime, refuses a scope the client may not request, dedups scopes | |
| `CliProbeTypes.cs` | `ConsentScreen`, `WebhookSubscription`, `TenantId`, `RedirectAllowList`, `PostLogoutRedirects`, `RoleSet` — guard shapes probed for the CLI | |

## CLI recaps (verbatim)

```text
$ any generate ClientId Scope RedirectUri
Analyzing Identity.Domain.ClientId
  value  Guid  Any.Guid().NonEmpty()  guard
✓ AnyClientId.cs — 1 of 1 parameters inferred.

✗ Scope: several types answer to that name.
  Identity.Domain.Scope
  JustDummies.Xunit.ReproducibleAttribute.Scope
  System.Formats.Asn1.AsnWriter.Scope
  System.Threading.Lock.Scope
  Name the one you mean in full.

Analyzing Identity.Domain.RedirectUri
  value  Uri  Any.Uri().Web()  to verify, unread guards
✓ AnyRedirectUri.cs — 1 of 1 parameters inferred, 1 to verify.
exit=1

$ any generate RoleAssignment --entry-point static:Arbitrary
  subjectId   Guid       Any.Guid().NonEmpty()        guard
  role        Role       Any.Enum<Role>()
  validFrom   DateOnly   Any.DateOnly()               to verify, unread guards
  validUntil  DateOnly?  Any.DateOnly().AsNullable()  to verify, unread guards
✓ AnyRoleAssignment.Entry.cs — entry point Arbitrary.RoleAssignment()

$ any generate TokenPolicy --entry-point any
  maximumLifetime  TimeSpan  Any.TimeSpan()                                to verify, unread guards
  maximumScopes    int       Any.Int32().Positive().LessThanOrEqualTo(20)  guard
✓ AnyTokenPolicy.Entry.cs — entry point Any.TokenPolicy()

$ any generate Identity.Domain.Scope ClientApplication AccessToken
  value  string  Any.String().NotBlank()  to verify, guard, unread guards
  id             ClientId                          new AnyClientId()                            AnyX
  displayName    string                            Any.String().NotBlank()                      to verify, guard, unread guards
  redirectUris   IReadOnlyCollection<RedirectUri>  Any.ListOf(new AnyRedirectUri()).NonEmpty()  guard
  allowedScopes  IReadOnlyCollection<Scope>        Any.ListOf(new AnyScope()).NonEmpty()        guard
  tokenId    Guid                        Any.Guid().NonEmpty()                  to verify, unread guards
  clientId   ClientId                    new AnyClientId()                      AnyX
  subjectId  Guid                        Any.Guid().NonEmpty()                  to verify, unread guards
  scopes     IReadOnlyCollection<Scope>  Any.ListOf(new AnyScope()).NonEmpty()  guard
  issuedAt   DateTimeOffset              Any.DateTimeOffset()                   to verify, unread guards
  expiresAt  DateTimeOffset              Any.DateTimeOffset()                   to verify, unread guards

$ any generate Role TokenIssuer RolePermissions
Analyzing Identity.Domain.Role
  constructor Role()
✓ AnyRole.cs — no constructor parameters to infer.
  policy  TokenPolicy  new AnyTokenPolicy()  AnyX
✗ RolePermissions: nothing here constructs it.   (static class: fair refusal)

$ any generate RoleSet PostLogoutRedirects
  roles  IReadOnlyCollection<Role>  Any.ListOf(Any.Enum<Role>())
✓ AnyRoleSet.cs — 1 of 1 parameters inferred.
  uris  IReadOnlyCollection<RedirectUri>  Any.ListOf(new AnyRedirectUri())
✓ AnyPostLogoutRedirects.cs — 1 of 1 parameters inferred.

$ any generate ConsentScreen WebhookSubscription TenantId RedirectAllowList --dry-run
  requestedScopes  ...  Any.ListOf(new AnyScope())        to verify, unread guards   (Distinct().Count() on the parameter)
  callback  Uri  Any.Uri().Web()  to verify ; retryAfter TimeSpan Any.TimeSpan() to verify
  value  Guid  Any.Guid().NonEmpty()  to verify, unread guards   (switch-expression guard)
  uris  ...  Any.ListOf(new AnyRedirectUri())  to verify, unread guards   (HashSet in a local)
```

`--format json` on a re-run without `--force`: exit 1, yet the document says
`"status": "Scaffolded"`, `"summary": { "scaffolded": 1, "failed": 0 }`, no files, no refusal.

## Fixed by hand

* `AnyScope`: `Any.String().NotBlank()` → `Any.StringMatching("^[a-z][a-z0-9_]{1,31}:(read|write|admin)$")`;
  return type and the `With…(Func<AnyString, AnyString>)` signature both had to become `AnyPattern`.
* `AnyRedirectUri`: `Any.Uri().Web()` → `.UsingHttps()` (no fragment is drawn unless asked).
* `AnyTokenPolicy`: `Any.TimeSpan()` → `.Positive().LessThanOrEqualTo(TokenPolicy.HardMaximumLifetime)`.
* `AnyClientApplication`: display name `.WithMaxLength(80)`. The redirect-URI list was **left as
  scaffolded**: the recap said `guard` with no `to verify`, so nothing told me about the distinctness check.
* `AnyAccessToken` / `AnyRoleAssignment`: cross-field invariants (`expiresAt` after `issuedAt` within
  24 h; `validUntil >= validFrom`). Per-parameter bases are independent, so I used two adjacent,
  disjoint windows. Nothing in the docs says how to express a relation between two parameters; the
  workaround means a test pinning only `WithIssuedAt(x)` outside the window breaks — both must be pinned.
* The `Guid` "to verify" lines in `AnyAccessToken` were already correct (the combined `||` guard is K2).
* `AnyRole` (an enum) and `AnyRoleSet` kept as emitted to demonstrate the two bugs.

Every hand-fixed generator passes its 500-draw harness test (`ScaffoldedGeneratorTests`).

## Findings

1. **Bug (high) — guard checked on a field copy is silently missed.** `_roles = new HashSet<Role>(roles);
   if (_roles.Count != roles.Count) throw` → recap `Any.ListOf(Any.Enum<Role>())`, "1 of 1 inferred",
   no `to verify`; the generator throws on most draws (`Bug_Scaffolded_AnyRoleSet_…`). The same check on a
   local (`RedirectAllowList`) or directly on the parameter (`ConsentScreen`) is correctly flagged.
2. **Bug (medium) — `any generate Role` on an enum** scaffolds `return new Role();`: a generator
   documented as "arbitrary Role values" that always returns `Role.Reader` (`Bug_Scaffolded_AnyRole_…`).
   Should refuse and point to `Any.Enum<Role>()`.
3. **Bug (medium) — `--format json` on an existing file** reports `Scaffolded`/`scaffolded: 1`/`failed: 0`
   while exiting 1 and writing nothing; the reason is only on stderr.
4. **CLI friction (medium) — type lookup offers inaccessible / unusable candidates.** `Scope` is ambiguous
   with `JustDummies.Xunit.ReproducibleAttribute.Scope` (an `internal` nested class, confirmed in
   `JustDummies.Xunit/ReproducibleAttribute.cs:190` after the fact) and `System.Threading.Lock.Scope`
   (a ref struct). Only one candidate is visible from the project.
5. **Doc (medium) — cross-field invariants in a scaffolded generator** have no documented pattern.
6. **Doc (medium) — `WithGranularity` on a `DateTimeOffset` with an offset** snaps the UTC instant:
   day granularity at `+02:00` yields `02:00` local. The docs say "snaps onto a grid" without saying which clock.
7. **Missing capability (low) — no RFC 9562 version-4 `Guid`.** A drawn Guid is 128 random bits; code
   checking `id.Version == 4` rejects most draws. Workaround: a `StringMatching` v4 pattern + `Guid.Parse`.
8. **Low** — `ThrowIfLessThanOrEqual(timeSpan, TimeSpan.Zero)` is unread while the `int` helper twin is
   read (honestly flagged); `ThrowIfGreaterThan(name.Length, 80)` unread (honestly flagged); docs never show
   `using JustDummies.Xunit;`; the conflict for `After(Max-1h).WithOffset(+14h)` names both sides but not why;
   no analyzer for `Any.Guid().Empty().NonEmpty()`; no analyzer for a drawn value reaching an assertion.

## Analyzers seen

JD014 (port 0 / 70000, negative segments, IDN host, 30-second and 15-hour offsets, reversed offset range,
count above a million) — helpful. JD005 on `.WithPathSegments(5000).ToString()` — helpful (error).
JD016 (5 distinct of a 4-member enum), JD034 (`UsingHttps().UsingHttp()`, `WithOffset` + `WithOffsetBetween`),
JD026 (empty relative reference) — all helpful, and all silent when the chain is the whole body of a lambda,
as JD016's page documents. None fired on the generated `Any*.cs` files or the real tests.

## Fluency: 4 / 5

The scalar API reads like the domain: `Any.Uri().Web().UsingHttp().WithHost("127.0.0.1").WithPort()` is
exactly the loopback redirect the test needs, conflicts are refused at the declaration with both sides named,
regex refusals name the construct and position, and seeds replayed exactly (`[Reproducible]` and
`Any.Reproducibly`). Scaffolding saved real typing and the nested `With…(lambda)` refinements
(`WithPolicy(p => p.WithMaximumScopes(s => s.GreaterThanOrEqualTo(2)))`) felt natural. One point off for
cross-field invariants with no story, a silently missed guard that broke the trust the recap asks for,
and the enum scaffold.

## After #331

The CLI fixes merged in [#331](https://github.com/Reefact/just-dummies/pull/331) were re-checked on this domain by re-scaffolding with `--dry-run`. `any generate Role` is now refused, naming `Any.Enum<Role>()` (#298). `RoleSet`'s guard on the field copy is now marked: `roles  IReadOnlyCollection<Role>  Any.ListOf(Any.Enum<Role>())  to verify, unread guards` (#297). `AnyRole.cs` and both `Bug_` tests are removed.
