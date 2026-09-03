# ADR-0091 | Tirer un `Half` parmi les valeurs qu'il sait représenter

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0091-draw-a-half-from-the-values-it-can-represent.md)

**Statut :** Accepted
**Proposé :** 2026-08-31
**Accepté :** 2026-08-31
**Décideurs :** Reefact

## Contexte

L'[ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.fr.md) a nommé le défaut que ce
record termine. Un tirage flottant uniforme sur un intervalle est *uniforme par valeur, pas par
magnitude* : il y a autant de place entre les deux dernières décades de l'intervalle que dans tout ce
qui est en dessous, donc l'essentiel de la masse de probabilité se tient près du maximum du type, et
**« les magnitudes où tourne le code ordinaire, et où vivent les défauts d'arrondi, de comparaison et
de formatage, ne sont jamais visitées »**.

Son remède était une fenêtre : une valeur flottante arbitraire se tire dans une magnitude ordinaire
d'un million. Pour `Double` et `Single`, cette fenêtre rogne un intervalle immense jusqu'à un
intervalle qu'un test peut raisonner. Pour `Half`, elle ne rogne **rien** — le type s'arrête à 65 504,
entièrement à l'intérieur de la fenêtre — et le record le dit explicitement, concluant que **« `Half`
n'a besoin d'aucun cas particulier : une règle qui restreint l'extravagant et se tait ailleurs est une
règle, pas une liste d'exceptions »**.

Cette conclusion n'a jamais été mesurée. `Half` est dans la fenêtre et porte pourtant exactement le
défaut que la fenêtre devait supprimer, parce que le défaut ne tient pas à la *taille* de l'intervalle
mais à l'espacement géométrique des valeurs qu'il contient. Seize bits placent 63 487 valeurs finies
distinctes sur une échelle dont les barreaux doublent de largeur à chaque bloc d'exposant : un tirage
uniforme sur les réels tombe donc presque toujours sur les plus larges.

Mesuré sur la ligne non contrainte, 200 000 tirages :

| | uniforme sur l'intervalle | uniforme sur les valeurs représentables |
|---|---|---|
| valeurs distinctes atteintes | **14 143** sur 63 487 | **60 728** sur 63 487 |
| \|x\| = 0 | 0,00 % | 0,00 % |
| 0 < \|x\| < 1e-4 | **0,00 %** | 5,21 % |
| 1e-4 ≤ \|x\| < 1 | **0,00 %** | 43,23 % |
| 1 ≤ \|x\| < 100 | 0,15 % | 21,26 % |
| 100 ≤ \|x\| < 1000 | 1,32 % | 10,95 % |
| \|x\| ≥ 1000 | **98,53 %** | 19,34 % |

`Any.Half()` ne tire pas de valeur inférieure à 1. Pas rarement — pas une seule fois sur deux cent
mille tirages. Un générateur incapable de produire `0.5` ne certifie rien des chemins de code où un
`Half` est une fraction, c'est-à-dire de la plus grande partie du code qui a une raison d'en utiliser un.

Le même espacement est apparu par l'autre bout, dans l'outil. L'[ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.fr.md)
interdit au moteur de scaffolding d'interroger la bibliothèque, il miroite donc les cardinalités
d'élément dont il a besoin ; `Half` énonce désormais 63 487 et le moteur le miroite. Mais une borne que
la ligne ne peut pas atteindre est une borne qui ne veut rien dire : le scaffolder déclarerait un
plancher distinct de 30 000 sur un `ISet<Half>`, la bibliothèque l'accepterait, et le tirage
s'épuiserait après un budget de redraw dimensionné sur la demande.

## Décision

Un `Half` arbitraire se tire **uniformément sur les valeurs que l'intervalle déclaré sait
représenter**, sur l'échelle de ses propres motifs binaires, plutôt qu'uniformément sur cet intervalle
vu comme un domaine de réels.

`Double` et `Single` sont intouchés : leur tirage reste uniforme sur l'intervalle, rogné par la fenêtre
de magnitude ordinaire d'ADR-0031. L'échelle est fournie par la ligne qui la possède, pas adoptée par
le moteur partagé.

Ce record corrige aussi ADR-0031 : `Half` **est** le cas particulier, et la phrase affirmant qu'il n'en
faut aucun est remplacée par la mesure ci-dessus.

## Justification

**Il termine ADR-0031 plutôt qu'il ne le contredit.** Ce record voulait rendre les magnitudes
ordinaires atteignables et a corrigé les deux types dont l'intervalle était extravagant. `Half` en a
été exclu au motif que tout son domaine est déjà ordinaire — vrai du domaine, faux du tirage. La mesure
est la preuve qu'ADR-0031 s'imposait à lui-même : *« les générateurs entiers sont exclus sur preuve,
pas par commodité »*.

