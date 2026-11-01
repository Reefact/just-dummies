# ADR-0117 | Tirer une valeur limite que le type permet une fois sur dix

🌍 🇬🇧 [English](0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.md) · 🇫🇷 Français (ce fichier)

**Status:** Proposed
**Proposed:** 2026-11-01
**Decision Makers:** Reefact

Supersède l'[ADR-0097](0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.fr.md) et l'[ADR-0064](0064-never-draw-null-for-a-nullable-parameter.fr.md).

## Contexte

Une valeur flottante ou décimale est tirée dans une magnitude ordinaire d'un million
([ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.fr.md), supersédée par
l'[ADR-0097](0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.fr.md)). Les mesures qui fondent la
fenêtre tiennent toujours : un tirage uniforme en valeur sur tout un `double` tombe presque toujours à quelques
décades du maximum du type — 0 tirage sur 5 000 d'`Any.Double()` sous 1e6 — là où `x + 1 == x` et où une
multiplication déborde. L'ADR-0031 a envisagé de tirer une valeur extrême une fois sur dix et l'a rejeté : une
suite qui échoue une fois sur dix « pour une raison que le test n'a jamais nommée ».

La fenêtre rétrécit aussi ce que l'appelant a déclaré. L'ADR-0097 ne tire un intervalle bilatéral en entier que là
où le rétrécir laisserait moins de deux valeurs, alors que sa propre justification dit qu'« un intervalle
explicitement borné appartient à l'appelant ». Le second essai UX développeur en a mesuré la conséquence
([CORE-14](../../../../tools/ux-trial/REPORT-2.fr.md#core-14)) : `Any.Double().GreaterThan(0.0).LessThanOrEqualTo(100e6)`
n'a jamais tiré au-delà de 999 823 sur 2 000 tirages, si bien qu'une pression de 50 MPa — ordinaire, en pleine
précision — n'a jamais été tirée, et qu'une dépendance à elle n'a jamais été révélée.

`OrNull()` donne `null` environ une fois sur deux, sur ses deux surcharges. Le scaffolder ne l'applique jamais à un
paramètre nullable ([ADR-0064](0064-never-draw-null-for-a-nullable-parameter.fr.md)) : une signature nullable énonce
une permission, non une intention, et tirer `null` ferait échouer des tests écrits pour le chemin ordinaire. L'essai
en a mesuré le coût ([CLI-19](../../../../tools/ux-trial/REPORT-2.fr.md#cli-19)) : un constructeur documentant `null`
comme valeur valide — une vache jamais traite — n'a jamais été exercé dessus, et un test qui le voulait devait écrire
un cast pour l'atteindre.

Le premier fondamental de la bibliothèque, dans `CLAUDE.md` : un dummy est une valeur dont le test ne se soucie pas,
et le tirage existe parce que le *test* qui ne s'en soucie pas n'est pas le *code* qui ne s'en soucie pas — un tirage
peut révéler une mauvaise dépendance qu'un littéral choisi une fois pour toutes peut cacher. Chaque valeur tirée
respecte les contraintes déclarées, et chaque exécution se rejoue depuis la graine qu'elle rapporte. La relation
d'une graine à ses valeurs n'est pas un contrat versionné avant 1.0
(l'[ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.fr.md) s'applique à partir de `1.0.0-preview.1`).

Une règle de la bibliothèque nomme un remède au site d'appel, là où un défaut incommode rencontre le développeur :
JD030 le fait pour une chaîne sans longueur déclarée.

## Décision

Une valeur limite que le type permet survient une fois sur dix : un tirage flottant ou décimal non borné ou borné
d'un seul côté atteint tout le domaine de son type une fois sur dix et reste dans la magnitude ordinaire sinon — ou,
à côté d'une borne unilatérale si grande qu'aucune bande ordinaire n'y est représentable, couvre le domaine déclaré à
chaque fois, comme l'ADR-0097 le tirait —, un intervalle que l'appelant a borné des deux côtés est tiré en entier, `OrNull()` donne `null` une fois sur dix, et le
scaffolder tire un paramètre nullable par `OrNull()` — les deux taux fixes, et gelés avec la relation de graine à
partir de 1.0.

## Justification

**Un taux pour une idée.** Une magnitude que le type permet et un `null` que la signature déclare sont le même genre
de valeur : permises, rarement ordinaires, et exactement là où une dépendance cachée se montre. Une fois sur deux
faisait de `null` le cas ordinaire ; jamais le rendait inatteignable. Une fois sur dix garde la plupart des tirages
ordinaires et atteint pourtant la limite en une poignée d'exécutions. Un taux, énoncé en une phrase, est ce qu'un
lecteur peut prédire et ce qu'une graine rejoue.

**L'objection de l'ADR-0031 trouve sa réponse dans les fondamentaux, non dans un autre taux.** Un test qui échoue
une exécution sur dix parce qu'une magnitude non déclarée ou un `null` a atteint son code n'utilisait pas un dummy
sur cet axe : la valeur prend part au résultat. C'est à cela que sert le tirage, et l'échec se reproduit depuis sa
graine — une propriété de la bibliothèque, pas un test instable. Ce que la décision doit au développeur, c'est le
remède, dit là où il écrit l'appel : une règle Info sur un tirage flottant ou décimal sans intervalle déclaré, qui
nomme `Between(…)`, comme JD030 le fait pour les chaînes ; et pour `null`, une valeur fixée ou un affinage qui n'en
donne jamais.

**Un intervalle déclaré appartient à l'appelant.** Ses deux bornes ont été écrites ; un intervalle dont l'appelant a
choisi les magnitudes n'est pas le tirage sur tout le domaine contre lequel l'ADR-0031 protège. Le tirer en entier,
c'est la justification propre de l'ADR-0097 appliquée sans son exception, et c'est ce qui fait de la déclaration de
l'intervalle un remède complet.

**Ce que « fiable » veut dire.** Chaque valeur tirée respecte les contraintes déclarées, et chaque exécution se
rejoue depuis sa graine. Cela ne veut pas dire qu'un test n'échoue jamais : un test qui dépend d'une valeur qu'il
n'a pas déclarée est précisément ce que le tirage teste. Le README le dit en ces termes.

**Geler les taux.** Changer l'un des taux change ce qu'une graine tire, ce que l'ADR-0049 ne permet que sur une
version majeure. Avant 1.0 la relation n'est pas un contrat : cette décision arrive maintenant et gèle les deux
taux à partir de 1.0.

## Alternatives considérées

### Garder la fenêtre et ne jamais tirer de valeur extrême

Envisagé parce que c'est le comportement documenté et que cela ne fait échouer aucun test existant.

Rejeté parce qu'une valeur qui ne quitte jamais ±1e6 ne révèle jamais une dépendance à la magnitude, et que la
fenêtre tronquait aussi ce que les appelants avaient déclaré.

### Une répartition log-uniforme

Envisagée parce qu'elle visite chaque décade.

Rejetée parce qu'avec une borne à 0 elle place quelque 300 décades sous 1 contre une poignée au-dessus —
`0 < p <= 1e8` tirerait des pressions autour de 1e-150 — et que choisir une distribution élargit l'ambition du
générateur ([ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md)). Le tirage reste uniforme en
valeur dans son intervalle.

### Garder `OrNull` à une fois sur deux

Envisagé parce que c'est le taux documenté et la pièce la plus simple.

Rejeté parce que cela fait de `null` le cas ordinaire d'une valeur optionnelle plutôt que la limite qu'il est, et
qu'un test qui veut une autre part peut l'écrire.

## Conséquences

### Positives

* Une dépendance à une magnitude extrême ou à `null` finit par échouer, de façon reproductible depuis sa graine.
* Un intervalle déclaré des deux côtés est tiré en entier : déclarer l'intervalle est le remède complet, et la règle
  Info le nomme.
* Le `null` documenté d'un paramètre nullable est exercé par défaut.

### Négatives

* Les séquences tirées changent : un tirage flottant ou décimal non borné consomme un tirage de plus, et ses valeurs
  comme celles d'`OrNull` changent. Le golden master des graines est rebasé une fois, et les changelogs le disent.
* Un `Any.Double()` non borné écrit contre la fenêtre documentée atteint désormais une magnitude extrême une
  exécution sur dix, et le seul signal dans le code est une règle Info, invisible dans `dotnet build`. La note de
  version est le canal principal : ce qui change, pourquoi, et comment déclarer l'intervalle.

### Risques

* Une suite contenant beaucoup de tirages flottants non bornés, ou beaucoup d'`OrNull()`, verra des échecs qu'elle
  n'a jamais vus. Chacun nomme une dépendance que le test n'a pas déclarée ; la note de version dit comment le lire.

## Actions de suivi

* À l'acceptation, passer l'ADR-0097 et l'ADR-0064 à `Superseded by ADR-0117` et y ajouter le lien retour.
* La partie scaffolder — `OrNull()` par défaut, un `Without…()` généré, un affinage qui ne donne jamais `null` —
  arrive avec l'issue #411.

## Références

* [ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.fr.md), [ADR-0097](0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.fr.md) — la fenêtre de magnitude ordinaire.
* [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.fr.md) — ne jamais tirer null pour un paramètre nullable.
* [ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.fr.md) — rejouer une graine d'une version à l'autre.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — borner l'ambition, jamais la correction.
* Issues [#402](https://github.com/Reefact/just-dummies/issues/402), [#410](https://github.com/Reefact/just-dummies/issues/410), [#411](https://github.com/Reefact/just-dummies/issues/411), [#415](https://github.com/Reefact/just-dummies/issues/415).
