# Inspecting a pool

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./inspecting-a-pool.fr.md)

When you draw from a list you supplied yourself, the constraints you declare beside it **narrow that
list**: each value either satisfies them or it does not, and the domain is the values that do. A value
that does not simply stops being drawn. Nothing is said about it, and nothing needs to be — until the
list is a catalogue you maintain.

```csharp
string[] firstNames = ["Camille", "Sylvain", "Ada", "Bo"];

string name = Any.String().OneOf(firstNames).WithMinLength(3).Generate();
```

`"Bo"` will never come out of that generator. Whether that is a defect depends on something the library
cannot know: either the catalogue is wrong and `"Bo"` should not be in it, or the invariant is wrong and
`WithMinLength(3)` is stricter than the code it stands for. **Both repairs need the same fact**, and
that is what a pool inspection hands back.

## Reaching the inspection

The generators whose pool you supply implement `IPoolInspection<T>` **explicitly**, so it never appears
among the constraints while you are writing them. You reach it with a cast:

```csharp
string[] firstNames = ["Camille", "Sylvain", "Ada", "Bo"];

IPoolInspection<string> pool = Any.String().OneOf(firstNames).WithMinLength(3);

IReadOnlyList<string>                drawable = pool.GetSurvivors();
IReadOnlyList<PoolRejection<string>> refused  = pool.GetRejections();
```

Nothing here draws. The domain is fixed the moment you declare the constraints, so both calls return the
same answer every time, under every seed, and an inspection between two draws leaves a seeded run
replaying exactly as it would have.

## Reading a rejection

Each rejection carries the value and **every** constraint that refuses it — not the first one met, since
loosening one of two reasons would change nothing:

```csharp
string[] firstNames = ["Camille", "Sylvain", "Ada", "Bo"];

IPoolInspection<string> pool = Any.String().OneOf(firstNames).WithMinLength(3);

foreach (PoolRejection<string> rejection in pool.GetRejections()) {
    string reasons = string.Join(", ", rejection.RejectedBy);

    // Bo never draws: WithMinLength(3)
    Console.WriteLine($"{rejection.Value} never draws: {reasons}");
}
```

A `DeclaredConstraint` keeps its `Name` and its rendered `Arguments` apart, so you can group or filter by
constraint instead of parsing text. Its `Arguments` read `...` when the values are ones the library must
not render — a pool of your own type, whose `ToString` is yours and could be anything.

## Locking a catalogue in a test

The inspection's reason for existing is that you can turn it into a check that runs where the catalogue
lives, instead of noticing a shrunken pool months later:

```csharp
string[] firstNames = ["Camille", "Sylvain", "Ada"];

IPoolInspection<string> pool = Any.String().OneOf(firstNames).WithMinLength(3);

Assert.Empty(pool.GetRejections());
```

That test fails the day someone adds a name the invariant refuses, and its message names both the value
and the constraint. An emptied pool never gets that far: a value set the constraints leave with nothing
is a `ConflictingAnyConstraintException` at the arrange line, naming both sides.

## What it does not do

The library **reports**; it does not judge. It never warns that part of your pool was narrowed away,
because narrowing a shared catalogue at one call site is exactly what declaring a constraint beside a
value set is *for* — a generator that treated it as a mistake would be wrong more often than right.

The interface is also **optional**. It is carried by the generators whose pool you supply whole —
`Any.String().OneOf(...)` and `Any.OneOf(...)`/`Any.ElementOf(...)` — and not by the builders that shape
a value or narrow within their own domain, so write the cast as a test when you do not know what you
hold:

```csharp
IAny<string> generator = Any.String().OneOf("Camille", "Ada");

if (generator is IPoolInspection<string> inspectable && inspectable.IsPooled) {
    Console.WriteLine(inspectable.GetRejections().Count);
}
```

`IsPooled` is the second half of that question: a string generator that builds its value rather than
picking from supplied ones answers `false`, with an empty report rather than an exception.

---

[← All guides](../README.md)
