# ADR-0122 | Accepter une exclusion vide comme l'identité

🌍 🇬🇧 [English](0122-accept-an-empty-exclusion-as-the-identity.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

## Contexte

L'`Except` de chaque builder refusait une liste vide par une `ArgumentException` (« At least one value is
required »), et JD014 signalait une liste vide constante comme un appel qui lève. Aucun enregistrement ne justifiait
ce refus, et l'ADR-0033 critiquait déjà son message.

Le second essai d'expérience développeur a rencontré le cas dans quatre contextes : une fixture dont l'ensemble des
valeurs déjà utilisées commence vide demande une condition à chaque site d'appel, parce qu'`Except(taken)` lève tant
que l'ensemble ne contient pas une valeur.

La bibliothèque accepte déjà deux exclusions qui ne retirent rien : une valeur non finie sur un builder flottant
(ADR-0101), et une valeur hors des membres d'une énumération, que JD017 signale. Une exclusion est une contrainte dont
le domaine est celui qui existait, moins ce qu'elle nomme (ADR-0054). JD024 signale une contrainte qui ne restreint
rien, en sévérité information.

Un `OneOf` vide énonce autre chose : un ensemble de valeurs sans aucune valeur déclare un domaine vide.

## Décision

Un `Except` qui reçoit une liste vide est une opération neutre sur tout builder, un `OneOf` vide reste refusé, et
JD024 — et non JD014 — signale une exclusion dont la liste est écrite vide.

## Justification

**Une exclusion vide est l'identité, pas une contradiction.** Elle ne retire rien, donc le domaine après elle est le
domaine d'avant. La refuser traitait un élément neutre comme une erreur, et l'essai en montre le coût : une garde à
chaque site d'appel pour un ensemble qui n'est vide qu'à sa première utilisation.

**Cohérent avec les exclusions qui ne retirent déjà rien.** Une valeur non finie sur un builder flottant et une valeur
hors d'une énumération sont acceptées pour la même raison ; la liste vide est le cas limite des deux.

**`OneOf` reste refusé parce que sa forme vide dit autre chose.** Un ensemble de valeurs liste le domaine ; ne rien
lister déclare qu'aucune valeur n'existe, ce qui est la contradiction que la bibliothèque refuse bruyamment.

**JD024, parce que l'appel ne lève plus.** JD014 veut dire « cet argument fait lever l'appel », ce qui cesse d'être
vrai. JD024 veut dire « cette contrainte ne restreint rien », ce qu'est exactement une exclusion vide, et elle signale
en sévérité information pour la raison que donne sa page : une contrainte qui ne change rien est un fait à peser. Seule
une liste écrite vide est signalée ; une liste calculée à l'exécution qui se trouve vide est le cas de fixture que
l'opération neutre sert.

## Alternatives envisagées

### Continuer à refuser, et documenter une garde

Envisagé parce que cela ne change aucun comportement. Rejeté : la garde est la même ligne à chaque site d'appel, pour
un ensemble dont le vide est l'état premier ordinaire d'une fixture.

### Signaler l'exclusion vide sous un nouvel identifiant en sévérité avertissement

Envisagé d'abord par l'issue. Rejeté après revue : une exclusion vide est une contrainte dont le domaine est celui qui
existait déjà, ce qui est la définition de JD024, et elle ne porte pas plus de risque que les autres contraintes
inertes que JD024 signale en sévérité information.

## Conséquences

### Positives

* L'ensemble des valeurs utilisées d'une fixture passe directement à `Except`, vide ou non.
* Les surcharges d'`Except` perdent une exception documentée.

### Négatives

* Un appel qui levait réussit désormais en silence ; JD024 le signale quand il est écrit en constante.

### Risques

* Aucun au-delà de ceux de JD024 : un diagnostic d'information n'apparaît pas par défaut dans la sortie de
  `dotnet build`.

## Actions de suivi

* `Except(IEnumerable<T>)` (issue #393) prend la même sémantique, liste vide comprise.

## Références

* [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.fr.md) — contraintes constructives et
  rejectives.
* [ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.fr.md) — un builder typé ne tire que des valeurs valides.
* [ADR-0101](0101-accept-a-non-finite-exclusion-as-a-no-op.fr.md) — une exclusion non finie comme opération neutre.
* Issue #392 ; CORE-1 du second essai d'expérience développeur.
