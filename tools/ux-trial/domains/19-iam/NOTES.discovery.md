# Discovery notes — 19 IAM
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Read, in order: `MODEL.md` and the nine domain files; `docs/PROJECT-README.md`; `docs/README.md`; `guides/getting-started.en.md`;
  `guides/core-concepts.en.md`; `packages/justdummies-cli.en.md` (all of it, it is long); `packages/justdummies-xunit.en.md`;
  `generators/README.md`; `generators/guids-and-uris.en.md`; `generators/dates-and-times.en.md`; the Patterns part of
  `generators/strings.en.md`; `guides/composition.en.md` (to the end of `.AsNullable()`); the Distinctness part of `generators/collections.en.md`;
  `guides/reproducibility.en.md` (first part).
- Created `Identity.Tests.Discovery.csproj` from the template, `dotnet restore`, then
  `any generate ClientId Scope RedirectUri TokenPolicy RoleAssignment ClientApplication AccessToken` (one command, seven files).
  `Role`, `Permission`, `TokenIssuer`, `ScopeNotAllowedException` were not scaffolded (enum / no value to build).
- Generators completed by hand (the build was stopped by `TODO_verify_the_generator_for_*` on five of the seven files, as announced):
  - `AnyScope`: `Any.StringMatching("[a-z][a-z0-9_]{1,31}:(read|write|admin)")`, retyped `AnyString` to `AnyPattern` on the 4 places the TODO named.
  - `AnyRedirectUri`: `Any.Uri().Web().UsingHttps()`.
  - `AnyTokenPolicy`: lifetime `Between(1 s, TokenPolicy.HardMaximumLifetime)`.
  - `AnyRoleAssignment`: `validFrom` bounded to 2000..2100, and a hand-written `Generate()` that draws `validUntil` with `AfterOrEqualTo(validFrom).OrNull()`.
  - `AnyClientApplication`: `.Distinct()` added on the list of redirect URIs.
  - `AnyAccessToken`: both instants bounded to 2000..2100, and a clamp in `Generate()` for the 24 h cap (the scaffolded `OrderedPair` already guaranteed `expiresAt > issuedAt`).
- Tests (101 methods, 187 cases, 4 skipped on domain defects), all with `[assembly: Reproducible]`:
  `ClientIdTests` (6), `ScopeTests` (8: format Theory with 6 valid / 17 malformed inputs, null, blank, Resource/Action, equality, skipped defect),
  `RedirectUriTests` (13: https/loopback http Theory, null, relative / non-loopback http / foreign schemes / fragment drawn with `Any.Uri()`, equality incl. case),
  `TokenPolicyTests` (5 methods, boundary Theories on lifetime ticks and scope limit), `RolePermissionsTests` (4),
  `RoleAssignmentTests` (8: refusals, one-day grant, inclusive boundaries Theories), `ClientApplicationTests` (19: every refusal, duplicate URIs, scope collapsing,
  `AcceptsRedirectTo` exact-match Theory of 15 candidates, `MayRequest`, 2 skipped defects),
  `AccessTokenTests` (20: each refusal, 24 h boundary to the tick, offsets, expiry / remaining lifetime / `Authorizes` boundaries),
  `TokenIssuerTests` (18: cap, scope refusal, count refusal and their order, non-positive lifetimes, one skipped defect).
- Dummies are used where a value must exist and does not matter: the scope limit when testing the lifetime of a policy (and the reverse), the rest of a
  `ClientApplication` when one of its fields is made invalid (`new AnyClientApplication().WithDisplayName(...)`), the non-loopback host of an `http` URI,
  the path of a relative URI, ids and scopes when testing token dates. Everything an assertion is about (dates, ticks, scope texts, URIs) is a literal.
- Result: green with `dotnet test --project Tests.Discovery` (183 passed, 4 skipped), then **11 more runs** (fresh seeds, `--no-build`), all green.
  A temporary 20 000-iteration loop of every generator and of `Any.Uri().Web().UsingHttp()` (never loopback) / `UsingHttps().WithFragment()` (fragment always present) found nothing; removed.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not run on a .NET 10 SDK out of the box.** Exact output:
   `MSBUILD : error MSB1001: Unknown switch. ... --project Tests.Discovery`, and with `dotnet test Tests.Discovery`:
   `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`
   Nothing in the docs mentions it: the README only says `dotnet test --solution ...` and that the repository pins its SDK in `global.json` (a file a consumer does not have).
   The `JustDummies.Xunit` page says "xUnit v3 4.0.2 or later" and nothing about the runner opt-in. I created `W/global.json` with
   `{"test":{"runner":"Microsoft.Testing.Platform"}}` and it worked at once. Expected: the xunit adapter page (or getting started) to list the three things a fresh xUnit v3 test project needs on .NET 10.
