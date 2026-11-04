# Generator reference

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./README.fr.md)

Every `Any.*` factory in the library, grouped by family, with the page that documents it. If you know
the type you need, this page gets you to the right constraints in one hop.

The **Builder** column names the type each factory returns — the one to write for a field, a helper's
return type or a recipe parameter. It follows the factory's name: `Any.Int32()` returns an `AnyInt32`,
`Any.ListOf(item)` an `AnyList<T>`. Three do not: `Any.StringMatching` returns an `AnyPattern`,
`Any.ElementOf` an `AnyOneOf<T>`, and `Any.PermutationOf` a plain `IAny<List<T>>`, with no constraint of
its own to add.

## Numbers

| Factory | Builder | Draws | Page |
| --- | --- | --- | --- |
| `Any.Byte()` | `AnyByte` | `byte` | [Numbers](./numbers.en.md) |
| `Any.SByte()` | `AnySByte` | `sbyte` | [Numbers](./numbers.en.md) |
| `Any.Int16()` | `AnyInt16` | `short` | [Numbers](./numbers.en.md) |
| `Any.Int32()` | `AnyInt32` | `int` | [Numbers](./numbers.en.md) |
| `Any.Int64()` | `AnyInt64` | `long` | [Numbers](./numbers.en.md) |
| `Any.UInt16()` | `AnyUInt16` | `ushort` | [Numbers](./numbers.en.md) |
| `Any.UInt32()` | `AnyUInt32` | `uint` | [Numbers](./numbers.en.md) |
| `Any.UInt64()` | `AnyUInt64` | `ulong` | [Numbers](./numbers.en.md) |
| `Any.Decimal()` | `AnyDecimal` | `decimal` | [Numbers](./numbers.en.md) |
| `Any.Double()` | `AnyDouble` | `double` | [Numbers](./numbers.en.md) |
| `Any.Single()` | `AnySingle` | `float` | [Numbers](./numbers.en.md) |
| `Any.Int128()` 🔹 | `AnyInt128` | `Int128` | [Numbers](./numbers.en.md) |
| `Any.UInt128()` 🔹 | `AnyUInt128` | `UInt128` | [Numbers](./numbers.en.md) |
| `Any.Half()` 🔹 | `AnyHalf` | `Half` | [Numbers](./numbers.en.md) |

## Strings and characters

| Factory | Builder | Draws | Page |
| --- | --- | --- | --- |
| `Any.String()` | `AnyString` | `string` | [Strings and patterns](./strings.en.md) |
| `Any.Char()` | `AnyChar` | `char` | [Strings and patterns](./strings.en.md) |
| `Any.StringMatching(pattern)` | `AnyPattern` | `string` matching a regular pattern | [Strings and patterns](./strings.en.md) |

## Dates and times

| Factory | Builder | Draws | Page |
| --- | --- | --- | --- |
| `Any.DateTime()` | `AnyDateTime` | `DateTime` | [Dates and times](./dates-and-times.en.md) |
| `Any.DateTimeOffset()` | `AnyDateTimeOffset` | `DateTimeOffset` | [Dates and times](./dates-and-times.en.md) |
| `Any.TimeSpan()` | `AnyTimeSpan` | `TimeSpan` | [Dates and times](./dates-and-times.en.md) |
| `Any.DateOnly()` 🔹 | `AnyDateOnly` | `DateOnly` | [Dates and times](./dates-and-times.en.md) |
| `Any.TimeOnly()` 🔹 | `AnyTimeOnly` | `TimeOnly` | [Dates and times](./dates-and-times.en.md) |

## Collections

| Factory | Builder | Draws | Page |
| --- | --- | --- | --- |
| `Any.ArrayOf(item)` | `AnyArray<T>` | `T[]` | [Collections](./collections.en.md) |
| `Any.ListOf(item)` | `AnyList<T>` | `List<T>` | [Collections](./collections.en.md) |
| `Any.SequenceOf(item)` | `AnySequence<T>` | `IEnumerable<T>` | [Collections](./collections.en.md) |
| `Any.SetOf(item)` | `AnySet<T>` | `HashSet<T>` | [Collections](./collections.en.md) |
| `Any.DictionaryOf(keys, values)` | `AnyDictionary<TKey, TValue>` | `Dictionary<TKey, TValue>` | [Collections](./collections.en.md) |
| `Any.PermutationOf(items)` | `IAny<List<T>>` | `List<T>`, the items in a random order | [Collections](./collections.en.md) |

