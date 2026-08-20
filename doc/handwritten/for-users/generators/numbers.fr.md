# Nombres

🌍 **Langues :**  
🇬🇧 [English](./numbers.en.md) | 🇫🇷 Français (ce fichier)

Chaque type numérique du BCL possède un générateur, et tous partagent un même vocabulaire de
contraintes. Apprenez les cinq familles ci-dessous et vous connaissez les quatorze générateurs.

## Quel générateur pour quel type

| Fabrique | Tire | Disponibilité |
| --- | --- | --- |
| `Any.Byte()` | `byte` | partout |
| `Any.SByte()` | `sbyte` | partout |
| `Any.Int16()` | `short` | partout |
| `Any.Int32()` | `int` | partout |
| `Any.Int64()` | `long` | partout |
| `Any.UInt16()` | `ushort` | partout |
| `Any.UInt32()` | `uint` | partout |
| `Any.UInt64()` | `ulong` | partout |
| `Any.Decimal()` | `decimal` | partout |
| `Any.Double()` | `double` | partout |
| `Any.Single()` | `float` | partout |
| `Any.Int128()` | `Int128` | .NET 8+ |
| `Any.UInt128()` | `UInt128` | .NET 8+ |
| `Any.Half()` | `Half` | .NET 8+ |

La fabrique porte le nom du type CLR, jamais celui du mot-clé C# — `Any.Int32()`, et non
`Any.Int()`. Un nom par type : rien à retenir, rien à lever comme ambiguïté.

## Bornes

Cinq contraintes resserrent l'intervalle, et elles se composent :

<!-- jd:allow=JD031 -->
```csharp
int quantity   = Any.Int32().Between(1, 100).Generate();          // inclusif des deux côtés
int positive   = Any.Int32().GreaterThan(0).Generate();
int atLeastTen = Any.Int32().GreaterThanOrEqualTo(10).Generate();
int belowMax   = Any.Int32().LessThan(1_000).Generate();
int atMostMax  = Any.Int32().LessThanOrEqualTo(999).Generate();

// Composé : une quantité de ligne de commande, bornée des deux côtés par deux appels distincts.
int lineQuantity = Any.Int32().GreaterThanOrEqualTo(1).LessThanOrEqualTo(50).Generate();
```

`Between` est inclusif aux deux extrémités. Des bornes qui se croisent sont refusées immédiatement,
avec un message les nommant toutes deux — voir
[Erreurs et conflits](../guides/errors-and-conflicts.fr.md).

La ligne composée ci-dessus est délibérée : deux bornes inclusives déclarées séparément se comportent
exactement comme `Between(1, 50)`, ce qui est précisément ce qui garde un intervalle décomposable — un
helper partagé peut poser le plancher et un site d'appel ajouter le plafond.
[JD031](../analyzers/JD031.fr.md) désigne la forme intervalle quand les deux bornes tiennent dans une
même chaîne, en information et non en verdict ; la paire reste correcte dans les deux cas.

**Un entier non contraint parcourt tout son type.** Ne déclarez rien et le tirage est uniforme sur
toute la plage, et c'est délibéré
([ADR-0031](../../for-maintainers/adr/0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.fr.md)) :
un grand entier reste un entier ordinaire, là où un grand `double` cesse de se comporter comme de
l'arithmétique — la règle sur la virgule flottante, plus bas, est l'exception et non la norme.
Uniforme sur une plage signifie aussi que l'essentiel de cette plage se trouve près de ses extrêmes :
un `Int128` non contraint porte 38 ou 39 chiffres dans environ 94 tirages sur 100. Déclarez une
borne quand votre domaine en a une.

## Signe et zéro

```csharp
int     positive = Any.Int32().Positive().Generate();   // > 0
int     negative = Any.Int32().Negative().Generate();   // < 0
int     nonZero  = Any.Int32().NonZero().Generate();    // != 0
int     zero     = Any.Int32().Zero().Generate();       // toujours 0
decimal price    = Any.Decimal().Positive().Generate();
```

`Positive()` et `Negative()` n'existent que là où le type a un signe. `byte`, `ushort`, `uint`,
`ulong` et `UInt128` portent `NonZero()` et `Zero()` mais pas les deux autres — demander un `byte`
négatif n'est pas une contrainte que la bibliothèque refuse à l'exécution, c'est une méthode qui
n'existe pas.

