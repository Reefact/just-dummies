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
DateTime anyOrdered   = Any.DateTime().Between(new DateTime(2020, 1, 1), new DateTime(2025, 12, 31)).Generate();
DateTime anyRecent    = Any.DateTime().After(new DateTime(2024, 1, 1)).Generate();
DateTime anyOnOrAfter = Any.DateTime().AfterOrEqualTo(new DateTime(2024, 1, 1)).Generate();
DateTime anyPast      = Any.DateTime().Before(new DateTime(2030, 1, 1)).Generate();
DateTime anyOnOrBefore = Any.DateTime().BeforeOrEqualTo(new DateTime(2030, 1, 1)).Generate();
```

`TimeSpan` est une durée et non un instant : il conserve donc le vocabulaire numérique et ajoute la
famille du signe :

```csharp
TimeSpan anyTimeout  = Any.TimeSpan().Between(TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5)).Generate();
TimeSpan anyPositive = Any.TimeSpan().Positive().Generate();
TimeSpan anyNonZero  = Any.TimeSpan().NonZero().Generate();
TimeSpan anyShorter  = Any.TimeSpan().LessThan(TimeSpan.FromHours(1)).Generate();
```

## Granularité

`WithGranularity` aligne la valeur tirée sur une grille. C'est ce qui transforme un instant brut en
un instant que votre domaine stockerait réellement :

```csharp
// Minutes entières : plus de secondes ni de ticks parasites pour casser une assertion d'égalité.
DateTime anyAppointment = Any.DateTime()
                             .Between(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31))
                             .WithGranularity(TimeSpan.FromMinutes(1))
                             .Generate();

// Jours entiers.
DateTime anyBusinessDay = Any.DateTime().WithGranularity(TimeSpan.FromDays(1)).Generate();

// Une durée en secondes entières.
TimeSpan anyRetryAfter = Any.TimeSpan().Positive().WithGranularity(TimeSpan.FromSeconds(1)).Generate();
```

Sans elle, un instant tiré porte une précision inférieure à la seconde, et un test qui le fait
transiter par un stockage tronquant aux secondes échoue pour une raison sans rapport avec le code
testé. Déclarer la granularité que votre stockage a réellement est le remède, et c'est un véritable
invariant du domaine plutôt qu'un contournement.

`DateOnly` n'a pas de contrainte de granularité — c'est déjà un jour entier.

## Les tirages `DateTime` sont `Kind.Utc`, et une borne `Local` est refusée

Toute valeur générée par `Any.DateTime()` porte `DateTimeKind.Utc`, et les bornes se comparent par leurs `Ticks` —
exactement comme le `<=` de `DateTime`. Une borne peut être `Utc` ou `Unspecified` — `new DateTime(2026, 1, 1)` est
`Unspecified` — et l'une comme l'autre est lue telle qu'écrite. Une borne `Local` est **refusée** :

```csharp
DateTime localStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local);

// Refusé : une ArgumentException qui nomme `start`, et les deux façons d'en sortir.
AnyDateTime impossible = Any.DateTime().Between(localStart, localStart.AddDays(1));
```

Une valeur `Local` peut désigner une heure sur l'horloge de cette machine ou un instant, et la bibliothèque ne peut
pas savoir lequel. Lue en ticks, elle pourrait donner une valeur `Utc` dont l'*instant* sort de ceux des bornes, du
décalage local : une valeur qui satisfait toutes les contraintes selon la comparaison de `DateTime`, et qui n'est
pourtant pas dans la fenêtre voulue. Plutôt que de deviner, l'appel est refusé, et son message nomme les deux façons
d'en sortir : convertir la borne avec `ToUniversalTime()`, ou utiliser
[`DateTimeOffset`](#datetimeoffset-ne-porte-un-decalage-que-si-vous-en-declarez-un) quand c'est l'instant qui compte
([ADR-0114](../../for-maintainers/adr/0114-refuse-a-local-datetime-bound.fr.md)).

Une valeur fournie par `OneOf` n'est pas une borne : elle est rendue telle quelle, `Kind` compris. Sans borne
déclarée, l'intervalle va de `DateTime.MinValue` à `DateTime.MaxValue`.

## Un tirage sans contrainte couvre le type entier

Sans borne déclarée, chaque générateur tire sur tout ce que son type peut contenir :

| Générateur | Étendue par défaut |
|---|---|
| `Any.DateTime()` | de `DateTime.MinValue` à `DateTime.MaxValue` — du `0001-01-01` au `9999-12-31` |
| `Any.DateTimeOffset()` | de `DateTimeOffset.MinValue` à `DateTimeOffset.MaxValue`, décalage `+00:00` — les mêmes instants |
| `Any.DateOnly()` | de `DateOnly.MinValue` à `DateOnly.MaxValue` — du `0001-01-01` au `9999-12-31` |
| `Any.TimeOnly()` | de `TimeOnly.MinValue` à `TimeOnly.MaxValue` — de `00:00:00` à `23:59:59.9999999`, la journée entière |

Les tirages atteignent les extrémités du type, donc **tout** décalage peut y déborder : une valeur dans la
dernière heure de l'intervalle fait lever `AddHours(1)` aussi sûrement qu'une valeur dans les six dernières
années fait lever `AddYears(6)`. La fréquence ne dépend que de la portée du décalage. Mesuré sur des tirages
sans contrainte, `AddYears(6)` a débordé 68 fois sur 100 000 et `AddMonths(120)` 104 fois, tandis
qu'`AddHours(1)` n'a pas débordé une seule fois sur un million — un échec plus rare, pas un calcul sûr. Une
date que le code testé décale ensuite est une valeur qui a un domaine, donc **bornez-la avant d'en faire du
calcul de dates** :

```csharp
DateTimeOffset anyShippedAt = Any.DateTimeOffset()
                                 .Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                          new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero))
                                 .Generate();

