# Expert notes — 19 IAM
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `PROJECT-README.md`, `guides/getting-started.en.md`, `packages/justdummies-cli.en.md` (what the last column means,
guard table, "Adjusting a parameter", "When a guard ties two parameters"), `packages/justdummies-xunit.en.md`, `generators/guids-and-uris.en.md`,
`strings.en.md` (patterns), `dates-and-times.en.md`, `enums-and-choices.en.md`, the floating-point part of `numbers.en.md`.

Commands: `dotnet restore` (the tool refuses to run before it), then
`any generate ClientId Scope RedirectUri TokenPolicy RoleAssignment ClientApplication AccessToken --output Generators --namespace Identity.Domain.Tests.Expert.Generators --entry-point any`.
Recap: 4 of the 7 files arrived "to verify" (Scope: regex; RedirectUri: `Validate(value)`; TokenPolicy: `ThrowIfGreaterThan(x, HardMaximumLifetime)`;
RoleAssignment and AccessToken: ties between two parameters; ClientApplication: the `HashSet.Count != Count` distinctness check).

Generators completed by hand (all `TODO(any)` lines removed, build green):
- `AnyScope`: `Any.StringMatching("[a-z][a-z0-9_]{1,31}:(read|write|admin)")`, retyped `AnyString` -> `AnyPattern` in the 3 places the TODO named (plain search/replace worked).
- `AnyRedirectUri`: `Any.Uri().Web().UsingHttps()`.
- `AnyTokenPolicy`: lifetime `Any.TimeSpan().Positive().LessThanOrEqualTo(TokenPolicy.HardMaximumLifetime)`.
- `AnyClientApplication`: redirect URIs `Any.ListOf(new AnyRedirectUri()).NonEmpty().Distinct()`.
- `AnyRoleAssignment`: rewrote the tail of the file: `validUntil` is no longer a parameter of the generator but "lasting days" (0 to 3650, or none), `Generate()` adds them to `validFrom`;
  kept `WithValidUntil(DateOnly?)` (pin), `WithoutValidUntil()`, added `LastingDays(int)`; dropped the lambda overload of `WithValidUntil`.
- `AnyAccessToken`: the scaffold had paired `issuedAt`/`expiresAt` with `Any.OrderedPair(..., strict: true)` (from `expiresAt <= issuedAt`), which does not honour the 24 h cap.
  I replaced the pair by `issuedAt` (2020..2040) + `lifetime` (`Positive().LessThanOrEqualTo(24 h)`), `Generate()` computes `expiresAt`; `WithExpiresAt` is replaced by `WithLifetime`.

Tests (xUnit v3, `[assembly: Reproducible]`): 121 methods (107 `[Fact]`, 14 `[Theory]`), 171 cases:
`ClientIdTests` (5), `ScopeTests` (4 methods, 29 cases: split/format/blank/null/equality), `RedirectUriTests` (13 methods: https, loopback http theory, other schemes, relative, null, fragment, check order, equality theories),
`TokenPolicyTests` (12 methods: boundaries 1 tick / 24 h / 1..20, refusals), `RolePermissionsTests` (1 + theories), `RoleAssignmentTests` (10: construction, refusals, inclusive both-ends `IsActiveOn`),
`ClientApplicationTests` (20: construction, name length 80/81, blank/null, empties, duplicates, `AcceptsRedirectTo` exactness, `MayRequest`),
`AccessTokenTests` (23: construction, refusals, 24 h boundary, expiry at/after, remaining lifetime, `Authorizes`), `TokenIssuerTests` (18: issued content, lifetime cap, scope refusal/maximum/ordering of checks).

Result: green, 168 passed + 3 skipped (3 domain defects, below). 1 initial run (one red, my own wrong expectation: `new Uri("https://x/cb#").Fragment` is `"#"`, so a bare hash IS refused),
then 15 further runs with fresh seeds, all green.

## Frictions and surprises, in order
1. `dotnet test --project Tests.Expert` -> `MSBUILD : error MSB1001: Unknown switch ... --project`; and `dotnet test Tests.Expert` ->
   `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later ... opt-in to the new dotnet test experience`.
   Expected the documented setup to run as is. The fix is a `global.json` (`{"test":{"runner":"Microsoft.Testing.Platform"}}`), which I added in W. Neither the xunit adapter page nor the
   getting-started page mentions it; `PROJECT-README.md` only says the repo's own `global.json` pins the SDK. A user on .NET 10 + xunit.v3 hits this before writing a test.
2. `any generate ...` before restore: `✗ ClientId: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`. Clear message, one extra command.
3. Scaffold noise for `Scope`: `Scope resolved to Identity.Domain.Scope; System.Formats.Asn1.AsnWriter.Scope also answers to that name.` Harmless but alarming on first read; I could not tell if it could pick the wrong type
   (it did not; the generated file compiles against the domain type).
