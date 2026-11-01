# ADR-0123 | Tirer un décimal sur la grille d'un pas

🌍 🇬🇧 [English](0123-draw-a-decimal-on-a-steps-grid.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

## Contexte

[ADR-0015](0015-draw-lattice-constrained-scalars-on-the-grid.fr.md) a donné à `decimal` un seul treillis,
`WithScale(n)` : les multiples de `10⁻ⁿ`. Il fixait le nombre de décimales, pas le pas. Le premier essai
d'expérience développeur a rencontré deux grilles qui ne sont pas un nombre de décimales — un pas de cotation de
`0.25` (domaine 02) et des notes au demi-point (domaine 18) — et les participants ont écrit soit un `OneOf` de 41
valeurs, soit `Any.Int32().Between(0, 80).As(k => k * 0.25m)`, la projection qu'ADR-0015 existe pour remplacer :
l'intervalle déclaré ne dit plus ce qu'il dit, et la valeur sort de l'algèbre des contraintes. ADR-0015 réservait
un pas général « si l'usage réel montre » qu'il en faut un ; l'issue #359 est cet usage.

Un pas général diffère d'une échelle sur un point qui compte. Toute valeur que le type porte à une magnitude trop
grande pour `n` décimales en a moins, donc elle est encore sur la grille de `WithScale(n)` : cette grille atteint
tout le domaine. Un pas `p × 10⁻ⁿ` avec `p` différent de un, non : près de `decimal.MaxValue`, `k × 0.25` demande
plus de chiffres significatifs que la mantisse de 96 bits n'en porte, et le type l'arrondit — hors de la grille. Les
multiples de `0.25` que le type porte exactement s'arrêtent vers `decimal.MaxValue × 10⁻²`.

## Décision

`Any.Decimal().MultipleOf(step)` restreint la valeur aux multiples d'un pas strictement positif, tirés par
construction comme un nombre entier de pas, jamais arrondis ni filtrés dessus.

* Il se déclare une fois, comme le `MultipleOf` des entiers : un second pas, différent, entre en conflit avec le
  premier, et le même pas à nouveau est une répétition.
* Il tient à côté de `WithScale(n)` quand le pas est sur la grille de cette échelle — `0.25` sur celle de
  `WithScale(2)` — et la grille tirée est alors celle du pas. Un pas hors de la grille de l'échelle entre en conflit,
  en nommant les deux, dans les deux ordres : `MultipleOf(0.25m).WithScale(1)`.
* La grille s'arrête là où ses points cessent d'être exacts. Un pas écrit avec `n` décimales atteint des valeurs
  jusqu'à environ `decimal.MaxValue × 10⁻ⁿ` ; un intervalle déclaré entièrement au-delà est refusé en nommant le pas,
  et un tirage dont un côté est ouvert — y compris celui sur dix
  qu'[ADR-0117](0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.fr.md) étale sur le domaine déclaré — est
  étalé sur la partie que la grille atteint. Un pas qui vaut lui-même `10⁻ⁿ` est la grille de `WithScale(n)` et
  atteint tout le domaine.

Ceci amende ADR-0015 pour `decimal` seulement. La virgule flottante binaire ne garde aucun pas, pour la raison
qu'ADR-0015 donnait.

## Justification

**Par construction, comme ADR-0015 l'a décidé pour tout treillis.** Un point vaut `k × step` avec `k` entier, et la
multiplication comme la division qui trouve `k` restent dans la plage où le type est exact : une valeur tirée est
sur la grille par la façon dont elle a été construite, non parce qu'un contrôle l'a laissée passer. Ni nouveau
tirage, ni filtre.

**L'échelle et le pas s'accordent ou se contredisent ; ils ne s'intersectent pas.** Les multiples de `0.25` qui
ont une décimale sont les multiples de `0.5` : une intersection existe, et la calculer est la combinaison par plus
petit commun multiple qu'ADR-0015 a rejetée comme disproportionnée. Quand le pas est sur la grille de l'échelle,
chaque point de la grille du pas satisfait les deux, donc garder la grille du pas est exact et ne demande aucun
calcul. Quand il ne l'est pas, l'appelant a écrit deux grilles qui ne décrivent pas les mêmes valeurs, et nommer les
deux le dit.

**Arrêter la grille plutôt qu'arrondir dessus.** Au-delà de la plage exacte, un multiple du pas est soit une valeur
que le type ne porte pas, soit une qu'il ne porte qu'en l'arrondissant hors de la grille. Aller plus loin
demanderait soit une recherche exacte, en précision arbitraire, du multiple représentable le plus proche — un
mécanisme que personne ne peut raisonner depuis le site d'appel —, soit une valeur qui viole la contrainte sous
laquelle elle a été tirée. [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) demande la
borne, énoncée : chaque valeur tirée est un multiple du pas, et la région où la bibliothèque ne va pas est nommée.
Elle se situe au-delà de `10²⁶` pour un pas à deux décimales, donc un test qui la rencontre y déclare un intervalle
exprès.

**Une puissance de dix est la grille de l'échelle.** Les multiples de `10⁻ⁿ` sont exactement les valeurs de
`WithScale(n)`, dont la grille atteint tout le domaine. Tirer `MultipleOf(0.01m)` sur elle évite que deux
écritures d'une même grille se comportent différemment en haut de la plage.

## Alternatives envisagées

### Garder `WithScale` comme seul treillis décimal

Envisagé parce que cela ne demande rien de nouveau. Rejeté parce que le retour de terrain montre que la grille dont
on a besoin est un pas, et que les contournements qui restent — un long `OneOf`, ou la projection qu'ADR-0015 a
remplacée — sont ce que cette décision existe pour retirer.

### Tirer sur tout le domaine et arrondir au pas

Envisagé parce que cela atteindrait `decimal.MaxValue` comme le fait `WithScale`. Rejeté parce qu'au-delà de la
plage exacte la valeur arrondie est hors de la grille : une valeur qui viole la contrainte déclarée, ce que la
bibliothèque ne rend jamais.

### Intersecter un pas et une échelle qui se contredisent

Envisagé parce que l'intersection de deux grilles est souvent non vide. Rejeté pour la raison qu'ADR-0015 donnait
contre la combinaison de treillis : un recoin de plus petit commun multiple que la demande ne montre pas, là où « le
pas doit être sur la grille de l'échelle » est une règle qu'un lecteur vérifie de tête.

## Conséquences

### Positives

* Un pas de cotation, une note au demi-point ou toute grille décimale régulière tient en un appel, tiré sur la
  grille avec un intervalle déclaré qui garde son sens, et exclusions, ensembles de valeurs et indice de cardinalité
  s'appliquent tous.
* Un pas et une échelle qui se contredisent échouent à la ligne d'arrangement, en nommant les deux, et JD034 les
  signale à la compilation quand les deux arguments sont constants.

### Négatives

* Un pas autre qu'une puissance de dix n'atteint pas le haut du domaine décimal : les valeurs au-delà d'environ
  `decimal.MaxValue × 10⁻ⁿ` ne sont jamais tirées, et un intervalle entièrement au-delà est refusé bien que le type
  y porte quelques multiples.
* `decimal` porte désormais deux treillis, et leur règle d'accord est une chose de plus à apprendre.

### Risques

* Un lecteur peut attendre de `MultipleOf(0.25m).WithScale(1)` les multiples de `0.5`. Atténuation : le refus nomme
  les deux contraintes et dit que le pas n'est pas un multiple de `0.1`, et la page des nombres montre le cas qui
  compose.

## Actions de suivi

* Aucune. Une combinaison par plus petit commun multiple d'un pas et d'une échelle n'est revue que si un retour de
  terrain la demande.

## Références

* Issue [#359](https://github.com/Reefact/just-dummies/issues/359) — la décision que porte cet enregistrement.
* [ADR-0015](0015-draw-lattice-constrained-scalars-on-the-grid.fr.md) — les treillis que cet enregistrement amende
  pour `decimal`.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — borner l'ambition, jamais la justesse.
* [ADR-0117](0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.fr.md) — le tirage à côté ouvert qui atteint
  le domaine déclaré.
* La page des nombres — [`numbers.fr.md`](../../for-users/generators/numbers.fr.md).
