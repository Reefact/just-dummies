# ADR-0116 | Accumuler les affinages d'un paramètre sur sa recette courante

🌍 🇬🇧 [English](0116-accumulate-a-parameters-refinements-on-its-current-recipe.md) · 🇫🇷 Français (ce fichier)

**Status:** Proposed
**Proposed:** 2026-11-01
**Decision Makers:** Reefact

Supersède l'[ADR-0109](0109-let-a-test-refine-a-parameters-valid-base-rather-than-replace-it.fr.md).

> Les renvois de section (§N) pointent vers la [spécification de `any`](../specifications/justdummies-tool.fr.md).

## Contexte

L'[ADR-0109](0109-let-a-test-refine-a-parameters-valid-base-rather-than-replace-it.fr.md) a donné à chaque
paramètre d'un générateur scaffoldé deux surcharges `With…` : l'une fixe une valeur, l'autre remet à la lambda
du test la base valide du paramètre — la fabrique `AnyValid…()` — et tire de ce que la lambda retourne. Il a
envisagé de faire partir la lambda de la recette **courante**, après un appel `With…` antérieur, et l'a rejeté
pour deux raisons : le champ devrait conserver le type concret du générateur, et une fois une valeur fixée il ne
reste plus de générateur d'où partir. Pour toutes les autres méthodes `With…` le dernier appel l'emportait, et
il en allait de même pour la surcharge d'affinage.

