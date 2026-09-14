# ADR-0101 | Accepter comme sans effet l'exclusion d'une valeur non finie sur un générateur flottant

🌍 🇬🇧 [English](0101-accept-a-non-finite-exclusion-as-a-no-op.md) · 🇫🇷 Français (ce fichier)

**Status:** Proposed
**Proposed:** 2026-09-14
**Decision Makers:** Reefact

Restreint l'[ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.fr.md) sur un point — la règle côté
arguments pour les exclusions flottantes. Tout le reste de ce qu'a décidé l'ADR-0054 demeure.

## Contexte

L'[ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.fr.md) a décidé qu'un générateur typé *tire, et
accepte comme argument, uniquement des valeurs valides du domaine qu'il représente*. Sur les générateurs
flottants (`Any.Double()`, `Any.Single()`, `Any.Half()`), cette règle était appliquée par une garde unique sur
chaque argument sans distinction : une borne, une valeur de `OneOf`, une valeur d'`Except` et une valeur de
`DifferentFrom` étaient toutes refusées lorsqu'elles n'étaient pas finies, avec la même phrase. Parmi les
alternatives que l'ADR-0054 a envisagées puis rejetées figurait *« Limiter la règle aux tirages, pas aux
arguments »* — accepter une valeur non finie *comme borne ou comme exclusion* — au motif qu'*« une borne qui
ne peut pas être comparée ne peut pas participer à un modèle ordonné, de sorte que la garde devrait être
réintroduite plus profondément, là où l'échec se manifesterait par une valeur fausse plutôt que par un argument
refusé »*, et qu'un `NaN` atteignant une règle de distinction fondée sur un comparateur se dédupliquerait alors
que le `==` de l'appelant y voit deux valeurs.

Les deux sortes d'arguments n'entrent pas dans le moteur de la même façon. Une borne et une valeur de `OneOf`
**définissent** le domaine : le moteur d'intervalle continu compare les bornes, échantillonne entre elles et
parcourt l'échelle des représentables à partir d'elles, et une valeur de vivier est tirée telle qu'elle a été
donnée. Une exclusion ne fait que **retirer** d'un domaine défini ailleurs : elle est honorée par un test
d'égalité sur chaque candidat, et son seul autre usage se trouve dans le contrôle de satisfiabilité et le
message d'épuisement, qui l'un et l'autre ne comptent une exclusion que lorsqu'elle tombe dans les bornes.
Toute comparaison ordonnée avec `NaN` est fausse, et aucune valeur tirée n'est jamais non finie — une exclusion
non finie ne peut donc correspondre à aucun candidat, ne tombe dans aucune borne, et n'est jamais comptée ni
nommée. Qu'elle soit portée ou refusée, les valeurs que produit le générateur sont les mêmes.

