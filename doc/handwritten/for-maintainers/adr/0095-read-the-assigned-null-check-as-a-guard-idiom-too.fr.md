# ADR-0095 | Lire aussi le null-check assigné comme un idiome de garde

🌍 🇬🇧 [English](0095-read-the-assigned-null-check-as-a-guard-idiom-too.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-09-03
**Accepted:** 2026-09-03
**Decision Makers:** Reefact

> Les références de section (§N) pointent dans la [spécification `dum`](../specifications/justdummies-tool.fr.md).

## Contexte

L'ensemble fermé d'idiomes de garde reconnus du §5.3 lit déjà un null-check en deux graphies :
`ArgumentNullException.ThrowIfNull(value)` comme appel, et `if (value is null) { throw … }` comme
condition. Les deux sont lus comme compris et n'ajoutant rien — le generator ne retourne jamais
`null` de toute façon (ADR-0064) — plutôt que laissés non lus.

Une troisième graphie, courante, fusionne la même vérification dans l'assignation qu'elle
précède : `Field = value ?? throw new ArgumentNullException(nameof(value));`. Aucune des deux
lignes existantes ne la couvre : ce n'est ni un appel autonome, ni un `if`. Le scan des guards en
tête (§5.3) la lit comme une écriture ordinaire sur l'état, ce qui est là où le problème s'aggrave.
Ce scan taille déjà une exception pour une écriture qui n'est pas ordinaire — le helper de
librairie de guard d'ADR-0086, assigné directement à un champ ou une propriété, qui valide et
stocke à la fois sans terminer le scan. La forme `?? throw` fait de même, mais n'est pas cette
exception, donc le scan s'arrête au premier paramètre écrit ainsi.

Un constructeur validant plusieurs paramètres de la même façon écrit une telle ligne par
paramètre — `Field1 = a ?? throw …; Field2 = b ?? throw …; …` — et le scan ne lit que le premier
comme rejetant, puis s'arrête. Chaque paramètre suivant gardé de façon identique est lu comme si
rien ne lui avait jamais été demandé : pas de marque `unread guards`, pas de contrainte, un
silence rigoureusement indiscernable d'un paramètre sans aucun guard. Ceci a été trouvé en
scaffoldant un type de domaine composant plusieurs constructeurs de ce genre — mesuré sur un
`Order(OrderReference, CustomerId, Money, OrderStatus)` réel, quoiqu'illustratif.

## Décision

Le lecteur reconnaît `Field = value ?? throw new ArgumentNullException(nameof(value));` comme une
troisième graphie du null-check que le §5.3 lit déjà, comprise et n'ajoutant rien, et — comme
l'idiome d'assignation-guard d'ADR-0086 — cette assignation ne termine pas le scan en tête.

## Justification

Les deux graphies déjà établies traitent un null-check comme une question réglée : lue, et ne
valant aucune contrainte, parce que la propriété qu'aucun tirage ne peut jamais violer n'a rien à
défendre. La troisième graphie énonce l'invariant identique ; refuser de la lire à cause de
l'endroit où se trouve le `throw`, plutôt qu'à cause de ce qu'elle dit, serait incohérent avec les
deux lignes juste à côté.

Ne pas terminer le scan est la plus importante des deux corrections, et la plus lourde de
conséquences : un constructeur qui n'atteint jamais cette forme n'est pas affecté, mais dès que
deux paramètres sont chacun gardés ainsi, le second perd silencieusement sa propre lecture. Le
silence ici est pire qu'une marque non lue — une marque dit au développeur que quelque chose a été
demandé et non compris ; le silence lui dit que rien n'a jamais été demandé du tout, et un guard
réel, sans rapport, sur ce même paramètre — une borne de taille que le lecteur aurait signalée —
disparaît avec lui.

Restreindre étroitement l'exception reconnue — le type de l'expression levée doit se résoudre en
exactement `ArgumentNullException` — garde l'ensemble fermé fermé (ADR-0046) : une exception
différente levée depuis la forme identique énonce un invariant que cette ligne ne sait pas nommer,
et est laissée à la lecture ordinaire « rejette, et le moteur ne sait pas pourquoi » qu'un `if` non
reconnu reçoit déjà.

## Alternatives envisagées

### La lire comme un appel, en la repliant dans le chemin de reconnaissance des appels existant

Envisagée parce que le moteur a déjà un chemin de reconnaissance des appels, et la réutilisation
semblait moins coûteuse qu'un nouveau.

Rejetée parce que `??` est un opérateur, pas un appel — il n'y a pas d'`InvocationExpressionSyntax`
pour que ce chemin le parcoure, et forcer la forme à y passer aurait signifié un second
comparateur, parallèle, portant le nom du premier.

### La laisser non lue, et compter sur le développeur pour remarquer le silence

Envisagée parce que cela ne change rien et que l'outil dit déjà au développeur de vérifier ce
qu'il ne peut pas cautionner ailleurs.

Rejetée parce que « ailleurs » est précisément le problème : cette forme n'atteint même pas une
marque non lue aujourd'hui — le scan s'arrête avant même que le second paramètre soit examiné, il
n'y a donc aucun signal à remarquer. Un développeur qui scaffolde ce constructeur voit chaque
paramètre rapporté comme propre.

## Conséquences

### Positives

* Un constructeur validant plusieurs paramètres via `?? throw new ArgumentNullException(...)` les
  voit tous lus, pas seulement le premier.
* Un paramètre composé gardé ainsi n'a besoin d'aucune vérification et se tire via son propre
  generator en ligne (§4.2), exactement comme un paramètre non gardé — le null-check n'ajoute rien
  à vérifier.
* L'ensemble fermé reste fermé : seul ce type d'exception est reconnu, par symbole résolu.

### Négatives

* Une quatrième forme de lecture de guard est une de plus que le mainteneur garde en tête en
  raisonnant sur le §5.3.

### Risques

* Un constructeur mêlant cet idiome à un guard réellement non lu sur le même paramètre bloque
  toujours la compilation, comme il se doit — le risque se limite aux formes que cet enregistrement
  élargit, pas à celles qu'il laisse de côté.

## Actions de suivi

* Aucune.

## Références

* [ADR-0086](0086-read-the-guard-helpers-of-named-libraries.fr.md) — l'exception d'assignation que
  cet enregistrement étend à un second idiome.
* [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) — le mécanisme de lecture des
  guards auquel cet enregistrement ajoute une graphie.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — pourquoi le type
  d'exception reconnu reste étroit plutôt que n'importe quel type levé.
* [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.fr.md) — pourquoi un null-check n'ajoute
  rien à la chaîne d'un generator.
