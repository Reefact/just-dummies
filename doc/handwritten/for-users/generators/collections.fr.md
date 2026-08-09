# Collections

🌍 **Langues :**  
🇬🇧 [English](./collections.en.md) | 🇫🇷 Français (ce fichier)

Un générateur de collection se construit à partir d'un générateur d'**élément** : vous décrivez un
article, et le générateur de collection en tire autant que les contraintes d'effectif le demandent.
Tout ce que vous savez déjà sur la contrainte d'un scalaire s'applique à l'élément.

## Les cinq générateurs de collection

| Fabrique | Tire | Ajoute |
| --- | --- | --- |
| `Any.ArrayOf(item)` | `T[]` | `Distinct()` |
| `Any.ListOf(item)` | `List<T>` | `Distinct()` |
| `Any.SequenceOf(item)` | `IEnumerable<T>` | `Distinct()` |
| `Any.SetOf(item)` | `HashSet<T>` | distinction par construction |
| `Any.DictionaryOf(keys, values)` | `Dictionary<TKey, TValue>` | contraintes de clés |

```csharp
int[]            quantities = Any.ArrayOf(Any.Int32().Between(1, 100)).WithCount(5).Generate();
List<string>     references = Any.ListOf(Any.String().StartingWith("ORD-").WithLength(12)).NonEmpty().Generate();
IEnumerable<Guid> ids       = Any.SequenceOf(Any.Guid().NonEmpty()).WithCountBetween(2, 6).Generate();
HashSet<OrderStatus> states = Any.SetOf(Any.Enum<OrderStatus>()).WithMaxCount(3).Generate();
```

## Le vocabulaire d'effectif partagé

Tout générateur de collection porte les mêmes sept contraintes :

```csharp
IAny<int> anyQuantity = Any.Int32().Between(1, 100);

int[] exactly5   = Any.ArrayOf(anyQuantity).WithCount(5).Generate();
int[] two2Six    = Any.ArrayOf(anyQuantity).WithCountBetween(2, 6).Generate();
int[] atLeast3   = Any.ArrayOf(anyQuantity).WithMinCount(3).Generate();
int[] atMost10   = Any.ArrayOf(anyQuantity).WithMaxCount(10).Generate();
int[] notEmpty   = Any.ArrayOf(anyQuantity).NonEmpty().Generate();
int[] empty      = Any.ArrayOf(anyQuantity).Empty().Generate();
```

`Empty()` n'est pas une curiosité : la collection vide est le cas le plus susceptible de casser le
code de production, et la nommer se lit mieux que `WithCount(0)`.

Des effectifs incompatibles entre eux — un minimum au-dessus d'un maximum, `WithCount(3)` à côté
d'`Empty()` — sont refusés avec un message les nommant tous deux, et l'analyzer
[JD016](../analyzers/JD016.fr.md) attrape les cas constants dès la compilation.

Un effectif supérieur à 1 000 000 est refusé
([ADR-0029](../../for-maintainers/adr/0029-let-a-size-maximum-cap-without-steering-the-draw.fr.md)).

## Exiger des éléments précis

Deux contraintes placent quelque chose de connu dans une collection par ailleurs arbitraire :

```csharp
// Une valeur précise doit être présente.
List<OrderStatus> withDraft = Any.ListOf(Any.Enum<OrderStatus>())
                                 .WithCountBetween(3, 6)
                                 .Containing(OrderStatus.Draft)
                                 .Generate();

// Une valeur satisfaisant un second générateur doit être présente.
List<int> withABigOne = Any.ListOf(Any.Int32().Between(1, 100))
                           .WithCountBetween(3, 6)
                           .ContainingAny(Any.Int32().Between(90, 100))
                           .Generate();
```

`ContainingAny` est la contrainte à saisir quand le test a besoin d'« au moins un élément qui
qualifie » sans figer laquelle des valeurs qualifie — l'équivalent, pour une collection, de
contraindre plutôt que d'affirmer.

## Distinction

