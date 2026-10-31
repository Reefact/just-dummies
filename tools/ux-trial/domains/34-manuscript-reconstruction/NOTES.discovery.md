# Discovery notes — 34 manuscript reconstruction

## What I did
- Read MODEL.md and the domain, then getting-started, the CLI page (`justdummies-cli.en.md`), the
  strings / numbers / collections references, core-concepts ("Arbitrary does not mean dummy") and the xUnit page.
- Created the test project, restored, and scaffolded seven types in one command:
  `any generate WitnessSiglum PassageReference Witness Variant Lacuna CopyRelation EditorialHypothesis --entry-point any`.
- Built once on purpose to see the 13 `TODO_verify_the_generator_for_*` CS0103 errors, then resolved each.
- Wrote 31 tests (recording readings, lacunae, alignment, hypotheses, filiation), `[assembly: Reproducible]`.
  All pass; 15 further runs with fresh seeds: 0 failures.

## Frictions and surprises, in order
1. Recap for `WitnessSiglum`: `to verify, unread guards` at line 13, `if (value.Length is 0 or > MaxLength)`.
   A plain C# pattern guard on a length is not read. Only the *first* unread guard is named; the regex
   guard on line 18 (the stricter one) is not mentioned in the recap or the TODO comment. A newcomer who
   fixes only the length (`WithMaxLength(12)`) gets a constructor exception on some runs.
2. `ArgumentOutOfRangeException.ThrowIfGreaterThan(text.Length, MaxTextLength, nameof(text))` is unread too
   (Variant.text, EditorialHypothesis.rationale), whereas `ThrowIfGreaterThan(book, MaxBook)` on an int is
   read perfectly (`Positive().LessThanOrEqualTo(99)`). The BCL helper over `.Length` is a common shape.
3. `confidence` came out as bare `Any.Double()` (to verify) for
   `if (double.IsNaN(c) || c <= 0.0 || c > 1.0)`. Correctly flagged, but the bounds were readable.
4. Generated files are in the *domain* namespace (`ManuscriptReconstruction.Domain`) inside my test
   project. Documented (`--namespace` default), but surprising at first; it does save a `using`.
5. Cross-parameter guards (Witness from<=to, Lacuna from<=to, CopyRelation exemplar!=copy, Variant
   kind/text/hand, EditorialHypothesis conjecture/witnesses): the CLI page's advice "derive one draw from
   another inside Generate()" works, but it breaks the scaffolded pin: if Generate() computes `datedTo`
   from `datedFrom`, `WithDatedTo(1300)` silently stops mattering. I made the derived field nullable
   (null = derive, otherwise pinned/refined) in three files. Workable, but nothing in the docs shows how
   to keep both the derivation and the `With…` pin; I had to invent the pattern.
6. For the conjecture/witnesses tie I added `AnyEditorialHypothesis.Custom.cs` with `AsConjecture()`,
   following "Adding the overload yourself" — that section was exactly what I needed (praise).
7. Swapping `Any.String().NonEmpty()` for `Any.StringMatching(...)` broke the build with the type
   change, as the doc warned; the "two other lines" recipe was accurate.
8. Hesitation on the dummy rule: sigla and passages are identity handles. When they reach the assertion
   (`AncestorsOf` order, `SharedErrors` result) the guide says "an any in the assertion is a property".
   I switched those two tests to literals ("O", "P", "Vat.3225", 1.1.3). The guide never discusses
   identifiers / keys, which are the most common thing a domain test compares; a sentence would help.
9. Scaffolded defaults: `Any.Int32().Positive()` (folio count up to 2^31) and
   `Any.String().NotBlank()` with no length cap for a repository name. Legal but odd-looking; I capped them.
   JD030 is Info, so nothing showed up in a CLI `dotnet build` — I only know it exists from the docs.
10. No error message from the library itself was met: every draw I wrote was valid first time.

## What worked well
- One command scaffolded seven types, and `AnyX` composition (`new AnyWitnessSiglum()` inside AnyVariant,
  AnyLacuna...) wired the graph without me touching it.
- The TODO-that-does-not-compile is excellent: each error lands on the exact factory with the guard
  quoted. I could not forget one.
- `--entry-point any`: `Any.Variant().WithWitness(s).WithKind(...)` reads like the domain.
- Refinement lambdas composing with the base (`WithHand(h => h.GreaterThan(Variant.FirstHand))` on
  `Between(1, 9)`) and `Any.Enum<T>().Except(...)`, `ListOf(...).Distinct()` over a value object.
- `[assembly: Reproducible]` — one line, then forget about seeds.

## Fluency: 4 / 5
Productive within an hour, and the scaffolder did most of the typing. Lost a point for the
cross-parameter guards (five of seven types), where the documented recipe conflicts with the scaffolded
pins and I had to design my own pattern, and for unread guards that hide the stricter regex guard.
