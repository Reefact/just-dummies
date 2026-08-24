# ADR-0089 | Tirer un paramètre composé par le generator que son type possède

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0089-draw-a-composed-parameter-through-the-generator-its-type-owns.md)

**Status:** Accepted
**Proposed:** 2026-08-24
**Accepted:** 2026-08-24
**Decision Makers:** Reefact

> Les références de section (§N) pointent vers la [spécification `dum`](../specifications/justdummies-tool.fr.md).

## Contexte

Un paramètre dont la table de base n'a pas de ligne pour le type est un paramètre composé : un type
de domaine propre au développeur. Le moteur avait deux façons d'en tirer un. Si la compilation
contenait déjà un generator pour ce type, il émettait un appel à celui-ci. Sinon il dépliait la
fabrique statique à un paramètre du type, lisait les gardes du corps de cette fabrique (§5.3), et
dérivait ici une recette pour le paramètre.

La recette dérivée décrit l'invariant du value object. Ce même invariant est ce que lit le generator
scaffoldé pour ce value object, depuis les mêmes gardes, quand le développeur lance le tool sur le
type lui-même.

Un type de domaine est composé par beaucoup d'autres. Un agrégat porte une référence, une ligne
aussi, un événement aussi ; chacun est un paramètre de constructeur distinct dans un fichier généré
distinct.

Le tool écrit chaque fichier une fois et en transfère la propriété au développeur
([ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.fr.md)). Il ne les
régénère pas, et ne les revoit pas.

Un paramètre que le moteur ne sait pas tirer trouve déjà sa réponse dans l'émission d'un identifiant
qui n'existe pas, de sorte que le build du développeur le signale à cette ligne
([ADR-0060](0060-seed-generators-from-constructor-guards.fr.md)).

Le moteur nomme un generator par une seule fonction sur le nom du type (§11.3), et ce nom ne porte
aucun argument de type.

`dum generate` refuse une cible générique (§3.2).

Trois passes adversariales sur le lecteur de gardes ont trouvé la même forme de défaut à chaque
fois : une recette dérivée que le tool rapportait comme inférée, et un tirage réel que le
constructeur du domaine rejetait. La réponse permanente à une garde dont le lecteur ne peut pas
répondre est de la marquer et de bloquer le build
([ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.fr.md),
[ADR-0085](0085-change-the-guard-reader-only-against-a-field-report.fr.md)).

## Décision

Un paramètre composé est tiré par le generator que son propre type possède, nommé que la compilation
porte déjà ce generator ou non.

## Justification

La recette d'un value object a une seule bonne adresse : le generator du type qui déclare
l'invariant. La dériver à chaque site composant en faisait autant de copies qu'il y avait de sites,
et comme le tool remet chaque fichier et n'y revient jamais, ces copies ne pouvaient que diverger —
entre elles, et du constructeur qu'elles décrivaient, dès la première fois que ce constructeur
changeait. Une lecture par type remplace N lectures par type, et la lecture qui survit est celle que
le développeur ira regarder quand il voudra savoir comment se tire une `OrderReference`.

Que les copies aient aussi été *fausses* est ce qui a forcé la question, mais ce n'est pas
l'argument. Chaque passe fermait les défauts trouvés et la suivante en trouvait d'autres, dans les
correctifs eux-mêmes ; un mécanisme dont la correction demande autant de tours porte plus d'ambition
que la base n'en autorise
([ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md)). Le retirer coûte moins
cher que continuer à le rendre honnête, et cela ne coûte aucune couverture : les gardes qu'il lisait
sont toujours lues, une fois, là où elles appartiennent.

Nommer le generator quand il est absent est le geste que l'ADR-0060 fait déjà, et il est ici
strictement plus informatif que la sentinelle qu'il remplace. Tous deux produisent une erreur de
compilation à la ligne propre du paramètre, dans l'éditeur, la liste d'erreurs et l'intégration
continue. Mais un identifiant inventé dit seulement que quelque chose manque, tandis qu'un nom de
type dit sur quel type lancer le tool. Le développeur allait écrire ce generator de toute façon —
l'alternative était une copie de sa recette inlinée ailleurs — donc l'erreur n'est pas un obstacle
posé sur son chemin, elle est le chemin, énoncé un pas plus tôt.

Cela n'affaiblit pas
l'[ADR-0059](0059-emit-only-members-resolved-in-the-target-compilation.fr.md), et la frontière mérite
d'être énoncée parce que c'est la première question qu'un lecteur posera. Ce record gouverne les
membres de la *bibliothèque* : il existe parce qu'un membre absent de l'asset du développeur est une
erreur de compilation qu'il n'a pas causée et ne peut pas interpréter. Un generator pour son propre
type de domaine n'est ni l'un ni l'autre — il l'a causée en ne l'ayant pas scaffoldé, et le message
nomme le remède. La règle elle-même lie toujours là où elle s'applique : rien de la bibliothèque
n'est chaîné sur un generator que cette compilation ne voit pas, puisqu'il n'y a aucun type contre
lequel résoudre un membre.

