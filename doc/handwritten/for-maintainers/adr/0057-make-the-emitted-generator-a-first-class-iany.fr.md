# ADR-0057 | Faire du generator émis un `IAny<T>` de plein droit

🌍 🇬🇧 [English](0057-make-the-emitted-generator-a-first-class-iany.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

> Les renvois de section (§N) pointent vers la [spécification de `dum`](../specifications/justdummies-tool.fr.md), le document dont cet enregistrement a été extrait.

## Contexte

`IAny<T>` est le seam de composition de la bibliothèque : `As`, `Combine`, les generators de
collection et ceux de choix le consomment et le produisent tous (§14.4).

L'interface est documentée comme une recette immuable, et tous les generators de la bibliothèque
l'honorent — chaque contrainte fluide retourne une nouvelle instance (§14.5).

La catégorie `Usage` des analyzers reconnaît un generator comme l'interface `IAny<T>` elle-même ou
tout type qui l'implémente, plutôt que comme une liste fixe de types intégrés (§14.6).

Le type émis expose une méthode fluide par paramètre de constructeur, ce qui lui donne la forme
d'un builder. Les builders de l'écosystème mutent conventionnellement et retournent `this`.

## Décision

Le type émis implémente `IAny<T>` et est immuable, chaque méthode `With` retournant une nouvelle
instance.

## Justification

Implémenter le seam est ce qui fait fonctionner les agrégats imbriqués sans code supplémentaire. Un
generator émis est directement utilisable comme generator d'élément, comme opérande de `Combine` ou
comme source de `As` ; sans l'interface, soit le tool émettrait des adaptateurs, soit le
développeur les écrirait.

Le second bénéfice est moins évident et vaut autant : les analyzers `Usage` s'appuient sur
l'interface, donc un type émis qui l'implémente est couvert par eux exactement comme un generator
intégré. Cette couverture compte plus ici qu'ailleurs, parce que le fichier émis est celui que le
développeur édite ([ADR-0058](0058-leave-the-scaffolded-file-open-to-the-analyzers.fr.md)), souvent en découvrant cette API.

L'immuabilité n'est pas une préférence de style mais le contrat documenté du seam. Un `With` mutant
ferait du type émis le seul generator mutable de l'écosystème, et se comporterait de façon
surprenante : deux generators dérivés d'une base partagée interféreraient. Le coût est une
allocation par appel à `With`, sur un chemin de code qui n'est pas chaud.

## Alternatives considérées

##### Un builder mutant retournant `this`

Considéré parce que c'est la forme conventionnelle du builder et qu'il alloue moins.

Écarté parce qu'il contredit le contrat documenté de l'interface qu'il implémenterait, et parce que
dériver deux generators d'une base partagée les corromprait silencieusement tous les deux.

##### Un type ordinaire exposant `Generate`, n'implémentant pas `IAny<T>`

Considéré parce qu'il garde le fichier émis exempt de toute interface de bibliothèque.

Écarté parce qu'il abandonne les deux bénéfices d'un coup : aucune composition avec les seams de la
bibliothèque, et aucune couverture d'analyzer sur le fichier qui en a le plus besoin.

## Conséquences

**Positives.** La composition avec tous les seams de la bibliothèque est gratuite. Quatre règles
d'analyzer s'étendent au type émis sans rien coûter.

**Négatives.** Une allocation par appel à `With`. Le constructeur privé complet grossit avec le
nombre de paramètres, donc le fichier émis est verbeux pour les constructeurs larges.

**Risques.** Si la bibliothèque relâchait un jour le contrat d'immuabilité, la forme émise serait
plus stricte que nécessaire — inoffensif, et aucune action ne serait requise.

## Actions de suivi

* Aucune.

## Références

* §4.2, §14.4, §14.5, §14.6 de cette spécification.

---
