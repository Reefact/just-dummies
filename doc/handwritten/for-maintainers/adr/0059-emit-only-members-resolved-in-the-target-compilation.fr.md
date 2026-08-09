# ADR-0059 | N'émettre que des membres résolus dans la compilation cible

🌍 🇬🇧 [English](0059-emit-only-members-resolved-in-the-target-compilation.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

> Les renvois de section (§N) pointent vers la [spécification de `dum`](../specifications/justdummies-tool.fr.md), le document dont cet enregistrement a été extrait.

## Contexte

La bibliothèque publie deux assets divergents. Le moderne porte cinq points d'entrée de generator
qui n'existent pas sur celui de bas niveau, parce que les types de framework sous-jacents n'y
existent pas (§14.1).

Les generators d'entiers non signés n'exposent ni contrainte `Positive` ni `Negative`, un type non
signé ne pouvant exprimer ni l'une ni l'autre (§14.3).

Le tool ne détient aucune référence sur la bibliothèque ([ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.fr.md)), donc il ne peut pas voir l'API de
celle-ci à sa propre compilation.

La compilation du développeur fait autorité sur ce qui est réellement disponible dans son projet :
son framework cible choisit l'asset, et sa version de package choisit la surface.

Un membre émis mais absent est une erreur de compilation dans le projet du développeur, imputée au
tool.

## Décision

Le moteur n'émet un membre JustDummies qu'après avoir résolu ce membre dans la compilation du
développeur.

## Justification

L'alternative est une table, à l'intérieur du tool, de ce qui existe par version de bibliothèque et
par framework cible. Elle demanderait un entretien à chaque publication de la bibliothèque, serait
fausse pour toute version antérieure au tool, et encoderait des faits que la compilation connaît
déjà exactement.

La résolution remplace quatre cas particuliers indépendants par une règle : le clivage d'assets, la
surface numérique non signée, le tool plus ancien ou plus récent que la bibliothèque, et la
découverte des generators du développeur. Aucun n'a à être nommé où que ce soit dans l'émetteur.

Le mode d'échec qu'elle produit est le bon. Un membre non résoluble transforme le paramètre en
paramètre non résolu ([ADR-0060](0060-seed-generators-from-constructor-guards.fr.md)) — un état que le tool traite et signale déjà — plutôt qu'en une émission
que le développeur rencontre comme une erreur de compilation qu'il n'a pas causée et ne peut pas
interpréter.

Elle rend aussi gratuite la garantie d'API publique au lieu d'en faire une contrainte à imposer :
tout ce qui est résoluble dans la compilation fait par construction partie de la surface publique
publiée, donc le tool ne peut pas émettre contre un membre interne ni hors de la baseline de
compatibilité.

## Alternatives considérées

##### Une table de membres codée en dur par version de bibliothèque

Considérée parce qu'elle est plus simple, ne demande aucune recherche de symbole, et rend la
connaissance de l'émetteur explicite et relisible.

Écartée parce qu'elle est inmaintenable au fil des versions et tout simplement fausse pour toute
version publiée après le tool.

##### Référencer la bibliothèque et émettre contre ses types de compilation

Considérée parce qu'elle laisserait le compilateur vérifier l'usage que l'émetteur fait de l'API,
supprimant le mode d'échec « faute de frappe silencieuse » que [ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.fr.md) accepte.

Écartée parce qu'elle contredit [ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.fr.md), et parce qu'elle répondrait de toute façon à la mauvaise
question : la version que le tool référence n'est pas celle du projet du développeur.

## Conséquences

**Positives.** Le tool est correct contre n'importe quelle version de bibliothèque et n'importe quel
framework cible, sans détenir la moindre connaissance par version.

**Négatives.** La dégradation est discrète par nature : un membre qui ne se résout pas n'apparaît
simplement pas dans l'émission, et sans un signalement délibéré le développeur ne peut pas
distinguer un paramètre que le tool n'a pas su inférer d'un paramètre dont le generator existe mais
n'est pas disponible ici.

**Risques.** Un défaut de résolution — chercher un mauvais nom de métadonnée — dégraderait tout en
TODO d'un coup, ce qui se lit comme un tool qui ne marche pas plutôt que comme un bug. Atténué par
le test de sélection d'asset (§12), qui asserte le cas présent et le cas absent.

## Actions de suivi

* Le §6 porte la valeur de provenance `unavailable` pour cette raison. Conserver un test qui
  l'asserte : sans lui, la dégradation que cette décision accepte redevient invisible et l'exigence
  se dégrade en commentaire.

## Références

* §5.2, §5.3, §6, §14.1, §14.3 de cette spécification.

---
