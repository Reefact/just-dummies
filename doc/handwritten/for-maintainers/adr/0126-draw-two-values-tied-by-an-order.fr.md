# ADR-0126 | Tirer deux valeurs liées par un ordre

🌍 🇬🇧 [English](0126-draw-two-values-tied-by-an-order.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

## Contexte

Quinze contextes sur vingt du second essai d'expérience développeur avaient deux paramètres liés par un ordre — un
témoin daté d'un début à une fin, le minimum et le maximum d'une plage, l'ouverture et la clôture d'une session —
et environ la moitié des paires liées rencontrées étaient un ordre. Deux tirages indépendants le brisent une fois
sur deux. Chaque correction à la main a échoué d'une façon ou d'une autre : dériver la fin du début dans
`Generate()` ignore en silence une fin épinglée ; des bandes disjointes lèvent sur la plupart des graines dès que
le début est épinglé haut. La difficulté est la direction : épingler la *fin* doit contraindre le *début*. Pour une
relation générale, cette inversion demande un solveur,
qu'[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) tient à l'écart. Pour un ordre, non :
l'inverse de « second au moins au premier » est une borne de plus sur le premier, et les builders ordonnés prennent
déjà des bornes.

Le mainteneur a fixé le principe : le cœur d'abord, pour que la règle s'écrive facilement à la main ; le scaffolder
(#405) ne la génère que là où c'est simple. L'issue #404 a décidé le comportement ; cet enregistrement règle sa
surface.

## Décision

`Any.OrderedPair(first, second, strict = false)` tire `(first, second)` avec le premier au plus au second, ou
strictement sous lui quand `strict` est posé, sur chaque builder ordonné : les entiers, les types flottants,
`decimal`, `DateTime`, `DateTimeOffset`, `DateOnly` et `TimeSpan`.

* **Par construction, jamais par retirage.** Le premier est tiré depuis sa base maintenue au plus à la plus grande
  valeur que la seconde base peut produire — son sommet ramené sur le treillis et hors des exclusions, ou la plus
  grande de son ensemble de valeurs —, puis le second depuis sa base maintenue au moins au premier. Le seul
  resserrement qui fait réussir tout tirage sans épingle se fait une fois, à la déclaration de la paire ; deux bases
  qu'aucun premier ne peut précéder y sont refusées.
* **Une épingle borne l'autre tirage de la même façon.** `WithFirst(v)` tire le second depuis sa base maintenue au
  moins à `v` ; `WithSecond(v)` tire le premier maintenu au plus à `v`. Chaque épingle se déclare une fois. Deux
  épingles dans le mauvais ordre, ou une épingle qu'aucune valeur de l'autre base ne peut suivre, sont refusées à
  l'appel, en nommant les deux côtés ; le refus cite la borne que l'autre base a levée. Une valeur épinglée est un
  littéral, comme avec `Any.Fixed` : elle est vérifiée contre l'autre base, pas contre la sienne.
* **Une surcharge par builder ordonné**, rendant `AnyOrderedPair<T>`, un builder qui implémente
  `IAny<(T First, T Second)>` : le tuple se déconstruit directement dans les deux paramètres d'un constructeur, et le
  builder porte les épingles. La surcharge capture le vocabulaire de bornes de son propre builder — `GreaterThan` et
  `LessThanOrEqualTo`, ou `After` et `BeforeOrEqualTo` —, si bien que la paire reste constructive sans interface de
  bornes partagée, que les builders n'ont pas. `strict` passe par la borne stricte de la base elle-même, qui sur les
  types flottants veut déjà dire la valeur représentable suivante.
* **Pas sur `AnyContext`.** La paire est une fabrique de composition : elle hérite du contexte par ses opérandes,
  comme `Combine` et `PairOf`, et la garde de miroir lit un opérande builder comme elle lit un `IAny<T>`.
* **Pas de paire `TimeOnly`.** Dès qu'une fenêtre franchit minuit
  ([ADR-0125](0125-let-a-time-of-day-window-cross-midnight.fr.md)), l'ordre du type est cyclique, et « le premier
  avant le second » a deux lectures — linéaire sur la journée, ou le long d'une fenêtre —, aucune juste partout. Le
  cas attend un retour de terrain qui dise laquelle.
* **Pas d'`OrderedTriple`.** Trois valeurs liées par un ordre sont une paire et un `Then` qui tire celle du milieu
  entre les deux. Une primitive, qui compose avec ce qui existe ; le registre consigne le triplet comme non adopté.

## Justification

**L'inverse d'un ordre est une borne.** C'est ce qui sépare ce cas de la relation liée générale qu'ADR-0046 refuse :
ni recherche, ni propagation, un appel de plus sur un builder qui le prend déjà. La construction est aussi exacte,
puisque la plus grande valeur que la seconde base peut produire se calcule dans chaque moteur — la spécification
est satisfiable, donc descendre depuis son sommet s'arrête sur une valeur.

**Un resserrement à la déclaration, pas une supposition à la génération.** Maintenu au plus à la plus grande valeur
du second, tout premier a un second qui le suit, donc un tirage sans épingle ne peut pas échouer. La forme à bandes
disjointes qui levait sur la plupart des graines tire désormais le premier dans le recouvrement des bandes. Des
bases sans aucun recouvrement sont une contradiction écrite par l'appelant, refusée là où elle a été écrite et en
nommant les deux côtés, comme tout conflit.

**Un tuple, porté par un builder.** La valeur dont un test a besoin, ce sont deux nombres pour deux paramètres ; un
tuple s'y déconstruit, un record demanderait un déballage. Le builder existe pour les épingles : un simple
`IAny<(T, T)>` ne pourrait pas prendre `WithFirst` sans perdre son type.

**Des surcharges par builder plutôt qu'une méthode générique.** Un `OrderedPair<T>(IAny<T>, IAny<T>)` générique ne
pourrait épingler qu'en retirant jusqu'à ce que l'ordre tienne, ce qui est le retirage borné de `DifferentFrom`, pas
une construction. Les surcharges sont mécaniques, et elles gardent chaque paire sur le même pied que les builders
dont elle est faite.

**Où la paire s'arrête.** Un ordre cyclique et une troisième valeur sont deux endroits où une réponse générale
serait une supposition, ou une surface doublée pour un cas que l'essai a rencontré moins souvent que la paire ;
les deux restent dehors jusqu'à ce qu'un retour le demande.

## Alternatives envisagées

### Un `OrderedPair<T>(IAny<T> first, IAny<T> second)` générique

Envisagé parce que c'est une seule signature. Rejeté parce qu'un `IAny<T>` ne prend pas de borne : la paire devrait
retirer jusqu'à ce que l'ordre tienne, et `WithSecond(v)` ne pourrait contraindre le premier que par rejet.

### Un drapeau `strict` sur chaque épingle plutôt que sur la paire

Envisagé parce qu'une paire pourrait être stricte d'un seul côté. Rejeté parce qu'aucun cas de terrain ne l'a
demandé, et que l'ordre est une relation avec une seule stricte.

### Un `OrderedTriple`

Envisagé parce que `min < optimum < max` a été rencontré. Rejeté au profit d'une paire et d'un `Then`, qui
l'expriment déjà avec une primitive chacun ; trois épingles et leurs refus croisés doubleraient la surface.

### Une paire `TimeOnly` sur la journée linéaire

Envisagé par souci de complétude. Rejeté parce qu'une fenêtre de nuit (ADR-0125) lit la même paire dans l'autre
sens, si bien que la lecture linéaire voudrait dire en silence la mauvaise chose sur la moitié des domaines qui
tirent la paire.

## Conséquences

### Positives

* Une période, une plage ou une session tient en un appel, tiré ordonné sur chaque graine, chaque épingle
  contraignant l'autre côté.
* Chaque refus est à la ligne d'arrangement et nomme les deux côtés, borne levée par une base comprise.
* Le scaffolder peut émettre la paire là où deux paramètres sont visiblement un ordre (#405), puisque la règle
  écrite à la main tient en une ligne.

### Négatives

* Dix-huit surcharges, une par builder ordonné, tenues en phase par un script et le test de parité.
* La plus grande valeur de la seconde base se lit dans chaque moteur, un membre de plus que les quatre moteurs
  gardent en phase.

### Risques

* Un lecteur peut attendre une paire sur `TimeOnly`. Atténuation : la page de composition dit pourquoi il n'y en a
  pas, et la fenêtre de nuit est `Between` depuis ADR-0125.
* Les analyzers lisent une épingle déclarée deux fois (JD034, JD035) mais pas encore deux épingles constantes dans
  le mauvais ordre. Atténuation : le refus à l'exécution nomme les deux ; une règle à la compilation suivra un
  retour de terrain.

## Actions de suivi

* Le côté scaffolder, #405 : émettre la paire là où deux paramètres sont liés par un ordre que le lecteur de
  gardes voit.

## Références

* Issue [#404](https://github.com/Reefact/just-dummies/issues/404) — la décision que porte cet enregistrement.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — pourquoi un ordre et non une
  relation.
* [ADR-0119](0119-draw-a-value-that-depends-on-another-draw.fr.md) — `Then`, la dérivation à sens unique que la paire
  complète.
* [ADR-0125](0125-let-a-time-of-day-window-cross-midnight.fr.md) — pourquoi `TimeOnly` n'a pas de paire.
* La page de composition — [`composition.fr.md`](../../for-users/guides/composition.fr.md).
