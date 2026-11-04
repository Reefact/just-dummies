# Référence des générateurs

🌍 **Langues :**  
🇬🇧 [English](./README.md) | 🇫🇷 Français (ce fichier)

Toutes les fabriques `Any.*` de la bibliothèque, regroupées par famille, avec la page qui les
documente. Si vous connaissez le type dont vous avez besoin, cette page vous amène aux bonnes
contraintes en un saut.

La colonne **Builder** nomme le type que retourne chaque fabrique — celui à écrire pour un champ, le type
de retour d'un helper ou le paramètre d'une recette. Il suit le nom de la fabrique : `Any.Int32()`
retourne un `AnyInt32`, `Any.ListOf(item)` un `AnyList<T>`. Trois font exception : `Any.StringMatching`
retourne un `AnyPattern`, `Any.ElementOf` un `AnyOneOf<T>`, et `Any.PermutationOf` un simple
`IAny<List<T>>`, sans contrainte propre à ajouter.

## Nombres

| Fabrique | Builder | Tire | Page |
| --- | --- | --- | --- |
| `Any.Byte()` | `AnyByte` | `byte` | [Nombres](./numbers.fr.md) |
| `Any.SByte()` | `AnySByte` | `sbyte` | [Nombres](./numbers.fr.md) |
| `Any.Int16()` | `AnyInt16` | `short` | [Nombres](./numbers.fr.md) |
| `Any.Int32()` | `AnyInt32` | `int` | [Nombres](./numbers.fr.md) |
| `Any.Int64()` | `AnyInt64` | `long` | [Nombres](./numbers.fr.md) |
| `Any.UInt16()` | `AnyUInt16` | `ushort` | [Nombres](./numbers.fr.md) |
| `Any.UInt32()` | `AnyUInt32` | `uint` | [Nombres](./numbers.fr.md) |
| `Any.UInt64()` | `AnyUInt64` | `ulong` | [Nombres](./numbers.fr.md) |
| `Any.Decimal()` | `AnyDecimal` | `decimal` | [Nombres](./numbers.fr.md) |
| `Any.Double()` | `AnyDouble` | `double` | [Nombres](./numbers.fr.md) |
| `Any.Single()` | `AnySingle` | `float` | [Nombres](./numbers.fr.md) |
| `Any.Int128()` 🔹 | `AnyInt128` | `Int128` | [Nombres](./numbers.fr.md) |
| `Any.UInt128()` 🔹 | `AnyUInt128` | `UInt128` | [Nombres](./numbers.fr.md) |
| `Any.Half()` 🔹 | `AnyHalf` | `Half` | [Nombres](./numbers.fr.md) |

## Chaînes et caractères

| Fabrique | Builder | Tire | Page |
| --- | --- | --- | --- |
| `Any.String()` | `AnyString` | `string` | [Chaînes et motifs](./strings.fr.md) |
| `Any.Char()` | `AnyChar` | `char` | [Chaînes et motifs](./strings.fr.md) |
| `Any.StringMatching(pattern)` | `AnyPattern` | une `string` satisfaisant un motif régulier | [Chaînes et motifs](./strings.fr.md) |

## Dates et heures

| Fabrique | Builder | Tire | Page |
| --- | --- | --- | --- |
| `Any.DateTime()` | `AnyDateTime` | `DateTime` | [Dates et heures](./dates-and-times.fr.md) |
| `Any.DateTimeOffset()` | `AnyDateTimeOffset` | `DateTimeOffset` | [Dates et heures](./dates-and-times.fr.md) |
| `Any.TimeSpan()` | `AnyTimeSpan` | `TimeSpan` | [Dates et heures](./dates-and-times.fr.md) |
| `Any.DateOnly()` 🔹 | `AnyDateOnly` | `DateOnly` | [Dates et heures](./dates-and-times.fr.md) |
| `Any.TimeOnly()` 🔹 | `AnyTimeOnly` | `TimeOnly` | [Dates et heures](./dates-and-times.fr.md) |

## Collections

