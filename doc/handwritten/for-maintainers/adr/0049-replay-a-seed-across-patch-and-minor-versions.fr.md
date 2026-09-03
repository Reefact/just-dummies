# ADR-0049 | Rejouer une graine à travers les versions patch et mineures

🌍 🇬🇧 [English](0049-replay-a-seed-across-patch-and-minor-versions.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-01
**Accepted:** 2026-08-01
**Decision Makers:** Reefact

## Contexte

Une génération qui échoue reporte la graine qui l'a produite, et le lecteur rejoue le run en épinglant
cette graine — `Dummy.Reproducibly(1234, ...)`, `Dummy.WithSeed(1234)`, ou `[Fact, Reproducible(Seed =
1234)]` avec l'adaptateur xUnit. Cette boucle est l'ergonomie centrale de la bibliothèque : une valeur
arbitraire ne vaut la peine d'être tirée que si le run qui a échoué dessus peut être reproduit.

Le rejeu est garanti aujourd'hui **à version constante** : `SeededRandom` encapsule un unique
`new Random(seed)`, dont la BCL maintient la séquence stable pour le constructeur à graine, et tous les
tirages passent par lui.

Il est aussi déjà garanti **à travers les target frameworks**, et cette garantie est vérifiée plutôt
qu'affirmée : `justdummies.yml` compare octet pour octet la bannière `SEEDBATCH` que
`tools/justdummies-check` tire de `CrossTfmSeed`, entre les assets `lib/netstandard2.0` et
`lib/net8.0` du package, de sorte que les deux jambes ne peuvent pas diverger en silence.

Ce qui n'a jamais été décidé, c'est le troisième axe : la graine `1234` doit-elle tirer les mêmes
valeurs en `1.0.1` qu'en `1.0.0` ? Le README énonce franchement la position actuelle — *nothing is
promised before 1.0, and that includes the values a given seed draws* — ce qui est honnête pour une
préversion et ne tranche rien au-delà.

Cet axe compte parce qu'une graine épinglée est en général **commitée**. Un mainteneur épingle la
graine qu'un run en échec a reportée pour que le cas reste couvert, et le test entre dans la suite. Si
le mapping bouge à la montée de version, ce test n'échoue pas : il tire d'autres valeurs et reste vert,
ayant discrètement cessé de tester ce pour quoi il avait été épinglé. Le mode de panne est une perte de
couverture, pas un build cassé.

Une propriété de l'implémentation actuelle façonne toutes les options ci-dessous. Les tirages viennent
d'un **unique flux séquentiel partagé par tout le scope**, si bien que la valeur produite par un
`Dummy.String()` dépend de tout ce qui a été tiré avant lui. Un changement du nombre de tirages que
consomme un générateur décale toutes les valeurs qui le suivent dans le même scope — y compris celles
produites par des générateurs qu'on n'a pas touchés.

## Décision

Une graine se rejoue à travers les versions patch et mineures : au sein d'une version majeure, une
graine donnée tire les mêmes valeurs. Le mapping peut changer lors d'une version majeure.

La promesse est appliquée par un golden master qui épingle, pour chaque fabrique à graine fixée, à la
fois les **valeurs produites** et le **nombre de tirages consommés**.

## Justification

**La promesse est le produit.** JustDummies existe pour qu'un test puisse utiliser une valeur
arbitraire sans perdre la capacité de reproduire le run qui a échoué. Une graine qui cesse de se
rejouer à la montée de version suivante est un outil de diagnostic dont la durée de vie se compte en
releases, et une graine épinglée et commitée devient un test qui ressemble à de la couverture sans en
être. Trancher cet axe n'est pas du perfectionnisme ; c'est le laisser ouvert qui éroderait
discrètement la raison d'être de la bibliothèque.

**Cela prolonge une garantie qui existe déjà plutôt que d'en inventer une.** La stabilité de la graine
à travers les TFM est déjà promise et déjà vérifiée octet pour octet. La stabilité inter-versions est
la même propriété sur un autre axe, et elle emprunte la même forme d'application.

**Épingler la consommation de tirages est ce qui rend une vérification locale suffisante.** Avec un
flux séquentiel unique et partagé, « la graine 1234 se rejoue » est une propriété d'un corps de test
entier, et les corps de test sont sans borne — un golden master sur des séquences d'appels serait
combinatoire. Épingler la *consommation* de chaque fabrique fait s'effondrer ce problème : si aucune
fabrique ne change ni ses valeurs ni le nombre de tirages qu'elle prend, aucune séquence d'appels ne
peut dériver, quoi qu'ait écrit l'appelant. La vérification reste par fabrique et la garantie reste
globale.

**Épingler les seules valeurs ne suffirait pas, et échouerait en silence.** Un changement qui laisse la
sortie propre d'une fabrique identique tout en consommant un tirage de plus décale toutes les valeurs
produites après elle, dans tous les tests qui l'appellent — et un golden master limité aux valeurs
reste vert du début à la fin. C'est exactement la perte de couverture silencieuse que cet
enregistrement existe pour empêcher, reproduite à l'intérieur du mécanisme censé l'empêcher.

**Le coût tombe là où il doit.** La contrainte dit : améliorer les tirages d'un générateur est une
version majeure. C'est une vraie restriction, et c'est le prix honnête de la promesse. Elle
n'interdit pas d'*ajouter* une fabrique — un test existant et non modifié ne l'appelle pas, donc sa
séquence de tirages est intacte — ce qui est là où se fait l'essentiel de la croissance de la
bibliothèque sous la 1.0.

## Alternatives considérées

### Ne rien promettre entre versions

La position qu'énonce le README aujourd'hui, et celle qui était recommandée avant cette décision :
traiter la graine comme un outil de diagnostic valable pour la version qui l'a reportée. Rejeté : cela
fait d'une graine épinglée et commitée un passif plutôt qu'un actif, et la panne est silencieuse — le
test continue de passer en couvrant autre chose. Un outil de reproductibilité sur lequel on ne peut pas
compter à travers une montée de version est nettement moins utile que ce que la documentation de la
bibliothèque laisse entendre.

### Dériver un flux indépendant par générateur, puis promettre la stabilité

Remplacer le flux séquentiel partagé par des flux par générateur dérivés de la graine (par exemple
depuis `hash(graine, identité de la fabrique, index d'appel)`), afin que les tirages d'un générateur ne
puissent pas perturber ceux d'un autre. Rejeté, au motif que cela n'achète pas ce qu'on croit : sous la
promesse décidée ici, modifier `Dummy.String()` modifie les valeurs de `Dummy.String()`, ce qui est un
changement majeur que les flux soient partagés ou indépendants. L'indépendance réduit le **rayon de
souffle** d'un tel changement — seules les valeurs de cette fabrique bougent, au lieu de tout ce qui est
tiré après elle — mais elle n'accorde aucune liberté de faire ce changement en mineure. C'est un
remaniement du cœur du générateur pour un bénéfice qui ne se matérialise qu'en version majeure, et cela
va aussi à l'encontre d'[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md).
L'option reste disponible plus tard ; l'adopter serait elle-même une version majeure, c'est-à-dire le
moment où elle ne coûte rien de plus.

### Promettre la stabilité sans golden master

Écrire la garantie dans le README et s'en remettre au soin qu'on y met. Rejeté : une promesse non
vérifiée sur un mapping que personne ne voit est la pire des options, parce qu'elle se casse en silence
et que les consommateurs agissent dessus. L'habitude de ce dépôt est qu'une garantie énoncée est une
garantie vérifiée — la comparaison de la bannière inter-TFM en est le précédent.

## Conséquences

### Positives

* Une graine épinglée et commitée dans un test garde son sens pour toute la durée d'une version
  majeure.
* Un changement des tirages d'un générateur devient visible au moment où il est fait, sous la forme
  d'un golden master rouge, plutôt qu'à la prochaine montée de version d'un consommateur.
* Le golden master valeurs-et-consommation documente le mapping actuel, ce que rien ne fait aujourd'hui.

### Négatives

* Améliorer le comportement de tirage d'un générateur — une distribution, une dimension nouvelle, un
  alphabet — est une version majeure. La croissance de la bibliothèque sous la 1.0 se fait surtout par
  ajout de fabriques, ce qui n'est pas concerné, mais la restriction est réelle dès la 1.0 sortie.
* Le golden master doit couvrir les fabriques, et une fabrique ajoutée sans cas de golden master est un
  trou dans la garantie que rien ne signale.

### Risques

* La garantie suppose que la séquence de `System.Random` à graine est elle-même stable. Elle l'est,
  pour le constructeur à graine, et la bannière inter-TFM attraperait un changement — mais l'hypothèse
  est extérieure à ce dépôt.
* La consommation de tirages n'est observable que depuis l'intérieur de l'assembly. Le golden master
  atteint donc des membres internes, et un futur remaniement de `SeededRandom` doit préserver cette
  observation, faute de quoi la garantie s'affaiblit en silence pour ne plus porter que sur les valeurs.

## Actions de suivi

* Ajouter le golden master : par fabrique, à graines fixées, les valeurs produites et les tirages
  consommés.
* Énoncer la garantie dans le README, en remplacement de la formulation actuelle de préversion, à la
  sortie de la 1.0.0.

## Références

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — la préférence pour un
  mécanisme borné, que cet enregistrement met en balance et suit en choisissant le golden master plutôt
  qu'un remaniement du cœur du générateur.
* `JustDummies/RandomSource.cs` — le flux séquentiel unique et partagé qui façonne le mécanisme de cet
  enregistrement, et la garantie inter-TFM qu'il porte déjà.
* `tools/justdummies-check` — la vérification de stabilité de graine existante, sur l'axe des target
  frameworks.