Un type générique est laissé au §5.5 parce que la fonction de nommage ne sait pas le nommer.
`Repository<Order>` et `Repository<Line>` s'entendraient tous deux dire d'écrire `AnyRepository`, qui
n'est le nom d'aucun des deux, et `dum generate` refuserait la cible de toute façon. Une sentinelle
qui ne dit rien vaut mieux qu'un nom qui dit faux — le même biais vers la sous-lecture que
l'ADR-0060 a choisi pour les gardes.

## Alternatives considérées

### Continuer à dériver une recette, et marquer là où la lecture est incertaine

Considérée parce que c'est ce vers quoi trois passes de travail construisaient, et parce que cela
garde un paramètre composé résoluble sans que le développeur scaffolde un second type.

Rejetée parce que marquer répond à la mauvaise moitié du problème. Une marque dit que la lecture
peut être fausse ; elle ne dit pas que la recette est dupliquée, et la duplication est la faute qui
survit même à une lecture parfaitement correcte. Deux fichiers composant le même value object
porteraient toujours deux copies de son invariant, toutes deux correctes le jour où elles ont été
écrites, et rien dans le tool ne les réconcilierait jamais.

### N'émettre la recette dérivée que si aucun generator n'existe, en repli

Considérée parce qu'elle garde le cas courant identique et ne produit jamais un fichier qui ne
compile pas, ce qui est le défaut le plus doux.

Rejetée parce qu'elle fait dépendre la recette émise de ce que la compilation contient le jour où le
tool tourne. Le même paramètre se scaffolderait de deux façons dans deux projets, et scaffolder le
value object plus tard changerait silencieusement ce qu'une relance produit. Elle préserve aussi la
duplication exactement dans la situation où elle fait le plus mal — le développeur n'a pas encore
pensé à ce type, donc la copie est le seul énoncé de son invariant où que ce soit.

### N'émettre rien et laisser le paramètre ouvert, comme avant que ce chemin existe

Considérée parce qu'elle revendique le moins, ce qui sied à un tool dont tout l'argument est
l'honnêteté.

Rejetée parce que la sentinelle qu'elle émet est moins informative que le nom du type sans rien
gagner. Le développeur doit découvrir quel type scaffolder depuis la déclaration du paramètre ; le
tool le sait déjà et refuserait de le dire.

## Conséquences

### Positives

* L'invariant d'un value object est lu une fois et vit à une seule adresse : changer son
  constructeur change un fichier généré plutôt que tous ceux qui le composent.
* Un fichier émis énonce tout son graphe de dépendances dans l'initialiseur de son constructeur :
  chaque type composé dont il a besoin est un nom que le compilateur vérifiera.
* Le chemin de composition cesse de lire des gardes, ce qui retire la surface où trois passes
  n'arrêtaient pas de trouver des défauts.
* Un paramètre composé ne porte pas de méthode : un fichier émis est plus court d'une méthode par
  paramètre composé et se lit comme une liste d'appels.

### Négatives

* Scaffolder un agrégat avant ses value objects produit désormais un fichier qui ne compile pas, là
  où il en produisait un qui compilait. Le remède est nommé à la ligne fautive, mais c'est une étape
  que le développeur n'avait pas à faire avant.
* Une contrainte que le constructeur du type *composant* déclare sur un paramètre composé ne peut
  plus être appliquée — un generator de type de domaine ne porte pas de `WithMaxLength`. Elle est
  rapportée plutôt que jetée en silence, mais elle n'est pas honorée.
* Le mot de provenance `factory` et la liste de candidates par paramètre ne décrivent plus rien et
  sont retirés, ce qui est un changement cassant pour le récapitulatif et pour `--format json`.

### Risques

* Un développeur scaffoldant d'abord un agrégat profond rencontre plusieurs `CS0246` d'un coup et
  peut les lire comme un tool cassé plutôt que comme une liste de travail. Le récapitulatif nommant
  chaque generator est ce qui devrait rendre la liste lisible ; qu'il y parvienne est une question
  de terrain.
* La contrainte retirée ci-dessus est le seul endroit où cette décision échange de la couverture
  contre une adresse unique, et l'échange ne vaut que tant qu'un paramètre composé est rarement
  contraint par son type composant. Si le terrain dit le contraire, c'est un rapport contre ce
  record.

## Actions de suivi

* Surveiller le cas de l'agrégat profond à l'usage : si plusieurs generators manquants d'un coup se
  lisent comme une panne plutôt que comme une liste de travail, le remède est dans la présentation
  du récapitulatif, pas dans l'émission.

## Références

* [ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.fr.md) — le fichier est remis et jamais régénéré, ce qui explique qu'une copie ne peut que diverger.
* [ADR-0059](0059-emit-only-members-resolved-in-the-target-compilation.fr.md) — les membres de la bibliothèque, et la frontière que ce record ne franchit pas.
* [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) — le mécanisme d'erreur de compilation que cette décision épelle comme un nom de type.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — borner l'ambition, jamais la correction.