## Enums and choices

| Factory | Builder | Draws | Page |
| --- | --- | --- | --- |
| `Any.Enum<TEnum>()` | `AnyEnum<TEnum>` | a declared member of `TEnum` | [Enums and choices](./enums-and-choices.en.md) |
| `Any.OneOf(values)` | `AnyOneOf<T>` | one of the listed values | [Enums and choices](./enums-and-choices.en.md) |
| `Any.ElementOf(collection)` | `AnyOneOf<T>` | one element of a collection | [Enums and choices](./enums-and-choices.en.md) |
| `Any.Boolean()` | `AnyBoolean` | `bool` | [Enums and choices](./enums-and-choices.en.md) |

## Identifiers and URIs

| Factory | Builder | Draws | Page |
| --- | --- | --- | --- |
| `Any.Guid()` | `AnyGuid` | `Guid` | [Identifiers and URIs](./guids-and-uris.en.md) |
| `Any.Uri()` | `AnyUri` | `Uri` — web, WebSocket, FTP, mailto or relative | [Identifiers and URIs](./guids-and-uris.en.md) |

## Composition

These do not draw a new kind of value; they build a generator out of other generators.

| Factory | Produces | Page |
| --- | --- | --- |
| `generator.As(factory)` | `IAny<TResult>` | [Composition](../guides/composition.en.md) |
| `Any.Combine(…, compose)` | `IAny<TResult>` from 2 to 8 generators | [Composition](../guides/composition.en.md) |
| `Any.PairOf(first, second)` | `IAny<(T1, T2)>` | [Composition](../guides/composition.en.md) |
| `Any.TripleOf(first, second, third)` | `IAny<(T1, T2, T3)>` | [Composition](../guides/composition.en.md) |
| `Any.Pair(item1, item2)` | `AnyPair<T>`, two values of one ordered type, ordered through `Having` | [Composition](../guides/composition.en.md) |
| `generator.OrNull()` | `IAny<T?>`, `null` one draw in ten | [Composition](../guides/composition.en.md) |
| `generator.AsNullable()` | `IAny<T?>`, never `null` | [Composition](../guides/composition.en.md) |
| `Any.Fixed(value)` | `IAny<T>` returning the value you chose, drawing nothing | [Composition](../guides/composition.en.md) |

## Reproducibility

| Factory | Does | Page |
| --- | --- | --- |
| `Any.Reproducibly(body)` | runs a body under a fresh seed, reporting it on failure | [Reproducibility](../guides/reproducibility.en.md) |
| `Any.Reproducibly(seed, body)` | replays a body under a known seed | [Reproducibility](../guides/reproducibility.en.md) |
| `Any.ReproduciblyAsync(body)` | the awaitable counterpart | [Reproducibility](../guides/reproducibility.en.md) |
| `Any.UseSeed(seed)` | pins the ambient context until disposed | [Reproducibility](../guides/reproducibility.en.md) |
| `Any.WithSeed(seed)` | returns an isolated `AnyContext` | [Reproducibility](../guides/reproducibility.en.md) |

🔹 Available on the `net8.0` asset only — the type itself does not exist below .NET 8.

## The shared vocabulary

Constraint names mean the same thing everywhere they appear, so most of the surface is learnable
once:

| Name | Everywhere it appears |
| --- | --- |
| `Between(min, max)` | inclusive at both ends |
| `Except(…)` / `DifferentFrom(x)` | removes values from the domain |
| `OneOf(…)` | restricts the draw to an explicit pool |
| `NonEmpty()` / `Empty()` | the non-empty and empty cases, for strings, collections and `Guid` |
| `WithCount*` / `WithLength*` | the size family, on collections and strings respectively |

---

[← Documentation index](../README.md)
