# ADR-0103 | Exécuter les suites de tests sur Microsoft.Testing.Platform

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0103-run-the-suites-on-microsoft-testing-platform.md)

**Statut :** Proposed
**Proposé :** 31/08/2026
**Décideurs :** Reefact

## Contexte

Jusqu'ici, toutes les suites de ce dépôt s'exécutaient via **VSTest** : `dotnet test` appelait la cible
`VSTest` du SDK, `xunit.runner.visualstudio` y adaptait xUnit, et `coverlet.collector` — un collecteur de
données VSTest — produisait le rapport OpenCover que lit le quality gate Sonar.

xUnit.net est depuis passé à **Microsoft.Testing.Platform** (MTP), un exécuteur où un projet de test est
un exécutable qui héberge lui-même ses extensions, au lieu d'une bibliothèque chargée par un exécuteur
externe. Les deux plateformes ont coexisté tant que MTP 1.x fournissait une passerelle vers la cible
`VSTest`. **MTP 2.x a supprimé cette passerelle sur le SDK .NET 10**, celui que `global.json` épingle : la
cible s'interrompt désormais sur une erreur explicite, qui invite à adopter la nouvelle expérience
`dotnet test`.

`xunit.v3` 4.x est malgré tout arrivé dans le dépôt avant cette décision. Son paquet principal installe la
variante MTP 2.x et positionne `IsTestingPlatformApplication` à `true` par défaut, ce qui est précisément
ce qui a fait échouer `dotnet test`. La montée de version a tout de même été intégrée, au moyen d'un
contournement : repasser cette propriété à `false`, ce qui ramène tous les projets de test sur la cible
`VSTest`. Un état intermédiaire existe donc bel et bien, et c'est celui dans lequel `main` se trouve
aujourd'hui : xUnit épinglé en 4.0.1, les suites exécutées via VSTest, la couverture toujours produite par
`coverlet.collector`.

Trois éléments décrivent ce que coûte le maintien de cet état.

* Il s'oppose à la trajectoire de l'écosystème, et non à une anomalie passagère. xUnit cible MTP, la
  passerelle a disparu sur le SDK épinglé, et toutes les versions ultérieures de xUnit se trouvent de
  l'autre côté. La valeur par défaut que le contournement inverse est celle que l'amont fixe
  délibérément.
* Le dépôt exécute déjà ses suites sur les deux plateformes en parallèle. Les quatre configurations
  Stryker pilotent leurs jobs avec `"test-runner": "mtp"`, et Stryker lance directement l'application de
  test au lieu de passer par la cible `VSTest` : le contournement ne l'atteint donc jamais. Un job de
  mutation exerce ces suites sur MTP pendant que `dotnet test` exerce les mêmes suites sur VSTest.
* `coverlet.collector` est un collecteur de données VSTest. Il ne fonctionne que tant que les suites
  restent sur la plateforme à laquelle il se raccorde.

Un dernier élément rendait l'attente préférable, et ne la justifie plus. L'exécuteur `mtp` de Stryker était
inutilisable ici avant la version 5.0.0 : la 4.16.0 comptait tous les tests de la solution lors de
l'exécution initiale d'un job, au lieu des seuls tests du projet concerné, et déclenchait ainsi son propre
garde-fou, celui qui interrompt l'analyse quand plus de la moitié des tests échouent (issue amont
`stryker-mutator/stryker-net#3117`). Un job de mutation restait de ce fait durablement rouge lors de la
première tentative de cette migration. Le dépôt épingle désormais la 5.0.0, où l'exécution initiale d'un
job ne compte que sa propre suite.

`JustDummies.Xunit` compile contre `xunit.v3.extensibility.core` et le déclare comme dépendance
**publiée** : sa version minimale compatible a donc suivi la version épinglée au moment de la montée de
version, et `main` déclare déjà 4.0.1. Relever cette version minimale ne relève donc pas de
cette décision, et rien ici ne modifie une dépendance publiée.

## Décision

Les suites de tests du dépôt s'exécutent sur Microsoft.Testing.Platform, adopté pour tout appelant via
`global.json`, avec une couverture produite par `coverlet.MTP` et configurée par un fichier de réglages
copié à côté de chaque application de test.

## Justification

**Le contournement était un sursis, et un sursis ne vaut que tant qu'il apporte quelque chose.** Il a
permis la montée de xUnit : la CI était rouge sur toutes les suites, et remettre une seule propriété à sa
valeur précédente a suffi à la rétablir, sans toucher en même temps à l'exécuteur, au collecteur et à
quatre invocations CI. Désormais, il ne fait plus que maintenir le dépôt sur une plateforme que son propre
framework de test a quittée, au prix de l'inversion d'une valeur par défaut que l'amont fixe
volontairement — un coût qui se reproduit à chaque version de xUnit, sans bénéfice en contrepartie.
Migrer pendant que le sursis tient encore, c'est choisir le moment plutôt que de le subir.

