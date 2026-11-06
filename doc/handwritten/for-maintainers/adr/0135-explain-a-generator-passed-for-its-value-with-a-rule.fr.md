# ADR-0135 | Expliquer par une règle un générateur passé pour sa valeur

🌍 🇬🇧 [English](0135-explain-a-generator-passed-for-its-value-with-a-rule.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-11-06
**Accepted:** 2026-11-06
**Decision Makers:** Reefact

## Contexte

Un `With…` scaffoldé prend une valeur à fixer ou une lambda pour affiner la base valide du paramètre, et
aucun générateur : un générateur préparé remplacerait cette base avec chaque règle qu'il a gardée
([ADR-0109](0109-let-a-test-refine-a-parameters-valid-base-rather-than-replace-it.fr.md)). En passer un
ne compile donc pas, et le compilateur dit seulement `CS1503`, conversion impossible. Le troisième essai
UX développeur l'a rencontré dans quatre sessions (CLI-21) ; deux d'entre elles avaient lu le paragraphe
de la page qui l'explique.

La même erreur attend un générateur passé partout où sa valeur est attendue — un constructeur, une
méthode utilitaire —, là où l'on a simplement oublié d'en tirer.

Deux endroits peuvent porter l'explication : une règle livrée avec la bibliothèque, ou une surcharge
émise par le scaffolder pour échouer avec un message à elle.

## Décision

Un avertissement de la famille usage, JD039, signale un argument dont le type est un générateur de `T`
passé à un appel dont la résolution de surcharge échoue alors qu'un paramètre candidat prend un `T`, et
nomme la sortie : la surcharge lambda quand la méthode en a une, `Generate()` sinon.

## Justification

**Les types le disent déjà : aucun marqueur n'est nécessaire.** La résolution de surcharge garde les
candidats qu'elle a rejetés ; le type de l'argument dit que c'est un générateur de `T`, et un paramètre
candidat dit qu'il prend un `T`. Contrairement à une règle sur une chaîne scaffoldée entière, rien du
fichier scaffoldé n'a besoin d'être reconnu.

**Elle explique là où est l'erreur.** Le diagnostic se place sur l'argument, à côté de `CS1503`, dans
chaque IDE et chaque build, là où un paragraphe de page avait été lu puis manqué.

**Elle couvre la même erreur hors du scaffolder.** Un générateur passé à un constructeur ou à une méthode
qui prend sa valeur est le même faux pas, et reçoit la même explication.

**Elle n'ajoute aucune barrière.** L'appel ne compile déjà pas ; la règle dit seulement pourquoi, et ne
peut donc pas bloquer du code qui compilerait sans elle.

## Alternatives envisagées

### Une surcharge `[Obsolete(error: true)]` émise par `any`

Envisagée parce que le compilateur afficherait alors le message de la bibliothèque, depuis le seul train
du CLI. Rejetée : chaque fichier scaffoldé porterait une surcharge inutilisable par paramètre, proposée
par la complétion, dans un fichier qui appartient au développeur
([ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.fr.md)), et elle
n'aiderait que les générateurs scaffoldés.

### Garder le seul paragraphe documenté

Envisagée parce que l'erreur est voulue et documentée. Rejetée : deux des quatre sessions l'avaient lu
et ont quand même buté sur l'erreur, sans rien qui y renvoie.

## Conséquences

### Positives

* `CS1503` sur un argument générateur vient avec sa raison et son remède, scaffoldé ou non.

### Négatives

* Une règle de plus à tenir en phase avec ses pages, sa constante de catalogue et son décompte.

### Risques

* La règle lit les candidats d'un appel qui ne se lie pas ; un compilateur qui cesserait de les
  rapporter la ferait taire. Les cas nommés de `JustDummies.Analyzers.UnitTests` compilent l'appel en
  échec et le montreraient.

## Actions de suivi

* Aucune au-delà de l'implémentation et de sa documentation.

## Références

* [ADR-0109](0109-let-a-test-refine-a-parameters-valid-base-rather-than-replace-it.fr.md) — pourquoi un
  `With…` scaffoldé ne prend aucun générateur.
* [Essai UX 3, CLI-21](../../../../tools/ux-trial/REPORT-3.fr.md#cli-21).
* Issue [#486](https://github.com/Reefact/just-dummies/issues/486).
