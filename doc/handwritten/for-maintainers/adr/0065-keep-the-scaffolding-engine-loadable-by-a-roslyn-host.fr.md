# ADR-0065 | Garder le moteur de scaffolding chargeable par un hôte Roslyn

🌍 🇬🇧 [English](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

> Les renvois de section (§N) pointent vers la [spécification de `dum`](../specifications/justdummies-tool.fr.md), le document dont cet enregistrement a été extrait.

## Contexte

La CLI doit ouvrir un projet sur disque, ce qui exige un workspace conscient de MSBuild ; celui-ci
n'est disponible que sur .NET moderne, pas sur la cible de bas niveau.

Un assembly chargé par le compilateur d'un consommateur — analyzer, code fix, code refactoring —
doit cibler le framework de bas niveau et être compilé contre la version minimale de Roslyn sous
laquelle il doit se charger. Construit contre une plus récente, il échoue à se charger, et il échoue
silencieusement.

Un code refactoring Roslyn est une seconde surface plausible pour le moteur : la bibliothèque publie
déjà des analyzers, donc le chemin de packaging et de chargement existe, et appliquer un document
est l'opération naturelle d'un refactoring.

Le travail du moteur est de l'inspection de symboles, de la lecture de syntaxe et de la construction
de chaînes. Il n'a besoin ni de système de fichiers, ni de console, ni de MSBuild.

La surface de tests décrite au §12 est dominée par le comportement du moteur plutôt que par la
plomberie de commandes.

Le dépôt hôte mesure la mutation sur tout projet dont le code est publié ou s'exécute (§13.5).

## Décision

Le moteur de scaffolding est une bibliothèque séparée ciblant le framework de bas niveau et compilée
contre le plancher Roslyn de l'analyzer, ne faisant aucune entrée-sortie, la CLI étant une coquille
par-dessus.

## Justification

La contrainte est asymétrique dans le temps. Cibler le plancher ne coûte presque rien au moteur
aujourd'hui, parce qu'aucune partie de son travail n'a besoin d'une API moderne. Découvrir plus tard
qu'il doit être chargeable par un compilateur signifie re-vérifier chaque API qu'il utilise contre ce
plancher, dans un code écrit sans cette contrainte à l'esprit. Payer maintenant est bon marché,
payer plus tard ne l'est pas, et c'est ce qui justifie de construire pour un consommateur qui
n'existe pas encore.

La frontière qu'exige le consommateur futur est celle-là même que veut le code présent. Un moteur
qui prend une compilation et retourne un modèle, sans sortie propre, est la forme testable : le
résolveur et l'émetteur s'exercent sur une compilation en mémoire, sans projet sur disque ni analyse
d'arguments dans le chemin.

Les séparer sépare aussi le budget de mutation. La plomberie de commandes et les règles de
résolution ne méritent pas la même attention, et un projet unique ne peut pas exprimer cette
différence.

L'argument selon lequel la CLI pourrait gagner d'autres verbes ne justifie rien de tout cela. Des
verbes en plus sont des fichiers en plus au-dessus du même moteur, et après [ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.fr.md) la liste plausible
est de toute façon quasi vide.

## Alternatives considérées

##### Un projet CLI unique contenant tout

Considéré parce que c'est la plus petite chose qui fonctionne pour un outil à un seul verbe, et que
cela évite deux projets et deux suites de tests.

Écarté parce qu'il ferme la voie de l'hôte Roslyn à l'instant de sa création, et parce qu'il force
chaque test du moteur à passer par les dépendances de la CLI.

##### Un moteur séparé ciblant .NET moderne

Considéré parce qu'il garde la frontière, et avec elle les bénéfices de test et de mutation, sans
accepter la contrainte de bas niveau.

Écarté parce que la raison d'être principale de la frontière est le consommateur que cette variante
exclut.

## Conséquences

**Positives.** Le moteur est chargeable tel quel par un hôte compilateur. Ses tests n'ont besoin
d'aucun projet sur disque. La mesure de mutation peut être visée là où elle paie.

**Négatives.** Deux projets et deux suites de tests pour un verbe. Le moteur est écrit contre le
framework de bas niveau, donc les API de confort modernes lui sont indisponibles.

**Risques.** L'épinglage au plancher Roslyn peut dériver si la référence de package du moteur est
laissée flottante, et l'échec de chargement qui en résulte est silencieux. Atténué par un épinglage
sur la même propriété de plancher que celle du package d'analyzers (§13.2).

## Actions de suivi

* Si un code refactoring est un jour construit, le moteur devra être publié comme package propre
  (§16).

## Références

* §10, §12, §13.2, §13.5, §16 de cette spécification.

---