`Distinct()` exige que les éléments tirés diffèrent. `Any.SetOf` y parvient par construction — un
`HashSet<T>` ne peut pas contenir de doublon — tandis que `Distinct()` sur un tableau, une liste ou
une séquence est une exigence que le générateur doit activement satisfaire :

```csharp
int[]        distinctIds = Any.ArrayOf(Any.Int32().Between(1, 1_000)).WithCount(10).Distinct().Generate();
List<string> distinctRefs = Any.ListOf(Any.String().Alpha().WithLength(6)).WithCount(4).Distinct().Generate();

// Avec un comparateur explicite, quand l'égalité par défaut n'est pas celle qui compte.
List<string> caseInsensitive = Any.ListOf(Any.String().Alpha().WithLength(6))
                                  .WithCount(4)
                                  .Distinct(StringComparer.OrdinalIgnoreCase)
                                  .Generate();
```

Deux points méritent d'être compris ici.

**La distinction est filtrée par la cardinalité.** Avant de tirer, le générateur compare ce que vous
demandez à ce que le générateur d'élément peut réellement produire. Demander dix booléens distincts,
ou cent valeurs distinctes issues d'un vivier de trois, est refusé immédiatement et nommément plutôt
que tenté
([ADR-0004](../../for-maintainers/adr/0004-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md)).
L'analyzer [JD016](../analyzers/JD016.fr.md) signale les cas constants dès la compilation.

**Là où l'effectif est atteignable mais serré, un retirage borné termine le travail** — un nombre
fixe de tentatives, puis une `AnyGenerationException` explicite. Jamais une boucle non bornée.

**La distinction n'a de sens qu'avec une égalité de valeur.** La déclarer sur un type référence qui
ne redéfinit pas `Equals` est satisfait trivialement — chaque instance diffère — si bien que la
collection peut toujours contenir deux fois ce qu'un lecteur appellerait la même valeur. C'est le
diagnostic [JD028](../analyzers/JD028.fr.md).

## Dictionnaires

`Any.DictionaryOf` prend un générateur pour les clés et un pour les valeurs :

```csharp
Dictionary<string, int> stock = Any.DictionaryOf(
                                       Any.String().Alpha().UpperCase().WithLength(3),
                                       Any.Int32().Between(0, 500))
                                   .WithCountBetween(2, 5)
                                   .Generate();
```

Les clés sont distinctes par construction. Une seconde surcharge prend un
`IEqualityComparer<TKey>` quand l'égalité par défaut n'est pas celle qu'utilise votre domaine.

Trois contraintes sont propres aux dictionnaires :

```csharp
IAny<string> anyCode  = Any.String().Alpha().UpperCase().WithLength(3);
IAny<int>    anyLevel = Any.Int32().Between(0, 500);

// Une clé qui doit être présente.
Dictionary<string, int> withKey = Any.DictionaryOf(anyCode, anyLevel)
                                     .WithCountBetween(2, 5)
                                     .ContainingKey("ABC")
                                     .Generate();

// Une entrée entière qui doit être présente.
Dictionary<string, int> withEntry = Any.DictionaryOf(anyCode, anyLevel)
                                       .WithCountBetween(2, 5)
                                       .ContainingEntry("ABC", 42)
                                       .Generate();

// Une clé satisfaisant un autre générateur doit être présente.
Dictionary<string, int> withAnyKey = Any.DictionaryOf(anyCode, anyLevel)
                                        .WithCountBetween(2, 5)
                                        .ContainingAnyKey(Any.String().OneOf("ABC", "XYZ"))
                                        .Generate();
```

## Collections de vos propres types

Parce qu'un générateur composé est un `IAny<T>` ordinaire, une collection d'objets-valeurs ou
d'agrégats ne demande rien de nouveau :

```csharp
IAny<OrderReference> anyReference = Any.String()
                                       .StartingWith("ORD-")
                                       .WithLength(12)
                                       .As(OrderReference.Create);

List<OrderReference> basket = Any.ListOf(anyReference).WithCountBetween(1, 4).Generate();
```

---

[← Référence des générateurs](./README.fr.md) · [Sommaire de la documentation](../README.fr.md)
