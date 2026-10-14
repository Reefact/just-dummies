# Dates et heures

🌍 **Langues :**  
🇬🇧 [English](./dates-and-times.en.md) | 🇫🇷 Français (ce fichier)

Cinq générateurs couvrent les types temporels. Ils partagent un vocabulaire d'ordonnancement, et deux
d'entre eux ajoutent une dimension que les autres n'ont pas : la granularité, et le décalage horaire.

## Les cinq générateurs

| Fabrique | Tire | Disponibilité |
| --- | --- | --- |
| `Any.DateTime()` | `DateTime` | partout |
| `Any.DateTimeOffset()` | `DateTimeOffset` | partout |
| `Any.TimeSpan()` | `TimeSpan` | partout |
| `Any.DateOnly()` | `DateOnly` | .NET 8+ |
| `Any.TimeOnly()` | `TimeOnly` | .NET 8+ |

## Ordonnancement

Les instants s'ordonnent plutôt qu'ils ne se comparent en taille : le vocabulaire est donc temporel
— `After` et `Before` plutôt que `GreaterThan` et `LessThan` :

```csharp
DateTime ordered   = Any.DateTime().Between(new DateTime(2020, 1, 1), new DateTime(2025, 12, 31)).Generate();
DateTime recent    = Any.DateTime().After(new DateTime(2024, 1, 1)).Generate();
DateTime onOrAfter = Any.DateTime().AfterOrEqualTo(new DateTime(2024, 1, 1)).Generate();
DateTime past      = Any.DateTime().Before(new DateTime(2030, 1, 1)).Generate();
DateTime onOrBefore = Any.DateTime().BeforeOrEqualTo(new DateTime(2030, 1, 1)).Generate();
```

`TimeSpan` est une durée et non un instant : il conserve donc le vocabulaire numérique et ajoute la
famille du signe :

```csharp
TimeSpan timeout  = Any.TimeSpan().Between(TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5)).Generate();
TimeSpan positive = Any.TimeSpan().Positive().Generate();
TimeSpan nonZero  = Any.TimeSpan().NonZero().Generate();
TimeSpan shorter  = Any.TimeSpan().LessThan(TimeSpan.FromHours(1)).Generate();
```

## Granularité

`WithGranularity` aligne la valeur tirée sur une grille. C'est ce qui transforme un instant brut en
un instant que votre domaine stockerait réellement :

```csharp
// Minutes entières : plus de secondes ni de ticks parasites pour casser une assertion d'égalité.
DateTime appointment = Any.DateTime()
                          .Between(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31))
                          .WithGranularity(TimeSpan.FromMinutes(1))
                          .Generate();

// Jours entiers.
DateTime businessDay = Any.DateTime().WithGranularity(TimeSpan.FromDays(1)).Generate();

// Une durée en secondes entières.
TimeSpan retryAfter = Any.TimeSpan().Positive().WithGranularity(TimeSpan.FromSeconds(1)).Generate();
```

Sans elle, un instant tiré porte une précision inférieure à la seconde, et un test qui le fait
transiter par un stockage tronquant aux secondes échoue pour une raison sans rapport avec le code
testé. Déclarer la granularité que votre stockage a réellement est le remède, et c'est un véritable
invariant du domaine plutôt qu'un contournement.

`DateOnly` n'a pas de contrainte de granularité — c'est déjà un jour entier.

## `DateTimeOffset` ne porte un décalage que si vous en déclarez un

Un `DateTimeOffset` est un instant **et** un décalage par rapport à UTC. Sans contrainte, JustDummies
fait varier l'instant et laisse le décalage à `+00:00` : le décalage est une dimension que l'on
active explicitement, avec `WithOffset` pour un décalage fixe en minutes entières ou
`WithOffsetBetween` pour un décalage borné
([ADR-0016](../../for-maintainers/adr/0016-vary-the-datetimeoffset-offset-dimension.fr.md)).
Déclarez-en un quand le code testé y est sensible : un tirage sans contrainte n'atteint jamais le
code qui suppose l'heure locale.

```csharp
DateTimeOffset anyUtcInstant      = Any.DateTimeOffset().Generate();                 // le décalage reste +00:00
DateTimeOffset anyShiftedInstant  = Any.DateTimeOffset().WithOffset(TimeSpan.FromHours(2)).Generate();
DateTimeOffset anyEuropeanInstant = Any.DateTimeOffset()
                                       .WithOffsetBetween(TimeSpan.FromHours(0), TimeSpan.FromHours(3))
                                       .Generate();
```

Combiné à `OneOf(...)`, un décalage déclaré **filtre le vivier** au lieu de réécrire les valeurs
qu'il contient : une valeur du vivier est rendue telle quelle, décalage compris, et une valeur dont
le décalage n'est pas admis par la déclaration n'est jamais tirée
([ADR-0030](../../for-maintainers/adr/0030-filter-the-datetimeoffset-pool-by-the-declared-offset.fr.md)).

## Le piège de `DateTime.Now`

Voici l'erreur qui mérite d'être nommée, car elle survit à la revue et échoue à minuit :

```csharp
// Fragile : le dummy est tiré relativement à une horloge que le test ne contrôle pas.
DateTime createdAt = Any.DateTime().Before(DateTime.Now).Generate();
```

Le test dépend désormais du moment où il s'exécute. Pire, il n'est pas reproductible : rejouer la
graine rejoue le tirage, pas l'horloge.

Épinglez un instant que le test possède, et contraignez relativement à lui :

```csharp
DateTime now       = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
DateTime createdAt = Any.DateTime().Before(now).AfterOrEqualTo(now.AddYears(-1)).Generate();
```

## Appartenance et exclusion

Tout générateur temporel porte la famille habituelle du vivier et de l'exclusion :

```csharp
DateTime quarterEnd = Any.DateTime()
                         .OneOf(new DateTime(2025, 3, 31), new DateTime(2025, 6, 30), new DateTime(2025, 9, 30))
                         .Generate();

DateTime notEpoch = Any.DateTime()
                       .Between(new DateTime(2020, 1, 1), new DateTime(2025, 1, 1))
                       .DifferentFrom(new DateTime(2020, 1, 1))
                       .Generate();
```

## `DateOnly` et `TimeOnly`

Disponibles à partir de .NET 8, avec le même vocabulaire d'ordonnancement :

<!-- jd:net8 -->
```csharp
DateOnly dueDate = Any.DateOnly()
                      .Between(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31))
                      .Generate();

TimeOnly openingTime = Any.TimeOnly()
                          .Between(new TimeOnly(8, 0), new TimeOnly(20, 0))
                          .WithGranularity(TimeSpan.FromMinutes(15))
                          .Generate();
```

Sur `netstandard2.0`, ces types n'existent pas, et leurs fabriques non plus. Tout le reste de cette
page est disponible sur toutes les cibles.

---

[← Référence des générateurs](./README.fr.md) · [Sommaire de la documentation](../README.fr.md)
