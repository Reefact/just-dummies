# ADR-0090 | Exécuter les suites de tests sur Microsoft.Testing.Platform

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0090-run-the-suites-on-microsoft-testing-platform.md)

**Statut :** Proposed
**Proposé :** 31/08/2026
**Décideurs :** Reefact

## Contexte

Jusqu'ici, chaque suite de ce dépôt passait par **VSTest** : `dotnet test` invoquait la cible `VSTest`
du SDK, `xunit.runner.visualstudio` y adaptait xUnit, et `coverlet.collector` — un *data collector*
VSTest — produisait le rapport OpenCover que lit la barrière Sonar.

xUnit.net est depuis passé à **Microsoft.Testing.Platform** (MTP), un exécuteur où un projet de test est
un exécutable qui héberge ses propres extensions, au lieu d'une bibliothèque qu'un exécuteur séparé
charge. Les deux plateformes ont coexisté tant que MTP 1.x livrait un pont vers la cible `VSTest`.
**MTP 2.x a supprimé ce pont sur le SDK .NET 10**, celui-là même que `global.json` épingle : la cible
s'arrête désormais sur une erreur de première classe qui invite à opter pour la nouvelle expérience
`dotnet test`.

Trois faits rendent le déplacement obligatoire plutôt qu'optionnel, et en font un seul changement plutôt
que plusieurs :

* `xunit.v3` 4.0.0 dépend de MTP 2.x. Il ne peut pas être pris tant que le dépôt tourne sur VSTest.
* `xunit.v3` 3.2.2 épingle sa variante MTP sur un intervalle **exact** de la ligne v1 : impossible de
  hisser le dépôt sur MTP 2.x en restant en 3.2.2.
* `coverlet.MTP` — le remplaçant du collector par le même projet (`coverlet-coverage/coverlet`) —
  n'existe que face à MTP 2.x, dans chacune des versions publiées. La couverture ne peut pas franchir la
  nouvelle plateforme avant le majeur xUnit.

