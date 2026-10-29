# ADR-0111 | Confier à JD017 la valeur d'enum non définie d'un ensemble de valeurs, et en retirer JD014

🌍 🇬🇧 [English](0111-own-an-undefined-enum-value-as-jd017.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-10-29
**Accepted:** 2026-10-29
**Decision Makers:** Reefact

## Contexte

`Any.Enum<T>().OneOf(...)` refuse, dès son appel, une valeur que l'énumération ne définit pas : une valeur qui
n'est ni un membre déclaré ni, sur une énumération `[Flags]`, une combinaison de membres déclarés. Le refus est
une exception d'argument, levée avant que l'appel ne lise quoi que ce soit de ce que la chaîne a déclaré.

Deux règles signalaient cette valeur. JD017 existe pour elle : son domaine est ce que le type déclare, et son
message enseigne qu'une valeur que le type ne définit pas est une erreur de catégorie, et non un rétrécissement qui
se trouve être vide. JD014 signale un argument constant qu'une garde rejette à coup sûr, une règle sur une table de
gardes ; l'appartenance à l'énumération a été ajoutée à cette table pour que JD034 et JD035, qui cessent de lire une
chaîne à un appel susceptible de rejeter son argument, s'y arrêtent aussi. Rien ne faisait se retirer l'une ou
l'autre règle : `OneOf((Currency)42)`, une valeur `[Flags]` qu'aucune combinaison ne produit et un zéro non déclaré
dans un ensemble mêlé produisaient chacun deux avertissements à la même ligne et à la même colonne (issue #321,
relevée lors de l'essai d'expérience développeur).

JD017 ne jugeait l'appartenance que sur les chaînes qu'il savait faire remonter à `Any.Enum<T>()`, et plus du tout
dès qu'`AllowingCombinations()` était déclaré. JD014 la jugeait sur l'appel `OneOf` lui-même, quel que soit son
receveur, et lisait les expressions de collection. L'exécution refuse la valeur dans chacune de ces formes, avec ou
sans `AllowingCombinations()`.

L'ADR-0078 et l'ADR-0107 ont chacun confié à un seul propriétaire un phénomène signalé par deux règles, au motif
que deux diagnostics sur une expression pour une seule faute sont un bruit qui apprend au lecteur à désactiver les
deux. Ce dépôt traite un changement de sémantique d'un identifiant de diagnostic comme une décision d'architecture.

## Décision

Un élément d'ensemble de valeurs qu'`Any.Enum<T>()` refuse parce que le type ne le définit pas est signalé par la
seule JD017, sur l'appel `OneOf` quelle que soit l'origine de son receveur, et JD014 ne le signale plus.

## Justification

**Une faute produit un diagnostic.** Le raisonnement de l'ADR-0078 et de l'ADR-0107 s'applique tel quel : l'auteur a
commis une faute, et un second avertissement à la même position n'ajoute rien qu'une raison de faire taire les deux.

**Le propriétaire est la règle dont le message enseigne la faute.** Le message de JD014 dit qu'une garde rejette un
argument, ce qui est vrai de chaque ligne de sa table et ne dit rien de ce qui rend cette valeur fausse. Celui de
JD017 dit que le générateur ne tire que des valeurs que le type définit. Le lecteur qui le rencontre apprend
l'erreur de catégorie et cesse d'écrire des valeurs non déclarées, ce qui est la raison d'être d'un diagnostic.

**Le propriétaire ne doit rien perdre de ce que JD014 attrapait.** Céder le cas ne réduit le bruit que si toute
forme que l'exécution refuse reste signalée. JD017 juge donc l'appartenance sur l'appel `OneOf` lui-même, avec la
même lecture de la garde que JD014, plutôt qu'au fil de sa marche de chaîne : un receveur tenu dans une variable,
une expression de collection et une chaîne portant `AllowingCombinations()` restent couverts, puisque l'exécution
refuse les trois.

**Le refus arrête toujours la chaîne pour les règles qui lisent au-delà des appels.** Restreindre le rapport de
JD014 ne change rien à ce que fait la garde. JD034 et JD035 doivent toujours cesser de lire à l'appel : la notion
partagée d'argument rejeté garde le cas, et seul le diagnostic de JD014 se retire.

## Alternatives considérées

### JD014 comme propriétaire

Envisagée parce que JD014 jugeait déjà l'appel quel que soit son receveur, si bien qu'aucune couverture n'avait à
migrer.

Rejetée parce que son message nomme le symptôme, pas l'erreur. Migrer la couverture vers JD017 se paie une fois ;
un message qui n'enseigne rien se paie à chaque signalement.

### Garder les deux, à des sévérités différentes

Envisagée parce qu'elle conserve les deux lectures.

Rejetée parce que le lecteur voit toujours deux diagnostics pour une faute à la même position, le bruit que
l'ADR-0078 a rejeté. La sévérité change le volume du doublon, pas le fait qu'il en soit un.

## Conséquences

### Positives

* Une valeur d'énumération non définie produit un avertissement, formulé comme l'erreur de catégorie qu'elle est.
* JD017 signale désormais les formes qui lui échappaient : un générateur tenu dans une variable, une expression de
  collection, une chaîne portant `AllowingCombinations()`.

### Négatives

* Une équipe qui a supprimé JD017 et gardé JD014 perd le signalement de cette forme.

### Risques

* Une nouvelle garde sur les ensembles de valeurs d'énumération pourrait entrer dans la table de JD014 sans le
  retrait correspondant. La suite de propriétés différentielles vérifie que seule la règle propriétaire signale une
  valeur d'énumération que l'exécution refuse, et que l'autre se tait.

## Actions de suivi

* Aucune.

## Références

* [ADR-0078](0078-own-a-bound-declared-twice-as-one-rule.fr.md) — un propriétaire pour une borne déclarée deux fois.
* [ADR-0107](0107-own-a-constraint-repeated-identically-as-one-rule.fr.md) — un propriétaire pour une contrainte répétée à l'identique.
* [JD014](../../for-users/analyzers/JD014.fr.md), [JD017](../../for-users/analyzers/JD017.fr.md).
* Issue [#321](https://github.com/Reefact/just-dummies/issues/321).
