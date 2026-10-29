# ADR-0113 | Contrôler la cardinalité d'une collection distincte quand la spécification est complète

🌍 🇬🇧 [English](0113-check-a-distinct-collections-cardinality-when-the-specification-is-whole.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-10-29
**Accepted:** 2026-10-29
**Decision Makers:** Reefact

## Contexte

L'[ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md) a décidé qu'une collection
distincte rejette un nombre demandé supérieur à une cardinalité effective connue, et a décrit ce rejet comme
immédiat et survenant à la déclaration. Les sections de contexte des ADR-0009 et ADR-0011, la page de l'analyzer
[JD016](../../for-users/analyzers/JD016.fr.md) (« Les deux lèvent à la déclaration ») et la page des collections de la
documentation utilisateur (« refusé immédiatement ») le répètent.

La bibliothèque ne fait pas cela. Le contrôle s'exécute quand la collection est matérialisée, là où la spécification
est complète, et toujours avant qu'aucun élément ne soit tiré : `Any.SetOf(Any.Boolean()).WithCount(5)` se déclare
sans exception, et `Generate()` lève une `ConflictingAnyConstraintException` qui nomme le manque.

Ce déplacement était voulu. La spécification d'une collection se construit un appel à la fois, et la cardinalité
effective dépend de tous : un `Containing` ultérieur, un `Distinct()` ultérieur ou un nombre ultérieur changent ce qui
est satisfaisable. Contrôlées au milieu d'une chaîne, les mêmes déclarations étaient refusées dans un ordre et
acceptées dans l'autre. Par exemple, trois éléments distincts tirés d'un domaine de deux valeurs, avec une troisième
valeur fournie par `Containing`, est une demande satisfaisable. Contrôlée à la déclaration du nombre, elle était
refusée si `Containing` venait après le nombre et acceptée s'il venait avant : la même demande, deux verdicts.

Le tirage borné que l'ADR-0004 conserve pour les générateurs qui ne savent pas compter leur domaine, et son échec,
ne changent pas.

## Decision

Le conflit de cardinalité d'une collection distincte est levé quand la collection est générée, une fois sa
spécification complète et avant qu'aucun élément ne soit tiré, et non à la déclaration.

## Rationale

**Le résultat d'une chaîne ne devrait pas dépendre de l'ordre de ses appels.** Un contrôle de cardinalité exécuté sur un
préfixe de la spécification rendait un verdict différent pour les mêmes déclarations écrites dans un autre ordre.
Attendre la spécification complète supprime cela, et l'échec
garde la propriété qui comptait : il vient avant tout tirage, il est donc déterministe et nomme les deux côtés.

**Le coût est un échec plus tardif, non un échec différent.** Le test échoue toujours à l'étape d'arrangement qui
construit la valeur, avec le même type d'exception, de sorte que le lecteur n'est pas envoyé ailleurs.
Ce qui change, c'est si l'exception part de la ligne qui a terminé la chaîne ou de celle qui en tire une valeur.

**La règle de compilation n'est pas touchée.** [JD016](../../for-users/analyzers/JD016.fr.md) signale les cas
constants depuis le source ; le signal précoce reste donc disponible partout où les arguments sont des constantes.

## Alternatives Considered

### Rétablir le contrôle à la déclaration

Envisagé parce que l'ADR-0004 et la documentation le disent. Écarté parce qu'il ne peut juger que la partie de la
spécification déjà déclarée : il doit soit refuser une chaîne qu'un appel ultérieur aurait rendue satisfaisable, soit
en accepter une qu'il aurait dû refuser, selon l'ordre.

### Contrôler aux deux moments

Envisagé pour garder le signal précoce. Écarté parce que le contrôle précoce est précisément celui qui a tort dans
les cas ci-dessus : le garder garde la dépendance à l'ordre.

## Consequences

### Positive

* Les mêmes déclarations rendent le même verdict dans n'importe quel ordre.
* Les enregistrements et la documentation disent ce que fait la bibliothèque.

### Negative

* Un conflit de cardinalité apparaît un cran plus tard que ne le dit l'ADR-0004, sur l'appel qui génère plutôt que sur
  celui qui déclare.

### Risks

* Un lecteur de l'ADR-0004 seul, ou des sections de contexte des ADR-0009 et ADR-0011, trouve encore « à la
  déclaration ». Parade : cet enregistrement amende l'ADR-0004 et est cité depuis les pages qui le disaient.

## Follow-up Actions

* Corriger la documentation utilisateur (JD016 et la page des collections, EN et FR) pour dire que le conflit est levé
  à `Generate()`.

## References

* [ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md) — la décision amendée. Elle garde
  son statut ; seul le moment du rejet change.
* [ADR-0025](0025-make-the-per-pull-request-mutation-gate-advisory.fr.md) — un amendement exemplaire qui laisse
  l'enregistrement amendé en place.
* Issue [#320](https://github.com/Reefact/just-dummies/issues/320).
