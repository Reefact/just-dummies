# Collections

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./collections.fr.md)

A collection generator is built from an **element** generator: you describe one item, and the
collection generator draws as many as the count constraints ask for. Everything you already know
about constraining a scalar applies to the element.

## The five collection generators

| Factory | Draws | Adds |
| --- | --- | --- |
| `Any.ArrayOf(item)` | `T[]` | `Distinct()` |
| `Any.ListOf(item)` | `List<T>` | `Distinct()` |
| `Any.SequenceOf(item)` | `IEnumerable<T>` | `Distinct()` |
| `Any.SetOf(item)` | `HashSet<T>` | distinctness by construction |
| `Any.DictionaryOf(keys, values)` | `Dictionary<TKey, TValue>` | key constraints |

```csharp
int[]            anyQuantities = Any.ArrayOf(Any.Int32().Between(1, 100)).WithCount(5).Generate();
List<string>     anyReferences = Any.ListOf(Any.String().StartingWith("ORD-").WithLength(12)).NonEmpty().Generate();
IEnumerable<Guid> anyIds       = Any.SequenceOf(Any.Guid().NonEmpty()).WithCountBetween(2, 6).Generate();
HashSet<OrderStatus> anyStates = Any.SetOf(Any.Enum<OrderStatus>()).WithMaxCount(3).Generate();
```

## The shared count vocabulary

Every collection generator carries the same six count constraints:

```csharp
IAny<int> anyQuantity = Any.Int32().Between(1, 100);

int[] anyExactly5 = Any.ArrayOf(anyQuantity).WithCount(5).Generate();
int[] anyTwo2Six  = Any.ArrayOf(anyQuantity).WithCountBetween(2, 6).Generate();
int[] anyAtLeast3 = Any.ArrayOf(anyQuantity).WithMinCount(3).Generate();
int[] anyAtMost10 = Any.ArrayOf(anyQuantity).WithMaxCount(10).Generate();
int[] anyNotEmpty = Any.ArrayOf(anyQuantity).NonEmpty().Generate();
int[] anyEmpty    = Any.ArrayOf(anyQuantity).Empty().Generate();
```

`Empty()` is not a curiosity: the empty collection is the case most likely to break production code,
and naming it reads better than `WithCount(0)`.

Counts that cannot all hold — a minimum above a maximum, `WithCount(3)` beside `Empty()` — are
refused with a message naming both, and the analyzer [JD016](../analyzers/JD016.en.md) catches the
constant cases at build time.

A count above one million is refused
([ADR-0076](../../for-maintainers/adr/0076-let-a-declared-maximum-steer-the-size-draw.md)).

## Requiring specific elements

Two constraints put something known inside an otherwise arbitrary collection:

```csharp
// A specific value must be present.
List<OrderStatus> anyWithDraft = Any.ListOf(Any.Enum<OrderStatus>())
                                    .WithCountBetween(3, 6)
                                    .Containing(OrderStatus.Draft)
                                    .Generate();

// A value satisfying a second generator must be present.
List<int> anyWithABigOne = Any.ListOf(Any.Int32().Between(1, 100))
                              .WithCountBetween(3, 6)
                              .ContainingAny(Any.Int32().Between(90, 100))
                              .Generate();
```

`ContainingAny` is the one to reach for when the test needs "at least one element that qualifies"
without pinning which value qualifies — the collection equivalent of constraining rather than
asserting.

## Distinctness

`Distinct()` requires the drawn elements to differ. `Any.SetOf` gets there by construction — a
`HashSet<T>` cannot hold a duplicate — while `Distinct()` on an array, list or sequence is a
requirement the generator must actively satisfy:

```csharp
int[]        anyDistinctIds = Any.ArrayOf(Any.Int32().Between(1, 1_000)).WithCount(10).Distinct().Generate();
List<string> anyDistinctRefs = Any.ListOf(Any.String().Alpha().WithLength(6)).WithCount(4).Distinct().Generate();

// With an explicit comparer, when the default equality is not the one that matters.
List<string> anyCaseInsensitive = Any.ListOf(Any.String().Alpha().WithLength(6))
                                     .WithCount(4)
                                     .Distinct(StringComparer.OrdinalIgnoreCase)
                                     .Generate();
```

