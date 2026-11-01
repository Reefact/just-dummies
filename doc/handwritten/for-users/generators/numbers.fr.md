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
int anyQuantity   = Any.Int32().Between(1, 100).Generate();          // inclusif des deux côtés
int anyPositive   = Any.Int32().GreaterThan(0).Generate();
int anyAtLeastTen = Any.Int32().GreaterThanOrEqualTo(10).Generate();
int anyBelowMax   = Any.Int32().LessThan(1_000).Generate();
int anyAtMostMax  = Any.Int32().LessThanOrEqualTo(999).Generate();

// Composé : une quantité de ligne de commande, bornée des deux côtés par deux appels distincts.
int anyLineQuantity = Any.Int32().GreaterThanOrEqualTo(1).LessThanOrEqualTo(50).Generate();
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
([ADR-0031](../../for-maintainers/adr/0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.fr.md), remplacée par l'[ADR-0097](../../for-maintainers/adr/0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.fr.md), puis par l'[ADR-0117](../../for-maintainers/adr/0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.fr.md)) :
un grand entier reste un entier ordinaire, là où un grand `double` cesse de se comporter comme de
l'arithmétique — la règle sur la virgule flottante, plus bas, est l'exception et non la norme.
Uniforme sur une plage signifie aussi que l'essentiel de cette plage se trouve près de ses extrêmes :
un `Int128` non contraint porte 38 ou 39 chiffres dans environ 94 tirages sur 100. Déclarez une
borne quand votre domaine en a une.

## Signe et zéro

```csharp
int     anyPositive = Any.Int32().Positive().Generate();   // > 0
int     anyNegative = Any.Int32().Negative().Generate();   // < 0
int     anyNonZero  = Any.Int32().NonZero().Generate();    // != 0
int     anyZero     = Any.Int32().Zero().Generate();       // toujours 0
decimal anyPrice    = Any.Decimal().Positive().LessThan(10_000m).Generate();   // > 0, et une plage déclarée
```

`Positive()` et `Negative()` n'existent que là où le type a un signe. `byte`, `ushort`, `uint`,
`ulong` et `UInt128` portent `NonZero()` et `Zero()` mais pas les deux autres — demander un `byte`
négatif n'est pas une contrainte que la bibliothèque refuse à l'exécution, c'est une méthode qui
n'existe pas.

`Zero()` paraît inutile jusqu'au jour où il faut un dummy qui soit *précisément* le cas vide, tout
en gardant un site d'appel qui se lit comme tous les autres.

## Appartenance et exclusion

```csharp
int      anyHttpPort   = Any.Int32().OneOf(80, 443, 8080).Generate();
int      anyNotReserved = Any.Int32().Between(1, 10).Except(3, 7).Generate();
int      anyNotTheSame = Any.Int32().Between(1, 100).DifferentFrom(42).Generate();
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
int    anyEvenQuantity = Any.Int32().Between(1, 100).MultipleOf(2).Generate();
int    anyOnTheHour    = Any.Int32().Between(0, 1_440).MultipleOf(60).Generate();
long   anyPageOffset   = Any.Int64().GreaterThanOrEqualTo(0).MultipleOf(25).Generate();
```

`WithScale` s'applique à `decimal` et garde la valeur exprimable avec au plus *n* décimales — un
multiple de 10⁻ⁿ, ce qui fait qu'un dummy de montant se comporte comme un montant :

```csharp
decimal anyAmount = Any.Decimal().Between(0m, 10_000m).WithScale(2).Generate(); // p. ex. 4172,35
decimal anyRate   = Any.Decimal().Between(0m, 1m).WithScale(4).Generate();      // p. ex. 0,0725
```

Elle décrit un treillis de valeurs, pas une représentation. Un tirage porte en général l'échelle demandée —
`5.30` sous `WithScale(2)` — mais ce n'est pas promis : aux échelles les plus hautes que le type permet, un
tirage qui finit par un zéro peut le perdre, environ un sur dix sous `WithScale(28)`. Comparez des valeurs,
pas leur texte. Une valeur fournie par `OneOf`
ou par un intervalle réduit à un point est rendue telle qu'écrite ([ADR-0015](../../for-maintainers/adr/0015-draw-lattice-constrained-scalars-on-the-grid.fr.md)).

Combiner `Between` et `MultipleOf` est le seul endroit à surveiller : un intervalle ne contenant
aucun multiple du pas n'admet aucune valeur, et il est refusé.
`Any.Int32().Between(1, 10).MultipleOf(50)` nomme les deux côtés dans son message, et l'analyzer
[JD023](../analyzers/JD023.fr.md) l'attrape dès la compilation quand les deux arguments sont
constants.

