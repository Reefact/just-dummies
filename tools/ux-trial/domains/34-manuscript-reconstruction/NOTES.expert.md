# Expert crafter notes — 34 manuscript reconstruction

## What I did
- Scaffolded all nine domain types in one call, with entry points:
  `any generate WitnessSiglum PassageReference Witness Variant Lacuna CopyRelation EditorialHypothesis AttestedReading Tradition --entry-point any --output ./Arbitrary`
  (Tradition in a second call). 8 of 9 files came out with deliberate `TODO_verify` lines.
- Refined every base in `AnyValid…()` and rewrote four `Generate()` bodies so the cross-parameter
  rules hold by construction (Witness dating, Lacuna bounds, CopyRelation exemplar != copy,
  Variant kind/text/hand, EditorialHypothesis conjecture <=> no witness).
- Added my own entry point `Any.Reading()` (a C# 14 `extension(Any)` block, same trick as the tool's):
  `Any.StringMatching("[a-z]{1,12}( [a-z]{1,12}){0,5}")` — already normalised, so drawn readings
  compare equal to what the domain stores.
- `Recipes.cs` holds the arrange vocabulary (`AnyTraditionWith`, `FirstHandReads`, `CorrectorWrites`,
  `Proposes`, `AnyStandaloneReading`, `AnyDistinctSigla`). `[assembly: Reproducible]`.
- 35 tests: Variant rules, recording/lacunae, alignment, hypotheses lifecycle, shared errors, stemma.
  Literals where the assertion is about the value (sigla A/B/C, readings, passages, datings that
  decide a refusal); drawn everything else. All pass; 25 consecutive runs green.

## Frictions and surprises, in order met
1. `WitnessSiglum`: `if (value.Length is 0 or > MaxLength)` is an "unread guard". A pattern-match
   length guard is common; I expected `.WithLengthBetween(1, 12)`.
2. The recap names only the *first* unread guard per parameter. The siglum's `[GeneratedRegex]`
   check was never mentioned; I found it only by reading the constructor. Documented, but a second
   line ("and N more") would have saved a read.
3. `ArgumentOutOfRangeException.ThrowIfGreaterThan(text.Length, MaxTextLength, nameof(text))` is unread
   (Variant text, EditorialHypothesis rationale). The tool reads `ThrowIfGreaterThan(hand, …)` on an int,
   so the `.Length` form looks like a natural `WithMaxLength` to infer.
4. `if (double.IsNaN(c) || c <= 0.0 || c > 1.0)` → `Any.Double()` (unbounded) + to verify. An OR of
   simple comparisons on one parameter seems readable.
5. Retyping `AnyValidValue()` to `AnyPattern` gave
   `error CS0029: Cannot implicitly convert type 'JustDummies.AnyPattern' to 'JustDummies.AnyString'`
   exactly as the CLI page predicts; the fix is the two lines it names. Good doc, but I hit it again on
   `EditorialHypothesis.proposedReading` because I reused `Any.Reading()` there — every factory that
   changes kind costs the same two edits.
6. The biggest one: rules *between* parameters. The doc's "derive inside Generate()" is right, but to
   honour pins I had to sniff whether a field was pinned or drawn with type tests
   (`_datedTo is AnyInt32 anyDatedTo ? anyDatedTo.GreaterThanOrEqualTo(from) : …`,
   `_text is AnyPattern ? null : _text.Generate()`) because `FixedValue<T>` is private and the
   fields are `IAny<T>`. It works and composes beautifully (a test's
   `WithDatedTo(to => to.GreaterThanOrEqualTo(exemplar.DatedFrom))` stacks with my derived bound),
   but it is brittle and `--force` wipes it. Wished for: a scaffolded hook such as
   `partial void` / `Func<TOther, IAny<T>>` per dependent parameter, or an `IsPinned` notion.
7. For `Lacuna` there is no way to say "a passage after this one" on `AnyPassageReference`
   (composed, not ordered), so two drawn bounds are swapped into order and a pinned one is taken as is.
8. JD030 (note) fires on the scaffold's own `Any.String().NotBlank()` rows (repository, editor, cause,
   work title). By design per the doc; only visible in a SARIF log, not in `dotnet build` output.
9. Generated types land in `ManuscriptReconstruction.Domain` (the target's namespace) inside the test
   assembly. Convenient (no import), slightly surprising; `--namespace` exists.

## What worked well
- `--entry-point any`: `Any.Witness().WithSiglum(A).Generate()` reads like the rest of the arrange.
- The deliberate non-compiling `TODO_verify` lines: I could not forget a single cross-parameter rule.
- Refinement lambdas over pins (`WithKind(k => k.DifferentFrom(Correction))`) — a named recipe
  `AnyStandaloneReading()` came for free.
- `Any.ArrayOf(Any.WitnessSiglum()).WithCount(n).Distinct()` on a scaffolded generator, no ceremony.
- `Any.StringMatching` for a philological reading shape; `AnyEnum` `DifferentFrom` + `OneOf` compose.
- `[assembly: Reproducible]` once, nothing per test. Zero false-positive analyzer warnings.

## Fluency: 4 / 5
The library itself never got in my way, and the scaffolder's honesty (stop the build, name the
guard) is right. One point off for the cross-parameter story: this domain has five such rules, and
each cost a hand-written, pin-sniffing `Generate()` that the next `--force` will erase.
