# ADR-0125 | Laisser une fenêtre horaire franchir minuit

🌍 🇬🇧 [English](0125-let-a-time-of-day-window-cross-midnight.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

## Contexte

`Any.TimeOnly().Between(start, end)` refusait un début après la fin par une `ArgumentException`, comme tout type
ordonné : sur une droite, cet ordre est une transposition. Huit domaines sur vingt du premier essai d'expérience
développeur — équipes de nuit, couvre-feux, sessions de marché, fenêtres nocturnes — ont écrit
`Between(new TimeOnly(22, 0), new TimeOnly(6, 0))` et ont rencontré ce refus. Une heure du jour n'est pourtant pas
une droite. Elle boucle à minuit, et `TimeOnly.IsBetween(22:00, 06:00)` lit déjà cette paire comme la nuit. Sur un
domaine cyclique, un début après la fin est la seule façon de nommer une fenêtre qui franchit minuit ; le refus
changeait un sens documenté en erreur.

L'issue #357 a décidé le comportement, que porte cet enregistrement parce qu'un refus documenté devient un
comportement.

## Décision

Sur `TimeOnly`, `Between(start, end)` avec un début après la fin est la fenêtre de `start`, en avançant à travers
minuit, jusqu'à `end`, les deux inclus.

* Elle est tirée uniformément sur la fenêtre : sur ses deux segments en proportion de leur longueur.
* `Between(t, t)` reste la seule valeur `t`, comme sur tout autre type. La journée entière est `Any.TimeOnly()`.
* Dès qu'une fenêtre franchit minuit, chaque borne se lit le long de sa direction : `After` et `AfterOrEqualTo`
  déplacent son début, `Before` et `BeforeOrEqualTo` sa fin, un autre `Between` resserre les deux. L'ordre dans
  lequel les appels sont écrits ne compte pas : `After(23:00).Between(22:00, 06:00)` est
  `Between(22:00, 06:00).After(23:00)`.
* Une heure hors de la fenêtre — `After(12:00)` sur la nuit — n'admet aucune valeur. Elle est refusée par une
  `ConflictingAnyConstraintException` qui nomme les deux déclarations, dans les deux ordres.
* `Except`, `OneOf` et `WithGranularity` s'appliquent aux deux segments. Une granularité qui ne divise pas la
  journée n'a pas de grille unique à travers minuit — sa grille y recommence — et elle est refusée sur une telle
  fenêtre, en nommant les deux.
* Un début après la fin reste une erreur d'argument sur tout autre type temporel, et une fenêtre qui ne franchit
  pas minuit se comporte exactement comme avant : mêmes tirages, mêmes messages.

## Justification

**La lecture du type lui-même.** `TimeOnly.IsBetween` définit la paire ainsi, et l'essai montre que c'est ce
qu'on écrit. La refuser mettait la bibliothèque en désaccord avec le type qu'elle génère.

**Un intervalle dans un repère qui commence la journée au début de la fenêtre.** Mesurée depuis son début, la
fenêtre est un seul intervalle, donc le moteur qui tire tout autre type ordonné la tire uniformément par
construction, et chaque mécanisme existant — exclusions, ensembles de valeurs, grille, indice de cardinalité,
messages de conflit — s'applique sans changement. Les déclarations faites avant la fenêtre sont appliquées à
nouveau dans ce repère, ce qui rend leur ordre indifférent : une borne veut dire la même chose qu'elle soit écrite
avant la fenêtre ou après. Un générateur dont les fenêtres ne franchissent jamais minuit ne quitte jamais le repère
ordinaire, donc ses tirages et ses graines sont intacts.

**Le long de la direction, ou pas du tout.** Une borne dans la fenêtre a une seule lecture : elle déplace le début
ou la fin. Une heure hors de la fenêtre n'en a aucune qui ne soit pas une supposition — `Before(12:00)` sur la nuit
pourrait vouloir dire toute la nuit ou rien —, donc elle est refusée plutôt que de recevoir un sens en silence. Deux
fenêtres franchissant minuit dont l'intersection fait deux morceaux sont refusées pour la même raison : le résultat
n'est pas une fenêtre.

**Une grille qui ne divise pas la journée est refusée, pas tordue.** Une granularité de sept minutes sur la nuit
place 23:55 puis 00:00 : un écart de cinq minutes à minuit. Aucun pas unique ne décrit cet ensemble, et le tirer
demanderait une seconde grille que l'appelant n'a jamais déclarée.
[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) demande le refus, énoncé ; toute
granularité qui divise la journée, c'est-à-dire toute granularité ronde, est acceptée.

## Alternatives envisagées

### Garder le refus, et documenter une recette par décalage

Envisagé parce que cela ne change rien. Rejeté parce que la paire est la lecture qu'a le type d'une nuit, et
qu'une recette — tirer un décalage depuis le début, l'ajouter modulo une journée — est ce que huit domaines ont dû
redécouvrir.

### Lire `Between(t, t)` comme la journée entière

Envisagé parce que, sur un cycle, une fenêtre de `t` jusqu'à `t` en faisant le tour est la journée. Rejeté parce
qu'un test qui a écrit `Between(9:00, 9:00)` pour neuf heures tirerait en silence n'importe quelle heure, et que
`Between` voudrait dire sur `TimeOnly` ce qu'il ne veut dire sur aucun autre type.

### Intersecter les bornes linéairement, quelle que soit la fenêtre

Envisagé parce que cela ne demande aucun repère. Rejeté parce que `Between(22:00, 06:00).After(23:00)` perdrait les
heures après minuit, et que l'ordre des appels changerait ce qu'ils veulent dire.

### Tirer deux segments comme deux spécifications

Envisagé parce que chaque segment est un intervalle ordinaire. Rejeté parce que chaque borne devrait toujours se
lire le long de la fenêtre, et que les messages de conflit, l'indice de cardinalité et l'inspection de vivier
demanderaient chacun une version à deux segments de ce que l'intervalle unique fournit déjà.

## Conséquences

### Positives

* Une nuit, un couvre-feu ou une session nocturne tient en un appel, tiré uniformément, avec toute autre contrainte
  qui s'y applique.
* L'ordre des déclarations ne compte pas, et une heure hors de la fenêtre échoue à la ligne d'arrangement en
  nommant les deux.
* Les fenêtres qui ne franchissent pas minuit sont inchangées, tirages compris.

### Négatives

* Sur `TimeOnly`, `Between` n'attrape plus un début et une fin écrits à l'envers : il tire l'autre partie de la
  journée. Un test qui voulait 06:00 à 22:00 et l'a écrit à l'envers obtient la nuit.
* Une granularité qui ne divise pas la journée ne peut pas servir sur une fenêtre qui franchit minuit.

### Risques

* JD014 n'a jamais lu les arguments `TimeOnly`, qui ne sont pas des constantes de compilation, donc il ne signale
  rien dans un sens ni dans l'autre ; JD032 ne replie pas encore un `Between` avec une borne du même côté, et
  aucune règle ne signale encore à la compilation une heure hors de la fenêtre. Atténuation : ces règles sont
  suivies par leurs propres issues (#355, #356) ; le refus à l'exécution nomme les deux déclarations en attendant.

## Actions de suivi

* JD032 qui replie un `Between` avec une borne du même côté, le long de la fenêtre (#355).
* Un signalement à la compilation d'une heure hors d'une fenêtre qui franchit minuit, et de `Between(t, t)` (#356).

## Références

* Issue [#357](https://github.com/Reefact/just-dummies/issues/357) — la décision que porte cet enregistrement.
* [ADR-0015](0015-draw-lattice-constrained-scalars-on-the-grid.fr.md) — la grille de granularité.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — refuser à la limite plutôt que
  deviner.
* La page des dates et heures — [`dates-and-times.fr.md`](../../for-users/generators/dates-and-times.fr.md).
