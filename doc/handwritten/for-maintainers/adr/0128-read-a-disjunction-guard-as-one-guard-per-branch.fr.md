# ADR-0128 | Lire une garde disjonctive comme une garde par branche

🌍 🇬🇧 [English](0128-read-a-disjunction-guard-as-one-guard-per-branch.md) · 🇫🇷 Français (ce fichier)

**Status:** Proposed
**Proposed:** 2026-11-02
**Decision Makers:** Reefact

> Les références de section (§N) renvoient à la [spécification de `any`](../specifications/justdummies-tool.fr.md).

## Contexte

L'[ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) lit un ensemble clos d'idiomes de garde et garde
l'appariement conservateur : un paramètre, aucune composition booléenne, des opérandes constants. Le §5.3 énonce la
règle comme une condition qui « ne contient ni `&&` ni `||` », et une garde hors de l'ensemble marque son paramètre
`to verify` et bloque le build ([ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.fr.md)).
La même garde écrite en deux `if`, ou via les helpers `ThrowIf…`, est lue.

L'[ADR-0085](0085-change-the-guard-reader-only-against-a-field-report.fr.md) ne laisse le lecteur changer que sur un
rapport de terrain : un vrai constructeur, l'idiome qu'il emploie, ce que le moteur en a fait, une forme de corpus,
et des cas pour ce que le changement lit ou refuse. Deux rapports sont arrivés. Le premier essai d'expérience
développeur a rencontré la garde d'intervalle écrite en une seule condition dans dix de ses vingt domaines
([CLI-C4](../../../../tools/ux-trial/REPORT.fr.md#cli-c4)) : `if (month is < 1 or > 12)`,
`if (month < 1 || month > 12)`. Le second l'a rencontrée dans dix-huit de ses vingt contextes, à chaque profil, et
l'a mesurée comme le coût le plus élevé de l'essai ([CLI-1](../../../../tools/ux-trial/REPORT-2.fr.md#cli-1),
[CLI-2](../../../../tools/ux-trial/REPORT-2.fr.md#cli-2)) : neuf à vingt-trois paramètres par domaine ont demandé un
`Between` écrit à la main, et les formes dépassaient l'intervalle numérique — `double.IsNaN(k) || k < 1.0 || k > 5.0`,
`string.IsNullOrWhiteSpace(code) || code.Length > 12`, `items.Count == 0 || items.Any(i => i is null)`, et un
pattern relationnel seul, `value.Length is > 12`. Plusieurs vérificateurs ont noté qu'un `if (A || B)` qui lève est
exactement `if (A) throw; if (B) throw;`, que le moteur lit déjà.

L'[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) demande, avant de rendre le générateur
plus malin, si la réponse honnête n'est pas un refus, et tient un mécanisme dont personne ne peut raisonner depuis
le site d'appel pour une décision à prendre. Il ne borne jamais la justesse : une contrainte fausse présentée comme
inférée est un défaut, et la direction dangereuse d'une lecture est celle qui resserre le tirage au-delà de ce que
le constructeur exige.

## Décision

Une garde qui lève dont la condition est une disjonction — `||`, ou un pattern `or` — est lue comme une garde par
branche, chaque branche contre l'ensemble clos comme si elle était seule, et un pattern relationnel ou constant seul
comme la comparaison qu'il épelle ; une conjonction reste non lue.

## Justification

**Une disjonction se scinde par construction, non par heuristique.** `if (A || B) { throw … }` refuse de construire
l'objet dès que `A` tient et dès que `B` tient, ce qui est ce que dit `if (A) { throw … } if (B) { throw … }`, et la
seconde forme est ce que l'ensemble clos lit aujourd'hui. Lire la première comme la seconde ne dérive rien : c'est
une réécriture qui tient pour tout `A` et tout `B`, sans cas où les deux divergent. La contrainte qui en sort est
exactement celle que gagne la graphie en deux `if`, si bien que le biais de l'ADR-0060 — sous-signaler plutôt que se
tromper — reste intact.

**Le même argument interdit de scinder une conjonction.** `if (A && B) { throw … }` ne refuse que là où les deux
tiennent ; une valeur pour laquelle `A` tient et `B` non est acceptée. Lire `A` seul contraindrait le tirage à des
valeurs que le constructeur n'a jamais refusées — plus étroit que le domaine, sous un récapitulatif disant inféré, et
en silence, puisque chaque tirage construit toujours. C'est l'issue « contrainte fausse » que l'ADR-0046 ne borne
jamais, donc `&&` garde la marque, et de même une disjonction nichée là où elle n'est plus toute la condition,
`!(A || B)`, qui est une conjonction déguisée.

**Chaque branche répond aux questions auxquelles répondait une condition entière.** La règle du paramètre unique,
le placement au-dessus de toute écriture de ce paramètre, l'opérande constant, l'ensemble clos lui-même : chacune
est posée à une branche, si bien que `a < 0 || b < 0` fait deux gardes sur deux paramètres, qu'une branche inconnue
de l'ensemble marque les paramètres qu'elle mentionne tandis que les autres branches apportent toujours ce qu'elles
lisent, et qu'une branche qui n'ajoute rien — `double.IsNaN(k)`, que le générateur ne tire jamais — n'ajoute rien.
Rien ne change dans la couche de placement : la branche est là où sa condition était.

**Un pattern est la comparaison qu'il épelle.** `month is < 1 or > 12` est `month < 1 || month > 12` dans l'autre
grammaire que le langage offre pour le même test, et `value.Length is > 12` est `value.Length > 12`. Un pattern
relationnel n'est permis que sur les types numériques intégrés, où la comparaison est celle du langage, si bien
qu'aucune des questions d'opérateur défini par l'utilisateur que posent les lignes d'égalité ne se pose.

**Le rapport de terrain est celui que l'ADR-0085 demande, deux fois.** Vingt-huit domaines entre les deux essais, le
coût le plus élevé mesuré au second, un contournement que les utilisateurs ne pouvaient pas découvrir parce que la
page utilisateur ne listait jamais les formes que le lecteur connaît (#383), et une réécriture que les vérificateurs
ont eux-mêmes proposée. Le corpus porte une forme par graphie — `||`, le pattern `or`, trois branches, deux
paramètres, une branche hors de l'ensemble — et la suite du résolveur les cas.

## Alternatives envisagées

### Continuer de refuser, et documenter le contournement

Envisagé parce que la marque est une issue voulue sous l'ADR-0085, et que scinder la garde en deux `if` ou passer par
les helpers `ThrowIf…` la fait lire.

Rejeté parce que le coût est tombé sur dix-huit contextes sur vingt et que la marque demande au développeur d'écrire
à la main une contrainte que le moteur lit exactement depuis l'autre graphie. L'ADR-0085 garde la marque comme
réponse permanente à ce que la surface ne couvre pas ; une forme que la surface couvre par construction, sous deux
rapports de terrain, est ce que sa propre procédure d'entrée existe pour admettre.

### Lire aussi une conjonction, en prenant le côté le plus faible

Envisagé parce que des gardes `A && B` se rencontrent aussi sur le terrain — un enum nullable testé
`is { } q && !Enum.IsDefined(q)` au second essai.

Rejeté parce qu'aucune lecture d'une conjonction n'est exacte : prendre l'un ou l'autre opérande resserre le tirage
au-delà du domaine, et n'en prendre aucun est la marque. Le §5.3 nomme ce coût comme voulu, et les rapports de
terrain demandaient la disjonction.

### Un normaliseur booléen sur toute la condition

Envisagé parce qu'il lirait `!(A || B)` et les formes que De Morgan atteint.

Rejeté sous l'ADR-0046 : un normaliseur est un mécanisme dont personne ne raisonne depuis le site d'appel, pour des
formes que les rapports n'ont pas rencontrées. Une disjonction au sommet de la condition est tout ce qui a été
demandé.

## Conséquences

### Positives

* Une garde d'intervalle écrite en une condition, dans l'une ou l'autre grammaire, se lit comme l'intervalle
  qu'elle est, et le coût le plus élevé du second essai prend fin.
* La règle qu'un utilisateur peut garder en tête reste courte : un `||`, ce sont ses branches ; un `&&` n'est pas
  lu.

### Négatives

* Une garde qui était marquée `to verify` est désormais lue, si bien qu'un fichier re-scaffoldé avec `--force` perd
  une sentinelle que le développeur a pu lever à la main avec une autre base. La chaîne écrite est celle que la garde
  énonce.

### Risques

* Une branche lue là où la condition entière aurait dû être refusée. Atténuation : rien n'est lu d'une branche que
  l'ensemble clos n'aurait pas lue seule, et les questions de placement sont posées à chaque branche.

## Actions de suivi

* `items.Count == 0 || items.Any(i => i is null)` se lit entière dès que la ligne de l'élément nul existe (#403).
* La page utilisateur liste les formes que le lecteur connaît et celles qu'il ignore, celle-ci comprise (#383).

## Références

* Issue [#364](https://github.com/Reefact/just-dummies/issues/364) — la décision que porte cet enregistrement.
* [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) — l'ensemble clos, dont cet enregistrement amende
  « aucune composition booléenne » pour la seule disjonction.
* [ADR-0085](0085-change-the-guard-reader-only-against-a-field-report.fr.md) — la procédure d'entrée que ce changement
  satisfait.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — borner l'ambition, jamais la justesse.
* [Spécification de `any`](../specifications/justdummies-tool.fr.md), §5.3 — l'ensemble clos et ses règles de
  placement.
