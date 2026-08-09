# ADR-0063 | Ne donner au scaffolder aucune dépendance sur le package JustDummies

🌍 🇬🇧 [English](0063-give-the-scaffolder-no-dependency-on-the-package.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

> Les renvois de section (§N) pointent vers la [spécification de `dum`](../specifications/justdummies-tool.fr.md), le document dont cet enregistrement a été extrait.

## Contexte

Le tool émet du code qui appelle l'API de la bibliothèque, mais n'appelle jamais cette API
lui-même.

Si le tool référençait la bibliothèque, le projet du développeur en détiendrait deux versions :
celle contre laquelle le tool a été construit et celle que le projet référence réellement.

Les analyzers de la bibliothèque résolvent déjà chaque symbole de celle-ci par nom de métadonnée
contre la compilation du consommateur, sans référencer aucun assembly de la bibliothèque ; une
règle dont le type est absent de la compilation se tait simplement.

Le dépôt hôte publie des familles de packages sur des trains de publication, chaque train publiant
ses membres à une version unique.

## Décision

Ni le moteur ni la CLI ne référencent le package ou le projet JustDummies ; chaque symbole
JustDummies est résolu par nom de métadonnée contre la compilation du développeur.

## Justification

La question de correction du tool n'est jamais « qu'offre la version de bibliothèque contre
laquelle j'ai été construit » mais « qu'offre la version de bibliothèque de ce projet ». Une
référence répond à la première en laissant croire qu'elle répond à la seconde, ce qui est
exactement ainsi qu'un outil se met à émettre du code qui ne compile pas chez quelqu'un d'autre.

Conjuguée à [ADR-0059](0059-emit-only-members-resolved-in-the-target-compilation.fr.md), l'absence de référence rend l'écart de version structurellement impossible plutôt
que seulement testé. Il n'y a aucun couple de versions à tester, parce que le tool ne détient
aucune version de la bibliothèque.

Les analyzers de la bibliothèque fonctionnent déjà ainsi, ce qui démontre que le motif suffit pour
exactement ce travail : des symboles résolus par nom, un silence gracieux quand un type est absent.

Cela découple aussi les trains de publication. Le tool sort quand le tool change et la bibliothèque
quand la bibliothèque change, et aucun ne force la publication de l'autre.

## Alternatives considérées

##### Référencer la bibliothèque et versionner les deux en lockstep

Considérée parce qu'elle laisse le compilateur vérifier l'usage que l'émetteur fait de l'API, et
parce qu'un numéro de version identique est une histoire de compatibilité évidente à présenter aux
utilisateurs.

Écartée parce que le lockstep ne garantit que la correspondance du tool avec la bibliothèque publiée
en même temps que lui, pas avec celle du projet du développeur — le seul cas qui compte — et parce
qu'elle forcerait une publication du tool à chaque publication de la bibliothèque.

## Conséquences

**Positives.** Aucune matrice de versions, aucune question de compatibilité à gérer, et des cadences
de publication indépendantes.

**Négatives.** La connaissance que l'émetteur a de l'API s'exprime en chaînes, donc un nom de membre
mal orthographié n'est pas une erreur de compilation dans le tool. Il remonte comme un membre non
résolu, que [ADR-0059](0059-emit-only-members-resolved-in-the-target-compilation.fr.md) transforme en TODO — une sortie fausse mais silencieuse.

**Risques.** Ce mode d'échec silencieux est le vrai coût de cette décision. Atténué par les tests de
compilation de la sortie et le test sur le code du dépôt (§12), qui exercent les expressions émises
contre une vraie compilation, où un membre mal orthographié apparaît en TODO à une place qui aurait
dû porter une valeur.

## Actions de suivi

* Le package du tool doit asserter au moment du packaging qu'il ne déclare aucune dépendance
  JustDummies (§13.6) — la forme exécutable de cette décision.

## Références

* §10.4, §13.6, §14.2 de cette spécification.

---
