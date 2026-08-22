# Strings and patterns

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./strings.fr.md)

`Any.String()` is the most constrained generator in the library, because strings are where domain
formats live. This page covers its four constraint families, the layout rule that explains how they
interact, `Any.Char()`, and pattern-driven generation with `Any.StringMatching`.

## What an unconstrained string looks like

<!-- jd:allow=JD030 -->
```csharp
string anything = Any.String().Generate();   // 0 to 1024 characters, anywhere in ASCII
string nonEmpty = Any.String().NonEmpty().Generate();
```

An unconstrained draw yields **0 to 1024 characters drawn from the whole of ASCII** — control
characters, tabs and newlines included — so it can be empty, long, and full of things your code may
not like.

**That is the point, and it is deliberate.** A dummy is a value the code under test had no say in;
restricting it in advance to short, tame text removes exactly the evidence the draw exists to
produce. A test that passes with one of these has shown something. A test that passes with `abc123`
has shown nothing about what happens at 300 characters, or when a `\r` arrives.

So constrain it — with the invariants your code actually has:

```csharp
string reference = Any.String().Printable().WithMaxLength(32).NonEmpty().Generate();
```

`NonEmpty()` when content is required, `WithMaxLength(...)` for the length your column or contract
allows, `Printable()` when a control character is not one of the things it allows. Each of those is
a fact about the surrounding code, written where it belongs
([ADR-0075](../../for-maintainers/adr/0075-draw-characters-from-the-whole-of-ascii.md),
[ADR-0076](../../for-maintainers/adr/0076-let-a-declared-maximum-steer-the-size-draw.md)).

## Length

<!-- jd:allow=JD030 -->
```csharp
string exact     = Any.String().WithLength(12).Generate();
string ranged    = Any.String().WithLengthBetween(3, 20).Generate();
string atLeast   = Any.String().WithMinLength(8).Generate();
string atMost    = Any.String().WithMaxLength(50).Generate();
string withStuff = Any.String().NonEmpty().Generate();
```

`NonEmpty()` is the odd one in that list: it raises the floor to one and leaves the ceiling where it
was, so a chain carrying only it still draws the whole spread. The analyzer
[JD030](../analyzers/JD030.en.md) says so at the call site, on that line and on every other chain
that declares no length.

**A declared bound is the bound you get.** `WithMaxLength(50)` draws across 0 to 50, and
`WithLengthBetween(1000, 5000)` draws across the whole range — the two spellings of a range behave
identically. With only a minimum, the draw reaches the default spread above it: `WithMinLength(1000)`
yields 1000 to 2024.

Every size argument is refused above one million, maxima included: past that point a test wanted a
load test, not a dummy
([ADR-0076](../../for-maintainers/adr/0076-let-a-declared-maximum-steer-the-size-draw.md)).

## Alphabet

The universe is the whole of ASCII, and **every constraint below narrows it** — there is no
exception to that rule.

| Family | Draws | Size |
| --- | --- | --- |
| *(none)* | every ASCII character | 128 |
| `Printable()` | 0x20 to 0x7E, the space included | 95 |
| `NonPrintable()` | the C0 controls and `DEL` | 33 |
| `Alpha()` | `A-Z a-z` | 52 |
| `Numeric()` | `0-9` | 10 |
| `AlphaNumeric()` | `A-Z a-z 0-9` | 62 |
| `Punctuation()` | printable, not a letter, a digit or the space | 32 |
| `Whitespaces()` | the space and the tab | 2 |
| `Hexadecimal()` | `0-9 A-F a-f` | 22 |

```csharp
string letters      = Any.String().Alpha().WithLength(10).Generate();          // A-Z a-z
string alphanumeric = Any.String().AlphaNumeric().WithLength(10).Generate();   // A-Z a-z 0-9
string digits       = Any.String().Numeric().WithLength(6).Generate();         // 0-9
string symbols      = Any.String().Punctuation().WithLength(4).Generate();     // !"#$%&'()*+,-./ etc.
string sha          = Any.String().Hexadecimal().InLowerCase().WithLength(40).Generate();
string anyText      = Any.String().Printable().WithLength(20).Generate();      // no control characters
string shouting     = Any.String().Alpha().InUpperCase().WithLength(4).Generate();
string noDigits     = Any.String().Printable().WithoutNumeric().WithLength(8).Generate();
string custom       = Any.String().WithChars("ACGT").WithLength(20).Generate(); // your own pool
```

