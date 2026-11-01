# ADR-0118 | Renvoyer un littéral par Any.Fixed

🌍 🇬🇧 [English](0118-return-a-literal-through-any-fixed.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

## Contexte

Toutes les jointures de composition de la bibliothèque prennent un `IAny<T>` : un opérande d'`Any.Combine`,
l'élément d'une collection, la recette qu'un générateur scaffoldé garde pour chaque paramètre de constructeur. Un
test a régulièrement besoin de placer à l'un de ces endroits une valeur qu'il a choisie — pas un dummy : le
paramètre sur lequel porte le test, un paramètre nullable épinglé sur son cas absent, la branche d'un tirage
dépendant qui ne doit pas varier.

La bibliothèque n'a aucun membre pour cela. `Any.OneOf(value)` s'en approche, mais refuse un élément `null` et
consomme un tirage, si bien qu'un épinglage décale chaque valeur tirée après lui lors d'un rejeu. Le scaffolder
`any` contourne le manque en émettant, dans chaque fichier généré, une classe imbriquée privée `FixedValue<TValue>`
qui renvoie sa valeur sans rien tirer ; sa spécification note `Any.Fixed<T>(value)` comme une suite côté
bibliothèque qui lui permettrait d'abandonner cet utilitaire. Le second essai d'expérience développeur a trouvé des
recettes écrites à la main recourant à `Any.OneOf<IEnumerable<T>>(array)` avec un argument de type explicite pour
renvoyer une constante, et a trouvé l'épinglage scaffoldé renvoyant une même instance d'un objet mutable à toutes
les valeurs construites à partir de lui.

Une composition promet un rejeu complet depuis la graine qu'elle annonce seulement quand chaque opérande tire de
cette graine. Un opérande qui n'annonce aucune source est traité comme étranger — un générateur que la bibliothèque
ne sait pas rejouer — et retire la promesse. Un générateur qui en enveloppe un autre et tire quelque chose de son
cru, `OrNull()` ou une collection, prend la source du générateur enveloppé et retombe sur la source ambiante quand
elle manque.

## Décision

`Any.Fixed<T>(value)` renvoie la valeur telle que donnée à chaque génération : il ne consomme aucun tirage, il
accepte `null`, il renvoie la même instance à chaque fois, et il est neutre dans une composition — il ne nomme pas la
source qu'annonce un rejeu et ne retire pas la promesse que font les autres opérandes.

## Justification

**Un littéral n'est pas un tirage, il n'y prend donc aucune part.** Le premier fondamental sépare la valeur dont un
test se soucie du dummy dont il ne se soucie pas ; `Any.Fixed` est la première sous la forme du second. Ne consommer
aucun tirage est ce qui empêche un épinglage de déplacer les valeurs qui l'entourent, la propriété que l'utilitaire
du scaffolder garantissait déjà à la main dans chaque fichier.

**`null` est accepté parce que le test l'a choisi.** L'ADR-0064 empêchait le scaffolder de *tirer* `null` ; une
valeur fixe n'est pas tirée, et épingler un paramètre nullable sur `null` est la seule façon pour un test d'énoncer
explicitement son cas absent. Le refuser renverrait ce test vers l'utilitaire que la fabrique remplace.

**La même instance, dite plutôt que cachée.** Copier un `T` arbitraire n'est pas quelque chose que la bibliothèque
sache faire correctement pour tout type. Renvoyer l'instance donnée est le seul comportement qui tienne pour tous, et
l'aliasing qu'il implique pour un objet mutable est énoncé dans la documentation du membre, là où l'utilisateur de
l'essai l'aurait trouvé.

**Neutre, parce que ne rien tirer n'est pas être étranger.** Un générateur étranger retire la promesse de rejeu parce
que la bibliothèque ne sait pas rejouer ce qu'il tire. Une valeur fixe ne tire rien, elle est donc rejouée par
n'importe quelle graine. Traiter un générateur reproductible qui n'annonce aucune source comme ne prenant aucune part
au tirage laisse les autres opérandes décider, et une enveloppe qui tire bel et bien par-dessus — la pièce
d'`OrNull()`, l'effectif d'une collection — nomme la source ambiante dont elle tire réellement.

## Alternatives envisagées

### Accepter `null` et sauter le tirage dans `Any.OneOf` quand il n'a qu'une valeur

Envisagé parce que cela n'ajoute aucun membre. Rejeté : une même surcharge aurait deux contrats selon le nombre de
valeurs passées, et un vivier d'une valeur cesserait d'être un vivier pour le seul rejeu.

### Garder l'utilitaire privé du scaffolder et ne rien ajouter à la bibliothèque

Envisagé parce que cela fonctionne déjà. Rejeté : l'utilitaire est dupliqué dans chaque fichier généré, et une
recette écrite à la main n'a pas d'équivalent, ce que l'essai a trouvé les utilisateurs improviser.

### Renvoyer une copie à chaque appel

Envisagé parce que cela supprime l'aliasing. Rejeté : aucune copie n'est correcte pour tout type, et une copie
superficielle pour certains types et profonde pour d'autres serait une surprise pire qu'une instance partagée
énoncée dans la documentation.

## Conséquences

### Positives

* Un épinglage, un opérande fixe ou une branche fixe tient en un appel, et ne décale jamais un rejeu.
* Le scaffolder peut abandonner son utilitaire imbriqué partout où la bibliothèque contre laquelle il compile porte
  `Any.Fixed`.

### Négatives

* Une valeur mutable épinglée par `Any.Fixed` est partagée par toutes les valeurs construites à partir du
  générateur.

### Risques

* Un test peut prendre `Any.Fixed` pour un dummy. Le nom et la page de composition disent que c'est un littéral.

## Actions de suivi

* Le scaffolder `any` émet `Any.Fixed` pour un épinglage quand la compilation cible le résout, et garde son
  utilitaire sinon (ADR-0059).

## Références

* [ADR-0059](0059-emit-only-members-resolved-in-the-target-compilation.fr.md) — le scaffolder n'émet que des membres
  résolus.
* [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.fr.md), remplacée par
  l'[ADR-0117](0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.fr.md) — ce que le scaffolder tire pour un
  paramètre nullable.
* Issue #397, et CORE-6 du second essai d'expérience développeur.
