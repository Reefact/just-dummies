# Expert notes — 47 international catalogue
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `PROJECT-README.md`, `README.md` (index), `guides/getting-started`, `guides/core-concepts` (skimmed),
`packages/justdummies-cli` (all of it: it is the page that matters for this domain), `generators/README`,
`guides/composition`, `generators/strings` (alphabet, patterns), `packages/justdummies-xunit`, `packages/justdummies` (analyzer severities).
Then the domain: `MODEL.md` and all 17 source files (to find the rules the tests must pin down).

`any` commands run (from `Tests.Expert`, after `dotnet restore`; the first run before the restore printed the clear
"this project has not been restored" message):

    any generate LanguageTag Title PersonName Contributor Isbn13 Isbn10 Issn CallNumber RecordId ArticleRule \
        ListingCollation BibliographicRecord Catalogue --output Generators --entry-point any
    any generate CultureInfo --dry-run        # only to understand what `AnyCultureInfo` in the recap meant

Result: 13 scaffolds, 1855 lines, 10 of 13 not compiling on purpose (`to verify` / missing `AnyCultureInfo`).

Generators completed by hand (`Tests.Expert/Generators/`):
* kept the scaffold and completed it: `AnyBibliographicRecord` (TODOs resolved, `Generate()` rewritten, six "pinned" flags
  added to the `With...` methods), `AnyContributor` (compiled as scaffolded once `AnyPersonName` existed), `AnyCallNumber`
  (retyped to `AnyPattern` exactly as the CLI page describes), `AnyIssn` (retyped to `IAny<string>`, refining overload
  deleted, check character appended with `.As`);
* replaced by a small hand-written generator of the same name (the scaffold shape did not fit): `AnyTitle`,
  `AnyPersonName`, `AnyLanguageTag`, `AnyIsbn13`, `AnyIsbn10`, `AnyRecordId`, `AnyCatalogue`, `AnyArticleRule`,
  `AnyListingCollation`; plus `ScriptTexts` (what each of the seven scripts draws its letters from).
  The scaffolded `.Entry.cs` files were kept, so tests read `Any.Title()`, `Any.BibliographicRecord()`...

Tests (`Tests.Expert/Tests/`): 322 test methods (236 facts, 86 theories), 723 cases, 5 skipped as domain defects.
Files: `TitleTests` (normalisation, trim, graphemes, controls, emoji, bidi controls/isolates, script ranges edge by edge,
RTL, equality), `PersonNameTests`, `ContributorTests`, `Isbn13Tests`, `Isbn10Tests`, `IssnTests`, `CallNumberTests`
(parsing, shelf order), `RecordIdTests`, `LanguageTagTests`, `ArticleRuleTests` (Strip), `ListingCollationTests`
(filing title, tie-breaks, de/sv ordering, sort keys), `BibliographicRecordTests` (every constructor rule, same-work
comparison), `CatalogueNameTests`, `CatalogueAddTests` (each duplicate reason and the order they are checked in),
`CatalogueMergeTests`, `CatalogueLookupTests` (withdraw, find), `CatalogueListingTests` (listing, rendering),
`GeneratorSanityTests` (the generators are mine, every other test trusts them), `DomainRealismTests` (skipped).
`[assembly: Reproducible]` makes every test replayable.

Result: green with `dotnet test --project Tests.Expert` (723 cases, 718 passed, 5 skipped). Re-run 33 more times
(3 by hand, then a loop of 30 with `--no-build`): 33/33 green, no failure on any drawn value. Build is warning-free with
every `JD0xx` rule raised to `warning` in `.editorconfig` (I checked the setting is live by compiling a probe that
trips JD030, then deleted it).

## Frictions and surprises, in order
1. `dotnet test --project Tests.Expert` (the command of the brief) fails at once on a .NET 10 SDK:
   `MSBUILD : error MSB1001: Unknown switch ... --project .` The SDK needs `global.json` with
   `{ "test": { "runner": "Microsoft.Testing.Platform" } }` to accept `--project`. Nothing in the JustDummies.Xunit page says
   so (it only states the `xunit.v3` 4.0.2 floor); I put the file in W. Not the library's fault, but the adapter page is where
   a user would look.