A family occupies **one slot**: declaring a second one contradicts the first, and the conflict names
both sides. `WithoutAlpha()` and `WithoutNumeric()` are different — they **subtract** and accumulate,
so `WithoutAlpha().WithoutNumeric()` leaves the punctuation, the whitespace and the controls.

Two things to know. `Punctuation()` is the POSIX `[:punct:]` block, which is **broader** than
`char.IsPunctuation` — that predicate reads `+`, `<` and `$` as symbols, so assert on the invariant
your code actually has rather than on it. And the space is not in it, deliberately: it is the one
character a `Trim()` removes in silence, so a separator you can rely on must not be one.
`Whitespaces()` is what names it.

**Nothing named reaches past ASCII**, and that bound is where localisation would start: a pool
following the runtime's Unicode version would draw differently on two target frameworks, against a
guarantee this library checks byte for byte. `WithChars` is the escape hatch — supply the exact pool
and the draw uses nothing else. It is how you express an alphabet the named families do not cover: a
DNA sequence, a base-32 alphabet, accented text, a set of allowed separators.

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

**A character constraint governs the filler, not your literals.** The alphabet you declare — a named
family, `WithChars`, a subtraction, a casing — narrows what the generator *draws*. A prefix, a suffix
or a contained value is text **you** wrote: it is kept exactly as written, and no character
constraint can contradict it. That is what lets a format say what it means, with each of its rules a
named call:

<!-- jd:allow=JD033 -->
```csharp
string reference = Any.String().StartingWith("ORD-").AlphaNumeric().InUpperCase().WithLengthBetween(8, 20).Generate();
// ORD-7K2P9QW, ORD-XZ4M1TB, ORD-B8N3TVJ2 — the hyphen separates, and the body stays alphanumeric
```

[JD033](../analyzers/JD033.en.md) notes the separator at the call site — as information, not a complaint: it
says the `-` lands in the prefix and nowhere else, which is the whole point of writing it there.

Declaring the pool by hand instead would put the hyphen back in the body, which is the opposite of
the rule being modelled.

The length budget is the one thing a literal does not escape — it still has to fit:

<!-- jd:allow=JD015,JD006 -->
```csharp
Any.String().WithLength(3).StartingWith("ORD-");  // the length cannot hold the prefix
```

The analyzer [JD015](../analyzers/JD015.en.md) reports it at build time whenever the arguments are
constants, so the failure usually arrives before the test ever runs.

## Membership and exclusion

<!-- jd:allow=JD029 -->
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
char letter      = Any.Char().Alpha().Generate();
char upper       = Any.Char().Alpha().InUpperCase().Generate();
char digit       = Any.Char().Numeric().Generate();
char punctuation = Any.Char().Punctuation().Generate();
char printable   = Any.Char().Printable().Generate();
char control     = Any.Char().NonPrintable().Generate();
char hex         = Any.Char().Hexadecimal().InLowerCase().Generate();
char separator   = Any.Char().OneOf('-', '_', '.').Generate();
char notVowel    = Any.Char().Alpha().InLowerCase().Except('a', 'e', 'i', 'o', 'u').Generate();
```

The families are the same ones `Any.String()` declares, they mean the same thing here, and the
default is the same too: **an unconstrained `Any.Char()` draws anywhere in ASCII**, so it may well
hand you a carriage return or a NUL. `Printable()` is what you declare when that is not acceptable;
`Punctuation()` when the character must not read as alphanumeric; `NonPrintable()` when a control
character is precisely the counter-example your test needs. Where the set is a specific one — three
allowed separators, not all thirty-two — `OneOf` says so and says it exactly.

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
