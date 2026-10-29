# ADR-0114 | Refuser une borne `DateTime` `Local` plutôt que la lire en ticks

🌍 🇬🇧 [English](0114-refuse-a-local-datetime-bound.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-10-29
**Accepted:** 2026-10-29
**Decision Makers:** Reefact

## Contexte

Un `DateTime` est un nombre de ticks et un `Kind` — `Utc`, `Local` ou `Unspecified`. Ses opérateurs de comparaison
ne lisent que les ticks : minuit `Local` à Paris et minuit `Utc` sont égaux, alors que ce sont deux instants
séparés d'une heure.

`AnyDateTime` suit ces opérateurs. Ses remarques XML, et aucun enregistrement de décision, fixent le contrat : toute
valeur tirée porte `Kind.Utc`, les bornes se comparent par ticks quel que soit leur `Kind`, et une valeur fournie à
`OneOf` est rendue telle quelle. Avec des bornes `Local`, le tirage peut donc être une valeur `Utc` dont les ticks
sont compris entre ceux des bornes et dont l'instant sort de ceux des bornes, du décalage local. Aucune contrainte
déclarée n'est violée selon la comparaison qu'un test écrirait, mais la valeur n'est pas dans la fenêtre que les
bornes désignent en tant qu'instants. L'essai UX développeur l'a rencontré dans six domaines sur vingt (issue #317,
candidat CORE-C7), et la documentation utilisateur le dit désormais sans détour.

Une borne `Local` se lit de deux façons — comme un instant, ou comme une heure sur l'horloge de cette machine — et
rien dans l'appel ne dit à la bibliothèque laquelle. Lues comme des heures d'horloge, les valeurs `Local` ont des
trous et des doublons : lors d'un changement d'heure, certaines heures n'existent jamais et d'autres existent deux
fois.

Dans un test, un `DateTime` `Local` vient presque toujours de `DateTime.Now` ou de `DateTime.Today`. Des bornes
prises sur l'horloge changent d'une exécution à l'autre, si bien que la graine qu'un échec rapporte ne le rejoue
pas ; la documentation utilisateur met déjà en garde contre cela sous le nom de « piège de `DateTime.Now` ».

`Unspecified` est ce que donne `new DateTime(2026, 1, 1)`, et c'est la façon courante d'écrire une borne. Il
n'affirme rien sur aucun fuseau.

L'essai a nommé trois options : garder `Utc` et le documenter ; hériter du `Kind` quand toutes les bornes partagent
un `Kind` non-`Utc` ; refuser les bornes `Local` ou mixtes. La bibliothèque n'a pas encore d'utilisateurs et est en
dessous de 1.0 : un changement de comportement ne casse personne.

## Decision

`AnyDateTime` refuse une borne dont le `Kind` est `Local` par une erreur d'argument, et continue de lire les bornes
`Utc` et `Unspecified` par leurs ticks et de tirer des valeurs `Utc`.

## Rationale

**La bibliothèque ne peut pas savoir ce que signifie une borne `Local`, elle ne doit donc pas répondre à la place de
l'appelant.** La lire en ticks répond « une heure d'horloge » tout en étiquetant le résultat `Utc`, ce qui a produit
la surprise. Hériter du `Kind` répond honnêtement « une heure d'horloge », mais doit alors traiter des heures qui
n'existent pas ou existent deux fois. L'ADR-0046 borne l'ambition de la bibliothèque, pas sa correction : un refus qui
nomme l'ambiguïté, et les deux façons d'en sortir — convertir la borne en UTC, ou utiliser `DateTimeOffset` quand
c'est l'instant qui compte — est préféré à une valeur tirée par un mécanisme que le lecteur ne peut pas raisonner.

**Refuser `Local` attrape aussi une borne lue sur l'horloge.** Comme une borne `Local` dans un test vient presque
toujours de `DateTime.Now` ou de `DateTime.Today`, le refus arrête, à la ligne qui la déclare, le test non rejouable
contre lequel la documentation met en garde.

**`Unspecified` reste accepté parce qu'il n'affirme rien que la lecture en ticks contredise.** Il ne nomme aucun
fuseau, si bien que le lire en ticks ne perd aucune information donnée par l'appelant. Le refuser écarterait la
manière la plus ordinaire d'écrire une date, sans rien gagner en sens.

**Les valeurs que l'appelant fournit ne sont pas des bornes.** `OneOf`, `Except` et `DifferentFrom` prennent des
valeurs choisies explicitement par l'appelant ; `OneOf` les rend telles quelles. Elles ne posent aucune question de
fenêtre et restent inchangées.

**Le coût de la rupture est nul aujourd'hui.** Sans utilisateurs et en dessous de 1.0, refuser maintenant coûte moins
que refuser plus tard.

## Alternatives Considered

### Garder `Utc` et le documenter

Envisagé parce que c'est le comportement actuel, qu'il suit les opérateurs de `DateTime` et qu'il est désormais
documenté. Écarté parce que la documentation décrit la surprise sans l'empêcher, et que l'appel qui la provoque
compile et s'exécute toujours.

### Hériter du `Kind` partagé par les bornes

Envisagé parce qu'il supprime la surprise pour des bornes `Local`. Écarté parce qu'un tirage `Local` doit alors éviter
les heures qui n'existent jamais et trancher celles qui existent deux fois, ce qui rend le générateur dépendant des
règles de fuseau : un mécanisme plus malin là où un refus est possible. Il exige aussi une règle pour des bornes
mixtes.

### Refuser aussi `Unspecified` ou les bornes mixtes

Envisagé par symétrie. Écarté pour `Unspecified`, qui ne porte aucun fuseau (ci-dessus). Des bornes mixtes `Utc` et
`Unspecified` se lisent toutes deux en ticks sans perte, et n'appellent aucun refus ; des bornes mixtes impliquant
`Local` sont refusées du côté `Local`.

## Consequences

### Positive

* Une fenêtre `DateTime` signifie la même chose en ticks et en instants pour toute borne que la bibliothèque accepte.
* Une borne prise sur l'horloge de la machine est refusée à la ligne qui la déclare.

### Negative

* Une chaîne qui passait une borne `Local` lève désormais une exception. Acceptable tant que la bibliothèque n'a pas
  d'utilisateurs ; le changelog le dit.

### Risks

* Une valeur `Kind.Local` fournie à `Except` ou `DifferentFrom` est toujours comparée par ticks. Parade : ce sont des
  valeurs explicites, pas une fenêtre, et la documentation utilisateur dit comment les valeurs sont comparées.

## Follow-up Actions

* Refuser une borne `Local` dans `After`, `AfterOrEqualTo`, `Before`, `BeforeOrEqualTo` et `Between`, avec un message
  qui nomme le paramètre et les deux façons d'en sortir.
* Mettre à jour les remarques XML, la page utilisateur `dates-and-times` (EN et FR) et le changelog de `core`.

## References

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — borner l'ambition du générateur, jamais
  sa correction.
* Issue [#317](https://github.com/Reefact/just-dummies/issues/317) et candidat CORE-C7 de l'essai UX développeur.