2. **`any generate` before `dotnet restore`.** `✗ ClientId: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear and actionable; I only note that getting-started never shows the tool, and the CLI page's install/"What it produces" does not say "restore first". Fixed by `dotnet restore`.
3. **Where is `any` in the beginner path?** `getting-started` ends with a "scaffold a generator" row in its table; the PROJECT-README shows `dotnet tool install --global`. In this trial the tool was given as a path, so no issue, but the
   CLI page is 900 lines; I read it to the end before running anything, because the recap column (`to verify`, `unread guards`, `guard`) is only explained there. A short "your first `any generate` in 5 lines" block at the top would have saved reading.
4. **`Scope resolved to Identity.Domain.Scope; System.Formats.Asn1.AsnWriter.Scope also answers to that name.`** printed in the recap. Harmless and correct (domain wins), but at first read it looks like a warning I must act on. Nothing in the docs explains this line (the CLI page mentions it only in the "Options" paragraph).
5. **Regex guard marked `to verify` with a very good instruction, and it worked.** The TODO comment named the 4 places to retype (`AnyString` to `AnyPattern`, field, constructor parameter, `Func` of `WithValue`); I did exactly that and the file built on the first try.
   Small thing: the recap row shows `Any.String().NotBlank()` as the inferred expression, and the generated `AnyValidValue()` returns it, but the pattern itself is only in the comment inside the file, not in the recap (the recap line says only `if (!ScopeFormat().IsMatch(value)) { ...`, the regex is hidden behind a `[GeneratedRegex]` method, so the tool could not quote it in the recap). I had to open `Scope.cs`.
6. **`TokenPolicy.HardMaximumLifetime` is `public static readonly`, not `const`, so `ThrowIfGreaterThan(maximumLifetime, HardMaximumLifetime)` is an `unread guard`**
   (`maximumLifetime  TimeSpan  Any.TimeSpan().GreaterThan(TimeSpan.Zero)  to verify, guard, unread guards`). Good that the build stops: the inferred recipe, left alone, would have drawn TimeSpans of millions of days that the constructor refuses.
   The docs say a guard against "a public constant" is referenced by name, but do not say that a `static readonly` field (the only way to write a `TimeSpan` constant) is not a constant for the tool. I wrote
   `Between(TimeSpan.FromSeconds(1), TokenPolicy.HardMaximumLifetime)` by hand. Idea: read `public static readonly` fields initialised with a literal factory.
7. **`validUntil is DateOnly end && end < validFrom` is not recognised as an order between two parameters** (`validFrom: unread guard ... — ties validUntil to validFrom`). The docs list `a > b`, `a >= b` forms and mention `&&` is "not read".
   Expected from the docs; but this nullable-optional-end shape is very common (`DateOnly? validUntil`) and `Any.OrderedPair` has no nullable form, so I wrote `Generate()` by hand
   (`_validUntilRecipe.AfterOrEqualTo(validFrom).OrNull()`). Consequence I accepted knowingly (the docs warn): `WithValidUntil(...)` pins no longer reach the draw, and nothing tells me that at compile time.
8. **The scaffold paired `issuedAt`/`expiresAt` through `Any.OrderedPair(..., strict: true)` (good), but the third rule (lifetime ≤ 24 h) cannot be expressed on the pair.** The recap still said `to verify`, correctly. I bounded both instants to 2000..2100 and
   clamped `expiresAt` to `issuedAt + 24 h` in `Generate()`. This works, but a clamp quietly overrides a pinned `WithExpiresAt(...)` that lies further than 24 h from `issuedAt`. I found no documented "maximum gap" on `OrderedPair`;
   the docs suggest "a start and a duration" instead, which I could not use without giving up the generated `WithIssuedAt`/`WithExpiresAt` pins. Idea: `OrderedPair(..., maxGap:)`.
9. **Pinning an invalid pair through the scaffolded `With…` throws the library's exception, not the domain's.** `new AnyAccessToken().WithIssuedAt(t0).WithExpiresAt(t0.AddMinutes(-90))` throws at the second call:
   `ConflictingAnyConstraintException : Cannot apply WithSecond(03/01/2026 09:30:00 +02:00) because WithFirst(03/01/2026 09:00:00 +00:00) already requires values greater than 03/01/2026 09:00:00 +00:00.`
   The docs do say "two pins in the wrong order are refused at the second call", I just did not connect it to my "token expiring before issue" test. I rewrote those tests with `new AccessToken(...)` and dummy parts from `Any.Guid().NonEmpty()`, `new AnyClientId()`, `Any.ListOf(new AnyScope()).NonEmpty()`.
   The message is accurate but speaks of `WithSecond`/`WithFirst`, which are not names on the generator I called (`WithExpiresAt`/`WithIssuedAt`); the exception does tell the stack, so it was easy to trace.
10. **Passing `null` to a pin needs a cast.** `.WithId(null)` is ambiguous between the pin and the `Func` overload; `(ClientId)null!` and `(IReadOnlyCollection<RedirectUri>)null!` worked. The CLI page says it (in the nullable section), I met it before reading it there. Compiler error (`CS0121`) is the usual one and does not point at the doc.
11. **"If an `anyXxx` reaches your assertion it is not a dummy" made me use many more literals than I expected**, notably in every "a client exposes what it was registered with" / "a token is issued to the client and subject asking" test:
    pass-through tests have the value reach the assertion by nature. I used named literals (`Registered`, `Subject`...), and kept dummies for the parts around them. It reads well, but there is no guidance for the middle case
    (a value the code only stores and returns): the docs call it a property with a sample of one. I followed that.
12. **No analyzer diagnostic fired at all**, on ~190 lines of arrange code (0 warnings in the build). I would have liked to see at least `JD030` (Info, string without length) once to know the analyzers were alive, but the docs state Info is invisible in `dotnet build`. Not a defect; just that I could not tell the analyzers were running from my side.
13. **`Any.Uri().Relative()` without segments is the empty reference** (JD026 documented): I used `.WithPathSegments(2)` straight away because the page says so. No hesitation there.
14. **Documentation links.** `PROJECT-README.md` links to `doc/handwritten/for-users/...`, and every page links to `../../for-maintainers/adr/...` and `../../../CONTRIBUTING.md`; none of those exist in the `docs/` folder I was given (`docs/guides`, `docs/generators`...). The relative links between user pages work.
    Maybe just the layout of this trial; noted because a newcomer reading the offline copy hits dead links on the very first page.

## What worked well
- The `any` recap table is excellent: the `guard` / `AnyX` / `to verify` / `unread guards` words, and the line under it naming the file, the line and the statement. Every one of the five `to verify` marks was a real gap (regex, `static readonly` bound, tie between instants, distinct URIs). Nothing was drawn behind my back.
- A non-compiling `TODO_verify_...` as the way to force the review: it felt heavy for a second, then obviously right.
- `Any.Guid().NonEmpty()` was inferred from `== Guid.Empty`; `Any.ListOf(new AnyScope()).NonEmpty()` composed through the generator the type owns; `displayName` got `NotBlank().WithMaxLength(80)` from the three throw helpers.
- `new AnyClientApplication().WithDisplayName(new string('n', 81)).Generate()` is a very pleasant way to write a refusal test: only the field under test is stated, the rest is valid by construction.
- `Any.OrderedPair(Any.DateOnly(), Any.DateOnly(), strict: true)` gave me "an earlier and a later date" in one line for the "ends before it starts" test.
- `[assembly: Reproducible]` in one file; the failure output printed the exact replay attribute (`[JustDummies.Xunit.Reproducible(Seed = 1542030207)]`) the one time a test failed (friction 9).
- The `Between(1, 20)`-style constraints in the docs map one to one to domain invariants; I never needed to fight the vocabulary (`Except` on `Int32` for an undefined enum value was the first guess and it existed).
- Draws were stable: 12 green runs plus a 20 000-draw loop, no flake.

## Fluency: 4 / 5
I had the first scaffolded generators compiling and the first test green within a few minutes, and the docs answered nearly every question (patterns, URIs, distinctness, dates). Points lost for the .NET 10 / Microsoft.Testing.Platform wall
(friction 1, unguessable from the docs), for the length of the CLI page a newcomer must read before the recap makes sense, and for three places where I had to hand-write `Generate()` (optional end date, 24 h gap, `static readonly` bound) with a silent loss of pin semantics.
