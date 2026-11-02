# ADR-0129 | Signaler une chaîne dont les constantes n'admettent qu'une seule valeur, sur tous les types exacts

🌍 🇬🇧 [English](0129-report-a-chain-whose-constants-admit-exactly-one-value.md) · 🇫🇷 Français (ce fichier)

**Status:** Proposed
**Proposed:** 2026-11-02
**Decision Makers:** Reefact

## Contexte

Le second fondement de la bibliothèque veut que ce qui est arbitraire devienne visiblement arbitraire et que ce qui
est significatif reste un littéral. Un générateur dont les contraintes constantes n'admettent qu'une seule valeur —
`Any.Int32().Between(5, 5)`, `GreaterThanOrEqualTo(x).LessThanOrEqualTo(x)`, un `OneOf` à une seule valeur — tire
une constante déguisée en tirage : la valeur que le test a choisie, écrite comme si le test ne s'en souciait pas. La
justification de l'ADR-0097 appelle cela « la constante choisie à la main que cette bibliothèque existe pour
remplacer ». Aucune règle ne le signalait : JD023 ne se déclenche que sur un domaine vide, et son état de repliement
tenait une chaîne à une seule valeur pour légale.

JD023 elle-même ne couvrait que les générateurs entiers, comme sa page le disait, si bien qu'une plage constante
croisée sur un autre type exact — `Any.Decimal().GreaterThan(100m).LessThan(10m)`,
`Any.DateOnly().After(later).Before(earlier)` — compilait et levait à chaque exécution. L'issue #356 a décidé de
l'étendre aux types dont la comparaison est exacte : `decimal`, et `DateOnly`, `DateTime`, `DateTimeOffset`,
`TimeSpan` et `TimeOnly`, qui se comparent par ticks ou par numéros de jour. Les familles flottantes binaires
restent dehors, puisque voisins et arrondis ne se replient pas comme des entiers. Sur `TimeOnly`, la règle suit la
fenêtre qui franchit minuit (ADR-0125) : une borne constante hors d'une fenêtre qui franchit minuit est une chaîne
qui n'admet aucune valeur.

Un diagnostic de niveau information n'apparaît pas dans la sortie de `dotnet build` (issue #382) : une règle à cette
sévérité n'enseigne rien là où une compilation se lit. `Any.Fixed` renvoie un littéral à travers un générateur là où
un générateur est requis (ADR-0118). `Between(t, t)` sur une heure de la journée reste la seule valeur `t`, et la
journée entière est `Any.TimeOnly()` (ADR-0125).

Les propres suites de tests de la bibliothèque tirent des chaînes à une seule valeur à dessein : la valeur tirée y est
le sujet du test, pas un dummy, comme le dit le guide de test du dépôt.

## Décision

Un avertissement de la famille des contraintes, JD038, signale une chaîne sur un type exact dont les contraintes
constantes n'admettent qu'une seule valeur, en réutilisant le repliement de JD023 — qui couvre désormais `decimal`
et les générateurs temporels — et nomme l'issue : écrire le littéral, ou `Any.Fixed` là où un générateur est requis,
et sur une heure de la journée `Any.TimeOnly()` si c'est la journée entière qui était visée.

* Elle signale `Between(a, a)`, un plancher et un plafond qui se rejoignent, un treillis dont un seul point reste
  dans la plage, et un ensemble de valeurs réduit à une seule, sur les générateurs entiers, décimal et temporels.
* Une valeur épinglée par son nom, `Zero()`, n'est pas signalée : elle se lit déjà comme la constante qu'elle est.
* Une borne calculée depuis un autre tirage ne se replie pas et n'est pas signalée.
* Il n'y a pas de correctif de code.
* Les propres suites de la bibliothèque déclinent la règle, puisque la valeur tirée est leur sujet.

## Justification

**L'erreur est celle que la bibliothèque existe pour prévenir.** Une chaîne à une seule valeur ressemble à un tirage
et est un littéral : un lecteur ne la distingue pas d'un dummy, ce qui est exactement ce que le second fondement
veut visible. La signaler garde le site d'appel honnête sur ce qui compte.

