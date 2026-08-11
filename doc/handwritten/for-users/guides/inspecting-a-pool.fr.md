# Inspecter un pool

🌍 **Langues :**  
🇬🇧 [English](./inspecting-a-pool.en.md) | 🇫🇷 Français (ce fichier)

Quand vous tirez d'une liste que vous avez fournie vous-même, les contraintes déclarées à côté d'elle
**rétrécissent cette liste** : chaque valeur les satisfait ou non, et le domaine est l'ensemble de celles
qui les satisfont. Une valeur qui échoue cesse simplement d'être tirée. Rien n'en est dit, et il n'y a
rien à en dire — jusqu'au jour où la liste est un catalogue que vous maintenez.

```csharp
string[] firstNames = ["Camille", "Sylvain", "Ada", "Bo"];

string name = Any.String().OneOf(firstNames).WithMinLength(3).Generate();
```

`"Bo"` ne sortira jamais de ce générateur. Que ce soit un défaut dépend d'une chose que la bibliothèque
ne peut pas savoir : ou bien le catalogue est faux et `"Bo"` n'a rien à y faire, ou bien l'invariant est
faux et `WithMinLength(3)` est plus strict que le code qu'il représente. **Les deux réparations tiennent
au même fait**, et c'est celui qu'une inspection de pool rend.

## Atteindre l'inspection

Les générateurs dont vous fournissez le pool implémentent `IPoolInspection<T>` **explicitement** : elle
n'apparaît donc jamais parmi les contraintes pendant que vous les écrivez. Vous l'atteignez par un cast :

```csharp
string[] firstNames = ["Camille", "Sylvain", "Ada", "Bo"];

IPoolInspection<string> pool = Any.String().OneOf(firstNames).WithMinLength(3);

IReadOnlyList<string>                drawable = pool.GetSurvivors();
IReadOnlyList<PoolRejection<string>> refused  = pool.GetRejections();
```

Rien ici ne tire. Le domaine est fixé au moment où vous déclarez les contraintes, donc les deux appels
rendent la même réponse à chaque fois et sous n'importe quelle graine, et une inspection entre deux
tirages laisse une exécution amorcée rejouer exactement comme elle l'aurait fait.

## Lire un rejet

Chaque rejet porte la valeur et **toutes** les contraintes qui la refusent — pas la première rencontrée,
puisque relâcher l'une de deux raisons ne changerait rien :

```csharp
string[] firstNames = ["Camille", "Sylvain", "Ada", "Bo"];

IPoolInspection<string> pool = Any.String().OneOf(firstNames).WithMinLength(3);

foreach (PoolRejection<string> rejection in pool.GetRejections()) {
    string reasons = string.Join(", ", rejection.RejectedBy);

    // Bo never draws: WithMinLength(3)
    Console.WriteLine($"{rejection.Value} never draws: {reasons}");
}
```

Un `DeclaredConstraint` garde son `Name` et ses `Arguments` rendus séparés, ce qui vous permet de
grouper ou de filtrer par contrainte au lieu de parser du texte. Ses `Arguments` valent `...` quand les
valeurs sont de celles que la bibliothèque ne doit pas rendre — un pool de votre propre type, dont le
`ToString` est le vôtre et pourrait être n'importe quoi.

## Verrouiller un catalogue par un test

La raison d'être de l'inspection est que vous pouvez en faire une vérification qui s'exécute là où vit le
catalogue, au lieu de constater un pool rétréci des mois plus tard :

```csharp
string[] firstNames = ["Camille", "Sylvain", "Ada"];

IPoolInspection<string> pool = Any.String().OneOf(firstNames).WithMinLength(3);

Assert.Empty(pool.GetRejections());
```

Ce test échoue le jour où quelqu'un ajoute un prénom que l'invariant refuse, et son message nomme la
valeur comme la contrainte. Un pool entièrement vidé ne va jamais jusque-là : un value set auquel les
contraintes ne laissent rien lève une `ConflictingAnyConstraintException` dès la ligne d'arrange, en
nommant les deux côtés.

## Ce qu'elle ne fait pas

La bibliothèque **rend compte** ; elle ne juge pas. Elle n'avertit jamais qu'une partie de votre pool a
été écartée, parce que rétrécir un catalogue partagé sur un appel précis est exactement ce à quoi sert
la déclaration d'une contrainte à côté d'un value set — un générateur qui y verrait une erreur aurait
tort plus souvent que raison.

L'interface est par ailleurs **optionnelle**. Elle est portée par les générateurs dont vous fournissez le
pool entier — `Any.String().OneOf(...)` et `Any.OneOf(...)`/`Any.ElementOf(...)` — et non par les
builders qui construisent une valeur ou narrowent dans leur propre domaine. Écrivez donc le cast comme un
test quand vous ne savez pas ce que vous tenez :

```csharp
IAny<string> generator = Any.String().OneOf("Camille", "Ada");

if (generator is IPoolInspection<string> inspectable && inspectable.IsPooled) {
    Console.WriteLine(inspectable.GetRejections().Count);
}
```

`IsPooled` est la seconde moitié de cette question : un générateur de chaîne qui construit sa valeur au
lieu de la choisir parmi des valeurs fournies répond `false`, avec un rapport vide plutôt qu'une
exception.

---

[← Tous les guides](../README.fr.md)