**Il ne rouvre pas ce qu'ADR-0032 a tranché.** L'[ADR-0032](0032-unify-discrete-generation-in-one-ordinal-space.fr.md)
refuse de faire passer le flottant par le moteur ordinal discret, parce que *« l'uniformité sur les
ordinaux devient l'uniformité sur les représentations, ce qui pour le flottant concentre le tirage près
de zéro »*. Cet argument porte sur les types larges, et il a raison à leur sujet : `Double` couvre
quelque six cents décades, et l'uniformité sur les représentations y noierait chaque tirage dans les
dénormaux. `Half` en couvre douze. La concentration qu'ADR-0032 refuse d'accepter est, à cette largeur,
l'étalement que mesure la table ci-dessus — 43 % des tirages entre 1e-4 et 1, 21 % entre 1 et 100.
C'est une décision sur une seule ligne de seize bits, et le moteur ordinal reste où ADR-0032 l'a mis.

**Une borne que la ligne ne peut pas atteindre est pire que pas de borne.** La cardinalité qu'énonce la
bibliothèque alimente un budget de redraw, une preuve d'impossibilité d'analyzer et le miroir de
l'outil. Les trois ne sont honnêtes que si compter et tirer s'accordent, et ils partagent désormais une
seule échelle — la même fonction répond *combien* et *lequel*.

**Ce que vaut un dummy, c'est ce qu'il expose.** L'[ADR-0075](0075-draw-characters-from-the-whole-of-ascii.fr.md)
a fait cet argument pour les caractères et il se transpose sans retouche : un défaut qui ne tire que des
magnitudes grandes et bien élevées retire précisément la preuve que le tirage existe pour produire. Un
dénormal, une valeur sous un, une valeur dont le rendu décimal n'est pas exact — c'est là que vivent les
défauts propres à un `Half`.

## Conséquences

### Positives

* Un tirage `Half` à graine atteint 96 % de son domaine sur 200 000 tirages là où il en atteignait 22 %.
* Les valeurs sous 1 deviennent ordinaires au lieu d'être impossibles.
* Les planchers que le scaffolder déclare sur un `ISet<Half>` sont des planchers que la ligne livre : le
  test d'accord épingle désormais les **deux** bords pour `Half` — le plus grand plancher déclaré est
  tiré, le suivant est refusé — ce qu'il ne pouvait pas faire avant.

### Négatives

* **La correspondance de graine de `Half` bouge.** Tout test à graine tirant un `Half` rejoue une autre
  valeur. Sous l'[ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.fr.md) c'est un
  changement de version majeure une fois 1.0.0 sortie ; il est pris ici tant qu'on est en amont de cette
  ligne.
* **Le golden master ne le signale pas**, et son silence n'est pas un accord. `SeedGoldenMaster.expected.txt`
  ne couvre que la surface commune aux deux frameworks cibles, et `Half` n'existe pas sur le plancher
  net472 — ce changement déplace donc une correspondance que rien n'épingle. C'est le trou qu'ADR-0049
  nomme dans ses propres conséquences, rencontré pour de vrai.
* **L'atteignabilité des bords d'un intervalle large devient probabiliste.** `CrossEngineReachabilityTests`
  demandait qu'un tirage arrive à moins de 1 % de chaque borne de `Between(-1000, 1000)` ; sur l'échelle
  cette bande vaut 41 barreaux sur quelque 51 000, et elle tient sur 182 graines sur 200. Le cas `Half`
  demande désormais la décade extérieure, que l'échelle atteint sur toutes les graines. La propriété
  défendue — un générateur qui ne quitte jamais le petit bout de ce qui a été déclaré — est la même ; la
  bande était calibrée pour un tirage uniforme sur les réels.

### Neutres

* `Double` et `Single` portent le même biais de magnitude à l'intérieur de leur fenêtre. Aucune de leurs
  fenêtres n'est un no-op, et aucune de leurs cardinalités n'est sous un plafond appliqué par les
  collections, donc rien ne déclare pour eux un plancher qu'ils ne livreraient pas. Les changer
  déplacerait deux correspondances de graine de plus pour un bénéfice dont personne n'a mesuré le besoin.

## Alternatives considérées

**Ne pas toucher au tirage et miroiter le compte atteignable (~21 000) au lieu du représentable.**
Rejeté : ce nombre est un artefact de l'échantillonneur, pas une propriété du type — il bouge avec le
nombre de tirages et la graine — et il mettrait le compte et le domaine à deux endroits différents, ce
que la discipline de miroir de l'[ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.fr.md)
existe précisément pour éviter. Il ferait aussi refuser à l'analyzer des planchers réellement légaux.

**Ne rien changer du tout.** Défendable du seul point de vue de l'outil : un plancher entre 21 000 et
63 487 est satisfaisable en principe, et le générateur le refuse bruyamment et de façon bornée, ce que
l'[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) sanctionne exactement.
Rejeté parce que l'outil n'a jamais été l'argument principal — une ligne `Half` incapable de tirer une
valeur sous 1 est un défaut de la bibliothèque, quoi qu'en fasse le scaffolder.

**Faire passer `Half` par le moteur ordinal discret d'ADR-0032.** Rejeté : le contrat de ce moteur est
que des ordinaux consécutifs désignent des *valeurs* consécutives, ce qui est vrai d'un entier et faux
d'un flottant. L'échelle est ici locale à la ligne qui connaît sa propre disposition binaire, et le
moteur continu partagé garde un tirage uniforme sur les réels pour tous les autres.