**Un même dépôt ne devrait pas exécuter ses suites sur deux plateformes.** Les jobs de mutation pilotent
déjà MTP quand `dotnet test` pilote VSTest : les mêmes tests s'exécutent sous deux exécuteurs selon
l'outil qui les demande. Une différence de découverte ou d'exécution entre les deux se manifeste alors par
un résultat de mutation impossible à réconcilier avec une suite verte — c'est exactement ce qu'a produit
la première tentative de cette migration, et ce qui explique le temps qu'il a fallu pour en identifier la
cause amont. Cette divergence était tolérable tant que personne ne l'avait décidée ; la maintenir
sciemment est autre chose.

**Adopter la plateforme via `global.json` place le choix là où le SDK va déjà le chercher.** L'exécuteur est une
propriété de *la chaîne d'outils du dépôt*, et non d'un projet ni d'une ligne de commande, et `global.json`
est déjà l'endroit où le dépôt déclare le SDK avec lequel il est construit. Une propriété MSBuild par
projet aurait dû être répétée sept fois, et aurait laissé un simple `dotnet test`, dans le shell d'un
contributeur, se comporter autrement que la même commande en CI — précisément la divergence que le SDK
épinglé sert à empêcher.

**Conserver OpenCover préserve la sincérité du quality gate.** Le collecteur alternatif de cette
plateforme produit le format de couverture propre à Microsoft, que Sonar lit via un importateur
*différent*. Changer le format du rapport en même temps que l'exécuteur aurait fait bouger deux variables
sous un quality gate dont les seuils ont été calibrés sur le premier, et toute dérive des chiffres aurait
été impossible à imputer. `coverlet.MTP` est le même outil qu'avant, des mêmes auteurs, produisant le même
format : le quality gate continue donc de mesurer ce qu'il mesurait, et la migration reste réfutable par
comparaison.

**Configurer le collecteur dans un fichier, et non sur une ligne de commande, préserve une décision
antérieure.** Les réglages qui se trouvaient dans `coverage.runsettings` y étaient tenus précisément pour
qu'une exécution locale et une exécution en CI ne puissent pas mesurer des choses différentes. Le fichier
de réglages de la plateforme remplit le même rôle : la garantie survit au changement de mécanisme, seuls
le nom et le format du fichier ont changé. La ligne de commande ne conserve que l'option qui *active* la
collecte, exactement comme auparavant.

**Restreindre le collecteur au job moderne vaut mieux que découvrir ses limites sur le plancher de
support.** Le plancher de support (ADR-0007) exécute les assemblys `netstandard2.0` sur le véritable CLR
.NET Framework, et ce job ne collecte aucune couverture : les chiffres proviennent du job moderne. Puisque
le collecteur documente .NET Core 8.0 comme son runtime pris en charge, le câbler dans un job qui n'en a
pas besoin et sur lequel il n'offre aucune garantie n'apporterait rien, et risquerait un échec au démarrage
dans le seul job dont la raison d'être est de prouver que le plancher fonctionne toujours.

**L'exécuteur relève de la chaîne d'outils : il ne change donc rien d'observable pour un consommateur.**
`JustDummies.Xunit` se lie à la surface d'extensibilité de xUnit, et sa version minimale compatible suit la
version épinglée, que la montée de version a déjà relevée. La façon dont ce dépôt *exécute* ses propres
suites n'atteint aucun paquet : le collecteur, le fichier de réglages et les quatre invocations CI
n'interviennent qu'à la construction. Cette décision peut donc être jugée sur ses seules preuves, et non au
regard d'une promesse de compatibilité.

## Alternatives envisagées

### Conserver le contournement indéfiniment

La CI est verte en l'état, les suites passent, et le quality gate de couverture lit les rapports qu'il a
toujours lus. Rien ne casse demain si la propriété reste simplement à `false`.

Rejetée parce que c'est une maintenance sans fin et sans bénéfice. Chaque version de xUnit arrive de
l'autre côté de la passerelle : le contournement doit donc continuer de contredire une valeur par défaut
que l'amont continue de fixer dans l'autre sens, pendant que la migration qu'il diffère garde la même
ampleur. Elle installe par ailleurs durablement la divergence entre les deux plateformes décrite plus
haut, au lieu d'en faire un simple accident de calendrier.

### Migrer l'exécuteur dans le même changement que la montée de xUnit

L'historique le plus propre : une seule pull request déplace le framework, l'exécuteur, le collecteur et les
quatre invocations CI d'un coup, et aucun contournement n'existe jamais.