2. `any generate` right after creating the csproj: `✗ LanguageTag: this project has not been restored, so its package references
   cannot be read. Restore it, then run the command again: dotnet restore`. Clear and actionable; I ran `dotnet restore`.
3. The recap for the 13 types was long and, for this domain, almost always the same sentence. Every value object whose
   guards live in a helper or a regex (`Isbn13` via `RemoveHyphens`, `LanguageTag`, `Issn`, `CallNumber`, `RecordId`,
   `Title`, `PersonName`, `Catalogue`) came out `Any.String()` + `to verify, unread guards`, and the file does not compile
   until I replace it. That is the documented design and the recipes are right to refuse to guess; but for a string-wrapping
   value object the guard is always "the factory validates", so 9 of 13 files ended up replaced rather than completed.
   The `to verify` comment on `Issn` names the regex and suggests `AnyPattern` ("retype ... on the recipe field, in the
   recipe constructor and in the Func of WithValue"), whereas the guard that really matters there (the check character,
   "and 1 more") cannot be a pattern at all: the route is `Any.StringMatching(...).As(...)`, an `IAny<string>`, and
   the CLI page documents that, the comment does not. I followed the page, not the comment.
4. Retyping to `AnyPattern` is mechanical but touches four places; I did it with a search and replace of `AnyString`, which
   also rewrote the XML `<remarks>` of the refining overload. A refining overload on an `AnyPattern` is then of little use
   (`AnyPattern` has `OneOf`/`Except`/`DifferentFrom` only), yet it stays in the generated file.
5. Cross-parameter ties are the heart of `BibliographicRecord` and the scaffold names them: `language ... — ties language to
   script`, `isbn ... — ties isbn to publicationYear`, plus the title/script check it could only call "unread guard". Good
   diagnosis, but the only way out is to rewrite `Generate()`, after which a pin no longer reaches a draw and a refining
   lambda no longer means "never null" (the CLI page warns of the first, not the second). To keep `With...` honest I added
   six flags (`_titlePinned`, `_subtitlePinned`, `_languagePinned`, `_scriptPinned`, `_yearPinned`, `_isbnPinned`) and two
   more (`_subtitleDeclared`, `_isbnDeclared`) edited into eight generated methods, and made a pinned title decide the script
   (`WithTitle(new Title("Война и мир"))` alone must give a Cyrillic record). That is about 60 lines of subtle code that
   `any generate --force` would erase. A supported seam for "this parameter depends on that one" would remove most of it.
6. `WithId(null!)` does not compile: `CS0121 The call is ambiguous between 'AnyBibliographicRecord.WithId(RecordId)' and
   'AnyBibliographicRecord.WithId(Func<AnyRecordId, AnyRecordId>)'`. The CLI page says the pin of a null needs a cast
   (`(Customer?)null`), so I wrote `WithId((RecordId)null!)`. It reads badly in tests whose point is "a record needs an id".