## Virgule flottante

`Double`, `Single` et `Half` portent les bornes, la famille du signe et celle de l'appartenance —
mais ni `MultipleOf` ni `WithScale`, car une grille en virgule flottante binaire n'est pas une
grille sur laquelle un test peut raisonner.

Trois comportements méritent d'être connus.

**Une plage que vous bornez des deux côtés est tirée entière, uniformément en valeur.**
`Between(0, 5_000_000)`, `GreaterThan(0).LessThanOrEqualTo(100e6)` ou `Between(1e300, 1e308)` : les deux
bornes sont les vôtres, donc chaque valeur entre elles est atteignable, et le tirage est uniforme sur la
*longueur* de l'intervalle — la moitié des tirages tombe sous son milieu — et non sur les valeurs que le
type représente, qui se pressent près de zéro
([ADR-0117](../../for-maintainers/adr/0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.fr.md)).
Ainsi, `Any.Double().Positive().LessThanOrEqualTo(50)` ne distribue pas de valeurs proches de
`1e-300` : un tirage sous `1e-6` survient environ une fois sur cinquante millions. Si le code a besoin
d'une plus petite valeur — une quantité minimale, un taux mesurable — c'est une borne basse, et vous la
déclarez : `Between(0.001, 50)`. C'est la forme à choisir chaque fois que votre domaine a une plage.

**Un côté laissé ouvert reste ordinaire neuf tirages sur dix, et atteint la limite du type le dixième.**
Un `double`, `float` ou `decimal` non contraint, ou borné d'un seul côté — `Positive()`, `LessThan(1e9)` —
est tiré dans un ordre de grandeur d'un million neuf fois sur dix, au lieu de parcourir toute la plage du
type, où des valeurs comme `1.7e308` transformeraient chaque assertion arithmétique en question sur le
dépassement de capacité. Le dixième tirage couvre délibérément tout le domaine déclaré : votre borne
unilatérale, vos exclusions et votre échelle tiennent toujours, mais la valeur peut se trouver à côté de
`double.MaxValue`. Que le test ne se soucie pas de la valeur ne veut pas dire que le code ne s'en soucie
pas, et seul un tel tirage sépare les deux
([ADR-0117](../../for-maintainers/adr/0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.fr.md)).
[JD036](../analyzers/JD036.fr.md) nomme le remède au site d'appel quand le code s'en soucie : déclarez
la plage qu'il accepte.

Une borne unilatérale au-delà de l'ordre de grandeur ordinaire conserve, sur les tirages de la fenêtre,
un étalement ordinaire à côté de la valeur que vous avez nommée, si bien que
`GreaterThanOrEqualTo(1_000_000)` reste juste au-dessus du million neuf fois sur dix au lieu de partir
vers `1e308`
([ADR-0097](../../for-maintainers/adr/0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.fr.md), remplacée par l'[ADR-0117](../../for-maintainers/adr/0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.fr.md)).
Une borne si grande qu'aucune bande ordinaire n'est représentable à côté d'elle —
`GreaterThanOrEqualTo(1e300)` — ne laisse rien d'ordinaire à tirer : chaque tirage couvre alors son domaine
déclaré.
Écrire la limite propre du type comme borne opposée revient à ne pas la mettre :
`Between(1_000_000, double.MaxValue)` tire exactement comme `GreaterThanOrEqualTo(1_000_000)`. `Half` est
l'exception qui n'a besoin d'aucune règle : tout son domaine, jusqu'à 65 504, tient déjà dans la fenêtre.

**NaN et les infinis ne sont jamais tirés.** Une borne non finie ou une valeur `OneOf(...)` non
finie est rejetée comme une erreur d'argument, au même titre qu'une valeur hors de la plage que vous
avez déclarée — la valeur ne pourrait jamais être produite, donc l'écrire est une erreur qu'il vaut
mieux détecter immédiatement.

`Except(double.NaN)` et `DifferentFrom(double.NaN)` sont différents : ils sont **acceptés** et n'ont
aucun effet. Un NaN n'est de toute façon jamais tiré, donc en exclure un ne demande rien que le
générateur n'offre déjà — le refuser ne ferait que gêner un `DifferentFrom(existant)` écrit sur une
valeur issue d'un calcul, laquelle pourrait parfois être infinie.

Quand un test a réellement besoin d'un NaN, demandez-le explicitement via le vivier générique, qui
ne porte aucune règle de finitude :

```csharp
double anyMaybeNaN = Any.OneOf(double.NaN, 1.0, 2.0).Generate();
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
