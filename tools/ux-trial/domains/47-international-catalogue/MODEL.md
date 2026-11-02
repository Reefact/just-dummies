# International catalogue — domain model

Namespace `InternationalCatalogue.Domain` (`Domain/`). A national library's union catalogue: bibliographic records
whose titles and names may be in any of seven scripts, standard identifiers with check characters, duplicate
detection, and listings sorted by a culture's collation.

## Text rules shared by titles, names and the catalogue name
- Unpaired surrogates are refused (`ArgumentException`) before anything else; text is then put in **NFC** and trimmed.
- Lengths count **grapheme clusters** (`StringInfo.LengthInTextElements`), not UTF-16 code units: `"Café"` has 4,
  a supplementary CJK ideograph counts once.
- **Equality** of titles and names: NFC, bidirectional controls removed, simple case folding (`Rune.ToUpperInvariant`).
  `Café` = `CAFÉ`, but `'` ≠ `’` and an accented letter ≠ its bare letter.
- Scripts are recognised on **letters only** (digits, punctuation, spaces, marks belong to none):
  Latin (U+0000–02AF, 1E00–1EFF, 2C60–2C7F, A720–A7FF), Greek (0370–03FF, 1F00–1FFF), Cyrillic (0400–052F, 1C80–1C8F,
  2DE0–2DFF, A640–A69F), Hebrew (0590–05FF, FB1D–FB4F), Arabic (0600–06FF, 0750–077F, 08A0–08FF, FB50–FDFF, FE70–FEFF),
  Devanagari (0900–097F, A8E0–A8FF), Cjk (1100–11FF, 3040–30FF, 3400–4DBF, 4E00–9FFF, AC00–D7AF, F900–FAFF, 20000–3134F).
  Any other letter (Thai, Georgian…) is of an **unsupported** script.

## Value objects
- `LanguageTag(string)` — simple BCP 47: `^[A-Za-z]{2,3}(-[A-Za-z]{4})?(-([A-Za-z]{2}|[0-9]{3}))?\z`, else `FormatException`.
  Any letter case accepted, stored canonical (`sr-Latn-RS`, `es-419`). `Language`, `Script?`, `Region?`.
  `DeclaredScript`: `Latn`/`Grek`/`Cyrl`/`Arab`/`Hebr`/`Deva` → that script; `Hani`/`Hans`/`Hant`/`Jpan`/`Kore`/`Hira`/`Kana`/`Hang`
  → `Cjk`; other subtags (e.g. `Thai`) → `null`.
- `Title(value, allowBidiControls = false)` — not null; NFC, trimmed, not empty; must not **start with a combining mark**;
  1..**250** grapheme clusters (`ArgumentOutOfRangeException` beyond); refuses control characters (Cc) and U+2028/U+2029;
  refuses **emoji**: U+1F000–1FAFF, U+2600–27BF, U+2B50–2B55, U+FE0F, U+20E3 (ZWJ/ZWNJ stay allowed).
  **Bidirectional controls** (LRM 200E, RLM 200F, ALM 061C, LRE…RLO 202A–202E, LRI/RLI/FSI/PDI 2066–2069) are refused
  unless `allowBidiControls`; even then embeddings/overrides 202A–202E are always refused and **isolates must balance**
  (no PDI without an open isolate, none left open). `Length`, `HasBidiControls`, `LetterScripts()`,
  `HasLettersOfUnsupportedScript()`, `IsRightToLeft()` (first letter Arabic or Hebrew).
- `PersonName(familyName, givenNames = null)` — each part: NFC, trimmed, not empty, not starting with a combining mark,
  no two consecutive spaces, 1..**100** grapheme clusters, at least one letter; only letters, combining marks, space,
  `'`, `’`, `-`, U+2010, `.`, U+30FB; all letters of a **supported script** and of **one script**; **given names in the same
  script as the family name**. `Script`; `Heading` = `Family, Given` (Cjk: `FamilyGiven`, no separator).
- `Contributor(name, role)` — role defined. Equality by name and role.
- `Isbn13(string)` — hyphens allowed between groups (never leading, trailing or doubled → `ArgumentException`), removed;
  then 13 ASCII digits, prefix **978 or 979**, check digit (weights 1,3 mod 10), else `FormatException`.
  `HasIsbn10Form` (978 only), `ToIsbn10()` (`InvalidOperationException` for 979), `ComputeCheckDigit(12 digits)`.
- `Isbn10(string)` — same hyphen rule; 9 ASCII digits + check `0-9`/`X` (`x` accepted, stored `X`), weights 10..1, sum ≡ 0
  mod 11, else `FormatException`. `ToIsbn13()` (978 prefix, check recomputed), `ComputeCheckCharacter(9 digits)`.
