# Generator reference

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./README.fr.md)

Every `Any.*` factory in the library, grouped by family, with the page that documents it. If you know
the type you need, this page gets you to the right constraints in one hop.

## Numbers

| Factory | Draws | Page |
| --- | --- | --- |
| `Any.Byte()` | `byte` | [Numbers](./numbers.en.md) |
| `Any.SByte()` | `sbyte` | [Numbers](./numbers.en.md) |
| `Any.Int16()` | `short` | [Numbers](./numbers.en.md) |
| `Any.Int32()` | `int` | [Numbers](./numbers.en.md) |
| `Any.Int64()` | `long` | [Numbers](./numbers.en.md) |
| `Any.UInt16()` | `ushort` | [Numbers](./numbers.en.md) |
| `Any.UInt32()` | `uint` | [Numbers](./numbers.en.md) |
| `Any.UInt64()` | `ulong` | [Numbers](./numbers.en.md) |
| `Any.Decimal()` | `decimal` | [Numbers](./numbers.en.md) |
| `Any.Double()` | `double` | [Numbers](./numbers.en.md) |
| `Any.Single()` | `float` | [Numbers](./numbers.en.md) |
| `Any.Int128()` 🔹 | `Int128` | [Numbers](./numbers.en.md) |
| `Any.UInt128()` 🔹 | `UInt128` | [Numbers](./numbers.en.md) |
| `Any.Half()` 🔹 | `Half` | [Numbers](./numbers.en.md) |

## Strings and characters

| Factory | Draws | Page |
| --- | --- | --- |
| `Any.String()` | `string` | [Strings and patterns](./strings.en.md) |
| `Any.Char()` | `char` | [Strings and patterns](./strings.en.md) |
| `Any.StringMatching(pattern)` | `string` matching a regular pattern | [Strings and patterns](./strings.en.md) |

## Dates and times

| Factory | Draws | Page |
| --- | --- | --- |
| `Any.DateTime()` | `DateTime` | [Dates and times](./dates-and-times.en.md) |
| `Any.DateTimeOffset()` | `DateTimeOffset` | [Dates and times](./dates-and-times.en.md) |
| `Any.TimeSpan()` | `TimeSpan` | [Dates and times](./dates-and-times.en.md) |
| `Any.DateOnly()` 🔹 | `DateOnly` | [Dates and times](./dates-and-times.en.md) |
| `Any.TimeOnly()` 🔹 | `TimeOnly` | [Dates and times](./dates-and-times.en.md) |

## Collections

| Factory | Draws | Page |
| --- | --- | --- |
| `Any.ArrayOf(item)` | `T[]` | [Collections](./collections.en.md) |
| `Any.ListOf(item)` | `List<T>` | [Collections](./collections.en.md) |
| `Any.SequenceOf(item)` | `IEnumerable<T>` | [Collections](./collections.en.md) |
| `Any.SetOf(item)` | `HashSet<T>` | [Collections](./collections.en.md) |
| `Any.DictionaryOf(keys, values)` | `Dictionary<TKey, TValue>` | [Collections](./collections.en.md) |

## Enums and choices

| Factory | Draws | Page |
| --- | --- | --- |
| `Any.Enum<TEnum>()` | a declared member of `TEnum` | [Enums and choices](./enums-and-choices.en.md) |
| `Any.OneOf(values)` | one of the listed values | [Enums and choices](./enums-and-choices.en.md) |
| `Any.ElementOf(collection)` | one element of a collection | [Enums and choices](./enums-and-choices.en.md) |
| `Any.Boolean()` | `bool` | [Enums and choices](./enums-and-choices.en.md) |

## Identifiers and URIs

| Factory | Draws | Page |
| --- | --- | --- |
| `Any.Guid()` | `Guid` | [Identifiers and URIs](./guids-and-uris.en.md) |
| `Any.Uri()` | `Uri` — web, WebSocket, FTP, mailto or relative | [Identifiers and URIs](./guids-and-uris.en.md) |

## Composition

These do not draw a new kind of value; they build a generator out of other generators.

| Factory | Produces | Page |
| --- | --- | --- |
| `generator.As(factory)` | `IAny<TResult>` | [Composition](../guides/composition.en.md) |
| `Any.Combine(…, compose)` | `IAny<TResult>` from 2 to 8 generators | [Composition](../guides/composition.en.md) |
| `Any.PairOf(first, second)` | `IAny<(T1, T2)>` | [Composition](../guides/composition.en.md) |
| `Any.TripleOf(first, second, third)` | `IAny<(T1, T2, T3)>` | [Composition](../guides/composition.en.md) |
| `generator.OrNull()` | `IAny<T?>`, `null` about half the time | [Composition](../guides/composition.en.md) |
| `generator.AsNullable()` | `IAny<T?>`, never `null` | [Composition](../guides/composition.en.md) |

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
