# ADR-0070 | Émettre un point d'entrée à la demande, dans un fichier à lui

🌍 🇬🇧 [English](0070-emit-an-entry-point-on-request-as-a-file-of-its-own.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-12
**Accepted:** 2026-08-12
**Decision Makers:** Reefact

> Les renvois de section (§N) pointent vers la [spécification de `dum`](../specifications/justdummies-tool.fr.md).

## Contexte

Un generator scaffoldé s'atteint avec `new AnyOrder()`. Les generators de la bibliothèque, eux,
s'atteignent par une façade statique — `Any.Int32()`, `Any.String()` — de sorte que les deux moitiés
d'un même bloc d'arrangement s'écrivent sous deux formes différentes.

`JustDummies.Any` est déclarée `partial`, mais uniquement pour répartir un type sur des fichiers
frères au sein d'un même assembly. Une déclaration partielle ne franchit pas une frontière
d'assembly.

C# résout un nom de type simple dans le namespace englobant avant toute directive `using`
([ADR-0062](0062-emit-the-generator-into-the-target-types-namespace.fr.md)). Une classe statique
nommée `Any` déclarée dans le projet du développeur masque donc celle de la bibliothèque au lieu de
la compléter, et `Any.Int32()` cesse de compiler avec `CS0117` — vérifié.

Les membres d'extension statiques de C# 14 acceptent une classe statique comme receveur, ce qui
atteint `Any.Order()` sans déclarer un second `Any`. Ils compilent pour une cible `netstandard2.0`
aussi bien que pour `net10.0` — vérifié — donc ce qu'ils exigent est la **version de langage** du
projet, pas son framework cible. En deçà de C# 14 la construction ne parse pas.

Le generator émis n'utilise aucune construction plus récente que C# 7.3 (§4.4), parce qu'il atterrit
dans le projet du développeur et compile au `LangVersion` de ce projet.

L'outil scaffolde une fois et remet le fichier
([ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.fr.md)) ; la
régénération et la détection de dérive sont abandonnées, non reportées (§16). Une invocation écrit un
fichier, de façon déterministe, sans lire ce qui est déjà sur le disque (§8.1).

La surface publique de l'outil est sa ligne de commande, et il ne porte aucune baseline d'API
publique (§13.4). Il a publié une version, `cli-v1.0.0-beta.1`.

La CLI héberge un Roslyn courant et détient la compilation ; le moteur est épinglé au plancher Roslyn
(§13.2), qui n'a pas de nom pour C# 14.

La recette du generator tire du contexte ambiant, de sorte qu'un generator atteint depuis un
`AnyContext` ignorerait le contexte qu'on lui a donné
([ADR-0061](0061-draw-from-the-ambient-context-and-hold-no-state.fr.md)).

## Décision

L'outil n'émet un point d'entrée que si on le lui demande, toujours dans un second fichier à lui, et
atteint l'écriture `Any.` par un membre d'extension C# 14 plutôt que par un type nommé `Any` dans le
projet du développeur.

## Justification

**L'additivité est ce qui préserve chaque garantie existante.** Le fichier du generator est identique
octet pour octet qu'un point d'entrée ait été demandé ou non, donc le plancher de langage de §4.4
reste une propriété du generator et non de l'exécution, `new AnyOrder()` continue de fonctionner, et
la ligne de commande publiée ne gagne qu'une option dont le défaut est son comportement antérieur.
Rien de ce qui est déjà livré ne change de sens.

**Un fichier à lui est ce qui préserve §8.1 et l'ADR-0056.** Une racine unique rassemblant un membre
par type scaffoldé devrait être lue avant d'être réécrite, ce que l'outil ne fait jamais : le
déterminisme dépendrait alors de ce qui était déjà là, et « scaffolder une fois, le fichier est à
vous » deviendrait « scaffolder une fois, et l'outil l'édite ensuite ». Une racine `partial` avec une
part par scaffold atteint le même site d'appel sans rien de tout cela — les parts ne se rencontrent
jamais sur le disque.

**Le membre d'extension est le seul mécanisme qui ajoute l'écriture sans en retirer une.**
L'alternative à laquelle un lecteur pense d'abord — déclarer `Any` dans le projet du développeur — ne
complète pas la façade, elle la masque, et elle coûte `Any.Int32()`. Ce n'est pas un compromis qui
vaille d'être proposé.

**Refuser en deçà de C# 14 vaut mieux que rétrograder.** Un développeur qui a demandé `Any.Order()`
et a reçu silencieusement `Dummies.Order()` le découvrirait au site d'appel, dans un fichier que
l'outil n'a pas écrit. Le refus nomme la version de langage que le projet a résolue et l'option qui
n'exige pas C# 14, ce qui est la forme que prend tout autre refus : ce qui n'a pas pu être fait, puis
quoi faire à la place. Il revient à la coquille, parce que le moteur, épinglé au plancher Roslyn, ne
sait pas nommer la version qu'il devrait vérifier.

**Le namespace du point d'entrée se déplace, celui du generator non.** L'ADR-0062 facture cher un
import à chaque site d'appel, et ce prix est inchangé ici — il est payé par qui lit les tests, pas par
qui lance l'outil. Ce qu'un namespace dédié achète, c'est une racine unique atteignable à travers
plusieurs contextes bornés, ce qui vaut un import ; ce qu'il ne doit pas acheter, c'est le
déplacement du generator, que chaque site d'appel nomme. Garder les deux surcharges distinctes est ce
qui permet à l'une de bouger sans l'autre.

**Le contexte seedé reste en dehors.** `Any.WithSeed(...)` rend un contexte dont les generators
doivent être passés paramètre par paramètre, parce que la recette émise tire de la façade ambiante
(ADR-0061). Un point d'entrée sur `AnyContext` aurait l'air symétrique et ignorerait discrètement le
contexte qu'on lui a remis. Rendre le generator émis conscient du contexte est une décision à part
entière ; une option d'ergonomie ne doit pas l'y faire entrer.

## Alternatives envisagées

##### Une partielle d'`Any` apportée depuis le projet du développeur

L'écriture que le nom suggère : `Any` est déjà `partial`, donc une part déclarée dans le projet de
test semblerait la compléter.

Rejetée parce qu'une déclaration partielle ne franchit pas une frontière d'assembly. La part déclare
un second `Any` sans rapport dans l'assembly du développeur, qui gagne la résolution de nom face à
celui qui est importé et le masque pour tout son namespace — `Any.Order()` compile et `Any.Int32()`
non (`CS0117`, vérifié). Elle retire exactement ce qui rendait l'écriture désirable.

##### Un fichier racine partagé, réécrit au fil des scaffolds

Envisagée parce qu'un unique `Dummies.cs` listant tous les generators se lit bien comme l'annuaire de
ce qu'un projet sait arranger.

Rejetée parce que l'écrire suppose de le lire d'abord. Les octets émis dépendraient alors de l'arbre
de travail plutôt que du type analysé (§8.1), et chaque scaffold deviendrait l'édition d'un fichier
qui appartient au développeur (ADR-0056). La racine partielle atteint le même site d'appel et n'exige
ni l'un ni l'autre.

##### Faire de l'écriture `Any.` la forme par défaut

Envisagée parce que c'est la forme que la bibliothèque emploie elle-même, et un défaut que personne
n'a à découvrir.

Rejetée parce qu'elle porterait le plancher de langage de tout ce que l'outil écrit de C# 7.3 à
C# 14. §4.4 existe précisément parce que le fichier émis compile au `LangVersion` du développeur, pas
à celui de l'outil.

##### Dériver le namespace du point d'entrée de celui de la cible

Envisagée parce qu'un namespace côté test — `Shop.Domain` devenant `Shop.Domain.UnitTests` — est ce
dans quoi un développeur pourrait s'attendre à voir atterrir un helper.

Rejetée parce que le dériver par concaténation invente un namespace : le projet de test peut
s'appeler `Shop.Tests`, `Shop.UnitTests` ou `Tests.Shop`, et le fichier atterrirait là où aucun de
ses voisins ne se trouve. Lire le vrai root namespace du projet est une connaissance MSBuild que le
moteur ne porte pas ([ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.fr.md)),
ce qui est la raison même pour laquelle l'ADR-0062 a rejeté le namespace induit par le dossier de
sortie. Une surcharge explicite atteint la même disposition sans deviner.

##### Émettre un point d'entrée jumeau sur `AnyContext`

Envisagée par symétrie : la bibliothèque reflète sa façade sur `AnyContext`, donc un développeur qui a
appris l'une attend l'autre.

Rejetée parce que ce serait un mensonge. La recette émise tire de la façade ambiante, donc un
generator obtenu depuis un contexte ignorerait la graine de ce contexte (ADR-0061). L'honorer suppose
de rendre le generator émis conscient du contexte, ce qui est une décision distincte, et bien plus
grosse.

## Conséquences

**Positives.** Le défaut est inchangé, donc rien de ce qui est déjà livré ne bouge. Le plancher de
langage du code émis n'est relevé que pour un fichier, et seulement quand ce fichier a été demandé.
Une racine unique est atteignable à travers les namespaces sans que le generator quitte celui où
l'ADR-0062 l'a mis. Les deux fichiers d'un scaffold atterrissent ensemble ou pas du tout, donc un
arbre de travail n'en détient jamais la moitié.

**Négatives.** Un scaffold auquel on a demandé un point d'entrée écrit deux fichiers, donc `--force`
porte sur les deux, et les éditions du développeur sur l'un comme sur l'autre sont perdues par la
même phrase. La sortie de l'outil a désormais deux planchers de langage au lieu d'un, et lequel
s'applique est une propriété du fichier plutôt que de l'outil. Retirer un type d'un projet y laisse sa
part de point d'entrée, et aucun `--clean` ne viendra la ramasser — la régénération est abandonnée
(§16) ; la part orpheline casse le build en nommant un generator qui n'existe plus, ce qui est bruyant
plutôt que silencieux.

**Risques.** Un type cible dont le nom propre est celui de la racine choisie émet un membre nommé
comme sa classe englobante, ce qui ne compile pas (`CS0542` — vérifié). C'est bruyant au build du
développeur, dans l'esprit de
[l'ADR-0060](0060-seed-generators-from-constructor-guards.fr.md), et le remède est un autre nom de
racine.

## Actions de suivi

* Aucune bloquante. Le manque que ce changement rend visible est distinct et n'est pas tranché ici :
  une exécution qui a écrit des fichiers portant des paramètres ouverts sort quand même en `0` (§7),
  donc un bootstrap scripté sur de nombreux types ne peut pas distinguer une exécution complète d'une
  exécution incomplète par son seul code de sortie.

## Références

* §3, §4.4, §4.5, §7, §8.1, §13.2, §13.4, §16 de la spécification.
* [ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.fr.md),
  [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md),
  [ADR-0061](0061-draw-from-the-ambient-context-and-hold-no-state.fr.md),
  [ADR-0062](0062-emit-the-generator-into-the-target-types-namespace.fr.md),
  [ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.fr.md).
