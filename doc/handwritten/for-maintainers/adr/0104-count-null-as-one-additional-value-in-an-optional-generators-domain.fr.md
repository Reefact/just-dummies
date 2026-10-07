# ADR-0104 | Compter `null` comme une valeur supplémentaire dans le domaine d'un générateur optionnel

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0104-count-null-as-one-additional-value-in-an-optional-generators-domain.md)

**Statut :** Proposed
**Proposé :** 2026-10-07
**Décideurs :** Reefact

## Contexte

`OrNull()` rend un générateur optionnel : il produit `null` environ une fois sur deux et, sinon, une
valeur que tire le générateur enveloppé. Il a toujours été construit comme un `DerivedAny<T>`, le
générateur que produisent aussi `As` et `Combine`, et un générateur dérivé n'annonce aucun
`ICardinalityHint<T>` : une fabrique arbitraire n'a pas d'inverse pour répondre de l'appartenance.
Donner une cardinalité à toute cette famille a été envisagé puis écarté lors de l'introduction de la
levée nullable ([ADR-0094](0094-lift-a-nullable-value-type-rather-than-deriving-it.fr.md)).

Une collection distincte sur un générateur qui n'annonce rien n'a pas de plafond pour son effectif.
Elle tire une taille à partir de ses propres bornes et, quand le domaine des éléments est plus petit
que cette taille, le tirage dédoublonné borné s'épuise et signale le manque avec sa graine
([ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md)).

L'[issue #212](https://github.com/Reefact/just-dummies/issues/212) a mesuré ce que cela produit sur
un élément optionnel, sur les 2000 graines de 0 à 1999. `Any.SetOf(Any.Enum<Slot>().OrNull()).NonEmpty()`,
sur une énumération à trois membres, a échoué sur 1100 d'entre elles. Le même ensemble sur
`Any.Boolean().OrNull()` a échoué sur 1316, et sur `Any.OneOf("EUR", "USD", "GBP").OrNull()`, un type
référence, sur 1100. Les mêmes ensembles sur les générateurs nus, ou sur `AsNullable()`, n'ont jamais
échoué. Chacune de ces déclarations est satisfiable : un domaine de trois membres plus `null`
contient des ensembles d'une à quatre valeurs.

La levée a reçu un générateur à part entière dans l'ADR-0094 parce que son inverse est connu.
`OrNull` a lui aussi un inverse connu. Une valeur qu'il produit est soit `null`, soit une valeur
produite par le générateur enveloppé, et `null` est une valeur que le générateur enveloppé ne produit
jamais : un type valeur ne le peut pas, et les générateurs de types référence de la bibliothèque
refusent `null` comme élément. La seule exception est un générateur de type référence déjà
optionnel : appliquer `OrNull()` deux fois à un générateur de type référence compile toujours, car
les annotations de nullabilité des références ne changent pas le type à l'exécution. Un générateur
peut aussi relayer un tel domaine sans être celui qui l'a construit.

`ICardinalityHint<T>` porte ensemble un effectif et un test d'appartenance, pour qu'aucun générateur
ne puisse offrir l'un sans l'autre. L'interface est interne : toutes ses implémentations sont celles
de la bibliothèque et, pour un type référence, chacune répond aussi au test d'appartenance pour
`null`, par `true` seulement quand elle peut le tirer.

## Décision

`OrNull()` renvoie un générateur à part entière qui compte `null` comme une valeur supplémentaire
dans le domaine du générateur enveloppé.

## Justification

* **L'inverse est connu, donc les deux moitiés de l'indice répondent correctement.** L'effectif est
  celui du générateur enveloppé, plus un pour `null`. L'appartenance s'énonce « est `null`, ou est
  l'une des valeurs du générateur enveloppé ». C'est l'argument que l'ADR-0094 avançait pour la
  levée, appliqué à la seule autre projection qui le remplit. Rien n'est élargi pour les générateurs
  dérivés : `As` et `Combine` n'annoncent toujours rien, pour la raison qu'avait donnée l'ADR-0094.
* **Un cas jusque-là refusé réussit par construction, pas par chance**, la forme que
  l'[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) approuve. Le
  générateur tire les mêmes valeurs, dans le même ordre, sous la même graine. Ce qui change, c'est ce
  qu'une collection a le droit d'en savoir : la taille qu'elle tire est une taille que le domaine
  peut remplir, et une taille qu'il ne peut pas remplir est refusée avant le moindre tirage
  d'élément, plutôt qu'après un budget épuisé.
