# Collections

🌍 **Langues :**  
🇬🇧 [English](./collections.en.md) | 🇫🇷 Français (ce fichier)

Un générateur de collection se construit à partir d'un générateur d'**élément** : vous décrivez un
article, et le générateur de collection en tire autant que les contraintes d'effectif le demandent.
Tout ce que vous savez déjà sur la contrainte d'un scalaire s'applique à l'élément.

## Les cinq générateurs de collection

| Fabrique | Tire | Ajoute |
| --- | --- | --- |
| `Dummy.ArrayOf(item)` | `T[]` | `Distinct()` |
| `Dummy.ListOf(item)` | `List<T>` | `Distinct()` |
| `Dummy.SequenceOf(item)` | `IEnumerable<T>` | `Distinct()` |
| `Dummy.SetOf(item)` | `HashSet<T>` | distinction par construction |
| `Dummy.DictionaryOf(keys, values)` | `Dictionary<TKey, TValue>` | contraintes de clés |

```csharp
int[]            quantities = Dummy.ArrayOf(Dummy.Int32().Between(1, 100)).WithCount(5).Generate();
List<string>     references = Dummy.ListOf(Dummy.String().StartingWith("ORD-").WithLength(12)).NonEmpty().Generate();
IEnumerable<Guid> ids       = Dummy.SequenceOf(Dummy.Guid().NonEmpty()).WithCountBetween(2, 6).Generate();
HashSet<OrderStatus> states = Dummy.SetOf(Dummy.Enum<OrderStatus>()).WithMaxCount(3).Generate();
```

## Le vocabulaire d'effectif partagé

Tout générateur de collection porte les mêmes six contraintes d'effectif :

```csharp
IDummy<int> anyQuantity = Dummy.Int32().Between(1, 100);

int[] exactly5   = Dummy.ArrayOf(anyQuantity).WithCount(5).Generate();
int[] two2Six    = Dummy.ArrayOf(anyQuantity).WithCountBetween(2, 6).Generate();
int[] atLeast3   = Dummy.ArrayOf(anyQuantity).WithMinCount(3).Generate();
int[] atMost10   = Dummy.ArrayOf(anyQuantity).WithMaxCount(10).Generate();
int[] notEmpty   = Dummy.ArrayOf(anyQuantity).NonEmpty().Generate();
int[] empty      = Dummy.ArrayOf(anyQuantity).Empty().Generate();
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
List<OrderStatus> withDraft = Dummy.ListOf(Dummy.Enum<OrderStatus>())
                                 .WithCountBetween(3, 6)
                                 .Containing(OrderStatus.Draft)
                                 .Generate();

// Une valeur satisfaisant un second générateur doit être présente.
List<int> withABigOne = Dummy.ListOf(Dummy.Int32().Between(1, 100))
                           .WithCountBetween(3, 6)
                           .ContainingAny(Dummy.Int32().Between(90, 100))
                           .Generate();
```

`ContainingAny` est la contrainte à saisir quand le test a besoin d'« au moins un élément qui
qualifie » sans figer laquelle des valeurs qualifie — l'équivalent, pour une collection, de
contraindre plutôt que d'affirmer.

## Distinction

`Distinct()` exige que les éléments tirés diffèrent. `Dummy.SetOf` y parvient par construction — un
`HashSet<T>` ne peut pas contenir de doublon — tandis que `Distinct()` sur un tableau, une liste ou
une séquence est une exigence que le générateur doit activement satisfaire :

```csharp
int[]        distinctIds = Dummy.ArrayOf(Dummy.Int32().Between(1, 1_000)).WithCount(10).Distinct().Generate();
List<string> distinctRefs = Dummy.ListOf(Dummy.String().Alpha().WithLength(6)).WithCount(4).Distinct().Generate();

// Avec un comparateur explicite, quand l'égalité par défaut n'est pas celle qui compte.
List<string> caseInsensitive = Dummy.ListOf(Dummy.String().Alpha().WithLength(6))
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
fixe de tentatives, puis une `DummyGenerationException` explicite. Jamais une boucle non bornée.

**La distinction n'a de sens qu'avec une égalité de valeur.** La déclarer sur un type référence qui
ne redéfinit pas `Equals` est satisfait trivialement — chaque instance diffère — si bien que la
collection peut toujours contenir deux fois ce qu'un lecteur appellerait la même valeur. C'est le
diagnostic [JD028](../analyzers/JD028.fr.md).

## Dictionnaires

`Dummy.DictionaryOf` prend un générateur pour les clés et un pour les valeurs :

```csharp
Dictionary<string, int> stock = Dummy.DictionaryOf(
                                       Dummy.String().Alpha().InUpperCase().WithLength(3),
                                       Dummy.Int32().Between(0, 500))
                                   .WithCountBetween(2, 5)
                                   .Generate();
```

Les clés sont distinctes par construction. Une seconde surcharge prend un
`IEqualityComparer<TKey>` quand l'égalité par défaut n'est pas celle qu'utilise votre domaine.

Trois contraintes sont propres aux dictionnaires :

```csharp
IDummy<string> anyCode  = Dummy.String().Alpha().InUpperCase().WithLength(3);
IDummy<int>    anyLevel = Dummy.Int32().Between(0, 500);

// Une clé qui doit être présente.
Dictionary<string, int> withKey = Dummy.DictionaryOf(anyCode, anyLevel)
                                     .WithCountBetween(2, 5)
                                     .ContainingKey("ABC")
                                     .Generate();

// Une entrée entière qui doit être présente.
Dictionary<string, int> withEntry = Dummy.DictionaryOf(anyCode, anyLevel)
                                       .WithCountBetween(2, 5)
                                       .ContainingEntry("ABC", 42)
                                       .Generate();

// Une clé satisfaisant un autre générateur doit être présente.
Dictionary<string, int> withAnyKey = Dummy.DictionaryOf(anyCode, anyLevel)
                                        .WithCountBetween(2, 5)
                                        .ContainingAnyKey(Dummy.String().OneOf("ABC", "XYZ"))
                                        .Generate();
```

## Collections de vos propres types

Parce qu'un générateur composé est un `IDummy<T>` ordinaire, une collection d'objets-valeurs ou
d'agrégats ne demande rien de nouveau :

```csharp
IDummy<OrderReference> anyReference = Dummy.String()
                                       .StartingWith("ORD-")
                                       .WithLength(12)
                                       .As(OrderReference.Create);

List<OrderReference> basket = Dummy.ListOf(anyReference).WithCountBetween(1, 4).Generate();
```

---

[← Référence des générateurs](./README.fr.md) · [Sommaire de la documentation](../README.fr.md)
