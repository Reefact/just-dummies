# Strings and patterns

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./strings.fr.md)

`Any.String()` is the most constrained generator in the library, because strings are where domain
formats live. This page covers its four constraint families, the layout rule that explains how they
interact, `Any.Char()`, and pattern-driven generation with `Any.StringMatching`.

## What an unconstrained string looks like

```csharp
string anything = Any.String().Generate();   // 0 to 16 ASCII letters and digits
string nonEmpty = Any.String().NonEmpty().Generate();
```

An unconstrained draw yields **0 to 16 ASCII letters and digits**, so it can be empty. Chain
`NonEmpty()` whenever the surrounding code requires content — which is most of the time, and is
exactly the kind of invariant a constraint is for.

## Length

```csharp
string exact     = Any.String().WithLength(12).Generate();
string ranged    = Any.String().WithLengthBetween(3, 20).Generate();
string atLeast   = Any.String().WithMinLength(8).Generate();
string atMost    = Any.String().WithMaxLength(50).Generate();
string withStuff = Any.String().NonEmpty().Generate();
```

A length above 1 000 000 is refused: past that point a test wanted a load test, not a dummy
([ADR-0029](../../for-maintainers/adr/0029-let-a-size-maximum-cap-without-steering-the-draw.md)).

## Alphabet

Six constraints decide which characters may appear:

```csharp
string letters      = Any.String().Alpha().WithLength(10).Generate();          // A-Z a-z
string alphanumeric = Any.String().AlphaNumeric().WithLength(10).Generate();   // A-Z a-z 0-9
string digits       = Any.String().Numeric().WithLength(6).Generate();         // 0-9
string shouting     = Any.String().Alpha().UpperCase().WithLength(4).Generate();
string quiet        = Any.String().Alpha().LowerCase().WithLength(4).Generate();
string custom       = Any.String().WithChars("ACGT").WithLength(20).Generate(); // your own pool
```

`WithChars` is the escape hatch: supply the exact pool and the draw uses nothing else. It is how you
express an alphabet the built-in families do not cover — a DNA sequence, a base-32 alphabet, a set of
allowed separators.

## Shape: prefixes, suffixes, fragments

```csharp
string reference = Any.String().StartingWith("ORD-").WithLength(12).Generate();
string filename  = Any.String().EndingWith(".txt").WithMaxLength(30).Generate();
string path      = Any.String().Alpha().Containing("admin").WithMinLength(20).Generate();
```

## How the layout works

Strings are **built to satisfy** the constraints rather than generated and filtered. The layout is
always:

```text
prefix + filler + contained values + filler + suffix
```

Two consequences follow, and they explain almost every surprise:

**Fragments never overlap.** The length budget they need is the plain sum of their lengths. A prefix
of four characters plus a suffix of four needs at least eight, so `WithLength(6)` alongside both is
refused rather than quietly reusing characters.

**A fragment must belong to the declared alphabet.** Declaring digits only and then requiring a
letter prefix is a contradiction, not a widening. Both of these are refused at the moment they are
declared, with a message naming both sides:

<!-- jd:allow=JD015,JD006 -->
```csharp
Any.String().WithLength(3).StartingWith("ORD-");  // the length cannot hold the prefix
Any.String().Numeric().StartingWith("ORD-");      // 'ORD-' is not numeric
```

The analyzer [JD015](../analyzers/JD015.en.md) reports both at build time whenever the arguments are
constants, so the failure usually arrives before the test ever runs.

## Membership and exclusion

```csharp
string currency = Any.String().OneOf("EUR", "USD", "GBP").Generate();
string status   = Any.String().OneOf(["draft", "sent", "paid"]).Generate();
string notDraft = Any.String().OneOf("draft", "sent", "paid").DifferentFrom("draft").Generate();
string notEmpty = Any.String().WithLengthBetween(1, 5).Except("aaa", "bbb").Generate();
```

`OneOf` is the one constraint that **replaces** the layout rather than shaping it: you supply the
values, so the draw is a uniform pick from them and every other constraint narrows that set instead
of building a string.

Because of that, declare a value set **first**. Constraints that contradict each other on their own
terms are refused the moment they are declared — before a value set could reinterpret them as a
filter.

Exclusions are met by a **bounded** redraw, so excluding nearly everything a small domain can
produce ends in an explicit `AnyGenerationException` rather than a hang
([ADR-0012](../../for-maintainers/adr/0012-meet-string-exclusions-with-a-bounded-redraw.md)).

## Characters

`Any.Char()` carries the alphabet family and the membership family:

```csharp
char letter    = Any.Char().Alpha().Generate();
char upper     = Any.Char().Alpha().UpperCase().Generate();
char digit     = Any.Char().Numeric().Generate();
char separator = Any.Char().OneOf('-', '_', '.').Generate();
char notVowel  = Any.Char().Alpha().LowerCase().Except('a', 'e', 'i', 'o', 'u').Generate();
```

## Patterns

`Any.StringMatching` generates a value **from** a pattern rather than testing candidates against it,
which is what lets it guarantee a match. Both a string and a `Regex` are accepted:

```csharp
string sku       = Any.StringMatching(@"[A-Z]{3}-\d{4}").Generate();
string reference = Any.StringMatching(new Regex(@"ORD-\d{8}")).Generate();
string flag      = Any.StringMatching("(true|false)").Generate();
```

### Supported constructs

| Construct | Example |
| --- | --- |
| literals | `abc` |
| any character | `.` |
| character classes and ranges | `[A-Z]`, `[aeiou]`, `[^0-9]` |
| shorthand classes | `\d` `\D` `\w` `\W` `\s` `\S` |
| escapes | `\t` `\n` `\r` `\f` `\v` `\a` `\e` |
| quantifiers | `*` `+` `?` `{3}` `{2,5}` `{2,}` |
| grouping | `(…)`, `(?:…)`, `(?<name>…)` |
| alternation | `a|b` |
| anchors at the edges | `^…$` |

### Refused constructs

Anything that is not **regular** cannot be built by a finite automaton, so it is refused eagerly with
an `UnsupportedRegexException` naming the construct and its position — never mis-generated:

| Refused | Why |
| --- | --- |
| back-references, balancing groups `(?<a-b>…)` | they need the capture stack |
| lookahead `(?=…)`, `(?!…)` | not regular |
| lookbehind `(?<=…)`, `(?<!…)` | not regular |
| atomic groups `(?>…)` | not regular |
| conditional groups `(?(…)…)` | not regular |
| inline comments `(?#…)`, group options `(?i…)` | not part of the language being generated |
| an anchor away from an edge | `^` and `$` are only meaningful at the start and end of the pattern, or of a top-level alternation branch |

Widening this set would mean taking a regex-automaton dependency; the decision to keep a home-grown
parser and refuse loudly instead is
[ADR-0008](../../for-maintainers/adr/0008-generate-strings-from-a-home-grown-regular-subset.md).

### What you can still constrain

An `AnyPattern` carries only `Except` and `DifferentFrom`:

```csharp
string sku = Any.StringMatching(@"[A-Z]{3}-\d{4}").DifferentFrom("ABC-0000").Generate();
```

Length, alphabet or prefix constraints are deliberately absent: applying them would mean building a
value in the intersection of two regular languages. Put the requirement in the pattern instead — it
is already the more precise place to say it.

A generated value is guaranteed to match its pattern, by a bounded redraw where construction alone
cannot ensure it
([ADR-0027](../../for-maintainers/adr/0027-guarantee-a-generated-regex-value-matches-by-bounded-redraw.md)).

---

[← Generator reference](./README.md) · [Documentation index](../README.md)
