# Collections

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./collections.fr.md)

A collection generator is built from an **element** generator: you describe one item, and the
collection generator draws as many as the count constraints ask for. Everything you already know
about constraining a scalar applies to the element.

## The five collection generators

| Factory | Draws | Adds |
| --- | --- | --- |
| `Dummy.ArrayOf(item)` | `T[]` | `Distinct()` |
| `Dummy.ListOf(item)` | `List<T>` | `Distinct()` |
| `Dummy.SequenceOf(item)` | `IEnumerable<T>` | `Distinct()` |
| `Dummy.SetOf(item)` | `HashSet<T>` | distinctness by construction |
| `Dummy.DictionaryOf(keys, values)` | `Dictionary<TKey, TValue>` | key constraints |

```csharp
int[]            quantities = Dummy.ArrayOf(Dummy.Int32().Between(1, 100)).WithCount(5).Generate();
List<string>     references = Dummy.ListOf(Dummy.String().StartingWith("ORD-").WithLength(12)).NonEmpty().Generate();
IEnumerable<Guid> ids       = Dummy.SequenceOf(Dummy.Guid().NonEmpty()).WithCountBetween(2, 6).Generate();
HashSet<OrderStatus> states = Dummy.SetOf(Dummy.Enum<OrderStatus>()).WithMaxCount(3).Generate();
```

## The shared count vocabulary

Every collection generator carries the same six count constraints:

```csharp
IDummy<int> anyQuantity = Dummy.Int32().Between(1, 100);

int[] exactly5   = Dummy.ArrayOf(anyQuantity).WithCount(5).Generate();
int[] two2Six    = Dummy.ArrayOf(anyQuantity).WithCountBetween(2, 6).Generate();
int[] atLeast3   = Dummy.ArrayOf(anyQuantity).WithMinCount(3).Generate();
int[] atMost10   = Dummy.ArrayOf(anyQuantity).WithMaxCount(10).Generate();
int[] notEmpty   = Dummy.ArrayOf(anyQuantity).NonEmpty().Generate();
int[] empty      = Dummy.ArrayOf(anyQuantity).Empty().Generate();
```

`Empty()` is not a curiosity: the empty collection is the case most likely to break production code,
and naming it reads better than `WithCount(0)`.

Counts that cannot all hold — a minimum above a maximum, `WithCount(3)` beside `Empty()` — are
refused with a message naming both, and the analyzer [JD016](../analyzers/JD016.en.md) catches the
constant cases at build time.

A count above one million is refused
([ADR-0029](../../for-maintainers/adr/0029-let-a-size-maximum-cap-without-steering-the-draw.md)).

## Requiring specific elements

Two constraints put something known inside an otherwise arbitrary collection:

```csharp
// A specific value must be present.
List<OrderStatus> withDraft = Dummy.ListOf(Dummy.Enum<OrderStatus>())
                                 .WithCountBetween(3, 6)
                                 .Containing(OrderStatus.Draft)
                                 .Generate();

// A value satisfying a second generator must be present.
List<int> withABigOne = Dummy.ListOf(Dummy.Int32().Between(1, 100))
                           .WithCountBetween(3, 6)
                           .ContainingAny(Dummy.Int32().Between(90, 100))
                           .Generate();
```

`ContainingAny` is the one to reach for when the test needs "at least one element that qualifies"
without pinning which value qualifies — the collection equivalent of constraining rather than
asserting.

## Distinctness

`Distinct()` requires the drawn elements to differ. `Dummy.SetOf` gets there by construction — a
`HashSet<T>` cannot hold a duplicate — while `Distinct()` on an array, list or sequence is a
requirement the generator must actively satisfy:

```csharp
int[]        distinctIds = Dummy.ArrayOf(Dummy.Int32().Between(1, 1_000)).WithCount(10).Distinct().Generate();
List<string> distinctRefs = Dummy.ListOf(Dummy.String().Alpha().WithLength(6)).WithCount(4).Distinct().Generate();

// With an explicit comparer, when the default equality is not the one that matters.
List<string> caseInsensitive = Dummy.ListOf(Dummy.String().Alpha().WithLength(6))
                                  .WithCount(4)
                                  .Distinct(StringComparer.OrdinalIgnoreCase)
                                  .Generate();
```

Two things are worth understanding here.

**Distinctness is gated by cardinality.** Before drawing, the generator compares what you asked for
with what the element generator can actually produce. Asking for ten distinct booleans, or a hundred
distinct values from a pool of three, is refused immediately and by name rather than attempted
([ADR-0004](../../for-maintainers/adr/0004-gate-distinct-collections-by-cardinality-else-bounded-draw.md)).
The analyzer [JD016](../analyzers/JD016.en.md) reports the constant cases at build time.

**Where the count is feasible but tight, a bounded redraw finishes the job** — a fixed number of
attempts, then an explicit `DummyGenerationException`. Never an unbounded loop.

**Distinctness needs value equality to mean anything.** Declaring it over a reference type that does
not override `Equals` is satisfied trivially — every instance differs — so the collection can still
hold what a reader would call the same value twice. That is diagnostic
[JD028](../analyzers/JD028.en.md).

## Dictionaries

`Dummy.DictionaryOf` takes a generator for the keys and one for the values:

```csharp
Dictionary<string, int> stock = Dummy.DictionaryOf(
                                       Dummy.String().Alpha().InUpperCase().WithLength(3),
                                       Dummy.Int32().Between(0, 500))
                                   .WithCountBetween(2, 5)
                                   .Generate();
```

Keys are distinct by construction. A second overload takes an `IEqualityComparer<TKey>` when the
default equality is not the one your domain uses.

Three constraints are specific to dictionaries:

```csharp
IDummy<string> anyCode  = Dummy.String().Alpha().InUpperCase().WithLength(3);
IDummy<int>    anyLevel = Dummy.Int32().Between(0, 500);

// A key that must be present.
Dictionary<string, int> withKey = Dummy.DictionaryOf(anyCode, anyLevel)
                                     .WithCountBetween(2, 5)
                                     .ContainingKey("ABC")
                                     .Generate();

// A whole entry that must be present.
Dictionary<string, int> withEntry = Dummy.DictionaryOf(anyCode, anyLevel)
                                       .WithCountBetween(2, 5)
                                       .ContainingEntry("ABC", 42)
                                       .Generate();

// A key satisfying another generator must be present.
Dictionary<string, int> withAnyKey = Dummy.DictionaryOf(anyCode, anyLevel)
                                        .WithCountBetween(2, 5)
                                        .ContainingAnyKey(Dummy.String().OneOf("ABC", "XYZ"))
                                        .Generate();
```

## Collections of your own types

Because a composed generator is an ordinary `IDummy<T>`, a collection of value objects or aggregates
needs nothing new:

```csharp
IDummy<OrderReference> anyReference = Dummy.String()
                                       .StartingWith("ORD-")
                                       .WithLength(12)
                                       .As(OrderReference.Create);

List<OrderReference> basket = Dummy.ListOf(anyReference).WithCountBetween(1, 4).Generate();
```

---

[← Generator reference](./README.md) · [Documentation index](../README.md)
