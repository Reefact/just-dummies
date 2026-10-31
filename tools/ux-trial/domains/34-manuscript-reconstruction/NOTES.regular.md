# Manuscript reconstruction: notes from the regular developer

## What I did

- Created `Tests.Regular`, restored, then scaffolded everything in one call:
  `any generate WitnessSiglum PassageReference Witness Variant Lacuna CopyRelation EditorialHypothesis --entry-point static:Arbitrary --output ./Arbitrary`,
  plus `Tradition` afterwards.
- Resolved the 14 `to verify` markers in the `AnyValid…()` factories (no other generated line touched
  except retyping where the builder kind changed), added `[assembly: Reproducible]`, a small
  `Traditions` helper (`WithWitnesses`, `Reads`, `Proposes`) and 35 tests: value objects (parse,
  ordering, normalisation, omission/correction rules, lacuna coverage, dating) and the `Tradition`
  aggregate (lacunae vs readings, hands and corrections, final reading, alignment with provenance,
  propose/accept/withdraw/reject, competing readings, shared errors, stemma: dating, cycle, single
  exemplar, ancestors).
- `dotnet test --project Tests.Regular`: 35/35 green, and green on 15 consecutive re-runs (fresh seeds).

## Frictions and surprises, in order

1. **Length guards through BCL helpers are not read.** `ThrowIfGreaterThan(hand, MaxHand)` became
   `.LessThanOrEqualTo(9)`, but `ThrowIfGreaterThan(text.Length, MaxTextLength, nameof(text))` and the
   same on `rationale.Length` came out `to verify, unread guards`. Same for
   `if (value.Length is 0 or > MaxLength)` on the siglum. These are the most common .NET 8 string
   guards; I had to write `WithMaxLength(...)` by hand each time.
2. **A compound bound is not read either:** `if (double.IsNaN(c) || c <= 0.0 || c > 1.0)` →
   `Any.Double()` + `to verify`. I wrote `Any.Double().GreaterThan(0.0).LessThanOrEqualTo(1.0)`.
3. **Cross-parameter rules are the bulk of the work** (Witness from<=to, Lacuna from<=to, CopyRelation
   exemplar!=copy, Variant kind/text/hand, Hypothesis conjecture/witnesses: 5 of 8 types). Expected
   and documented, and the marker lines pointing at the exact guard line were genuinely useful.
   But of the three documented forms, "derive one draw from another in `Generate()`" breaks the pins
   (`WithDatedTo(...)` no longer means anything), so I split the domains instead: datedFrom in
   -800..400, datedTo in 400..1600; lacuna `from` in books 1-49, `to` in 50-99. It holds by
   construction, but the trap is silent: `Arbitrary.Witness().WithDatedFrom(1500).Generate()` now
   fails on most seeds, not all. Nothing in the docs warns that a pin on one side of a
   cross-parameter rule must come with the other. I always pin both, which is fine for my tests.
4. **Partitioning a composed type is awkward.** For exemplar != copy I wanted "a siglum starting A-M".
   `AnyWitnessSiglum.WithValue(Func<AnyPattern, AnyPattern>)` only offers OneOf/Except (documented: no
   pattern narrowing), so I bypassed the type's own generator:
   `Any.StringMatching("[A-M][A-Za-z0-9.]{0,11}").As(v => new WitnessSiglum(v))`, then deleted the two
   refining overloads by hand. This duplicates the siglum recipe that ADR-0089 says belongs to
   `AnyWitnessSiglum`.
5. **Retyping a factory**: replacing `Any.String().NonEmpty()` with `Any.StringMatching(...)` gives
   CS0029 until the factory's return type *and* the `Func<…>` of the refining overload are retyped.
   Documented clearly; still two edits that the tool's comment could name.
6. **Pins on value objects are verbose**: `.WithWitness(new WitnessSiglum("A"))` everywhere. I added
   test helpers rather than a `WithWitness(string)` overload, but it is noise in most tests.
7. Minor: Info-level analyzers (JD030 on the remaining `NotBlank()` rows) never show in a CLI build,
   so I only know of them from the docs.

## What worked really well

- One `any generate` call for seven types, with `--output` and `--entry-point static:Arbitrary`:
  `Arbitrary.Variant()` reads well next to `Any.Int32()`. Composed parameters were wired through
  `new AnyPassageReference()` / `new AnyWitnessSiglum()` without me asking.
- "The file will not compile until you resolve it" is the right call: every marker named the
  guard and the line, so I fixed bases in minutes instead of discovering them via flaky tests.
- Refining lambdas are the daily win: `.WithHand(h => h.GreaterThan(Variant.FirstHand))` for a
  correction keeps 1..9 from the base and reads exactly like the rule.
- Pins taking collection expressions (`.WithSupportingWitnesses([])`) for `IReadOnlyCollection<T>`.
- `[assembly: Reproducible]` once, and forget about seeds; no flake in 15 runs.
- Guard-derived rows (`Positive().LessThanOrEqualTo(999)`, `Guid().NonEmpty()`, `NotBlank()`) were
  right every time they were not flagged.

## Fluency: 4 / 5

Scaffold-then-refine is fast and the result reads well. One point off because cross-parameter
invariants (most of this domain) and plain `.Length` guards stay entirely manual, and the
split-domain workaround makes some single-side pins silently seed-dependent.