Rejetée pour une raison de calendrier — et c'est ce qui a été tenté en premier. À ce moment-là, Stryker
4.16.0 ne savait pas exécuter ces suites sur MTP (`stryker-net#3117`) : le changement groupé portait donc
un job de mutation durablement rouge et ne pouvait pas être fusionné. Pendant ce temps, `dotnet test` refusait
toutes les suites, ce qui constitue une panne de CI urgente, à ne pas suspendre à une migration
transversale. Les séparer a permis de livrer la partie urgente en deux fichiers, et de garder celle-ci
relisible pour elle-même.

### Remplacer le collecteur par l'extension de couverture de Microsoft

C'est le collecteur de première partie de la plateforme, et il dispose d'une version pour chaque ligne de
MTP, ce qui aurait rendu possible la migration en deux étapes évoquée plus haut.

Rejetée parce qu'il produit un format différent, lu par un importateur Sonar différent : il change donc ce
que consomme le quality gate de couverture au moment même où l'exécuteur change — et il aurait fallu
l'adopter deux fois : une fois pour étaler la migration, une fois pour retenir le collecteur définitif.
`coverlet.MTP` atteint l'état final en un seul mouvement.

### Retirer `xunit.runner.visualstudio` comme poids mort

Plus rien dans `dotnet test` ne charge l'adaptateur VSTest une fois la plateforme changée : le paquet
aurait donc pu être supprimé plutôt que monté de version.

Rejetée comme hors du périmètre de la migration, et pas gratuite : l'adaptateur est aussi ce qui permet à
un IDE ne parlant que VSTest de découvrir ces tests : le supprimer échangerait un gain à la compilation
contre une gêne quotidienne sur tout éditeur pas encore à l'aise avec la nouvelle plateforme. Il est monté
de version avec ses voisins et reste en place.

## Conséquences

### Positives

* Les suites s'exécutent sur la plateforme que xUnit cible lui-même : les futures versions majeures ne
  seront donc plus bloquées par une passerelle qui n'existe plus.
* Une seule plateforme, une seule réponse : `dotnet test` et les jobs de mutation exercent les suites de la
  même manière, de sorte qu'un résultat de mutation et une suite verte redeviennent comparables.
* Le contournement disparaît, et avec lui une valeur par défaut que ce dépôt devait sans cesse inverser.
* Sept copies du câblage de couverture se ramènent à un seul import partagé : les suites ne peuvent donc
  plus diverger sur ce qu'elles mesurent.

### Négatives

* Toutes les invocations documentées de `dotnet test` changent de forme : les automatismes, comme toute
  copie d'une commande conservée hors de ce dépôt, deviennent obsolètes d'un coup.
* Les contributeurs travaillant sur un IDE qui ne sait pas encore piloter la nouvelle plateforme ne
  conservent la découverte des tests qu'au travers de l'adaptateur VSTest maintenu, lequel ne correspond
  plus à la façon dont la CI exécute ces mêmes suites.

### Risques

* Le plancher de support est le seul job qui ne peut pas être exercé hors CI — .NET Framework exige
  Windows : sa migration est donc prouvée par le job `framework-floor` plutôt qu'en local. Ses projets se
  compilent sans avertissement avec les nouvelles versions épinglées ; qu'ils *s'exécutent* toujours est
  ce à quoi ce job répond.
* Le collecteur horodate chaque rapport afin que sept suites puissent partager un même répertoire de
  résultats. Deux rapports écrits dans la même milliseconde entreraient en collision ; le préfixe par
  module de la plateforme est le remède, si le cas est un jour observé.

## Actions de suivi

* Décider, lors de la prochaine publication du train `xunit`, si la version minimale compatible déjà
  relevée par la montée de version mérite son propre signal de version pour les consommateurs. Le dernier
  adaptateur publié est encore livré contre 3.2.2 : ce signal n'a donc pas encore lieu d'être.

## Références

* [ADR-0007](0007-floor-the-library-on-net-framework-4-7-2.fr.md) — le plancher de support .NET Framework
  que prouve le job `framework-floor`, et dont le collecteur est explicitement exclu.
* [ADR-0018](0018-adapt-dummies-to-xunit-v3-through-a-companion-package.fr.md) — pourquoi l'adaptateur
  existe et se lie à la surface d'extensibilité de xUnit.
* [ADR-0047](0047-declare-the-adapters-library-dependency-independently.fr.md) — comment la dépendance
  *bibliothèque* de l'adaptateur est choisie au moment du packaging ; sa dépendance xUnit n'est pas choisie
  de cette façon et suit la version épinglée.
* [ADR-0026](0026-measure-justdummies-mutation-against-the-unit-suite-only.fr.md) — la suite de mutation,
  déjà pilotée sur cette plateforme, et la moitié de la divergence entre plateformes qui existait en
  premier.
* [`workflows/sonar`](../workflows/sonar.fr.md) — comment le rapport de couverture parvient au quality
  gate.
