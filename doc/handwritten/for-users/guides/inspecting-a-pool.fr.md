# Inspecter un pool

🌍 **Langues :**  
🇬🇧 [English](./inspecting-a-pool.en.md) | 🇫🇷 Français (ce fichier)

Quand vous tirez d'une liste que vous avez fournie vous-même, les contraintes déclarées à côté d'elle
**rétrécissent cette liste** : chaque valeur les satisfait ou non, et le domaine est l'ensemble de celles
qui les satisfont. Une valeur qui échoue cesse simplement d'être tirée.

Avec quatre valeurs écrites à l'appel, c'est sans conséquence — vous les voyez. Dès l'instant où la liste
est un **catalogue** que vous ne pouvez pas lire d'un coup d'œil, ça cesse de l'être :

```csharp
// 2 417 prénoms, un par ligne, maintenus par quelqu'un qui n'a jamais vu ce test.
string[] firstNames = System.IO.File.ReadAllLines("first-names.txt");

string name = Any.String().OneOf(firstNames).Alpha().WithLengthBetween(2, 64).Generate();
```

Cette ligne d'arrange a l'air juste, et elle s'exécute. Mais `Alpha()` signifie lettres ASCII : chaque
*Anne-Marie*, chaque *N'Golo*, chaque *José* du fichier a disparu en silence — quelques centaines de
prénoms, peut-être, sur deux mille. Vos tests passent toujours. Ils ont simplement cessé de tirer du
catalogue dont vous les croyiez issus, et rien nulle part ne le dit.

Que ce soit un défaut dépend d'une chose que la bibliothèque ne peut pas savoir : ou bien le catalogue
est faux et ces prénoms n'y ont pas leur place, ou bien l'invariant est faux et `Alpha()` est plus strict
que le code qu'il représente. **Les deux réparations tiennent au même fait**, et c'est celui qu'une
inspection de pool rend.

## Atteindre l'inspection

Les générateurs dont vous fournissez le pool implémentent `IPoolInspection<T>` **explicitement** : elle
n'apparaît donc jamais parmi les contraintes pendant que vous les écrivez. Vous l'atteignez par un cast :

```csharp
string[] firstNames = System.IO.File.ReadAllLines("first-names.txt");

IPoolInspection<string> pool = Any.String().OneOf(firstNames).Alpha().WithLengthBetween(2, 64);

IReadOnlyList<string>                drawable = pool.GetSurvivors();
IReadOnlyList<PoolRejection<string>> refused  = pool.GetRejections();
```

Rien ici ne tire. Le domaine est fixé au moment où vous déclarez les contraintes, donc les deux appels
rendent la même réponse à chaque fois et sous n'importe quelle graine, et une inspection entre deux
tirages laisse une exécution amorcée rejouer exactement comme elle l'aurait fait.

## Lire le rapport

À cette échelle, vous ne voulez pas lire les rejets un par un — vous voulez la forme des dégâts. Chaque
rejet porte la valeur et **toutes** les contraintes qui la refusent, et un `DeclaredConstraint` est une
valeur comparable : le regroupement est donc le premier regard naturel :

```csharp
string[] firstNames = System.IO.File.ReadAllLines("first-names.txt");

IPoolInspection<string> pool    = Any.String().OneOf(firstNames).Alpha().WithLengthBetween(2, 64);
IReadOnlyList<PoolRejection<string>> refused = pool.GetRejections();

// 214 of 2417 names never draw
Console.WriteLine($"{refused.Count} of {firstNames.Length} names never draw");

// Alpha(): 213
// WithLengthBetween(2, 64): 1
foreach (IGrouping<DeclaredConstraint, PoolRejection<string>> reason in refused.GroupBy(rejection => rejection.RejectedBy[0])) {
    Console.WriteLine($"{reason.Key}: {reason.Count()}");
}
```

Cette seconde ligne est toute la réponse en un chiffre : 213 prénoms perdus par `Alpha()`, c'est un
invariant trop strict, tandis que le seul perdu par la borne de longueur est une ligne vide dans le
fichier. Deux réparations différentes, distinguées sans lire un seul prénom.

Un `DeclaredConstraint` garde son `Name` et ses `Arguments` rendus séparés, ce qui vous permet de
grouper et de filtrer par contrainte au lieu de parser du texte. Ses `Arguments` valent `...` quand les
valeurs sont de celles que la bibliothèque ne doit pas rendre — un pool de votre propre type, dont le
`ToString` est le vôtre et pourrait être n'importe quoi. Et quand une valeur échoue pour plus d'une
raison, `RejectedBy` les porte toutes plutôt que la première rencontrée, puisque relâcher l'une de deux
raisons ne changerait rien.

## Verrouiller un catalogue par un test

La raison d'être de l'inspection est que vous pouvez en faire une vérification qui s'exécute là où vit le
catalogue, au lieu de constater un pool rétréci des mois plus tard :

```csharp
string[] firstNames = System.IO.File.ReadAllLines("first-names.txt");

IPoolInspection<string> pool = Any.String().OneOf(firstNames).Alpha().WithLengthBetween(2, 64);

Assert.Empty(pool.GetRejections());
```

Ce test échoue le jour où quelqu'un ajoute un prénom que l'invariant refuse — et comme un rejet nomme la
valeur comme la contrainte, l'échec dit quel prénom et quel invariant, pas seulement qu'un compte a
changé. Un pool entièrement vidé ne va jamais jusque-là : un value set auquel les contraintes ne laissent
rien lève une `ConflictingAnyConstraintException` dès la ligne d'arrange, en nommant les deux côtés.

## Ce qu'elle ne fait pas

La bibliothèque **rend compte** ; elle ne juge pas. Elle n'avertit jamais qu'une partie de votre pool a
été écartée, parce que rétrécir un catalogue partagé sur un appel précis est exactement ce à quoi sert
la déclaration d'une contrainte à côté d'un value set — un générateur qui y verrait une erreur aurait
tort plus souvent que raison. Tirer un prénom d'adulte d'un catalogue qui contient aussi des prénoms
d'enfants, c'est le même mécanisme fonctionnant comme prévu.

L'interface est par ailleurs **optionnelle**. Elle est portée par tout générateur qui admet un value set
que vous fournissez — `Any.OneOf(...)`/`Any.ElementOf(...)`, `Any.String().OneOf(...)`, et toutes les
familles dotées d'un `OneOf` : les entiers, `Any.Decimal()`, les flottants, les dates et heures,
`Any.Char()`, `Any.Guid()` et `Any.Enum<T>()`. Un générateur sans pool à vous ne la porte pas du tout,
alors écrivez le cast comme un test quand vous ne savez pas ce que vous tenez :

```csharp
IAny<string> generator = Any.String().OneOf("Camille", "Ada");

if (generator is IPoolInspection<string> inspectable && inspectable.IsPooled) {
    Console.WriteLine(inspectable.GetRejections().Count);
}
```

`IsPooled` est la seconde moitié de cette question : un générateur de chaîne qui construit sa valeur au
lieu de la choisir parmi des valeurs fournies répond `false`, avec un rapport vide plutôt qu'une
exception.

Et ce n'est *pas* « ce générateur a un domaine dénombrable ». `Any.Int32().Between(1, 1_000_000)` est
parfaitement dénombrable et répond `false` : ce million de valeurs appartient au moteur, pas à vous. Il
n'y a rien de vôtre à auditer, donc rien à rendre — l'inspection ne parle jamais que d'une liste que vous
avez confiée.

---

[← Tous les guides](../README.fr.md)
