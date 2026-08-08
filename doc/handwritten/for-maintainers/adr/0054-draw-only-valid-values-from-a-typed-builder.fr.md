# ADR-0054 | Ne tirer que des valeurs valides depuis un builder typé, et ne rien juger dans un pool fourni par l'appelant

🌍 🇬🇧 [English](0054-draw-only-valid-values-from-a-typed-builder.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-08
**Accepted:** 2026-08-08
**Decision Makers:** Reefact

## Contexte

*« Des valeurs arbitraires mais **valides** »* est la première ligne du readme de cette bibliothèque et la
phrase qui la distingue d'un générateur de valeurs aléatoires. **Aucun ADR ne l'énonce.** Elle est répétée dans
les docs XML et sert de critère d'admission dans les discussions de conception, sans jamais avoir été écrite
comme une décision.

Ce n'est pas un slogan. C'est appliqué dans le code, comme un garde **d'entrée** et pas seulement comme une
propriété de sortie :

* Tout point d'entrée flottant qui prend un `double`, un `float` ou un `Half` — bornes, valeurs autorisées,
  exclusions — rejette un argument non fini. `Any.Double().Except(double.NaN)` lève. La bibliothèque refuse de
  discuter de `NaN`, des deux côtés.
* `Any.Enum<T>().OneOf(...)` rejette une valeur numérique que l'énumération ne déclare pas.

La règle a aussi servi à **refuser des fonctionnalités**. `Index` et `Range` ont été tenus hors de la surface au
motif que leur validité est contextuelle, donc que « arbitraire mais valide » ne peut pas tenir pour eux de
façon autonome. C'est un filtre de conception appliqué depuis une règle non écrite.

**La règle n'est pas un invariant global, et c'est le point qu'un énoncé sans nuance manquerait.** Les points
d'entrée génériques ne la portent pas, par construction :

* `Any.OneOf(...)` et `Any.ElementOf(...)` vérifient que le pool est non vide et ne contient pas de `null`.
  Rien d'autre. `Any.OneOf(double.NaN, 1.0)` compile et rend `NaN` aujourd'hui.
* `.As(...)` projette vers ce que l'appelant retourne.

Cette asymétrie est correcte — `T` est opaque et la bibliothèque ne peut pas juger la sémantique d'un type
qu'elle ne connaît pas — mais elle signifie qu'un ADR revendiquant un invariant valable partout serait faux le
jour de sa rédaction.

Trois coûts sont déjà observables :

1. **La règle ne peut pas être citée.** Une proposition `Any.Double().WithNaN()`, `Any.Enum<T>().Undeclared()`
   ou `Any.String().NotMatching(regex)` ne contredit aucune décision acceptée. Le refus est re-dérivé de zéro à
   chaque fois, ce qui est la façon dont une règle finit par perdre un débat qu'elle devrait gagner.
2. **Un voisin légitime ressemble exactement aux cas refusés.** Une combinaison `[Flags]` comme `Read | Write`
   est *non déclarée et parfaitement valide* : elle passe le critère. Un générateur de « membre d'énumération
   non déclaré », non. Sans le critère écrit, `AllowingCombinations()` et `Undeclared()` se lisent comme la
   même demande.
3. **La porte de sortie est invisible.** La forme légitime du besoin — un domaine où `NaN` signifie vraiment
   « mesure manquante » — est déjà servie par `Any.OneOf(...)`. Jusqu'à récemment rien ne le disait, et un
   utilisateur se cognait au mur en concluant qu'il manquait une fonctionnalité.
   [ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.fr.md) tranche une question voisine —
   quelles valeurs finies sont tirées — et pas celle-ci.

La bibliothèque est en `1.0.0-preview`. Après la 1.0 la surface est gelée, et un refus qui repose sur une
intuition est un refus qui ne tiendra pas.

**Cet enregistrement est écrit après coup.** La décision a été prise dans le code, plus d'une fois, avant que ce
dépôt ne tienne des ADR ; les gardes et les fonctionnalités refusées ci-dessus en sont la preuve. L'enregistrer
maintenant ne prétend pas qu'elle a été délibérée à une date — cela rend citable une règle existante, et les
dates de l'en-tête disent quand elle a été écrite, pas quand elle a été décidée.

## Décision

Un builder typé tire, et accepte en argument, uniquement des valeurs valides du domaine qu'il représente ; les
points d'entrée génériques — `OneOf`, `ElementOf`, `As` — ne portent aucune garantie de ce type, parce que le
pool de l'appelant est la spécification tout entière.

## Justification

**Une valeur choisie parce qu'elle est hors domaine est le sujet du test, pas un dummy.** Un dummy est une
valeur dont le test a besoin mais sur laquelle il n'assère jamais. Quand un appelant réclame `NaN`, un membre
d'énumération non déclaré ou une chaîne qui viole le format, il réclame le *cas testé* — l'arbitraire à
l'intérieur de cette classe ne le rend pas insignifiant. Une telle valeur a sa place au point d'appel, en
littéral, bien visible, là où un lecteur voit de quoi parle le test. Un générateur qui la cache rend le sujet
du test invisible.

**Tirer « parfois » une valeur invalide produit un test dont le sens dépend de la graine.** Un générateur qui
rend occasionnellement une valeur non finie exerce ce chemin sur certaines exécutions et pas sur d'autres, sans
dire quelle branche il a prise ni même que le choix a eu lieu. C'est pire que de ne pas couvrir le chemin : ça
ressemble à de la couverture.

**Sur les flottants, la contrainte est un fait du moteur, indépendant de la doctrine ci-dessus.** Le moteur
d'intervalle continu est un modèle **ordonné** — il compare des bornes, échantillonne entre elles et parcourt
l'échelle des valeurs représentables pour honorer une exclusion. Toute comparaison avec `NaN` est fausse, donc
`NaN` n'est pas une valeur de plus dans l'intervalle : c'est une valeur hors du modèle qu'est l'intervalle. Une
borne qu'on ne peut pas comparer n'est pas une borne. S'y ajoute que le comparateur par défaut dit que `NaN`
égale `NaN` alors que `==` dit le contraire : un `NaN` atteignant une règle de distinction se dédupliquerait
pendant que la comparaison de l'appelant voit deux valeurs différentes. Les deux lignes d'argument sont
indépendantes, et c'est ce qui rend la décision robuste : rejeter la doctrine laisse la contrainte du moteur
debout.

**Les points d'entrée génériques doivent rester exemptés, et ce n'est pas une incohérence.** La bibliothèque
juge les domaines qu'elle définit. Elle ne peut pas juger `T`, donc `OneOf` traite le pool comme la
spécification entière et ne refuse que ce qu'elle peut réellement savoir faux — un pool vide, un élément `null`.
Étendre la règle là-bas exigerait qu'elle ait un avis sur des types qu'elle n'a jamais vus, et fermerait la
seule porte par laquelle passe le besoin légitime.

**Énoncer la frontière est ce qui fait de l'exemption une conception et non un trou.** Aujourd'hui, un lecteur
qui remarque que `Any.OneOf(double.NaN, 1.0)` fonctionne là où `Any.Double().Except(double.NaN)` lève n'a aucun
moyen de savoir s'il a trouvé la porte de sortie ou un bug. Nommer le niveau auquel la règle tient répond à ça
en une phrase.

## Alternatives considérées

### L'enregistrer comme un invariant valable partout

La phrase la plus simple : *JustDummies ne produit jamais que des valeurs valides*. Rejetée parce qu'elle est
fausse. `Any.OneOf` et `.As` produisent ce que l'appelant fournit, et l'ont toujours fait. Un ADR dont la
première affirmation est contredite par le code apprend au lecteur à se méfier du corpus d'ADR.

### Ne rien écrire

Le statu quo, défendable tant que la surface peut encore changer. Rejetée sur le calendrier : la bibliothèque
est en `1.0.0-preview`, et après la 1.0 une règle non enregistrée est une règle qu'on ne peut pas opposer à une
demande qui arrive avec un cas d'usage plausible. L'intérêt de l'enregistrement est d'être citable *avant*
d'être contesté.

### Ajouter les générateurs et laisser l'appelant décider

`WithNaN()`, `Undeclared()`, `NotMatching(...)`. C'est la demande que la règle refuse, et elle n'est pas
déraisonnable en soi : le besoin derrière est réel. Rejetée parce que ce besoin est déjà servi par `Any.OneOf`
et par un littéral, et parce que l'API rendrait confortable l'écriture du test dépendant de la graine — celui
qui ne couvre un chemin que parfois. Sur les flottants, elle exigerait en plus que le moteur d'intervalle
représente une valeur qu'il ne peut pas comparer.

### Limiter la règle aux tirages, pas aux arguments

Refuser de *tirer* une valeur non finie tout en en acceptant une comme borne ou comme exclusion. Rejetée : une
borne qu'on ne peut pas comparer ne peut pas participer à un modèle ordonné, donc le garde devrait être
réintroduit plus profond, là où la panne se manifesterait comme une valeur fausse plutôt que comme un argument
refusé.

## Conséquences

### Positives

* Une demande de génération de valeur hors domaine a une réponse enregistrée, et cette réponse nomme
  l'alternative au lieu de seulement refuser.
* Les combinaisons `[Flags]` sont visiblement *à l'intérieur* du critère — une valeur combinée est non déclarée
  et valide — donc `AllowingCombinations()` n'est pas pesé par erreur à l'aune d'une proposition `Undeclared()`.
* Un lecteur qui voit `Any.OneOf` accepter ce qu'un builder typé refuse peut savoir que c'est la conception.

### Négatives

* Deux niveaux à expliquer au lieu d'un. Un utilisateur qui apprend « uniquement des valeurs valides » rencontre
  l'exception à la règle dès son premier `Any.OneOf`, et le readme doit porter la frontière plutôt que le
  slogan.
* La règle contraint la conception d'API future : un générateur dont les valeurs ne sont valides que dans un
  contexte que la bibliothèque ne voit pas n'a pas sa place sur un builder typé, aussi commode soit-il.

### Risques

* **La frontière est un jugement dans les cas limites.** « Valide pour le domaine que le builder représente »
  est clair pour un double non fini et pour un membre d'énumération non déclaré ; c'est discutable pour une
  chaîne dont le format est contextuel. L'enregistrement nomme le critère, pas chaque cas futur, et ceux qu'il
  ne tranche pas demanderont encore une décision.
* **`decimal` invite à une fausse symétrie.** `System.Decimal` n'a aucune représentation non finie, donc
  `Any.Decimal()` n'a rien à garder. Un lecteur qui lit la règle et part chercher le garde correspondant ne le
  trouvera pas et pourrait le signaler comme une lacune ; le readme le dit explicitement pour cette raison.

## Actions de suivi

* La recette *NaN and the infinities* du readme nomme déjà la porte de sortie pour le cas des flottants
  ([#31](https://github.com/Reefact/just-dummies/issues/31)) ; elle gagne un paragraphe énonçant la frontière
  elle-même, pour que la règle et sa limite soient lisibles dans le package, et pas seulement ici.

## Références

* [ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.fr.md) — quelles valeurs *finies* sont
  tirées, la décision voisine avec laquelle celle-ci est souvent confondue.
* [ADR-0011](0011-draw-arbitrary-values-from-an-explicit-top-level-pool.fr.md) — le pool de premier niveau dont
  cet enregistrement rend l'exemption délibérée.
* [ADR-0020](0020-draw-flag-enum-combinations-behind-an-opt-in.fr.md) — l'opt-in `[Flags]` que le critère
  admet.
* [Issue #30](https://github.com/Reefact/just-dummies/issues/30) — la lacune que cet enregistrement comble.
