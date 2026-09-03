# FAQ

🌍 **Langues :**  
🇬🇧 [English](./faq.en.md) | 🇫🇷 Français (ce fichier)

Réponses courtes aux questions les plus fréquentes. Chacune renvoie à la page qui traite le sujet
en profondeur.

## Choisir la bibliothèque

### Est-ce une bibliothèque de test à base de propriétés ?

Non, et la différence mérite d'être claire.

Une bibliothèque à base de propriétés (FsCheck, Hedgehog) exécute votre test de nombreuses fois, sur
de nombreuses entrées générées, et **rétrécit** un échec jusqu'à un contre-exemple minimal.
JustDummies tire **une** valeur par `Generate()` et ne rétrécit pas. Un échec se récupère en
rejouant sa graine, exactement tel qu'il s'est produit.

Les deux résolvent des problèmes différents et cohabitent très bien : le test à base de propriétés
explore un espace, JustDummies retire les littéraux dénués de sens des tests par l'exemple
ordinaires. Si vous voulez du rétrécissement, prenez une bibliothèque à base de propriétés —
celle-ci ne fera pas semblant.

### Pourquoi n'y a-t-il pas d'`Dummy.Object<T>()` qui remplirait tout un graphe d'objets ?

Parce qu'un générateur qui réfléchit sur votre type doit deviner ce qui rend une instance valide, et
il devine faux précisément là où la correction compte — l'invariant que votre constructeur impose, le
champ qui doit s'accorder avec un autre champ.

JustDummies vous demande de composer la valeur avec `.As(...)` et `Dummy.Combine` : cela coûte
quelques lignes et achète un dummy que votre propre fabrique accepte. Voir
[Composition](./composition.fr.md), et [Principes de conception](./design-principles.fr.md) pour le
raisonnement.

### Ai-je besoin du paquet `JustDummies.Xunit` ?

Seulement si vous utilisez xUnit **v3** et voulez `[Reproducible]` plutôt que d'envelopper les corps
dans `Dummy.Reproducibly`. Tout fonctionne sans lui. Voir
[sa page](../packages/justdummies-xunit.fr.md).

## Valeurs et contraintes

### Pourquoi `Generate()` renvoie-t-il une valeur différente à chaque appel ?

Parce qu'un générateur est une **recette**, pas une valeur. `Dummy.Int32().Between(1, 100)` décrit les
entiers acceptables ; chaque `Generate()` en tire un. Conservez la valeur dans une variable s'il vous
faut deux fois la même :

```csharp
DummyInt32 anyQuantity = Dummy.Int32().Between(1, 100);

int drawnOnce = anyQuantity.Generate();
int sameValue = drawnOnce;        // le même nombre
int another   = anyQuantity.Generate(); // généralement un autre nombre
```

Voir [Concepts fondamentaux](./core-concepts.fr.md).

### Une contrainte doit-elle correspondre à ce que mon test affirme ?

Non — c'est l'unique habitude qui décide si le test vaut quelque chose. Une contrainte énonce un
**invariant du domaine**. Si vous en ajoutez une pour faire passer une assertion, le test prouve
désormais que le code est d'accord avec la supposition du test lui-même, et il continuera de passer
après un changement de règle.

### Mes contraintes ont levé `ConflictingDummyConstraintException`. Est-ce un bogue ?

Non : c'est la bibliothèque qui refuse une spécification impossible au lieu de boucler ou de
renvoyer quelque chose d'arbitraire. Le message nomme **les deux** contraintes qui se contredisent.
Retirez celle des deux qui n'est pas un véritable invariant du domaine. Voir
[Erreurs et conflits](./errors-and-conflicts.fr.md).

### Puis-je rendre une valeur optionnelle ?

Oui — `.OrNull()` produit `null` environ une fois sur deux, et sinon une valeur contrainte :

```csharp
int?    discount = Dummy.Int32().Between(0, 100).OrNull().Generate();
string? note     = Dummy.String().Alpha().WithLengthBetween(1, 40).OrNull().Generate();
```