* **L'échec était un test instable, ce que cette bibliothèque existe pour supprimer.** Un test qui
  échoue sur la moitié de ses graines pour une raison étrangère à son assertion se rejoue fidèlement
  et ne dit pourtant rien. La déclaration était ordinaire, et la bibliothèque disposait déjà de tous
  les faits nécessaires pour l'honorer.
* **`null` compte une fois, quelle que soit la façon dont le domaine enveloppé en est venu à le
  contenir.** Savoir s'il le contient déjà se lit dans l'appartenance du domaine enveloppé, pas dans
  le type du générateur qui le porte. Un générateur optionnel rendu optionnel une seconde fois, ou
  relayé par tout ce qui transmet son domaine, n'ajoute aucune valeur ; compter `null` deux fois
  surestimerait le domaine exactement dans le sens qui pousse une collection distincte au-delà.

## Alternatives considérées

### Documenter la limite et renvoyer vers `AsNullable()`

Envisagée parce qu'elle ne demande aucun code : `AsNullable()` porte déjà une cardinalité, et une
collection distincte sur lui n'échoue jamais.

Écartée parce qu'`AsNullable()` signifie autre chose. Il ne produit jamais `null` : qui veut un
élément optionnel dans un ensemble, `null` compris, n'aurait aucune écriture qui fonctionne, et celle
qui se lit correctement continuerait d'échouer sur la moitié de ses graines. Documenter un test
instable ne l'arrête pas.

### Donner une cardinalité au générateur dérivé

Envisagée pour la raison que consigne l'ADR-0094 : une seule modification, et toute la famille en
profite.

Écartée pour la raison que consigne aussi l'ADR-0094. Un composeur sur plusieurs opérandes n'a pas de
cardinalité que quiconque puisse calculer, et transmettre l'appartenance exige un inverse qu'une
fabrique arbitraire n'a pas. `OrNull` n'est pas une fabrique arbitraire : c'est pourquoi il quitte la
famille, plutôt que la famille ne gagne une borne.

## Conséquences

### Positives

* Une collection distincte dont l'élément est rendu optionnel tire sur toutes les graines, pour les
  types valeur comme pour les types référence.
* Un effectif exact supérieur à la taille du domaine, `null` compris, est refusé par une
  `ConflictingAnyConstraintException` avant le moindre tirage d'élément, et un `null` épinglé par
  `Containing` est compté comme déjà dans le domaine.

### Négatives

* Deux types de générateurs internes de plus, un par surcharge d'`OrNull`, là où une dérivation
  générale suffisait.
* L'exception change pour un effectif exact impossible : un ensemble dont l'effectif exact dépasse la
  taille du domaine, `null` compris, lève désormais `ConflictingAnyConstraintException` à la
  génération, là où le budget épuisé levait `AnyGenerationException`. Un test qui attrapait la
  seconde verrait la première.

### Risques

* **L'effectif ne vaut que ce que vaut celui du générateur enveloppé.** Il le transmet et ajoute un ;
  il ne calcule rien. Un générateur qui surestime sa propre borne la surestime ici aussi.
* **La protection contre un double comptage de `null` repose sur une réponse d'appartenance.** Un
  générateur porteur de l'indice qui pourrait tirer `null` et répondrait `false` serait compté une
  fois de trop. L'interface est interne et énonce la question dans son propre contrat : le risque se
  limite à une future implémentation de la bibliothèque qui l'ignorerait.

## Actions de suivi

* Aucune. Aucun analyseur ne modélise le domaine d'`OrNull()` ni celui d'`AsNullable()`, donc aucune
  règle n'a à suivre.

## Références

* [ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md) — le contrat à
  deux niveaux que ceci rétablit pour un élément optionnel.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — par construction
  plutôt que par chance.
* [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.fr.md) — pourquoi un paramètre généré
  par le scaffolder reçoit `AsNullable()` et jamais `OrNull()`.
* [ADR-0069](0069-answer-a-cardinality-bound-under-the-comparer-that-will-use-it.fr.md) — la borne
  demandée sous le comparateur propre d'une collection, que les deux générateurs transmettent.
* [ADR-0094](0094-lift-a-nullable-value-type-rather-than-deriving-it.fr.md) — la levée, et
  l'argument que cet enregistrement applique une seconde fois.
* [Issue #212](https://github.com/Reefact/just-dummies/issues/212) — la mesure.