4. `AccessToken`: the tool paired `issuedAt`/`expiresAt` as an `OrderedPair` (strict) from the `expiresAt <= issuedAt` guard AND flagged both "to verify" because of `expiresAt - issuedAt > TokenPolicy.HardMaximumLifetime`.
   Reading the file, the TODO sits on `AnyValidIssuedAt`/`AnyValidExpiresAt`, which return a plain `Any.DateTimeOffset()`; but the real work is in the pair built in the constructor, which has no place to say "within 24 h".
   There is no `OrderedPair` option for a maximum gap. I had to remove the pair and rewrite `Generate()` (the doc's "draw a start and a duration" recipe) which deletes `WithExpiresAt` from the generator's surface and
   leaves ~60 lines of scaffold edited by hand. A re-scaffold with `--force` would lose all of it (the doc says so). It would help if the doc's recipe came with a sample on a scaffolded pair (what to delete, what to keep).
5. `RoleAssignment`: `if (validUntil is DateOnly end && end < validFrom)` is the idiomatic way to guard an optional end date, and the tool does not read `&&` patterns (documented), so both parameters are "to verify"
   and the scaffold is `Any.DateOnly()` x 2 with `.OrNull()`. Same outcome as 4: I had to rewrite `validUntil` as a duration and `Generate()` by hand. The generated `With...` for `validUntil` (lambda variant) would have been unsafe
   to keep, because a refinement on a derived parameter no longer reaches the draw; I deleted it. The doc warns of exactly this ("a parameter you now derive no longer draws from its field") - good, but easy to miss when the
   scaffolded `With...` methods stay in the file looking functional.
6. A pin that depends on a tie is a trap. With my `validFrom` random and `WithValidUntil(date)` pinned, the order holds only on some seeds; I made every such test pin both ends (`WithValidFrom(FirstDay).WithValidUntil(LastDay)`).
   The generated pin's summary does not warn about it; I wrote the warning in my own `<summary>`.
7. `null` pins need a cast: `Any.ClientApplication().WithDisplayName((string)null!)`, `WithRedirectUris((IReadOnlyCollection<RedirectUri>)null!)`, `WithId((ClientId)null!)` - because of the pin/lambda overloads. Documented
   in the CLI page for nullable pins; here it applies to refusal tests of non-nullable parameters, which read less well than constructor calls. For ArgumentNull tests of a generated object I find `Any.X().WithY((T)null!)`
   still preferable to hand-building the other five arguments.
8. A lambda refinement cannot cross the domain's boundary, so "a display name of 81 characters" needs the value drawn outside (`Any.String().NotBlank().WithLength(81).Generate()`) then pinned (the base says `WithMaxLength(80)`).
   Not a bug: the tool's design. I did like that `name => name.WithLength(80)` worked over the base `WithMaxLength(80)` (a bound is not a "declared once" constraint).
9. Analyzer feedback was silent: no JDxxx warning on the build at all (Info rules like JD030 are hidden). The first run of `dotnet build --no-incremental` printed nothing. I never found out whether my `Any.Int32().Negative()` or
   `Any.TimeSpan().GreaterThan(24h)` usages were "good" or merely unchallenged. (Not a complaint about the library: documented; but the trial could not exercise analyzers.)
10. Seeds: no test failed on a drawn value in 16 runs, so the replay path was exercised only once by accident (my wrong-expectation failure printed
    `[JustDummies] These arbitrary values were seeded with 73540804. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 73540804)].` - the message is precise and appears under "Standard output" of the failed test).
11. Domain observations (all kept as tests): see findings. `Scope` accepts `"orders:read\n"` (the MODEL.md documents the regex `$` but not that `Action` becomes `"read\n"`); `ClientApplication.AcceptsRedirectTo(null)` throws
    `NullReferenceException`; `RedirectUris` / `AllowedScopes` (and `AccessToken.Scopes`) are the live `HashSet`s, so `((ICollection<Scope>)client.AllowedScopes).Add(...)` widens a registered client.
    `TokenIssuer.Issue` says "the first one the client may not request" but a `HashSet` order is unspecified, so I only test one disallowed scope at a time.

## What worked well
- The scaffold's `Without`/pin/lambda triad and the `AnyValid...()` base made the arrange blocks short and honest: `Any.ClientApplication().WithRedirectUris(new[] { AppCallback }).Generate()` says exactly which part the test cares about, and
  the unconstrained rest (id, name, scopes) is visibly arbitrary.
- `any` read most constructor guards correctly: `Guid.Empty` -> `NonEmpty()`, `ThrowIfGreaterThan(displayName.Length, 80)` -> `WithMaxLength(80)`, scopes count `Positive().LessThanOrEqualTo(20)`, and wired composed types by their own generators
  (`new AnyClientId()`, `new AnyScope()`).
- The "to verify" mechanism: each of the 6 unreadable guards stopped the build at the right line, with the guard quoted. For the regex it even named the three places to retype - which is exactly what I needed.
- `Any.Uri().Web().UsingHttps().WithFragment()` / `.UsingHttp().WithHost(...)` / `.Ftp()` / `.Mailto()` / `.Relative()` made the RedirectUri refusal tests read as the domain rules they are, and a fragment/relative draw is invalid *by construction* rather than by luck.
- `Any.Guid().NonEmpty().DifferentFrom(x)`, `Any.Enum<Role>().DifferentFrom(...)`, `Any.TimeSpan().Negative().GreaterThan(...)` kept every "refused" input arbitrary but provably on the right side of the boundary.
- `[assembly: Reproducible]` is one line and the failing-test output carries the seed.

## Fluency: 4 / 5
Writing the 121 tests was fast and the generated API read well; the dummies/literals split came naturally (dates, scopes, URIs under test are literals; ids, names, unrelated scopes and policies are drawn). I lose a point for the two aggregates whose
invariant ties parameters (AccessToken, RoleAssignment): the scaffold got me to a compiling-but-wrong or blocked file and the correct fix was a manual rewrite of `Generate()` and of the `With...` surface, with no sample starting from a scaffolded file,
and for the undocumented `global.json` step needed before the first test can even run on the .NET 10 SDK.
