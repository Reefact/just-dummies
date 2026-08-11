# Inspecting a pool

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./inspecting-a-pool.fr.md)

When you draw from a list you supplied yourself, the constraints you declare beside it **narrow that
list**: each value either satisfies them or it does not, and the domain is the values that do. A value
that does not simply stops being drawn.

With four values written at the call site that is harmless — you can see them. The moment the list is a
**catalogue** you cannot read in one glance, it stops being harmless:

```csharp
// 2,417 names, one per line, maintained by someone who has never seen this test.
string[] firstNames = System.IO.File.ReadAllLines("first-names.txt");

string name = Any.String().OneOf(firstNames).Alpha().WithLengthBetween(2, 64).Generate();
```

That arrange line looks right, and it runs. But `Alpha()` means ASCII letters, so every *Anne-Marie*,
every *N'Golo*, every *José* in the file is quietly gone — a few hundred names, perhaps, out of two
thousand. Your tests still pass. They just stopped drawing from the catalogue you thought they were
drawing from, and nothing anywhere says so.

Whether that is a defect depends on something the library cannot know: either the catalogue is wrong and
those names do not belong in it, or the invariant is wrong and `Alpha()` is stricter than the code it
stands for. **Both repairs need the same fact**, and that is what a pool inspection hands back.

## Reaching the inspection

The generators whose pool you supply implement `IPoolInspection<T>` **explicitly**, so it never appears
among the constraints while you are writing them. You reach it with a cast:

```csharp
string[] firstNames = System.IO.File.ReadAllLines("first-names.txt");

IPoolInspection<string> pool = Any.String().OneOf(firstNames).Alpha().WithLengthBetween(2, 64);

IReadOnlyList<string>                drawable = pool.GetSurvivors();
IReadOnlyList<PoolRejection<string>> refused  = pool.GetRejections();
```

Nothing here draws. The domain is fixed the moment you declare the constraints, so both calls return the
same answer every time, under every seed, and an inspection between two draws leaves a seeded run
replaying exactly as it would have.

## Reading the report

At this scale you do not want to read rejections one by one — you want the shape of the damage. Each
rejection carries the value and **every** constraint that refuses it, and a `DeclaredConstraint` is a
value you can compare, so grouping is the natural first look:

```csharp
string[] firstNames = System.IO.File.ReadAllLines("first-names.txt");

IPoolInspection<string> pool    = Any.String().OneOf(firstNames).Alpha().WithLengthBetween(2, 64);
IReadOnlyList<PoolRejection<string>> refused = pool.GetRejections();

// 214 of 2417 names never draw
Console.WriteLine($"{refused.Count} of {firstNames.Length} names never draw");

// Alpha(): 213
// WithLengthBetween(2, 64): 1
foreach (IGrouping<DeclaredConstraint, PoolRejection<string>> reason in refused.GroupBy(rejection => rejection.RejectedBy[0])) {
    Console.WriteLine($"{reason.Key}: {reason.Count()}");
}
```

That second line is the whole answer in one number: 213 names lost to `Alpha()` is an invariant that is
too strict, while the single one lost to the length bound is a blank line in the file. Two different
repairs, told apart without reading a single name.

A `DeclaredConstraint` keeps its `Name` and its rendered `Arguments` apart, so you can group and filter
by constraint instead of parsing text. Its `Arguments` read `...` when the values are ones the library
must not render — a pool of your own type, whose `ToString` is yours and could be anything. And when a
value fails for more than one reason, `RejectedBy` carries them all rather than the first one met, since
loosening one of two reasons would change nothing.

## Locking a catalogue in a test

The inspection's reason for existing is that you can turn it into a check that runs where the catalogue
lives, instead of noticing a shrunken pool months later:

```csharp
string[] firstNames = System.IO.File.ReadAllLines("first-names.txt");

IPoolInspection<string> pool = Any.String().OneOf(firstNames).Alpha().WithLengthBetween(2, 64);

Assert.Empty(pool.GetRejections());
```

That test fails the day someone adds a name the invariant refuses — and because a rejection names both
the value and the constraint, the failure says which name and which invariant, not merely that a count
changed. An emptied pool never gets that far: a value set the constraints leave with nothing is a
`ConflictingAnyConstraintException` at the arrange line, naming both sides.

## What it does not do

The library **reports**; it does not judge. It never warns that part of your pool was narrowed away,
because narrowing a shared catalogue at one call site is exactly what declaring a constraint beside a
value set is *for* — a generator that treated it as a mistake would be wrong more often than right.
Drawing an adult's name from a catalogue that also holds children's is the same mechanism working as
intended.

The interface is also **optional**. It is carried by every generator that admits a value set you supply
— `Any.OneOf(...)`/`Any.ElementOf(...)`, `Any.String().OneOf(...)`, and every family with a `OneOf`: the
integers, `Any.Decimal()`, the floating-point builders, the dates and times, `Any.Char()`, `Any.Guid()`
and `Any.Enum<T>()`. A generator with no pool of yours to report on does not carry it at all, so write
the cast as a test when you do not know what you hold:

```csharp
IAny<string> generator = Any.String().OneOf("Camille", "Ada");

if (generator is IPoolInspection<string> inspectable && inspectable.IsPooled) {
    Console.WriteLine(inspectable.GetRejections().Count);
}
```

`IsPooled` is the second half of that question: a string generator that builds its value rather than
picking from supplied ones answers `false`, with an empty report rather than an exception.

And it is *not* "this generator has a countable domain". `Any.Int32().Between(1, 1_000_000)` is perfectly
countable and answers `false`: those million values are the engine's, not yours. There is nothing of
yours to audit, so there is nothing to report — the inspection only ever speaks about a list you handed
over.

---

[← All guides](../README.md)