`Zero()` paraît inutile jusqu'au jour où il faut un dummy qui soit *précisément* le cas vide, tout
en gardant un site d'appel qui se lit comme tous les autres.

## Appartenance et exclusion

```csharp
int      httpPort   = Any.Int32().OneOf(80, 443, 8080).Generate();
int      notReserved = Any.Int32().Between(1, 10).Except(3, 7).Generate();
int      notTheSame = Any.Int32().Between(1, 100).DifferentFrom(42).Generate();
```

`OneOf` restreint le tirage à un vivier explicite. `Except` retire des valeurs du domaine ;
`DifferentFrom` en est la forme à valeur unique et se lit mieux quand il n'y en a qu'une.

Lister deux fois la même constante dans un vivier est le diagnostic
[JD025](../analyzers/JD025.fr.md) : les doublons fusionnent, le vivier est donc plus petit qu'il n'y
paraît.

## Multiples et échelle

Deux contraintes placent la valeur sur une grille plutôt que simplement dans un intervalle.

`MultipleOf` s'applique aux types **entiers** :

```csharp
int    evenQuantity = Any.Int32().Between(1, 100).MultipleOf(2).Generate();
int    onTheHour    = Any.Int32().Between(0, 1_440).MultipleOf(60).Generate();
long   pageOffset   = Any.Int64().GreaterThanOrEqualTo(0).MultipleOf(25).Generate();
```

`WithScale` s'applique à `decimal` et fixe le nombre de décimales — ce qui fait qu'un dummy de
montant se comporte comme un montant :

```csharp
decimal amount = Any.Decimal().Between(0m, 10_000m).WithScale(2).Generate(); // p. ex. 4172,35
decimal rate   = Any.Decimal().Between(0m, 1m).WithScale(4).Generate();      // p. ex. 0,0725
```

Combiner `Between` et `MultipleOf` est le seul endroit à surveiller : un intervalle ne contenant
aucun multiple du pas n'admet aucune valeur, et il est refusé.
`Any.Int32().Between(1, 10).MultipleOf(50)` nomme les deux côtés dans son message, et l'analyzer
[JD023](../analyzers/JD023.fr.md) l'attrape dès la compilation quand les deux arguments sont
constants.

## Virgule flottante

`Double`, `Single` et `Half` portent les bornes, la famille du signe et celle de l'appartenance —
mais ni `MultipleOf` ni `WithScale`, car une grille en virgule flottante binaire n'est pas une
grille sur laquelle un test peut raisonner.

Deux comportements méritent d'être connus.

**Les tirages non contraints restent ordinaires.** Un `double`, `float` ou `decimal` non contraint
est tiré dans un ordre de grandeur d'un million, au lieu de parcourir toute la plage du type
([ADR-0031](../../for-maintainers/adr/0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.fr.md)).
Des valeurs comme `1.7e308` sont techniquement dans la plage et inutiles dans un test : elles
transforment chaque assertion arithmétique suivante en question sur le dépassement de capacité.
Déclarez une borne quand votre domaine en a une.

**NaN et les infinis ne sont jamais tirés, ni acceptés.** Le refus couvre aussi les arguments :
`Except(double.NaN)` et une borne non finie sont tous deux rejetés — un NaN ne restreint jamais
rien, puisque toute comparaison avec lui est fausse.

Quand un test a réellement besoin d'un NaN, demandez-le explicitement via le vivier générique, qui
ne porte aucune règle de finitude :

```csharp
double maybeNaN = Any.OneOf(double.NaN, 1.0, 2.0).Generate();
```

## Matrice de disponibilité

| Contrainte | entiers signés | `UInt*`, `byte` | `decimal` | `double`, `float`, `Half` |
| --- | :---: | :---: | :---: | :---: |
| `Between`, `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo` | ✅ | ✅ | ✅ | ✅ |
| `OneOf`, `Except`, `DifferentFrom` | ✅ | ✅ | ✅ | ✅ |
| `NonZero`, `Zero` | ✅ | ✅ | ✅ | ✅ |
| `Positive`, `Negative` | ✅ | ❌ | ✅ | ✅ |
| `MultipleOf` | ✅ | ✅ | ❌ | ❌ |
| `WithScale` | ❌ | ❌ | ✅ | ❌ |

---

[← Référence des générateurs](./README.fr.md) · [Sommaire de la documentation](../README.fr.md)