Le second essai UX développeur a mesuré ce que ce rejet coûtait
([CLI-9](../../../../tools/ux-trial/REPORT-2.fr.md#cli-9), issue
[#406](https://github.com/Reefact/just-dummies/issues/406)), dans huit de ses vingt contextes. Une politique de
parenté plafonne la parenté d'un couple reproducteur à 0,25, et le développeur écrit cette règle là où
l'ADR-0109 le lui disait, dans la base de la politique pour le coefficient composé :
`k => k.WithValue(v => v.LessThanOrEqualTo(0.25))`. Un test qui demande un plafond d'au moins 0,125 affine une
fois de plus le même coefficient. La surcharge d'affinage du coefficient repart de sa propre base, perd le 0,25,
et tire dans `[0.125, 1]` : le constructeur de la politique a rejeté 710 tirages sur 2 000 dans un contexte, et
dans un autre une borne contradictoire sous une base n'a jamais été signalée. Sur une feuille, la même forme
perd en silence un premier affinage quand on en écrit un second.

Cela contredit, à un niveau de distance, la justification même de l'ADR-0109 — affiner existe pour que le test
n'exprime que ce qui lui importe, et pour que la règle garde une seule adresse — et le D13 de la spécification
comme la page utilisateur disaient le contraire de ce que faisait le code émis (« une lambda qui s'appuie sur
son argument n'élargit jamais la base »).

Les générateurs de la bibliothèque accumulent les contraintes, et refusent deux contraintes qui se contredisent
là où la seconde est déclarée, en nommant les deux. L'[ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.fr.md)
rend le générateur émis immuable : chaque `With…` retourne une nouvelle instance.

Un enregistrement accepté ne se modifie pas sur le fond
([ADR-0115](0115-allow-an-editorial-correction-to-an-accepted-adr.fr.md)).

## Décision

Chaque paramètre résolu d'un générateur scaffoldé conserve sa recette courante à côté de ce qu'il tire, de sorte
qu'un affinage parte de la recette que les affinages précédents ont laissée et qu'une fixation remplace le tirage
sans écarter cette recette.

## Justification

La décision de l'ADR-0109 tient : un test fixe une valeur ou affine la base, et ne la remplace jamais. Ce qui
change, c'est le point de départ de l'affinage. Partir de la recette courante, c'est ce qui fait de la base une
adresse unique dans une chaîne d'appels, et pas seulement au premier : une règle que la base extérieure a écrite
sur une valeur composée est toujours là quand un test affine cette valeur, parce que la lambda du test reçoit le
générateur que la base extérieure a construit.

Les deux raisons du rejet initial trouvent leur réponse dans la forme, sans être écartées par l'argument. La
recette est conservée à côté du tirage, typée avec le générateur que retourne sa fabrique, donc une lambda
l'affine par les contraintes propres à ce générateur. Une fixation met une valeur fixe là où le tirage se fait et
laisse la recette telle quelle, donc il y a toujours un générateur d'où partir — celui que le dernier affinage a
laissé — et un affinage écrit après une fixation prend la place de la fixation.

Accumuler est aussi la règle de la bibliothèque, ce qui la rend prévisible : deux affinages qui se contredisent
sont refusés là où le second est écrit, en nommant les deux, à chaque exécution — la ligne de correction de
l'[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) appliquée au site d'appel, là où la
forme « le dernier appel l'emporte » perdait le premier affinage sans un mot.

## Alternatives considérées

### Garder « le dernier appel l'emporte » et le documenter

Envisagé parce que c'est la règle que suit chaque fixation, et qu'elle ne demande aucun état au-delà du tirage.

Rejeté parce que l'essai a montré qu'elle échoue en silence, et par intermittence, sur le cas pour lequel
l'ADR-0109 a été écrit : une règle que le développeur a écrite dans une base extérieure, perdue par un affinage
écrit un niveau plus bas.

### Amender l'ADR-0109 sur place

Envisagé parce que la décision elle-même ne change pas.

Rejeté parce que le changement renverse une alternative que l'enregistrement accepté rejetait explicitement, ce
qui est un changement de fond et non une correction éditoriale
([ADR-0115](0115-allow-an-editorial-correction-to-an-accepted-adr.fr.md)).

## Conséquences

### Positives

* Une règle qu'une base extérieure écrit sur une valeur composée survit à l'affinage de cette valeur par un test.
* Les affinages successifs s'additionnent, et ceux qui se contredisent sont refusés là où ils sont écrits, en
  nommant les deux.
* Un affinage après une fixation a un sens : il part de la dernière recette et remplace la fixation.

### Négatives

* Chaque paramètre résolu porte un second champ, donc le fichier émis est plus long. Pour éviter que chaque
  `With…` repasse tous les champs, chacun copie le générateur et écrit un seul paramètre de la copie, si bien que
  les champs ne sont plus `readonly` : l'instance qu'un appelant tient ne change toujours jamais, ce qui est
  l'objet de l'ADR-0057, mais c'est désormais la forme du fichier qui le garantit et non le compilateur. Le
  mainteneur a accepté ce compromis à condition que la spécification et la page utilisateur le disent, et qu'un
  `With…` écrit à la main dans la part du développeur n'écrive jamais un champ de `this`.
* Les fichiers scaffoldés avant cette décision gardent le comportement « le dernier appel l'emporte » ; un
  re-scaffold avec `--force` donne le nouveau, et le changelog `cli` dit comment corriger un fichier à la main.

### Risques

* Un `With…` écrit par le développeur qui modifierait un champ de `this` romprait l'immutabilité sans erreur de
  compilation ; la page utilisateur enseigne la forme « copier puis écrire » là où elle enseigne cette part.

## Actions de suivi

* À l'acceptation, passer l'ADR-0109 à `Superseded by ADR-0116` et y ajouter le lien retour.

## Références

* [ADR-0109](0109-let-a-test-refine-a-parameters-valid-base-rather-than-replace-it.fr.md) — l'enregistrement que celui-ci supersède.
* [ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.fr.md) — le générateur émis immuable.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — borner l'ambition, jamais la correction.
* [ADR-0115](0115-allow-an-editorial-correction-to-an-accepted-adr.fr.md) — ce qui peut changer, ou non, dans un enregistrement accepté.
* Issue [#406](https://github.com/Reefact/just-dummies/issues/406) ; [essai UX 2, CLI-9](../../../../tools/ux-trial/REPORT-2.fr.md#cli-9).
