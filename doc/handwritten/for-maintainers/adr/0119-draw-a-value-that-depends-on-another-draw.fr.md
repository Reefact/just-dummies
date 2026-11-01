# ADR-0119 | Tirer une valeur qui dépend d'un autre tirage

🌍 🇬🇧 [English](0119-draw-a-value-that-depends-on-another-draw.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

## Contexte

La bibliothèque compose les générateurs par `As` (une valeur transformée en une autre) et `Combine` (plusieurs
valeurs tirées indépendamment, puis réunies). Ni l'un ni l'autre ne sait tirer une valeur dont le *générateur*
dépend d'une autre valeur tirée : les cellules d'une grille dans ses dimensions tirées, les voix d'une mesure
remplissant sa signature rythmique tirée, un nombre de cellules dépendant de dimensions tirées, un scénario
construit autour d'orbites tirées.

Le premier essai d'expérience développeur a rencontré de telles dépendances, et l'issue #358 a refusé un
combinateur dédié, conseillant plutôt un décalage composé par `Combine`, et nommant son critère de réouverture :
des dépendances qu'un décalage ne sait pas exprimer. Le second essai en a apporté quatre. Les utilisateurs ont écrit
leurs propres classes `IAny<T>`, ou appelé `Generate()` sur un générateur intérieur dans une lambda d'`As(...)`. Cet
idiome fonctionne sous la graine ambiante mais, sous un contexte `Any.WithSeed(...)` explicite, tire la valeur
intérieure de la source ambiante plutôt que du contexte : l'exécution ne se rejoue alors qu'en partie depuis la
graine qu'elle annonce.

Les générateurs de la bibliothèque tirent de la source avec laquelle ils ont été créés : la source ambiante pour les
points d'entrée statiques d'`Any`, une source fixe pour un `AnyContext`. Un générateur construit dans une lambda à
partir de l'`Any` statique tire de la source ambiante, quel que soit le contexte dont vient la chaîne qui l'entoure.

La bibliothèque borne son ambition et jamais sa justesse : elle tire par construction, séquentiellement, et refuse
ce qu'elle ne sait pas honorer plutôt que de chercher une valeur (ADR-0046).

## Décision

`Then` dérive d'un générateur un autre, dont la valeur est tirée du générateur que la fonction d'un appelant
construit pour la première valeur tirée, et ce tirage dépendant utilise la même source que le premier — un
générateur construit à partir de l'`Any` statique dans la fonction tirant du contexte explicite d'où vient la chaîne.

## Justification

**Le critère nommé par #358 est atteint.** Quatre contextes avaient besoin d'une dépendance qu'un décalage ne sait
pas exprimer, et chaque contournement soit réimplémentait `IAny<T>`, soit scindait une exécution entre deux graines.
Un combinateur qui construit le second générateur à partir de la première valeur est la plus petite forme qui
couvre les quatre.

**Séquentiel, donc correct par construction.** Le second tirage se fait en connaissant le premier ; rien n'est
cherché, aucune contrainte n'est propagée à rebours. Cela garde le combinateur dans l'ADR-0046 : il élargit ce qu'un
test peut écrire, pas ce que le générateur tente.

**La même source est ce qui en fait une seule recette.** Le rejeu est le second fondamental. Un tirage dépendant
qui laisserait le générateur intérieur sur la source ambiante reproduirait, sous un contexte explicite, le rejeu
partiel même dont souffre l'idiome `As(...)`. Router la source ambiante vers le contexte le temps du tirage dépendant
est ce qui laisse un test construire son générateur intérieur avec l'écriture ordinaire `Any` et se rejouer quand
même depuis la seule graine qu'il annonce.

**Choisir parmi des générateurs n'appelle aucune API propre.** Un drapeau tiré qui choisit une branche est un
tirage dépendant dont le générateur intérieur est l'un des deux ; `Then` ne tire que celui qui est choisi. Une
fabrique dédiée le dupliquerait sous un second nom.

## Alternatives envisagées

### Continuer à conseiller un décalage par `Combine`

Envisagé parce qu'il tient par construction et ne demande rien de nouveau. Rejeté pour les cas qu'a apportés l'essai :
une cellule dans des dimensions tirées, ou un ensemble de voix remplissant une signature tirée, n'est le décalage de
rien.

### Une fabrique dédiée au choix parmi des générateurs

Envisagée parce qu'elle nomme l'intention du cas courant. Rejetée : c'est `Then` sur un drapeau tiré, et une seconde
écriture d'un même mécanisme est une seconde chose à apprendre et à garder cohérente.

### Laisser le générateur intérieur garder sa propre source

Envisagé parce que cela ne change la source d'aucun générateur. Rejeté : sous un contexte explicite, la chaîne ne se
rejouerait qu'en partie, ce qui est le défaut que le combinateur existe pour supprimer.

## Conséquences

### Positives

* Un tirage dépendant est une seule recette qui se rejoue depuis une seule graine, sous la source ambiante comme sous
  un contexte explicite.
* Choisir parmi des générateurs ne tire que celui qui est choisi.
* La règle qui signale un `Generate()` dans une lambda d'`As` ou de `Combine` peut nommer une issue pour chaque cas.

### Négatives

* Le temps d'un tirage dépendant sur un contexte explicite, la source ambiante est ce contexte : un générateur que la
  fonction construit à partir de l'`Any` statique en tire, ce qui est l'intention, mais c'est un routage qu'un lecteur
  doit connaître.

### Risques

* Un générateur construit à partir d'un *autre* contexte explicite, ou un générateur écrit à la main qui ne tire par
  aucun des deux, garde sa propre source ; la chaîne ne se rejoue alors que dans la mesure où ce générateur le fait.

## Actions de suivi

* La règle décidée par l'issue #358 pointe vers `Then` là où un décalage ne suffit pas.
* Le message de JD012 nomme `Then` comme la façon de choisir parmi des générateurs mis en vivier.

## Références

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — borner l'ambition, jamais la
  justesse.
* [ADR-0102](0102-let-a-collection-or-composition-inherit-its-operands-source.fr.md) — une composition hérite de la
  source de ses opérandes.
* Issues #396 et #358 ; CORE-5 et CORE-12 du second essai d'expérience développeur.
