# Discovery notes — 47 international catalogue
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`; `guides/getting-started.en.md`; `guides/core-concepts.en.md`;
`packages/justdummies-cli.en.md` (almost whole: it is the longest page); `packages/justdummies-xunit.en.md`;
`generators/README.md`; `generators/strings.en.md` (whole: the domain is text); `generators/collections.en.md` and
`enums-and-choices.en.md` (first part only); `guides/composition.en.md`; `packages/justdummies.en.md` (analyzers section);
`analyzers/README.md` (index only). I did not read `reproducibility`, `errors-and-conflicts`, the FAQ or any `JDxxx` page.

Setup: wrote `InternationalCatalogue.Domain.Tests.Discovery.csproj` from the template, `dotnet restore`, then
`any generate Title PersonName Contributor Isbn13 Isbn10 Issn CallNumber RecordId LanguageTag ArticleRule BibliographicRecord
--output Generators --entry-point any` (11 types, one command; the entry-point files `Any.Title()` etc. came out of it).
`AnyContributor` needed no edit. Ten others carried `TODO(any)` sentinels and did not compile until I resolved them.

Generators completed by hand (all in `Tests.Discovery/Generators`, helpers in `Support/`):
* `AnyTitle`, `AnyPersonName`: rewritten around a per-script letter class (`Support/Scripts.cs`, drawn with
  `Any.StringMatching("[Α-Ρ...]")`), because `Any.String()` is ASCII only. `InScript(script)` pins the script.
* `AnyIsbn13`, `AnyIsbn10`, `AnyIssn`: `Any.StringMatching("[0-9]{9}").As(body => body + check)`, with the check character
  computed by my own implementation in `Support/CheckCharacters.cs` (not the domain's `ComputeCheckDigit`, which the tests exercise).
* `AnyCallNumber`, `AnyLanguageTag`: one `Any.StringMatching(...)` each. `AnyRecordId`: `Any.Combine(agency, Any.Int32().Between(1, 99_999_999), ...)`
  so that "not all zero" holds by construction.
* `AnyArticleRule`: bare language + `Any.SetOf(Any.StringMatching("[a-z]{1,8}'?")).WithCountBetween(1, 20)`.
* `AnyBibliographicRecord`: rewritten entirely; `Generate()` draws the script first, then title/subtitle/language from it, then the year,
  then an ISBN kind the year allows (978 from 1970, 979 from 2007). Pins: `WithTitle` (also settles the script), `WithSubtitle`, `WithoutSubtitle`,
  `WithContributors`, `WithLanguage`, `WithScript`, `WithEdition`, `WithPublicationYear`, `WithIsbn`, `WithoutIsbn`, `WithIssn`, `WithoutIssn`, `WithCallNumber`.

Tests written (14 test classes in 14 files, 258 test methods, 621 cases once theories are expanded, 5 methods skipped):
`TitleTests` (scripts, NFC, trim, grapheme length 250/251 incl. astral and multi-mark clusters, controls, emoji, unpaired surrogates,
bidi marks/isolates/embeddings, RTL, equality); `PersonNameTests` (scripts, heading, allowed punctuation, mixed scripts, given-names script, 100-cluster limit, equality);
`ContributorTests`; `Isbn13Tests`, `Isbn10Tests`, `IssnTests`, `CallNumberTests` (shelf order R884 < R89), `RecordIdTests`, `LanguageTagTests` (canonical case, `DeclaredScript` table);
`ArticleRuleTests` (longest first, elision, `’`, "kept whole" rule); `ListingCollationTests` (filing title, tie-breaks, sv/es/en collation, sort keys, order independence);
`BibliographicRecordTests` (every refusal, year/ISBN windows 1970/2007, script tie, `DescribesSameWorkAs`); `CatalogueTests` (name, every `DuplicateReason` and its precedence,
merge rules and atomicity, retired ids, withdraw, listing, rendering); `DomainDefectTests` (5 skipped, see below).

Result: `dotnet test --project Tests.Discovery` green on the first complete run (621 cases, 0 failed, 5 skipped), then 15 further runs
(3 + 12, each with fresh `[Reproducible]` seeds) all green. No `var`; I also raised every `JD001`..`JD038` to `warning` in an `.editorconfig`: no diagnostic fired on the finished code.
Red runs happened on the way (a wrong expected value in two Isbn10 cases, the surrogate theory, three listing tests), all my own test mistakes (see 9 and 10 below).

## Frictions and surprises, in order
1. **Links to pages the package consumer does not have.** `PROJECT-README.md` sends to `doc/handwritten/for-users/guides/getting-started.en.md`; the pages say
   "see ADR-0060", "specification §5.3", `../../for-maintainers/...`. Under `W/docs/` none of those targets exist. Expected: a user-facing page that is self-sufficient.
   Several explanations ("why a regex guard is never read", "why no `With…` takes a generator") end with an ADR link as their only justification. I read on without them.
2. **`dotnet test --project Tests.Discovery` does not work out of the box.** Exact output on SDK 10.0.114: `MSBUILD : error MSB1001: Unknown switch ... --project .`
   (`dotnet test` stayed in VSTest mode). The README quotes `dotnet test --project ...` / `--solution` for contributors and no user page explains that it needs
   `global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }` (xunit.v3 with `OutputType Exe`). I added that file in W and it worked.
3. **`any generate` before a restore.** `✗ Title: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear and actionable; nothing to change, but the CLI page's "Run it from your test project" does not say a restore comes first.
4. **Ten of eleven scaffolds do not compile on purpose.** Every value object here validates a string through a helper or a regex, so the recap printed `to verify, unread guards`
   and the build stopped on `TODO_verify_the_generator_for_value`. The sentinel comment names the file, line and first guard, which was useful ("`Issn.cs`, line 11: ... a regex ...
   retype AnyString to ..."). Cost: I ended up rewriting most of each file, not "verifying" it (see 5, 6).
5. **A replacement that is not a builder changes the file's types.** For the check-digit types I needed `Any.StringMatching(...).As(...)`; the docs say to retype the field, the recipe
   constructor and delete the refining overload. I deleted the `With…(Func<AnyString, AnyString>)` overloads and kept only the pin. Fine, but each file was a manual rewrite.
6. **Cross-parameter rules force a hand-written `Generate()`.** Script of the title, subtitle, language subtag and `script` parameter; year and ISBN prefix; both name parts in one script.
   The tool correctly marked all of them (`ties language to script`, `ties isbn to publicationYear`), and the doc's section "When a guard ties two parameters" says to write the tie in
   `Generate()`. That is what I did, and it means `AnyBibliographicRecord` and `AnyPersonName` lost the scaffold's `With…(Func)` refinements. Expected: some way to keep them when only `Generate()` changes;
   accepted: a re-scaffold would erase it (the doc warns).
7. **`Any.String()` is ASCII only, so no script is reachable without a pattern.** The strings page says so ("Nothing named reaches past ASCII"; `WithChars` is the escape hatch). For this domain
   `WithChars` is not enough: `Any.String().WithChars("𠀀𠀁")` throws `ArgumentException: The character pool must not contain a surrogate: an emoji or other astral code point spans two UTF-16 code units ...
   Draw such values as whole strings with OneOf(...) instead.` (good message), and the strings page never mentions astral characters. I used `Any.StringMatching("[Α-Ρ...]")`, which works for the BMP.
8. **`Any.StringMatching` accepts astral characters and draws ill-formed strings.** Probe: `Any.StringMatching("𠀀{3}").Generate()` gives the code units `D840 DC00 DC00 DC00`;
   `Any.StringMatching("[a𠀀]")` gave in 12 draws `DC00 DC00 DC00 D840 0061 DC00 D840 D840 0061 D840 D840 D840` (a lone surrogate 2 times in 3). `Any.StringMatching("[𠀀-𠀅]{3}")` fails with
   "character class range '\uDC00-\uD840' is out of order". So `WithChars` refuses surrogates and `StringMatching` silently emits them. I wanted a generated supplementary ideograph for the "counts once" test and ended
   with a literal (`"\U00020000"`) instead.
9. **A dummy that was not neutral, twice (my errors, caught by the draw).** `Titles_that_differ_only_by_case_fall_to_the_following_keys` failed because `Any.BibliographicRecord()` draws a subtitle one time in two
   (the scaffold's `Title?` default) and the subtitle is the next sort key; `A_listing_does_not_depend_on_the_order...` failed because the language was drawn and only `en` has the article rule. Both fixed by pinning
   (`WithoutSubtitle()`, `WithLanguage("en")`), i.e. those values were part of what the test was about. The failure output (`Collections differ`) does not say which field mattered; the seed line
   was there, and reading the sort rule was enough.
10. **xUnit attribute data cannot carry a lone surrogate.** `[InlineData("\uD800")]` was compiled to U+FFFD, so `Title` happily accepted it and the test failed with "No exception was thrown" and a display name `surrogate: "���"`.
    Not a JustDummies matter, but it cost a detour; I switched to `MemberData` with `char`s.
11. **Transport glitch, not the library:** U+2028/U+2029 typed in a source literal are line terminators for the C# compiler (`CS1010 Newline in constant`); I now only write them as escapes.
12. **`.AsNullable()` / `.OrNull()` on reference types.** I first wrote `new AnyTitle().AsNullable()` and got `CS0453: The type 'Title' must be a non-nullable value type in order to use it as parameter 'T' ... AsNullable<T>`
    (the page says value types, I read past it). Then `As(d => (Isbn13?)new Isbn13(d)).OrNull()` produced `CS8634 ... Nullability of type argument 'Isbn13?' doesn't match 'class' constraint`; plain `.OrNull()` on an `IAny<Isbn13>` is the right form.
13. **Default namespace of the scaffold is the domain's own.** The generators and `Any.Title()` live in `InternationalCatalogue.Domain` inside the test assembly. It compiles, and the doc mentions `--namespace`, but a test assembly declaring types in the production namespace felt wrong.
14. **Domain, not library (found by thinking about real catalogue data, not by a failing draw).** The model's script tables leave out letters real records need: U+3005 `々` (`人々`, `佐々木` -> "belongs to no supported script"),
    full-width Latin (`ＡＩ入門`), half-width katakana, Hangul compatibility jamo; a title of only `U+200B` / `U+200D` / `U+FEFF` is accepted (non-empty, not whitespace to `Trim()`); a Persian name cannot hold U+200C.
    Kept as 5 skipped, deterministic tests in `DomainDefectTests` (no seed involved).

## What worked well
* `any generate` over 11 types in one command, with an honest per-parameter table. Guards were read where they could be: `edition` -> `.Positive().LessThanOrEqualTo(999)`, `publicationYear` ->
  `GreaterThanOrEqualTo(BibliographicRecord.FirstPrintingYear)` (it used my public constant), `RecordId` -> `WithLength(10)`; composed types came out as `new AnyRecordId()`, `new AnyTitle()`, so `AnyContributor` worked untouched
  as soon as `AnyPersonName` compiled.
* The sentinel design: it never let a plausible-but-wrong generator through. The file/line in each comment sent me straight to the guard.
* `Any.StringMatching` accepting `\uXXXX` classes (it made scripts cheap to draw), `.As(...)`, `Any.Combine`, `.Then(...)` (random script then text of that script), `Any.SetOf(...).WithCountBetween`, `.Distinct()` on contributors.
* `[Reproducible]` at class level was invisible when green and, on the surrogate failure, printed the seed line with the exact attribute syntax to replay.
* The "if an `anyXxx` reaches your assertion it is not a dummy" rule made the line easy to hold: refusal tests draw the neutral parts (`anyPrefix`, `anySuffix`, a valid `anyFamilyName`) and keep the forbidden character, the boundary and the expected exception as literals.
* Analyzers stayed quiet on correct usage, and fire as documented on wrong usage (checked JD005/JD006/JD030 on a throw-away file, with severities raised in `.editorconfig`).

## Fluency: 4 / 5
The library itself was easy to pick up from `getting-started` + `core-concepts`: I never wrote a wrong constraint, and every red run was a mistake in my own expectations. What kept it from 5 is the domain's shape, not the DSL:
nearly everything here is a validated string tied to another parameter, so the scaffolder's role shrank to producing the skeleton, the `Any.Title()` entry points and the file layout while I hand-wrote the real generators (the docs say this is the design, and they say how);
non-ASCII text needs a pattern, astral characters are not drawable, and `StringMatching` can silently emit lone surrogates. Running the tests required a `global.json` that no user page mentions.
