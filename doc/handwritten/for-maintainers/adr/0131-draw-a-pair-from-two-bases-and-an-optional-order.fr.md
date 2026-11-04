# ADR-0131 | Tirer une paire de deux bases et d'un ordre facultatif

🌍 🇬🇧 [English](0131-draw-a-pair-from-two-bases-and-an-optional-order.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-11-04
**Accepted:** 2026-11-04
**Decision Makers:** Reefact

Supersède l'[ADR-0126](0126-draw-two-values-tied-by-an-order.fr.md).

## Contexte

L'[ADR-0126](0126-draw-two-values-tied-by-an-order.fr.md) a introduit `Any.OrderedPair(first, second, strict)`,
qui tire deux valeurs, la première au plus égale à la seconde, par construction, sur chaque builder ordonné. Son
builder, `AnyOrderedPair<T>`, porte deux épingles, `WithFirst(value)` et `WithSecond(value)`. Chaque épingle est
vérifiée à son appel contre la base de l'autre côté, et une valeur épinglée remplace la base de son propre côté,
comme un littéral. Ni la bibliothèque ni le scaffolder `any` qui émet la paire n'ont été publiés avec elle : les
deux changelogs la tiennent sous *Unreleased*. Les analyzers lisent ces épingles : JD034 et JD035 signalent une
épingle déclarée deux fois, et le parcours de déclaration comme la règle des arguments constants les suivent.

Le troisième essai d'expérience développeur a constaté que deux épingles sont acceptées ou refusées selon l'ordre
dans lequel on les écrit (issue #428). Une première épingle est vérifiée contre l'autre base, qu'une seconde
épingle remplace ensuite. La même déclaration réussit donc dans un ordre et échoue dans l'autre, alors qu'une
fois les deux côtés épinglés aucune base n'est lue et la déclaration est satisfiable. Le générateur scaffoldé
reconstruit la paire à chaque appel `With…`, si bien qu'à travers lui le refus tombe sur l'appel qui vient en
premier. Comme une épingle *remplace* une base au lieu de la restreindre, un appel ultérieur peut rendre
satisfiable ce qu'un appel antérieur a refusé. Aucune vérification faite avant que les deux côtés soient connus
ne peut être indépendante de l'ordre.

Le même essai a constaté des messages de conflit qui nomment `WithFirst` et `WithSecond`, des appels que
l'utilisateur d'un générateur scaffoldé n'a jamais écrits (#429), et une épingle `DateTime` de genre `Local`
acceptée ou refusée selon l'ordre des épingles (#430). Une valeur `Local` peut aussi entrer par l'ensemble de
valeurs d'une base, que l'[ADR-0114](0114-refuse-a-local-datetime-bound.fr.md) admet parce qu'une valeur fournie
n'est pas une borne. Une fois tirée du premier côté, elle devient la borne du second, ce que l'ADR-0114 refuse.
Ce refus dépend alors de la graine et du côté où se trouve la valeur. Une relecture des épingles a aussi constaté
qu'un `NaN` ou un moins l'infini épinglé passe la vérification d'ordre, parce que le comparateur par défaut le
range sous tous les nombres, et que la paire le renvoie. En relisant la paire, le mainteneur n'a pas reconnu
`WithFirst` et `WithSecond`. Ces noms sont des positions, ils ne disent pas qu'ils prennent une valeur et non une
base, et sur un générateur scaffoldé `With…` veut dire épingler une valeur.

La relation est ce qu'un lecteur a besoin de voir, et la paire ne la nomme que par le nom de la fabrique et un
booléen. Chaque builder ordonné exprime déjà un ordre dans ses propres mots : `LessThan`, `LessThanOrEqualTo`,
`GreaterThan` et `GreaterThanOrEqualTo` sur les types numériques et `TimeSpan` ; `Before`, `BeforeOrEqualTo`,
`After` et `AfterOrEqualTo` sur `DateTime`, `DateTimeOffset` et `DateOnly`. L'inverse d'un ordre est l'une de ces
bornes, si bien qu'un côté connu est une borne sur l'autre, ce que la page de composition enseigne déjà. Un
ensemble d'une seule valeur, `OneOf(value)`, exprimerait un côté connu comme une base. Mais il tire un indice, donc
consomme un tirage, là où `Any.Fixed` n'en consomme aucun
([ADR-0118](0118-return-a-literal-through-any-fixed.fr.md)). Et JD038 le signale comme une constante déguisée en
tirage, en conseillant le littéral ou `Any.Fixed`
([ADR-0129](0129-report-a-chain-whose-constants-admit-exactly-one-value.fr.md)).

`Any.PairOf(first, second)` et `Any.TripleOf(…)` composent déjà des générateurs indépendants en un tuple, comme
raccourci de `Combine`. Aucun générateur de la bibliothèque ne renvoie un type de valeur que la bibliothèque
définit : les tirages sont des types .NET, et les compositions renvoient des tuples.

## Décision

`Any.Pair(first, second)` remplace `Any.OrderedPair` : elle tire deux valeurs de deux bases du même builder
ordonné, prend l'ordre entre elles comme une contrainte facultative déclarée par `Having`, et n'a aucune notion
de côté fixé.

* **La construction est celle de l'ADR-0126.** Par construction, jamais par nouveau tirage. Le côté tiré en premier
  est gardé, une fois et à la déclaration, dans ce que l'autre base peut suivre, puis l'autre côté est tiré gardé du
  bon côté de lui. L'inverse d'un ordre est une borne.
* **La relation parle la langue de son builder**, nommée d'`Item1` vers `Item2` : `Item1LessThanItem2`,
  `Item1LessThanOrEqualToItem2`, `Item1GreaterThanItem2` et `Item1GreaterThanOrEqualToItem2`, ou
  `Item1BeforeItem2`, `Item1BeforeOrEqualToItem2`, `Item1AfterItem2` et `Item1AfterOrEqualToItem2`. Une paire
  déclare au plus une relation : la même une seconde fois ne change rien, une autre est refusée comme déjà définie.
* **`Having` est une propriété qui ne fait qu'introduire la relation.** C'est le premier membre de la surface qui
  n'existe que pour la grammaire, et il lit une valeur que la paire détient au lieu d'en construire une.
* **Une paire sans relation tire ses deux valeurs indépendamment.** `Generate()` renvoie le tuple
  `(T Item1, T Item2)`.
* **Aucun côté fixé.** Un côté que le test fixe n'est pas tiré, il ne fait donc pas partie d'une paire. Avec un côté
  fixé, l'autre est tiré borné par lui, dans le vocabulaire propre à son builder. Avec les deux fixés, les valeurs
  s'écrivent telles qu'elles sont.
* **Une paire de `DateTime` refuse une valeur `Local` à la déclaration.** Une base dont l'ensemble de valeurs
  contient une valeur `Local` est refusée quand la relation est déclarée, en nommant le côté et la valeur.
* **Le scaffolder décide au tirage.** Son `Generate()` tire la paire quand aucun côté n'est fixé, borne le côté tiré
  par le côté fixé quand l'un l'est, et passe les deux valeurs fixées au constructeur, dont la propre garde refuse un
  ordre faux. `With…` ne fait que mémoriser la valeur.
* **Inchangé par rapport à l'ADR-0126 :** les dix-huit builders ordonnés, pas de paire sur `TimeOnly`, un ordre
  comme seule relation.

## Justification

**Une relation est une contrainte, elle se lit donc comme une contrainte.** JustDummies remplace un littéral par la
déclaration de ce que la valeur doit satisfaire. `Item1BeforeItem2` dit quel côté précède l'autre, dans les mots
que le builder emploie déjà pour une borne, là où `OrderedPair(…, strict: true)` cachait à la fois la relation et
sa stricte inégalité. Les noms `Item1` et `Item2` sont neutres à dessein : ce sont ceux d'un tuple, et le test
donne aux deux valeurs leurs noms de domaine en les déconstruisant.

**Retirer les épingles retire l'ordre des appels, par construction.** Le défaut de #428 n'est pas une vérification
placée au mauvais endroit. Il vient d'une épingle qui remplace une base, vérifiée avant que le remplacement soit
connu. Une déclaration qui reçoit les deux bases d'un coup n'a aucun ordre d'appels en elle. Un refus ne peut
alors être qu'une contradiction que l'appelant a écrite, et il est levé là où la paire est déclarée. Le trou du
`NaN` des épingles part avec elles.

**Un côté fixé est une borne sur l'autre, pas une base à lui.** Un côté connu n'a pas besoin de paire : l'inverse
d'un ordre est une borne, et chaque builder ordonné la prend déjà. L'écrire là garde la valeur littérale, ce que le
premier fondamental demande d'une valeur que le test a choisie. Cela ne tire rien pour cette valeur, comme le
demande l'ADR-0118. Et un refus nomme alors la borne dans les mots propres au builder. Une paire qui prendrait un
côté fixé porterait une constante déguisée en tirage, ce que JD038 existe pour signaler.

**Une valeur `Local` est refusée parce que, dans une paire, une valeur devient une borne.** L'ADR-0114 admet une
valeur fournie parce qu'elle est renvoyée telle quelle et ne borne rien. Dans une paire, la valeur tirée d'un côté
borne l'autre, si bien que l'ambiguïté que l'ADR-0114 refuse revient. La refuser à la déclaration rend le résultat
indépendant de la graine et du côté où se trouve la valeur.

**`Having` est là pour le lecteur.** La lisibilité est une règle de cette bibliothèque. Séparer les deux bases de la
relation permet de lire une déclaration comme ce qu'est chaque valeur, puis comment elles se rapportent l'une à
l'autre. Il détient la valeur qu'il renvoie, si bien que le lire coûte ce que coûte la lecture d'un champ.

**Le scaffolder décide quand il a les deux côtés.** Son choix n'est indépendant de l'ordre que s'il est fait après
que le test a tout dit des deux côtés, et dans un générateur scaffoldé ce moment est le tirage. Un côté fixé que
l'autre recette ne peut pas suivre est toujours refusé avant l'action, dans l'arrangement, par la borne qui le
nomme. Il est refusé au tirage plutôt que sur la ligne `With…`. Deux valeurs fixées relèvent du domaine, et son
constructeur garde déjà l'ordre.

**La paire reste là où l'ADR-0126 l'a bornée.** Les mêmes builders, la même relation, la même construction.
L'[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) décide toujours de ce qu'une paire
tente, et cet enregistrement change la façon de la déclarer, pas ce qu'elle peut tirer.

**Un tuple, comme le renvoient les autres compositions.** La valeur, c'est deux nombres pour deux paramètres, et un
tuple se déconstruit en eux. `PairOf` et `Combine` renvoient déjà des tuples, et aucun tirage ne renvoie un type
propre à la bibliothèque.

## Alternatives envisagées

### Garder `OrderedPair`, et ne vérifier une épingle seule qu'au tirage

Envisagée parce que c'est le plus petit changement qui rend le résultat indépendant de l'ordre. Rejetée parce
qu'elle garde deux sens de `With…` et une épingle à côté de celle du scaffolder, des noms qui ne disent pas s'ils
prennent une base ou une valeur, et une relation cachée dans un booléen.

### Garder `OrderedPair`, et vérifier chaque épingle contre l'autre base

Envisagée parce qu'elle garde chaque refus à l'appel. Rejetée parce qu'elle refuse des déclarations satisfiables,
avec un message qui les dit en conflit. Elle vérifierait aussi une épingle contre une base qu'une épingle de
l'autre côté a déjà écartée, sans jamais vérifier une épingle contre sa propre base.

### Un côté fixé comme une base d'une seule valeur, `OneOf(value)`

Envisagée parce qu'elle garde la paire comme l'entrée unique de tous les cas, et qu'un générateur scaffoldé pourrait
alors écrire une expression par côté. Rejetée parce qu'elle déguise le littéral du test en tirage, ce que JD038
signale. Elle consomme aussi un tirage là où `Any.Fixed` n'en consomme aucun, et une base tirée d'un contexte
explicite, appariée avec elle, ne se rejouerait plus à partir de la seule graine de ce contexte.

### Accepter `Any.Fixed` comme côté, par un `AnyFixed<T>` public

Envisagée parce qu'`Any.Fixed` est le littéral de la bibliothèque, et qu'il ne consomme aucun tirage. Rejetée parce
qu'un côté fixé n'a besoin d'aucune paire : la borne de l'autre côté dit tout. L'accepter donnerait aussi à
`Any.Fixed` un nouveau type de retour pour ce seul usage.

### Lire une valeur `Local` par ses ticks quand elle devient une borne

Envisagée parce qu'elle ne refuse rien. Rejetée parce que lire une valeur `Local` comme des ticks est l'ambiguïté
que l'ADR-0114 refuse : la bibliothèque ne peut pas savoir si l'appelant voulait une heure d'horloge ou un instant.

### Une entrée fluide, `Any.Pair().WithItem1(first).WithItem2(second)`

Envisagée parce qu'elle montre d'un coup d'œil quelle base devient quel élément. Rejetée au profit de la forme
positionnelle, plus courte et qui sert tout aussi bien le scaffolder. Un appelant qui veut voir les rôles peut
nommer les arguments.

### Un type de résultat propre à la bibliothèque, `Pair<T>` ou `GeneratedPair<T>`

Envisagée parce que `Pair<T> anyPeriod` se lit mieux qu'un tuple quand la paire est gardée entière. Rejetée parce
qu'aucun tirage de la bibliothèque ne renvoie un type qui lui est propre, que `PairOf` renvoie un tuple, et que
`Pair` est un nom qu'un domaine peut très bien employer déjà.

### Des relations nommées d'un seul côté, `Before(second)`

Envisagée parce qu'elles sont plus courtes. Rejetée parce que le sujet de la relation est alors implicite, et c'est
ainsi qu'une paire inversée s'écrit par erreur.

## Conséquences

### Positives

* Le résultat d'une déclaration ne dépend jamais de l'ordre dans lequel ses parties sont écrites, dans la
  bibliothèque comme à travers un générateur scaffoldé, ni, pour une valeur `Local`, de la graine.
* Un refus nomme toujours une contradiction que l'appelant a écrite. Pour un côté fixé, il nomme la borne dans le
  vocabulaire du builder.
* La relation est visible et s'écrit dans le vocabulaire propre à son builder, et la paire n'a aucune notion à elle
  qu'il faudrait apprendre.
* Aucune paire tirée ne peut renvoyer une valeur hors de sa relation par une épingle, puisqu'il n'y en a plus.

### Négatives

* Le `Generate()` scaffoldé porte quatre cas par paire : aucun côté fixé, l'un ou l'autre, ou les deux.
* À travers un générateur scaffoldé, un côté fixé que l'autre recette ne peut pas suivre est refusé au tirage, et
  non plus sur la ligne `With…` qui l'a fixé.
* Une base de `DateTime` dont l'ensemble de valeurs contient une valeur `Local` ne peut plus entrer dans une paire.
* Dix-huit surcharges de fabrique et soixante-douze membres de relation, tenus en phase par le test de parité.
* JD034 et JD035 cessent de lire les épingles, et ne lisent aucune relation déclarée deux fois : l'exécution la
  refuse à la ligne d'arrangement, en nommant les deux, et une règle à la compilation attend un retour de terrain.
  Chaque paire déjà scaffoldée, et le corpus de l'essai d'expérience développeur, sont réécrits dans le même
  changement. Aucun n'a été publié.

### Risques

* Une paire déclarée sans sa relation tire ses deux valeurs indépendamment, si bien qu'une relation oubliée casse
  l'ordre environ une fois sur deux. Atténuation : le scaffolder émet toujours la relation, la page de composition
  la montre en premier, et la propre garde du domaine refuse bruyamment l'ordre cassé.
* `Any.Pair` côtoie `Any.PairOf` sous un nom presque identique jusqu'à ce que le second soit absorbé. Atténuation :
  la page de composition dit lequel est lequel, et l'absorption est une action de suivi.
* Un conflit entre deux bases nomme encore `Item1` et `Item2`, pas les membres scaffoldés que le test a appelés
  (#429). Atténuation : le message nomme les deux côtés et la borne, et le nommage a son issue.
* Un générateur dont chaque paramètre est fixé tire une constante déguisée en tirage (#507). Atténuation : cette
  issue le signale.

## Actions de suivi

* Absorber `Any.PairOf` dans `Any.Pair` : deux types d'éléments, des éléments qui ne sont pas des builders typés, et
  une relation de différence, avec une résolution de surcharge qui n'envoie jamais une paire d'un même type vers la
  forme à deux types.
* Remplacer `Any.TripleOf` par `Any.Triple`, dont les relations, s'il y en a, forment une chaîne d'`Item1` à
  `Item3`. L'idiome documenté de la paire et d'un `Then` ne peut pas servir un triplet entier strict dont les
  valeurs extérieures sont tirées adjacentes.
* #429 et #507, chacune pour elle-même.

## Références

* Issue [#428](https://github.com/Reefact/just-dummies/issues/428) — le défaut d'ordre d'appel que cet
  enregistrement retire.
* Issue [#430](https://github.com/Reefact/just-dummies/issues/430) — la valeur `Local` que cet enregistrement refuse.
* [ADR-0126](0126-draw-two-values-tied-by-an-order.fr.md) — la paire que cet enregistrement supersède.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — pourquoi un ordre et rien de plus.
* [ADR-0114](0114-refuse-a-local-datetime-bound.fr.md) — pourquoi une valeur `Local` est refusée dès qu'elle devient
  une borne.
* [ADR-0118](0118-return-a-literal-through-any-fixed.fr.md) — un littéral ne tire rien.
* [ADR-0129](0129-report-a-chain-whose-constants-admit-exactly-one-value.fr.md) — une constante déguisée en tirage.
* La page de composition — [`composition.fr.md`](../../for-users/guides/composition.fr.md).