- `Issn(string)` — exactly `NNNN-NNNC` (`C` a digit or upper-case `X`), check over 7 digits with weights 8..2:
  `(11 − sum mod 11) mod 11`, 10 → `X`; else `FormatException`.
- `CallNumber(string)` — `^[0-9]{3}(\.[0-9]{1,8})? [A-Z][0-9]{1,4}\z` (e.g. `823.914 R884`), else `FormatException`.
  `IComparable`: class number (decimal), Cutter letter, Cutter digits as a decimal fraction (`R884` < `R89`), then ordinal.
- `RecordId(string)` — 2 upper-case ASCII letters (agency) + 8 ASCII digits, not all zero (`FR00412873`); `FormatException`.
- `ArticleRule(language, articles)` — the language tag must be **bare** (no script, no region); 1..20 articles, none null,
  each NFC-trimmed, letters/marks optionally followed by **one** apostrophe (`’` stored as `'`), ≤ 12 grapheme clusters,
  no two equal under case folding. `Strip(title)`: longest article first, case-insensitive ordinal, `'`≡`’`; a plain article
  needs a following space, an elided one (`L'`) is stripped directly; a title that would become empty is kept whole.
- `ListingCollation(culture, articleRules)` — `IComparer<BibliographicRecord>`; **at most one rule per language**.
  `FilingTitle(record)` = title without bidi controls, stripped by the rule of the record's **primary language subtag**.
  `Compare`: `culture.CompareInfo` with `IgnoreCase` on filing titles, then subtitle (absent first, same collation),
  publication year, edition, record id (ordinal). `SortKeyOf(record)` = the culture's `SortKey` of the filing title.
- Enums `WritingScript` (Latin, Greek, Cyrillic, Arabic, Hebrew, Devanagari, Cjk), `ContributorRole` (Author, Editor,
  Translator, Illustrator, Compiler), `DuplicateReason` (None, SameRecordId, RetiredRecordId, SameIsbn, SameIssn, SameWork).

## Entities
- `BibliographicRecord(id, title, subtitle?, contributors, language, script, edition, publicationYear, isbn?, issn?, callNumber?)`
  - non-null id/title/contributors/language; script defined; contributors: no null, ≤ 20, no duplicate (name + role);
  - edition **1..999**; publication year **1450..(current UTC year + 1)**;
  - **an ISBN requires year ≥ 1970; a 979 ISBN requires year ≥ 2007**;
  - **every letter of title and subtitle is in the declared script** (no letters, e.g. `1984`, fits any script;
    letters of an unsupported script are refused);
  - **a script subtag in the language tag must designate the declared script** (`sr-Latn` with Cyrillic is refused;
    `ru` with Latin — a transliteration — is accepted).
  - `IsSerial` (ISSN and no ISBN); `ContributorNames()`; `DescribesSameWorkAs(other)`: equal title and subtitle (under the
    equality above), same language tag, edition, year and set of contributor names (roles ignored) — never when both
    carry different ISBNs.

## Aggregate: `Catalogue(name)`
- Name: NFC, trimmed, not empty, no control/line-break character, ≤ 120 grapheme clusters.
- `Add(record)` — refused with `DuplicateRecordException` (`Reason`, `Existing`), checked in order: id already held
  (`SameRecordId`); id retired by a merge or a withdrawal (`RetiredRecordId`, `Existing` = survivor, or the id itself when
  withdrawn); same ISBN (`SameIsbn`); two serials with the same ISSN (`SameIssn` — a monograph sharing a series ISSN is not
  a duplicate); `DescribesSameWorkAs` an existing record (`SameWork`). `CheckDuplicate(record)` returns the reason, `None` when accepted.
- `Merge(survivorId, absorbedId)` — same id → `ArgumentException`; either not current (unknown or retired) →
  `InvalidOperationException`; refused unless **equal title, same language tag, script, edition and year**, and no conflicting
  ISBNs or ISSNs; contributors union over 20 refused. The survivor keeps its values, takes the absorbed subtitle / ISBN / ISSN /
  call number it lacks and appends contributors it does not list; the absorbed id is retired and resolves to the survivor.
- `Withdraw(id)` — current records only; the id is retired, never reused.
- `Find(id)` follows merge redirects (null when unknown or withdrawn); `FindByIsbn(Isbn13 | Isbn10)`; `FindByTitle(title)`
  (equality above); `Records()`, `Count`.
- `Listing(collation, script?)` — records sorted by the collation, optionally one script; `RenderListing` — one line each:
  filing title, ` / ` first contributor heading, ` (year)`.