| Fabrique | Builder | Tire | Page |
| --- | --- | --- | --- |
| `Any.ArrayOf(item)` | `AnyArray<T>` | `T[]` | [Collections](./collections.fr.md) |
| `Any.ListOf(item)` | `AnyList<T>` | `List<T>` | [Collections](./collections.fr.md) |
| `Any.SequenceOf(item)` | `AnySequence<T>` | `IEnumerable<T>` | [Collections](./collections.fr.md) |
| `Any.SetOf(item)` | `AnySet<T>` | `HashSet<T>` | [Collections](./collections.fr.md) |
| `Any.DictionaryOf(keys, values)` | `AnyDictionary<TKey, TValue>` | `Dictionary<TKey, TValue>` | [Collections](./collections.fr.md) |
| `Any.PermutationOf(items)` | `IAny<List<T>>` | `List<T>`, les éléments dans un ordre aléatoire | [Collections](./collections.fr.md) |

## Énumérations et choix

| Fabrique | Builder | Tire | Page |
| --- | --- | --- | --- |
| `Any.Enum<TEnum>()` | `AnyEnum<TEnum>` | un membre déclaré de `TEnum` | [Énumérations et choix](./enums-and-choices.fr.md) |
| `Any.OneOf(values)` | `AnyOneOf<T>` | l'une des valeurs listées | [Énumérations et choix](./enums-and-choices.fr.md) |
| `Any.ElementOf(collection)` | `AnyOneOf<T>` | un élément d'une collection | [Énumérations et choix](./enums-and-choices.fr.md) |
| `Any.Boolean()` | `AnyBoolean` | `bool` | [Énumérations et choix](./enums-and-choices.fr.md) |

## Identifiants et URI

| Fabrique | Builder | Tire | Page |
| --- | --- | --- | --- |
| `Any.Guid()` | `AnyGuid` | `Guid` | [Identifiants et URI](./guids-and-uris.fr.md) |
| `Any.Uri()` | `AnyUri` | `Uri` — web, WebSocket, FTP, mailto ou relative | [Identifiants et URI](./guids-and-uris.fr.md) |

## Composition

Celles-ci ne tirent pas un nouveau genre de valeur : elles construisent un générateur à partir
d'autres générateurs.

| Fabrique | Produit | Page |
| --- | --- | --- |
| `generator.As(factory)` | `IAny<TResult>` | [Composition](../guides/composition.fr.md) |
| `Any.Combine(…, compose)` | `IAny<TResult>` à partir de 2 à 8 générateurs | [Composition](../guides/composition.fr.md) |
| `Any.PairOf(first, second)` | `IAny<(T1, T2)>` | [Composition](../guides/composition.fr.md) |
| `Any.TripleOf(first, second, third)` | `IAny<(T1, T2, T3)>` | [Composition](../guides/composition.fr.md) |
| `Any.Pair(item1, item2)` | `AnyPair<T>`, deux valeurs d'un même type ordonné, ordonnées par `Having` | [Composition](../guides/composition.fr.md) |
| `generator.OrNull()` | `IAny<T?>`, `null` un tirage sur dix | [Composition](../guides/composition.fr.md) |
| `generator.AsNullable()` | `IAny<T?>`, jamais `null` | [Composition](../guides/composition.fr.md) |
| `Any.Fixed(value)` | `IAny<T>` renvoyant la valeur que vous avez choisie, sans rien tirer | [Composition](../guides/composition.fr.md) |

## Reproductibilité

| Fabrique | Rôle | Page |
| --- | --- | --- |
| `Any.Reproducibly(body)` | exécute un corps sous une graine fraîche, rapportée en cas d'échec | [Reproductibilité](../guides/reproducibility.fr.md) |
| `Any.Reproducibly(seed, body)` | rejoue un corps sous une graine connue | [Reproductibilité](../guides/reproducibility.fr.md) |
| `Any.ReproduciblyAsync(body)` | la contrepartie attendable | [Reproductibilité](../guides/reproducibility.fr.md) |
| `Any.UseSeed(seed)` | épingle le contexte ambiant jusqu'à libération | [Reproductibilité](../guides/reproducibility.fr.md) |
| `Any.WithSeed(seed)` | renvoie un `AnyContext` isolé | [Reproductibilité](../guides/reproducibility.fr.md) |

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