La bibliothèque a déjà une règle pour un argument qui demande ce qui est déjà garanti. *« Redéclarer la MÊME
contrainte n'est pas une contradiction ; c'est un sans-effet plutôt qu'un conflit : la seconde déclaration
demande exactement ce que la première garantit déjà »* est écrit à dix endroits dans les moteurs, vaut pour
chaque contrainte déclarée une fois, et a été étendu à un fragment de chaîne répété par le changement qui a clos
[#181](https://github.com/Reefact/just-dummies/issues/181). Exclure une valeur que le générateur ne tire jamais
a cette même forme : la déclaration de ce qui est déjà vrai.

`DifferentFrom` est documenté comme prenant *« typiquement une valeur existante que le test détient déjà »*.
Une telle valeur est souvent calculée plutôt qu'écrite, et un calcul est libre de produire un infini ; sous la
garde uniforme, cela transformait une exclusion sans effet en `ArgumentException` au moment où le test
s'arrangeait.

Le message du refus nommait l'issue de secours ainsi — *« utilisez un vivier explicite : `Any.OneOf(...)` »* —
pour chaque argument sans distinction. Émis depuis `OneOf`, ce conseil est l'appel que le lecteur vient
d'écrire ; émis depuis une exclusion, il propose un moyen d'*obtenir* une valeur que l'appelant cherchait à
*exclure*. Les deux ont été signalés dans [#180](https://github.com/Reefact/just-dummies/issues/180), ouverte
par le mainteneur lors d'une revue des cas limites de toute la bibliothèque, cinq semaines après l'acceptation
de l'ADR-0054, la moitié concernant les exclusions étant explicitement laissée ouverte comme décision.

L'issue de secours de l'ADR-0054 reste inchangée et n'est pas remise en cause ici : un test dont le domaine
contient réellement une valeur non finie la tire du `Any.OneOf(...)` générique, qui ne porte aucune règle de
finitude par construction.

La bibliothèque est en dessous de 1.0, et un changement de comportement est permis
([`CONTRIBUTING.md`](../../for-users/CONTRIBUTING.fr.md), « Base de référence de l'API publique »).

## Décision

Un générateur flottant accepte une valeur non finie dans `Except` et `DifferentFrom` comme un sans-effet,
tandis qu'une borne ou une valeur de `OneOf` non finie reste refusée comme l'a décidé l'ADR-0054.

## Justification

**Exclure une valeur qui ne peut pas être tirée redit une garantie que le générateur donne déjà, et la règle de
la bibliothèque pour une garantie redite est le sans-effet.** Le principe qui fait de `Between(0, 100)` deux
fois un sans-effet, et de `Containing("ab")` deux fois un sans-effet, fait d'`Except(double.NaN)` un
sans-effet : l'appelant demande exactement ce qui est déjà vrai. Le refuser était le seul endroit où la
bibliothèque traitait une garantie redite comme une erreur.

**La distinction tient en une phrase, et elle est de principe plutôt qu'une exemption.** Un argument qui
rendrait la contrainte insensée — une borne qui ne peut pas être comparée, une valeur de vivier qui serait
tirée — est refusé ; un argument qui ne fait que redire une garantie déjà tenue est un sans-effet. Cette phrase
couvre chaque point d'entrée flottant sans les énumérer, et c'est la phrase dont un lecteur a besoin pour voir
les deux comportements comme une seule règle plutôt que comme une incohérence.

**L'argument moteur de l'ADR-0054 vaut pour les bornes et n'atteint pas les exclusions.** *« Une borne qui ne
peut pas être comparée ne peut pas participer à un modèle ordonné »* est vrai, et cette décision en conserve
chaque conséquence : une borne non finie reste refusée. Une exclusion n'entre jamais dans le modèle ordonné. Sa
seconde inquiétude — un `NaN` qui se déduplique à travers un comparateur — exige qu'un `NaN` atteigne un tirage
ou un vivier de distinction, ce qu'une exclusion sans effet ne produit jamais. L'alternative rejetée a regroupé
les exclusions avec les bornes par généralisation, non par un argument portant sur les exclusions ; cette
décision les sépare et laisse le verdict sur les bornes là où il était.

**Un refus mérite sa place en protégeant quelque chose.** Le *« refuser bruyamment à la frontière »* de
l'ADR-0046 est la bonne réponse là où honorer la demande exigerait un mécanisme que personne ne peut raisonner,
ou laisserait passer une valeur fausse. Ici rien n'est en jeu : les valeurs produites sont identiques dans les
deux cas. Ce que le refus faisait, c'était bloquer un arrangement légitime — `DifferentFrom(existant)` sur une
valeur qu'un calcul a rendue infinie — et n'apprendre au lecteur rien sur quoi agir. Une friction sans
protection n'est pas une sécurité.

**Exclure une valeur non finie, ce n'est pas la rechercher.** La doctrine de l'ADR-0054 — un appelant qui
cherche `NaN` cherche le cas sous test, qui appartient au site d'appel comme littéral — porte sur
l'*obtention* de la valeur. Un appelant qui l'exclut déclare son indifférence à son égard, l'intention
inverse, et la doctrine ne s'applique pas.

## Alternatives envisagées

### Continuer à refuser l'exclusion, et ne corriger que le message

L'autre moitié de la direction donnée par #180 : garder la garde, et faire dire à sa phrase qu'une valeur non
finie n'est jamais tirée, de sorte que l'exclusion n'a rien à retirer. Envisagée parce qu'elle conserve une
règle uniforme sur les arguments et n'exige aucune décision. Rejetée parce que la règle uniforme est la mauvaise
règle pour une exclusion — c'est le seul endroit où une garantie redite est une erreur — et parce que
`DifferentFrom` sur une valeur calculée continue d'en payer le prix. La correction du message est conservée de
toute façon, pour les bornes et pour `OneOf`.

### Abandonner entièrement la règle côté arguments

Accepter aussi une borne et une valeur de `OneOf` non finies — l'alternative que l'ADR-0054 a rejetée, dans son
intégralité. Rejetée pour les propres motifs de l'ADR-0054, que cette décision laisse intacts : une borne qui ne
peut pas être comparée n'a pas sa place dans un modèle ordonné, et une valeur de vivier non finie serait tirée.

### Superséder l'ADR-0054 en bloc

Rejetée parce que presque tout y demeure : la règle côté tirages, les verdicts sur les bornes et le vivier,
l'exemption générique et l'issue de secours. Une décision de succession qui reformulerait tout cela pour changer
une clause enterrerait le changement ; restreindre la clause en question, à côté de la décision qu'elle
restreint, garde les deux lisibles.

## Conséquences

### Positives

* `DifferentFrom(existant)` est sûr quoi que produise un calcul, ce que sa documentation promettait.
* Le partage entre arguments refusés et acceptés a une explication en une phrase qu'un lecteur peut emporter.
* Le message du refus ne propose plus un vivier à un appelant qui excluait, et ne conseille plus `OneOf` à un
  appelant qui vient de l'écrire.

### Négatives

* Deux comportements sur un même générateur à documenter au lieu d'un ; le readme du package porte la phrase.
* Un appelant qui s'appuyait sur le refus comme fil déclencheur pour un `NaN` produit en amont perd ce signal.
  La bibliothèque ne l'a jamais promis, et le véritable échec du test se manifeste là où le `NaN` est utilisé.

### Risques

* Le sans-effet repose sur un fait du moteur — une exclusion n'est jamais comparée de façon ordonnée, et aucun
  tirage n'est jamais non fini. Un futur changement du moteur qui comparerait les exclusions, ou tirerait une
  valeur non finie, devrait maintenir cette décision explicitement ; la suite de propriétés épingle le
  comportement, de sorte qu'un tel changement fait échouer un test plutôt qu'un utilisateur.
* « Déjà garanti » doit se lire étroitement : cela vaut là où la valeur exclue n'est prouvablement jamais tirée.
  Ce n'est pas une licence pour accepter n'importe quel argument invalide qui se trouve être inoffensif sur un
  générateur.

## Actions de suivi

* La recette *NaN et les infinis* du readme du package et la page sur les nombres, en anglais et en français,
  énoncent la règle restreinte dans le même changement que celui qui livre le comportement.
* Que le statut de l'ADR-0054 devienne *Superseded by ADR-0101* ou reste *Accepted* à côté de cette
  restriction relève du mainteneur au moment de l'acceptation ; cette décision ne le modifie pas.

## Références

* [ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.fr.md) — la règle que cette décision restreint, et
  l'alternative rejetée qu'elle rouvre pour les seules exclusions.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — quand un refus est la réponse
  honnête.
* [ADR-0097](0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.fr.md) — quelles valeurs finies
  sont tirées, la question voisine.
* [Issue #180](https://github.com/Reefact/just-dummies/issues/180) — les deux messages qui ratent leur cible, et
  la question ouverte que cette décision tranche.
* [Issue #31](https://github.com/Reefact/just-dummies/issues/31) — la recette du readme qui a nommé l'issue de
  secours la première.