DateTimeOffset warrantyEndsAt = anyShippedAt.AddYears(6);              // n'atteint jamais l'extrémité du type
```

## `DateTimeOffset` ne porte un décalage que si vous en déclarez un

Un `DateTimeOffset` est un instant **et** un décalage par rapport à UTC. Sans contrainte, JustDummies
fait varier l'instant et laisse le décalage à `+00:00` : le décalage est une dimension que l'on
active explicitement, avec `WithOffset` pour un décalage fixe en minutes entières ou
`WithOffsetBetween` pour un décalage borné
([ADR-0016](../../for-maintainers/adr/0016-vary-the-datetimeoffset-offset-dimension.fr.md), remplacée par l'[ADR-0030](../../for-maintainers/adr/0030-filter-the-datetimeoffset-pool-by-the-declared-offset.fr.md)).
Déclarez-en un quand le code testé y est sensible : un tirage sans contrainte n'atteint jamais le
code qui suppose l'heure locale.

```csharp
DateTimeOffset anyUtcInstant      = Any.DateTimeOffset().Generate();                 // le décalage reste +00:00
DateTimeOffset anyShiftedInstant  = Any.DateTimeOffset().WithOffset(TimeSpan.FromHours(2)).Generate();
DateTimeOffset anyEuropeanInstant = Any.DateTimeOffset()
                                       .WithOffsetBetween(TimeSpan.FromHours(0), TimeSpan.FromHours(3))
                                       .Generate();
```

`WithGranularity` mesure la grille sur l'**instant UTC**, pas sur l'heure locale que le décalage affiche
ensuite. Des jours entiers sous un décalage `+02:00` se lisent `02:00` en local, et des heures entières sous
`+05:30` tombent à `:30` :

```csharp
DateTimeOffset anyTwoOClock = Any.DateTimeOffset()
                                 .WithGranularity(TimeSpan.FromDays(1))
                                 .WithOffset(TimeSpan.FromHours(2))
                                 .Generate();                          // local time 02:00, always
```

Quand c'est la lecture locale que votre domaine stocke, choisissez une granularité et un décalage qui
s'accordent, ou déclarez la grille de l'instant en tenant compte du décalage.

Combiné à `OneOf(...)`, un décalage déclaré **filtre le vivier** au lieu de réécrire les valeurs
qu'il contient : une valeur du vivier est rendue telle quelle, décalage compris, et une valeur dont
le décalage n'est pas admis par la déclaration n'est jamais tirée
([ADR-0030](../../for-maintainers/adr/0030-filter-the-datetimeoffset-pool-by-the-declared-offset.fr.md)).

## Le piège de `DateTime.Now`

Voici l'erreur qui mérite d'être nommée, car elle survit à la revue et échoue à minuit. `DateTime.Now` lui-même
est refusé comme borne, puisqu'il est `Local` (ci-dessus) ; `DateTime.UtcNow` est accepté, et le piège demeure :

```csharp
// Fragile : le dummy est tiré relativement à une horloge que le test ne contrôle pas.
DateTime anyCreatedAt = Any.DateTime().Before(DateTime.UtcNow).Generate();
```

Le test dépend désormais du moment où il s'exécute. Pire, il n'est pas reproductible : rejouer la
graine rejoue le tirage, pas l'horloge.

Épinglez un instant que le test possède, et contraignez relativement à lui :

```csharp
DateTime now          = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
DateTime anyCreatedAt = Any.DateTime().Before(now).AfterOrEqualTo(now.AddYears(-1)).Generate();
```

## Appartenance et exclusion

Tout générateur temporel porte la famille habituelle du vivier et de l'exclusion :

```csharp
DateTime anyQuarterEnd = Any.DateTime()
                            .OneOf(new DateTime(2025, 3, 31), new DateTime(2025, 6, 30), new DateTime(2025, 9, 30))
                            .Generate();

DateTime anyNotEpoch = Any.DateTime()
                          .Between(new DateTime(2020, 1, 1), new DateTime(2025, 1, 1))
                          .DifferentFrom(new DateTime(2020, 1, 1))
                          .Generate();
```

## `DateOnly` et `TimeOnly`

Disponibles à partir de .NET 8, avec le même vocabulaire d'ordonnancement :

<!-- jd:net8 -->
```csharp
DateOnly anyDueDate = Any.DateOnly()
                         .Between(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31))
                         .Generate();

TimeOnly anyOpeningTime = Any.TimeOnly()
                             .Between(new TimeOnly(8, 0), new TimeOnly(20, 0))
                             .WithGranularity(TimeSpan.FromMinutes(15))
                             .Generate();
```

Sur `netstandard2.0`, ces types n'existent pas, et leurs fabriques non plus. Tout le reste de cette
page est disponible sur toutes les cibles.

---

[← Référence des générateurs](./README.fr.md) · [Sommaire de la documentation](../README.fr.md)
