# ADR-0120 | Signaler un tirage fait dans une lambda de composition

🌍 🇬🇧 [English](0120-report-a-draw-made-inside-a-composition-lambda.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

## Contexte

La bibliothèque garde séparés une recette et un tirage : un générateur décrit ce qu'une valeur doit satisfaire, et
seul `Generate()` en produit une. Le premier essai d'expérience développeur a trouvé des utilisateurs dérivant une
valeur d'une autre en appelant `Generate()` sur un générateur intérieur dans la lambda passée à `As(...)` ou à
`Combine(...)`. La lambda s'exécute au moment de la génération, donc la valeur intérieure est tirée à chaque valeur —
mais depuis la source que porte le générateur intérieur. Sous un contexte `Any.WithSeed(...)` explicite, un
générateur intérieur construit à partir de l'`Any` statique tire de la graine ambiante, et l'exécution ne se rejoue
qu'en partie depuis la graine qu'elle annonce (ADR-0102).

Deux formes gardent une telle chaîne en une seule recette : un décalage tiré à côté de la première valeur et composé
par `Any.Combine`, qui tient par construction ; et `Then`, qui construit le générateur dépendant à partir de la
valeur tirée et le tire sur la même source (ADR-0119). Laquelle convient dépend de la relation, et l'unité et les
bornes d'un décalage sont celles que le développeur choisit.

Les règles de la bibliothèque signalent ce que le système de types ne voit pas, au site d'appel, avec un message
nommant l'issue (ADR-0038). Une règle qui reconnaît les `As` et `Combine` de JustDummies peut dire, à partir du seul
arbre d'opérations, qu'un `Generate()` se trouve dans une lambda passée à l'un d'eux.

## Décision

Un avertissement de la famille reproductibilité signale un `Generate()` sur un générateur dans une lambda passée aux
`As` ou `Combine` de JustDummies, à toute profondeur, et nomme les deux issues — un décalage composé par `Combine`, ou
`Then` — sans correctif automatique.

## Justification

**L'erreur est invisible et son coût est le rejeu.** La chaîne compile, tire, et passe sous la graine ambiante ; elle
ne manque que la promesse que fait le second fondamental, et seulement sous un contexte explicite. C'est le genre de
défaut pour lequel une règle existe : un défaut que le système de types accepte et qu'une exécution de test ne montre
pas.

**Un avertissement, parce que le remède est toujours meilleur.** Même quand le générateur intérieur vient du même
contexte et que la chaîne se rejoue, le tirage reste à l'écart de la recette, et les deux mêmes formes énoncent la
relation comme une seule recette qu'un lecteur peut voir. Il n'existe aucune écriture correcte que la règle devrait
tolérer, ce qui l'empêche de n'être qu'une information.

**Les deux issues, parce que la règle ne sait pas laquelle s'applique.** Que la seconde valeur soit un décalage par
rapport à la première ou un générateur construit à partir d'elle est un fait du domaine. Nommer les deux laisse le
lecteur choisir ; un correctif automatique devrait choisir l'unité et les bornes d'un décalage que personne n'a
énoncé.

**Où se trouve le tirage, pas d'où il tire.** Lire la source du générateur intérieur demanderait un suivi de flux de
données que les règles ne font délibérément pas (un générateur gardé dans une locale ou un champ), et scinderait une
même erreur en deux verdicts. La place de l'appel est certaine depuis l'arbre, donc la règle répond exactement à cela.

## Alternatives envisagées

### Ne signaler qu'un tirage intérieur depuis l'`Any` statique

Envisagé parce que c'est le cas qui casse le rejeu. Rejeté : il faut résoudre la source de chaque générateur
intérieur, ce que les règles ne suivent pas à travers les locales et les champs, et cela laisserait silencieuse une
forme que les deux remèdes améliorent tout autant.

### Une sévérité Info

Envisagée parce qu'un tirage intérieur depuis le même contexte se rejoue. Rejetée : la règle n'a aucun cas où
l'écriture signalée est la bonne à garder, ce qui est la distinction que l'ADR-0077 trace pour les règles
d'information.

### Un correctif automatique réécrivant la lambda en `Then`

Envisagé parce que `Then` couvre chaque cas mécaniquement. Rejeté : là où un décalage suffit, c'est la forme la plus
claire, et la réécriture ne peut pas savoir laquelle le développeur veut dire.

## Conséquences

### Positives

* Un rejeu partiel sous un contexte explicite est signalé là où il est écrit.
* Chaque signalement nomme une forme qui garde la chaîne en une seule recette.

### Négatives

* Un tirage intérieur depuis le même contexte, qui se rejoue, est signalé aussi.

### Risques

* Une lambda passée à l'`As` ou au `Combine` d'une autre bibliothèque n'est pas reconnue, par conception.

## Actions de suivi

* Aucune.

## Références

* [ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.fr.md) — les analyzers gardent la frontière
  entre recette et valeur.
* [ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.fr.md) — quand une règle signale une écriture correcte.
* [ADR-0102](0102-let-a-collection-or-composition-inherit-its-operands-source.fr.md) — le cas du rejeu partiel.
* [ADR-0119](0119-draw-a-value-that-depends-on-another-draw.fr.md) — `Then`.
* Issue #358 ; CORE-C2 du premier essai d'expérience développeur.