**Un repliement, deux questions.** JD023 reconstruit déjà le domaine contrainte par contrainte ; demander combien il
reste, plutôt que s'il reste quelque chose, ne coûte aucune lecture nouvelle et ne peut pas contredire le verdict de
vacuité. C'est aussi pourquoi l'extension aux types exacts se décide ici : le même parcours répond aux deux questions
sur tout type qu'il lit, et un type lu pour l'une est lu pour l'autre.

**Les types exacts seulement.** Les entiers, `decimal` et les types temporels se comparent exactement : une borne, un
treillis et une exclusion se replient sur les valeurs mêmes que l'exécution voit. Une borne flottante binaire non :
deux littéraux décimaux peuvent être un seul `float`, et le voisin d'une borne stricte dépend de la grandeur. La règle
est bornée là où sa certitude s'arrête (ADR-0046).

**Un avertissement, parce qu'il n'y a aucune écriture correcte à tolérer.** Une exclusion inerte a une lecture
défendable, et c'est pourquoi JD024 est de niveau information ; une plage constante réduite à un point n'en a aucune,
et l'information est invisible dans une compilation.

**`Zero()` par son nom.** `Zero()` dit ce qu'il fait ; un lecteur ne le prend pas pour un tirage. Le signaler
changerait un idiome documenté en bruit, et la règle vise la constante qui se cache.

**Des issues nommées, pas de correctif.** Un littéral change le type du site d'appel, `Any.Fixed` le garde
générateur, et sur une heure de la journée `Between(t, t)` a pu viser la journée entière : lequel s'applique est au
développeur de le dire.

## Alternatives envisagées

### La sévérité information

Envisagée parce que la chaîne tire et que le test passe. Rejetée : la sévérité est invisible dans une compilation, et
la règle n'a aucun cas où l'écriture signalée est celle à garder, ce qui est la distinction que fait l'ADR-0077.

### Étendre le message de JD023 plutôt qu'ajouter une règle

Envisagé parce que le repliement est partagé. Rejeté : JD023 signale une chaîne qui lève, et un consommateur qui la
réduit au silence ne doit pas taire une chaîne qui tire ; un identifiant par fait garde une suppression honnête.

### Signaler aussi `Zero()`

Envisagé par cohérence : il n'admet qu'une valeur. Rejeté : l'épinglage est nommé, le lecteur n'est pas trompé, et la
bibliothèque documente l'appel comme l'idiome du cas vide.

### Un correctif de code remplaçant la chaîne par le littéral

Envisagé parce que la réécriture est mécanique. Rejeté : elle change le type du site d'appel, et sur une heure de la
journée elle ne peut pas savoir si la journée était visée.

## Conséquences

### Positives

* Une constante déguisée en tirage est signalée là où elle est écrite, sur tous les types exacts.
* Les plages constantes croisées sur `decimal` et les générateurs temporels sont signalées à la compilation, comme
  celles des entiers l'étaient.

### Négatives

* Un test hors de ce dépôt qui tire une chaîne à une seule valeur comme sujet reçoit un avertissement qu'il doit
  supprimer sur place, ou décliner pour sa suite comme le font les suites de ce dépôt.

### Risques

* Une valeur unique atteinte en excluant une large plage valeur par valeur n'est pas comptée, et pas signalée : le
  parcours est borné comme celui de JD023.

## Actions de suivi

* Le corpus de l'essai UX développeur tire à dessein des chaînes à une seule valeur dans ses sondes ; le prochain
  essai décide si ces sondes suppriment la règle ou la déclinent.

## Références

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — la règle est bornée là où sa certitude s'arrête.
* [ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.fr.md) — quand une règle signale une écriture correcte.
* [ADR-0097](0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.fr.md) — la constante choisie à la main que cette bibliothèque existe pour remplacer.
* [ADR-0118](0118-return-a-literal-through-any-fixed.fr.md) — `Any.Fixed`.
* [ADR-0125](0125-let-a-time-of-day-window-cross-midnight.fr.md) — la fenêtre qui franchit minuit, et `Between(t, t)`.
* Issues #356 et #382 ; AN-C2 du premier essai UX développeur.
