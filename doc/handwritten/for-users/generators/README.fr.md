# Référence des générateurs

🌍 **Langues :**  
🇬🇧 [English](./README.md) | 🇫🇷 Français (ce fichier)

Toutes les fabriques `Dummy.*` de la bibliothèque, regroupées par famille, avec la page qui les
documente. Si vous connaissez le type dont vous avez besoin, cette page vous amène aux bonnes
contraintes en un saut.

## Nombres

| Fabrique | Tire | Page |
| --- | --- | --- |
| `Dummy.Byte()` | `byte` | [Nombres](./numbers.fr.md) |
| `Dummy.SByte()` | `sbyte` | [Nombres](./numbers.fr.md) |
| `Dummy.Int16()` | `short` | [Nombres](./numbers.fr.md) |
| `Dummy.Int32()` | `int` | [Nombres](./numbers.fr.md) |
| `Dummy.Int64()` | `long` | [Nombres](./numbers.fr.md) |
| `Dummy.UInt16()` | `ushort` | [Nombres](./numbers.fr.md) |
| `Dummy.UInt32()` | `uint` | [Nombres](./numbers.fr.md) |
| `Dummy.UInt64()` | `ulong` | [Nombres](./numbers.fr.md) |
| `Dummy.Decimal()` | `decimal` | [Nombres](./numbers.fr.md) |
| `Dummy.Double()` | `double` | [Nombres](./numbers.fr.md) |
| `Dummy.Single()` | `float` | [Nombres](./numbers.fr.md) |
| `Dummy.Int128()` 🔹 | `Int128` | [Nombres](./numbers.fr.md) |
| `Dummy.UInt128()` 🔹 | `UInt128` | [Nombres](./numbers.fr.md) |
| `Dummy.Half()` 🔹 | `Half` | [Nombres](./numbers.fr.md) |

## Chaînes et caractères

| Fabrique | Tire | Page |
| --- | --- | --- |
| `Dummy.String()` | `string` | [Chaînes et motifs](./strings.fr.md) |
| `Dummy.Char()` | `char` | [Chaînes et motifs](./strings.fr.md) |
| `Dummy.StringMatching(pattern)` | une `string` satisfaisant un motif régulier | [Chaînes et motifs](./strings.fr.md) |

## Dates et heures

| Fabrique | Tire | Page |
| --- | --- | --- |
| `Dummy.DateTime()` | `DateTime` | [Dates et heures](./dates-and-times.fr.md) |
| `Dummy.DateTimeOffset()` | `DateTimeOffset` | [Dates et heures](./dates-and-times.fr.md) |
| `Dummy.TimeSpan()` | `TimeSpan` | [Dates et heures](./dates-and-times.fr.md) |
| `Dummy.DateOnly()` 🔹 | `DateOnly` | [Dates et heures](./dates-and-times.fr.md) |
| `Dummy.TimeOnly()` 🔹 | `TimeOnly` | [Dates et heures](./dates-and-times.fr.md) |

## Collections

| Fabrique | Tire | Page |
| --- | --- | --- |
| `Dummy.ArrayOf(item)` | `T[]` | [Collections](./collections.fr.md) |
| `Dummy.ListOf(item)` | `List<T>` | [Collections](./collections.fr.md) |
| `Dummy.SequenceOf(item)` | `IEnumerable<T>` | [Collections](./collections.fr.md) |
| `Dummy.SetOf(item)` | `HashSet<T>` | [Collections](./collections.fr.md) |
| `Dummy.DictionaryOf(keys, values)` | `Dictionary<TKey, TValue>` | [Collections](./collections.fr.md) |

## Énumérations et choix

| Fabrique | Tire | Page |
| --- | --- | --- |
| `Dummy.Enum<TEnum>()` | un membre déclaré de `TEnum` | [Énumérations et choix](./enums-and-choices.fr.md) |
| `Dummy.OneOf(values)` | l'une des valeurs listées | [Énumérations et choix](./enums-and-choices.fr.md) |
| `Dummy.ElementOf(collection)` | un élément d'une collection | [Énumérations et choix](./enums-and-choices.fr.md) |
| `Dummy.Boolean()` | `bool` | [Énumérations et choix](./enums-and-choices.fr.md) |

## Identifiants et URI

| Fabrique | Tire | Page |
| --- | --- | --- |
| `Dummy.Guid()` | `Guid` | [Identifiants et URI](./guids-and-uris.fr.md) |
| `Dummy.Uri()` | `Uri` — web, WebSocket, FTP, mailto ou relative | [Identifiants et URI](./guids-and-uris.fr.md) |

## Composition

Celles-ci ne tirent pas un nouveau genre de valeur : elles construisent un générateur à partir
d'autres générateurs.

| Fabrique | Produit | Page |
| --- | --- | --- |
| `generator.As(factory)` | `IDummy<TResult>` | [Composition](../guides/composition.fr.md) |
| `Dummy.Combine(…, compose)` | `IDummy<TResult>` à partir de 2 à 8 générateurs | [Composition](../guides/composition.fr.md) |
| `Dummy.PairOf(first, second)` | `IDummy<(T1, T2)>` | [Composition](../guides/composition.fr.md) |
| `Dummy.TripleOf(first, second, third)` | `IDummy<(T1, T2, T3)>` | [Composition](../guides/composition.fr.md) |
| `generator.OrNull()` | `IDummy<T?>`, `null` une fois sur deux environ | [Composition](../guides/composition.fr.md) |
| `generator.AsNullable()` | `IDummy<T?>`, jamais `null` | [Composition](../guides/composition.fr.md) |

## Reproductibilité

| Fabrique | Rôle | Page |
| --- | --- | --- |
| `Dummy.Reproducibly(body)` | exécute un corps sous une graine fraîche, rapportée en cas d'échec | [Reproductibilité](../guides/reproducibility.fr.md) |
| `Dummy.Reproducibly(seed, body)` | rejoue un corps sous une graine connue | [Reproductibilité](../guides/reproducibility.fr.md) |
| `Dummy.ReproduciblyAsync(body)` | la contrepartie attendable | [Reproductibilité](../guides/reproducibility.fr.md) |
| `Dummy.UseSeed(seed)` | épingle le contexte ambiant jusqu'à libération | [Reproductibilité](../guides/reproducibility.fr.md) |
| `Dummy.WithSeed(seed)` | renvoie un `DummyContext` isolé | [Reproductibilité](../guides/reproducibility.fr.md) |

🔹 Disponible sur le seul asset `net8.0` — le type lui-même n'existe pas en deçà de .NET 8.

## Le vocabulaire partagé

Les noms de contraintes signifient la même chose partout où ils apparaissent : l'essentiel de la
surface s'apprend donc une fois pour toutes.

| Nom | Partout où il apparaît |
| --- | --- |
| `Between(min, max)` | inclusif aux deux extrémités |
| `Except(…)` / `DifferentFrom(x)` | retire des valeurs du domaine |
| `OneOf(…)` | restreint le tirage à un vivier explicite |
| `NonEmpty()` / `Empty()` | les cas non vide et vide, pour les chaînes, les collections et `Guid` |
| `WithCount*` / `WithLength*` | la famille de taille, respectivement sur les collections et les chaînes |

---

[← Sommaire de la documentation](../README.fr.md)