7. `any generate ListingCollation` wrote `new AnyCultureInfo()` for the `CultureInfo` parameter, labelled `AnyX` ("drawn
   through the generator that type owns"). `CultureInfo` is a framework type nobody here owns; the compiler then asks for
   `AnyCultureInfo` (CS0246) and the natural next step, `any generate CultureInfo`, scaffolds `new CultureInfo(Any.Int32(),
   Any.Boolean())`, an arbitrary LCID that throws `CultureNotFoundException`. I wrote the generator by hand (a pool of
   eight cultures). A `TODO` for a framework type would have been more honest than `AnyX`.
8. `Any.String()` is ASCII; scripts are what this domain is about. `WithChars` is the documented escape hatch and works for the
   BMP, but for the supplementary ideographs the domain explicitly counts once, `Any.String().WithChars("a\U00020000")`
   throws `ArgumentException: The character pool must not contain a surrogate: an emoji or other astral code point spans two
   UTF-16 code units ... Draw such values as whole strings with OneOf(...) instead.` An excellent message, and I followed it
   (`Any.ListOf(Any.ElementOf(graphemes)).As(string.Concat)`); but the strings page never mentions the limit, so I met it by
   running into it.
9. Not the library, but it cost a round: xUnit theory data silently replaces a lone surrogate by U+FFFD, so
   an `[InlineData]` holding a lone high surrogate made the refusal test fail with "No exception was thrown" while the data displayed as three U+FFFD replacement characters.
   I turned it into a `[Fact]` looping over an array.
10. Tooling aside: the dummies I wrote mix `Any.Enum<WritingScript>()`, `Then`, `ElementOf`, `Combine`, `SetOf(...).WithCountBetween`
    and `Any.StringMatching("([a-z]{2}|[0-9]{3})").OrNull()` and all of them behaved as documented; `Then` was what made
    "pick a script, then draw text in that script" a one-liner.

Domain findings (all reproduced by a test, kept and skipped in `Tests/DomainRealismTests.cs`):
* `A_japanese_surname_with_the_iteration_mark_is_accepted`: `new PersonName("佐々木", "太郎")` throws
  `Letter U+3005 belongs to no supported script.` 々 is category Lm and lies in 3000-303F, outside every Cjk range.
* `A_latin_name_with_a_modifier_letter_apostrophe_is_accepted` / `A_hawaiian_title_is_accepted_in_a_latin_record`: U+02BB (okina)
  and U+02BC are letters above the Latin range ending at U+02AF; `new PersonName("Oʻzbek")` is refused and `Hawaiʻi`
  is refused as a title of a Latin record (`contains letters of a script the catalogue does not support`).
* `A_japanese_title_with_fullwidth_latin_letters_is_accepted`: `ＡＢＣ 入門` is refused for the same reason (FF21-FF5A and the
  halfwidth katakana FF66-FF9F are in no range).
* `A_merge_never_leaves_two_records_describing_the_same_work`: `Merge` rebuilds the survivor without re-running the duplicate
  check, so after taking the absorbed subtitle and contributors the survivor can describe the same work as a third record the
  catalogue already holds (A + B merged equal C), a state `Add` would have refused as `SameWork`.
All four follow MODEL.md to the letter for the ranges; the model is what is short of real data.

## What worked well
* The scaffold of `BibliographicRecord` (11 parameters, 22 `With...` methods) and `Contributor` was real value: pins,
  lambdas, `Without...()`, and the recap's honest "ties language to script". `Any.BibliographicRecord().WithPublicationYear(1969)
  .WithIsbn(isbn978)` says exactly what each refusal test is about, and the rest of the record stays a dummy.
* The block-the-build-on-a-guard design is annoying for 13 files and exactly right for the two or three where it caught a
  tie I would have written wrong (isbn vs year, language vs script).
* `Any.Combine`, `.Then`, `.As`, `Any.ElementOf`, `SetOf(...).WithCountBetween`, `ListOf(...).Distinct()`: enough to write
  script-aware names, titles and a record, whose contributors are distinct by the domain's own equality.
* Error messages of the library are specific (the astral-pool refusal quoted above is the best I met).
* `[assembly: Reproducible]` plus seeds in the failure output; I never needed one for a drawn value, but I read three of them
  for failures of my own tests and they replayed.
* Analyzers stayed silent on the finished code with every rule raised to `warning`, and spoke (JD030) on the probe.

## Fluency: 4 / 5
Once the shape of the tool is understood (scaffold for aggregates, hand-write the value-object generators, own `Generate()`
where parameters are tied) the library lets the tests say what they mean: dummies are visibly arbitrary, every literal in the
suite is a rule of the domain, and 33 re-runs never produced a failure on a drawn value. What keeps it from five is the cost
of this particular domain: nine of thirteen scaffolds were replaced rather than completed, and tied parameters push you out
of the `With...` machinery into code that `--force` would erase. The first compile errors are, however, a precise work list, and
the documentation answered every question except the SDK runner switch and the astral-code-point limit.