L'exécuteur, le collector et le majeur xUnit forment donc une seule marche indivisible. Dependabot les a
pourtant proposés en trois pull requests distinctes (#85, #86, #87), chacune rouge isolément.

Deux autres contraintes portent sur le changement. Le workflow de mutation pilote déjà Stryker avec son
exécuteur `mtp` : il n'est pas concerné. Et `JustDummies.Xunit` compile contre
`xunit.v3.extensibility.core` et le déclare comme dépendance **publiée** : le plancher de compatibilité
de l'adaptateur bouge avec l'épingle — ce n'est pas une montée de développement seulement.

## Décision

Les suites de tests du dépôt s'exécutent sur Microsoft.Testing.Platform, choisi pour tout appelant via
`global.json`, avec une couverture produite par `coverlet.MTP` et configurée par un fichier de réglages
copié à côté de chaque application de test.

## Justification

**Aucune version de ce dépôt ne garde VSTest tout en prenant xUnit v4.** Les trois faits du Contexte
ferment tous les états intermédiaires : le pont a disparu sur le SDK épinglé, 3.2.2 ne peut pas
atteindre MTP 2.x, et le successeur du collector n'existe pas en dessous. Une décision qu'on
étagerait normalement — déplacer l'exécuteur, puis le framework — n'a aucun étage disponible ; la
consigner comme une seule décision décrit ce qui s'est produit plutôt que de le ranger après coup.

**Opter via `global.json` place le choix là où le SDK regarde déjà.** L'exécuteur est une propriété de
*la chaîne d'outils de ce dépôt*, pas d'un projet ni d'une ligne de commande, et `global.json` est déjà
l'endroit où ce dépôt dit par quel SDK il est bâti. Une propriété MSBuild par projet aurait dû être
répétée sept fois et aurait laissé un `dotnet test` nu, dans le shell d'un contributeur, se comporter
autrement que la même commande en CI — précisément la divergence que le SDK épinglé existe pour empêcher.

**Garder OpenCover garde la barrière qualité honnête.** Le collector alternatif de cette plateforme émet
le format de couverture propre à Microsoft, que Sonar lit par un importateur *différent*. Changer le
format du rapport en même temps que l'exécuteur aurait déplacé deux variables sous une barrière dont les
seuils ont été calibrés contre le premier, et toute dérive des chiffres serait devenue inattribuable.
`coverlet.MTP` est le même outil qu'avant, des mêmes auteurs, émettant le même format : la barrière
continue de mesurer ce qu'elle mesurait, et la migration reste réfutable par comparaison.

**Configurer le collector dans un fichier, non sur une ligne de commande, préserve une décision
existante.** Les réglages qui vivaient dans `coverage.runsettings` y étaient tenus précisément pour
qu'une exécution locale et une exécution CI ne puissent pas mesurer des choses différentes. Le fichier
de réglages de la plateforme sert le même but : la propriété survit au changement de mécanisme, seuls le
nom et le format du fichier ont bougé. La ligne de commande ne garde que l'interrupteur qui *active* la
collecte, exactement comme avant.

**Borner le collector à la jambe moderne vaut mieux que découvrir ses limites sur le plancher.** Le
plancher de support (ADR-0007) exécute les assets netstandard2.0 sur le vrai CLR .NET Framework, et
cette jambe ne collecte aucune couverture — les chiffres viennent de la jambe moderne. Puisque le
collector documente .NET Core 8.0 comme son runtime supporté, le câbler dans une jambe qui n'en a pas
besoin et à qui il n'est pas promis n'achèterait rien et risquerait un échec au démarrage dans le seul
job dont le but entier est de prouver que le plancher tourne encore.

**Le plancher de l'adaptateur bouge parce qu'une dépendance de compilation ne peut pas se publier plus
ancienne qu'elle n'est.** `JustDummies.Xunit` se lie à la surface d'extensibilité de xUnit ; il est bâti
contre ce que le dépôt épingle, et livrer un paquet prétendant fonctionner face à une version contre
laquelle il n'a pas été compilé serait une promesse que rien ne vérifie. Relever le plancher déclaré est
la lecture honnête de ce que le paquet est devenu.

## Alternatives envisagées

### Rester sur VSTest et décliner le majeur xUnit

Le dépôt fonctionne aujourd'hui : rien ne force le déplacement *cette semaine*. Fermer les trois pull
requests et demander à Dependabot d'ignorer le majeur ne coûterait rien dans l'immédiat.

Rejetée parce que le sursis est temporaire et rétrécit. Le pont a déjà disparu sur le SDK épinglé ;
chaque publication xUnit ultérieure est de l'autre côté, donc la dette grossit pendant que la migration
garde la même taille. Décliner gèlerait aussi la liaison de l'adaptateur sur une version que son propre
amont a dépassée — la position que l'ADR-0018 a donné au paquet compagnon justement pour l'éviter.

### Migrer l'exécuteur d'abord, prendre le majeur xUnit ensuite

L'étagement naturel : faire atterrir le changement risqué et transversal — exécuteur, couverture, quatre
invocations CI — seul, pour qu'il soit revu pour lui-même, puis laisser les trois montées de dépendances
devenir triviales.

Rejetée comme indisponible, non comme indésirable. C'était le plan préféré jusqu'à ce que la mesure
montre que `coverlet.MTP` n'a aucun build face à MTP 1.x, et que `xunit.v3` 3.2.2 épingle exactement la
ligne v1. La version étagée aurait donc dû franchir la frontière de plateforme sans aucune couverture,
sous une barrière qui bloque dessus.

### Remplacer le collector par l'extension de couverture de Microsoft

C'est le collector de première partie de la plateforme et il a un build pour chaque ligne MTP, ce qui
aurait rendu possible la migration étagée ci-dessus.

Rejetée parce qu'il émet un format différent, lu par un importateur Sonar différent : il change ce que
la barrière de couverture consomme au moment même où l'exécuteur change — et il faudrait l'adopter deux
fois, une pour étager la migration, une pour arrêter le collector final. `coverlet.MTP` atteint l'état
final en un seul mouvement.

### Retirer `xunit.runner.visualstudio` comme poids mort

Rien dans `dotnet test` ne charge l'adaptateur VSTest une fois la plateforme changée : le paquet aurait
pu être retiré plutôt que monté.

Rejetée comme hors du périmètre de la migration, et non gratuite : cet adaptateur est aussi ce qui
permet à un IDE ne parlant que VSTest de découvrir ces tests, et le retirer échangerait une économie à
la compilation contre une régression de travail quotidien sur tout éditeur pas encore à l'aise avec la
nouvelle plateforme. Il est monté avec ses frères et reste.

## Conséquences

### Positives

* Les trois pull requests Dependabot rouges (#85, #86, #87) sont répondues par un seul changement, et
  aucune ne peut être fusionnée seule.
* Les suites s'exécutent sur la plateforme que xUnit vise lui-même : les majeurs futurs cessent d'être
  bloqués sur un pont qui n'existe plus.
* Sept copies du câblage de couverture s'effondrent en un import partagé : les suites ne peuvent plus
  diverger sur ce qu'elles mesurent.

### Négatives

* **Le plancher de dépendance publié de `JustDummies.Xunit` monte à `xunit.v3.extensibility.core`
  4.0.0.** Un consommateur encore sur la ligne 3.x ne peut pas prendre la prochaine version de
  l'adaptateur sans bouger lui aussi. C'est un changement visible du consommateur sur le train `xunit`,
  pas un détail de build, et c'est la part de cette décision qui n'est pas réversible en éditant ce
  dépôt.
* Chaque invocation `dotnet test` documentée change de forme : les automatismes et toute copie d'une
  commande hors de ce dépôt périment d'un coup.
* Les contributeurs sur un IDE qui ne sait pas encore piloter la nouvelle plateforme ne gardent la
  découverte des tests que par l'adaptateur VSTest conservé, lequel ne correspond plus à la façon dont
  la CI exécute les mêmes suites.

### Risques

* Le plancher de support est la seule jambe qui ne peut pas être exercée hors CI — .NET Framework exige
  Windows — donc sa migration est prouvée par le job `framework-floor` plutôt que localement. Ses
  projets compilent proprement contre les nouvelles épingles ; qu'ils *tournent* encore est ce à quoi
  ce job répond.
* Le collector horodate chaque rapport pour que sept suites partagent un seul répertoire de résultats.
  Deux rapports écrits dans la même milliseconde entreraient en collision ; le préfixe par module de la
  plateforme est le remède si le cas est un jour observé.

## Actions de suivi

* Fermer #85, #86 et #87 comme supersédées une fois ceci atterri ; aucune n'est fusionnable seule.
* Décider, à la prochaine publication du train `xunit`, si la montée du plancher de l'adaptateur mérite
  son propre signal de version aux consommateurs.

## Références

* [ADR-0007](0007-floor-the-library-on-net-framework-4-7-2.fr.md) — le plancher de support .NET Framework
  que le job `framework-floor` prouve, et hors duquel le collector est borné.
* [ADR-0018](0018-adapt-dummies-to-xunit-v3-through-a-companion-package.fr.md) — pourquoi l'adaptateur
  existe et se lie à la surface d'extensibilité de xUnit.
* [ADR-0047](0047-declare-the-adapters-library-dependency-independently.fr.md) — comment la dépendance
  *bibliothèque* de l'adaptateur est choisie au pack ; sa dépendance xUnit ne l'est pas et suit l'épingle.
* [ADR-0026](0026-measure-justdummies-mutation-against-the-unit-suite-only.fr.md) — la suite de mutation,
  déjà pilotée sur cette plateforme et non concernée.
* [`workflows/sonar`](../workflows/sonar.fr.md) — comment le rapport de couverture atteint la barrière
  qualité.