Two things are worth understanding here.

**Distinctness is gated by cardinality.** Before drawing, the generator compares what you asked for
with what the element generator can actually produce. Asking for ten distinct booleans, or a hundred
distinct values from a pool of three, is refused by name rather than attempted: the conflict is raised at `Generate()`, once the whole chain is known
and before any element is drawn, not at the call that declared it
([ADR-0004](../../for-maintainers/adr/0004-gate-distinct-collections-by-cardinality-else-bounded-draw.md),
amended by [ADR-0113](../../for-maintainers/adr/0113-check-a-distinct-collections-cardinality-when-the-specification-is-whole.md)).
The analyzer [JD016](../analyzers/JD016.en.md) reports the constant cases at build time.

**Where the count is feasible but tight, a bounded redraw finishes the job** — a fixed number of
attempts, then an explicit `AnyGenerationException`. Never an unbounded loop.

**Distinct by a key is `DistinctBy`.** It is exactly `Distinct(comparer)` with a comparer by that key,
without the comparer class to write:

```csharp
IAny<Customer> customerGenerator = Any.Combine(
    Any.Guid().NonEmpty(),
    Any.String().Alpha().WithLengthBetween(3, 20),
    Any.String().Alpha().InLowerCase().WithLength(8),
    (id, name, localPart) => new Customer(id, name, $"{localPart}@example.test"));

List<Customer> anyCustomers = Any.ListOf(customerGenerator).WithCount(3).DistinctBy(customer => customer.Email).Generate();
```

The key's domain is not checked up front — it comes from a function, so it is known only by drawing —
which leaves a key that runs out of values to the bounded redraw, and its loud failure.

**Distinctness needs value equality to mean anything.** Declaring it over a reference type that does
not override `Equals` is satisfied trivially — every instance differs — so the collection can still
hold what a reader would call the same value twice. That is diagnostic
[JD028](../analyzers/JD028.en.md).

## Dictionaries

`Any.DictionaryOf` takes a generator for the keys and one for the values:

```csharp
Dictionary<string, int> anyStock = Any.DictionaryOf(
                                          Any.String().Alpha().InUpperCase().WithLength(3),
                                          Any.Int32().Between(0, 500))
                                      .WithCountBetween(2, 5)
                                      .Generate();
```

Keys are distinct by construction. A second overload takes an `IEqualityComparer<TKey>` when the
default equality is not the one your domain uses.

Three constraints are specific to dictionaries:

```csharp
IAny<string> anyCode  = Any.String().Alpha().InUpperCase().WithLength(3);
IAny<int>    anyLevel = Any.Int32().Between(0, 500);

// A key that must be present.
Dictionary<string, int> anyWithKey = Any.DictionaryOf(anyCode, anyLevel)
                                        .WithCountBetween(2, 5)
                                        .ContainingKey("ABC")
                                        .Generate();

// A whole entry that must be present.
Dictionary<string, int> anyWithEntry = Any.DictionaryOf(anyCode, anyLevel)
                                          .WithCountBetween(2, 5)
                                          .ContainingEntry("ABC", 42)
                                          .Generate();

// A key satisfying another generator must be present.
Dictionary<string, int> anyWithAnyKey = Any.DictionaryOf(anyCode, anyLevel)
                                           .WithCountBetween(2, 5)
                                           .ContainingAnyKey(Any.String().OneOf("ABC", "XYZ"))
                                           .Generate();
```

## Collections of your own types

Because a composed generator is an ordinary `IAny<T>`, a collection of value objects or aggregates
needs nothing new:

```csharp
IAny<OrderReference> anyReference = Any.String()
                                       .StartingWith("ORD-")
                                       .WithLength(12)
                                       .As(OrderReference.Create);

List<OrderReference> anyBasket = Any.ListOf(anyReference).WithCountBetween(1, 4).Generate();
```

---

[← Generator reference](./README.md) · [Documentation index](../README.md)