## Reproductibilité

### Un test a échoué une fois puis est passé à la relance. Que faire ?

Enveloppez le corps dans `Dummy.Reproducibly` (ou ajoutez `[Reproducible]` avec le paquet xUnit) pour
que le **prochain** échec rapporte sa graine. Épinglez ensuite cette graine pour rejouer l'exécution
exacte, corrigez le défaut, et retirez l'épingle.

Si l'exécution déjà en échec se trouvait dans une portée reproductible, la graine est dans la sortie
d'échec et vous pouvez la rejouer immédiatement. Voir
[Reproductibilité](./reproducibility.fr.md).

### Puis-je obtenir volontairement deux fois les mêmes valeurs ?

Oui, de trois façons, pour trois situations :

```csharp
// 1. Rejouer tout un corps sous une graine connue.
Dummy.Reproducibly(1743029518, () => Assert.True(Dummy.Int32().Positive().Generate() > 0));

// 2. Épingler le contexte ambiant pour un bloc.
using (IDisposable scope = Dummy.UseSeed(1743029518)) {
    Assert.True(Dummy.Int32().Positive().Generate() > 0);
}

// 3. Construire un contexte déterministe isolé, hors de tout corps de test.
DummyContext context  = Dummy.WithSeed(1743029518);
int        quantity = context.Int32().Between(1, 100).Generate();
```

### La séquence des valeurs tirées est-elle stable d'une version à l'autre ?

Au sein d'une version majeure, oui : depuis `1.0.0-preview.1`, une graine donnée tire les mêmes
valeurs sur chaque version corrective et mineure, garanti par un golden master
([ADR-0049](../../for-maintainers/adr/0049-replay-a-seed-across-patch-and-minor-versions.fr.md)).
Une version majeure peut la changer.

### Le rejeu survit-il aux tests parallèles ?

Le rejeu vaut par exécution **séquentielle**. Des tests exécutés en parallèle ont chacun leur portée
et se rejouent très bien. Des tâches exécutées en parallèle *dans une même portée* entrelacent leurs
tirages, et cet ordre n'est pas stable — donnez à chaque tâche sa propre portée de graine. Le
diagnostic [JD022](../analyzers/JD022.fr.md), actif par défaut, le signale.

## Plateforme et distribution

### Quels types exigent .NET 8 ?

`DateOnly`, `TimeOnly`, `Int128`, `UInt128` et `Half` n'existent pas en deçà de .NET 8 :
`Dummy.DateOnly()`, `Dummy.TimeOnly()`, `Dummy.Int128()`, `Dummy.UInt128()` et `Dummy.Half()` ne figurent donc
que sur l'asset `net8.0`. Tout le reste est disponible partout.

### Cela fonctionne-t-il sur .NET Framework ?

Oui. Le plancher supporté est **.NET Framework 4.7.2**, via l'asset `netstandard2.0`, et la CI y
exécute les suites
([ADR-0007](../../for-maintainers/adr/0007-floor-the-library-on-net-framework-4-7-2.fr.md)).

### Dois-je installer les analyzers séparément ?

Non. Les 33 règles sont embarquées dans le paquet `JustDummies` lui-même et se mettent à travailler
dès la compilation suivante. Le paquet distinct `JustDummies.DiagnosticCatalog` n'est nécessaire que
pour nommer une règle dans un `[SuppressMessage]` sans littéral de chaîne — voir
[sa page](../packages/justdummies-diagnosticcatalog.fr.md).

### L'API n'est pas encore stable — qu'est-ce que cela implique pour moi ?

La surface publique est déclarée dans `PublicAPI.Unshipped.txt`, ce qui signifie qu'elle n'est pas
figée et qu'une préversion peut la modifier. Le **contrat de graine**, lui, n'est plus dans ce
panier : il est promis depuis `1.0.0-preview.1`, comme indiqué plus haut.

---

[← Sommaire de la documentation](../README.fr.md)
