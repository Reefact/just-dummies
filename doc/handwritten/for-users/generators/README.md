# Generator reference

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./README.fr.md)

Every `Dummy.*` factory in the library, grouped by family, with the page that documents it. If you know
the type you need, this page gets you to the right constraints in one hop.

## Numbers

| Factory | Draws | Page |
| --- | --- | --- |
| `Dummy.Byte()` | `byte` | [Numbers](./numbers.en.md) |
| `Dummy.SByte()` | `sbyte` | [Numbers](./numbers.en.md) |
| `Dummy.Int16()` | `short` | [Numbers](./numbers.en.md) |
| `Dummy.Int32()` | `int` | [Numbers](./numbers.en.md) |
| `Dummy.Int64()` | `long` | [Numbers](./numbers.en.md) |
| `Dummy.UInt16()` | `ushort` | [Numbers](./numbers.en.md) |
| `Dummy.UInt32()` | `uint` | [Numbers](./numbers.en.md) |
| `Dummy.UInt64()` | `ulong` | [Numbers](./numbers.en.md) |
| `Dummy.Decimal()` | `decimal` | [Numbers](./numbers.en.md) |
| `Dummy.Double()` | `double` | [Numbers](./numbers.en.md) |
| `Dummy.Single()` | `float` | [Numbers](./numbers.en.md) |
| `Dummy.Int128()` 🔹 | `Int128` | [Numbers](./numbers.en.md) |
| `Dummy.UInt128()` 🔹 | `UInt128` | [Numbers](./numbers.en.md) |
| `Dummy.Half()` 🔹 | `Half` | [Numbers](./numbers.en.md) |

## Strings and characters

| Factory | Draws | Page |
| --- | --- | --- |
| `Dummy.String()` | `string` | [Strings and patterns](./strings.en.md) |
| `Dummy.Char()` | `char` | [Strings and patterns](./strings.en.md) |
| `Dummy.StringMatching(pattern)` | `string` matching a regular pattern | [Strings and patterns](./strings.en.md) |

## Dates and times

| Factory | Draws | Page |
| --- | --- | --- |
| `Dummy.DateTime()` | `DateTime` | [Dates and times](./dates-and-times.en.md) |
| `Dummy.DateTimeOffset()` | `DateTimeOffset` | [Dates and times](./dates-and-times.en.md) |
| `Dummy.TimeSpan()` | `TimeSpan` | [Dates and times](./dates-and-times.en.md) |
| `Dummy.DateOnly()` 🔹 | `DateOnly` | [Dates and times](./dates-and-times.en.md) |
| `Dummy.TimeOnly()` 🔹 | `TimeOnly` | [Dates and times](./dates-and-times.en.md) |

## Collections

| Factory | Draws | Page |
| --- | --- | --- |
| `Dummy.ArrayOf(item)` | `T[]` | [Collections](./collections.en.md) |
| `Dummy.ListOf(item)` | `List<T>` | [Collections](./collections.en.md) |
| `Dummy.SequenceOf(item)` | `IEnumerable<T>` | [Collections](./collections.en.md) |
| `Dummy.SetOf(item)` | `HashSet<T>` | [Collections](./collections.en.md) |
| `Dummy.DictionaryOf(keys, values)` | `Dictionary<TKey, TValue>` | [Collections](./collections.en.md) |

## Enums and choices

| Factory | Draws | Page |
| --- | --- | --- |
| `Dummy.Enum<TEnum>()` | a declared member of `TEnum` | [Enums and choices](./enums-and-choices.en.md) |
| `Dummy.OneOf(values)` | one of the listed values | [Enums and choices](./enums-and-choices.en.md) |
| `Dummy.ElementOf(collection)` | one element of a collection | [Enums and choices](./enums-and-choices.en.md) |
| `Dummy.Boolean()` | `bool` | [Enums and choices](./enums-and-choices.en.md) |

## Identifiers and URIs

| Factory | Draws | Page |
| --- | --- | --- |
| `Dummy.Guid()` | `Guid` | [Identifiers and URIs](./guids-and-uris.en.md) |
| `Dummy.Uri()` | `Uri` — web, WebSocket, FTP, mailto or relative | [Identifiers and URIs](./guids-and-uris.en.md) |

## Composition

These do not draw a new kind of value; they build a generator out of other generators.

| Factory | Produces | Page |
| --- | --- | --- |
| `generator.As(factory)` | `IDummy<TResult>` | [Composition](../guides/composition.en.md) |
| `Dummy.Combine(…, compose)` | `IDummy<TResult>` from 2 to 8 generators | [Composition](../guides/composition.en.md) |
| `Dummy.PairOf(first, second)` | `IDummy<(T1, T2)>` | [Composition](../guides/composition.en.md) |
| `Dummy.TripleOf(first, second, third)` | `IDummy<(T1, T2, T3)>` | [Composition](../guides/composition.en.md) |
| `generator.OrNull()` | `IDummy<T?>`, `null` about half the time | [Composition](../guides/composition.en.md) |
| `generator.AsNullable()` | `IDummy<T?>`, never `null` | [Composition](../guides/composition.en.md) |

## Reproducibility

| Factory | Does | Page |
| --- | --- | --- |
| `Dummy.Reproducibly(body)` | runs a body under a fresh seed, reporting it on failure | [Reproducibility](../guides/reproducibility.en.md) |
| `Dummy.Reproducibly(seed, body)` | replays a body under a known seed | [Reproducibility](../guides/reproducibility.en.md) |
| `Dummy.ReproduciblyAsync(body)` | the awaitable counterpart | [Reproducibility](../guides/reproducibility.en.md) |
| `Dummy.UseSeed(seed)` | pins the ambient context until disposed | [Reproducibility](../guides/reproducibility.en.md) |
| `Dummy.WithSeed(seed)` | returns an isolated `DummyContext` | [Reproducibility](../guides/reproducibility.en.md) |

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
