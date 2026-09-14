# ADR-0102 | Laisser une collection ou une composition hériter de la source de ses opérandes plutôt que de celle d'un contexte

🌍 🇬🇧 [English](0102-let-a-collection-or-composition-inherit-its-operands-source.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-09-14
**Accepted:** 2026-09-14
**Decision Makers:** Reefact

## Contexte

`Any.WithSeed(seed)` renvoie un `AnyContext` : un monde isolé dont les générateurs tirent leurs valeurs
d'une source dédiée, initialisée une fois avec la graine, indépendante du contexte ambiant qu'utilisent
les points d'entrée statiques de `Any`. Le contexte reflète les fabriques scalaires de `Any` — une
méthode par primitif, `OneOf` et `ElementOf` — et une convention par réflexion (`SurfaceParityTests`)
garantit que les deux surfaces exposent le même ensemble de fabriques scalaires : une fabrique ajoutée
d'un côté et oubliée de l'autre fait échouer un test.

Les fabriques de collection et de composition — `ListOf`, `ArrayOf`, `SequenceOf`, `SetOf`,
`DictionaryOf`, `Combine`, `PairOf`, `TripleOf` — n'existent que sur `Any`, et aucune ne choisit de
source : chacune réutilise celle qu'elle résout à partir de ses opérandes. Une collection réutilise la
source portée par son générateur d'éléments ; un dictionnaire celle portée par son générateur de clés,
ou celle de son générateur de valeurs quand le générateur de clés est étranger ; une composition celle du
premier opérande qui en porte une. Tous les générateurs fournis par la bibliothèque portent une source,
y compris ceux liés à la source ambiante ; seul un `IAny<T>` étranger n'en porte aucune, et une recette
dont aucun opérande n'en porte tire de la source ambiante au moment de la génération.

Ce qui est tiré de cette source résolue dépend du combinateur. Une composition ne tire rien elle-même :
elle génère chaque opérande et assemble. Une collection tire elle-même son **nombre d'éléments** et leur
**ordre** de la source résolue, et obtient ses éléments de son générateur d'éléments ; un dictionnaire
fait de même pour ses entrées. Ainsi `Any.ListOf(context.Int32())` tire son nombre d'éléments, ses
éléments et leur ordre de la source du contexte et se rejoue avec la graine du contexte exactement comme
`context.Int32()`, tandis que `Any.ListOf(Any.Int32())` est ambiant de bout en bout.

Une recette peut déjà mélanger les sources, dès que son auteur y écrit des générateurs de sources
différentes : `Any.ListOf(context.Int32()).ContainingAny(Any.Int32())` tire son nombre d'éléments et
leur ordre du contexte, mais l'élément imposé de la source ambiante ; une collection dont le générateur
d'éléments est étranger tire son nombre et son ordre de la source ambiante et ses éléments du générateur
étranger ; un `Combine` d'un générateur du contexte et d'un générateur ambiant tire chaque partie de sa
propre source. Une telle recette se génère, mais l'unique graine qu'elle signale ne rejoue qu'une partie
de ses tirages. Une composition le dit déjà : son indice de rejeu est qualifié dès que ses opérandes ne
tirent pas tous de la source résolue, parce que
l'[ADR-0021](0021-serialize-draws-on-a-random-source.fr.md) limite la reproductibilité à une séquence de
tirages effectués sur une seule source. Une collection ne porte pas encore d'indice de ce genre.

Rien de tout cela n'était dit sur la surface. Le résumé de la classe du contexte promettait que *chaque
générateur créé à partir de lui* tire de sa source ; le guide utilisateur le décrivait comme *un monde
autonome portant les mêmes fabriques* ; et seul le résumé du test de convention indiquait que les
combinateurs ne sont volontairement pas reflétés. Un lecteur qui voulait une liste déterministe
cherchait `context.ListOf(...)`, ne trouvait rien, et ne trouvait pas d'explication non plus —
[issue #184](https://github.com/Reefact/just-dummies/issues/184), ouverte par le mainteneur lors d'une
revue des cas limites de toute la bibliothèque, qui demandait de trancher entre compléter le miroir et
documenter la règle, et de consigner le résultat quel qu'il soit.

Compléter le miroir ne représente pas huit méthodes mais seize : `Combine` a sept arités, `SetOf` et
`DictionaryOf` deux surcharges chacune. Chacune recevrait des générateurs d'opérandes portant leur propre
source, et transmettrait malgré tout celle du contexte au combinateur. `context.ListOf(Any.Int32())`
tirerait alors son nombre d'éléments et leur ordre du contexte, et ses éléments de la source ambiante —
la recette mélangée décrite plus haut, produite cette fois par l'écriture canonique et non par un auteur
qui a écrit deux sources dans une même recette — et `Any.ListOf(context.Int32())` et
`context.ListOf(context.Int32())` seraient deux écritures résolvant les mêmes opérandes selon deux
règles. Un générateur est immuable et un générateur étranger ne porte aucune source : une fabrique côté
contexte ne peut pas non plus rattacher ses opérandes au contexte.

L'API publique est protégée par une ligne de base versionnée ; la bibliothèque n'a pas atteint la 1.0.

## Décision

Un contexte est une source de tirages scalaires : les fabriques de collection et de composition restent
sur `Any` seul, ne choisissent aucune source propre, et réutilisent celle résolue à partir de leurs
opérandes.

## Justification

**Une seule règle de résolution par recette, quel que soit le point d'entrée.** La source dont tirent
une collection, un dictionnaire ou une composition ne dépend que de leurs opérandes, et la règle tient en
une ligne : celle du générateur d'éléments ; celle du générateur de clés, sinon celle du générateur de
valeurs ; celle du premier opérande qui en porte une. Cette règle unique explique pourquoi
`Any.ListOf(context.Int32())` est déterministe avec la graine du contexte, pourquoi un dictionnaire aux
clés ambiantes reste ambiant quelle que soit la source de ses valeurs, pourquoi une composition mélangée
qualifie son rejeu, et pourquoi `AnyContext` n'a pas de `ListOf` : le combinateur n'a jamais eu de
source à choisir. Une fabrique côté contexte introduirait une seconde règle, qui résoudrait les mêmes
opérandes différemment selon le point d'entrée employé.

**Une recette mélangée doit être le fait de son auteur, jamais de l'écriture par défaut.** Le mélange
est possible aujourd'hui, et chacun de ses cas se lit dans le code : un `ContainingAny` sur une autre
source, un générateur d'éléments étranger, un `Combine` de deux sources. Le miroir ajouterait le seul cas
qui ne se lit pas : `context.ListOf(Any.Int32())` se lit comme une liste *du contexte* et ne se rejoue
pas avec `context.Seed`, sans aucun indice pour le dire, puisque les collections n'en portent pas.
Garder les combinateurs sur `Any` fait du mélange un acte explicite, et fait de la seule écriture qui
atteint le contexte celle qui le nomme sur l'opérande.

**Le miroir ne supprimerait pas ce qu'il semble supprimer.** Pour que la collection se rejoue, le
générateur d'éléments doit être celui du contexte : l'appelant écrit donc `context.Int32()` dans tous les
cas, et `context.ListOf(context.Int32())` ne dit rien de plus que `Any.ListOf(context.Int32())`. Ce que
le miroir ajoute, ce sont seize méthodes publiques sur un type livré et un second endroit où oublier le
contexte.

**La convention applique déjà la règle ; il lui manquait un foyer.** `SurfaceParityTests` exclut les
combinateurs du miroir par construction et échoue dès qu'on en ajoute un à `AnyContext`. Ce qui manquait,
c'était la règle énoncée là où le lecteur la cherche — la documentation du contexte lui-même et le guide
de reproductibilité — et l'enregistrement qui en explique la raison, ce que cette décision apporte.

## Alternatives envisagées

### Compléter le miroir

Ajouter les seize méthodes à `AnyContext`, chacune transmettant la source du contexte au combinateur,
avec un test par réflexion garantissant la parité. Envisagé parce que c'est ce que promettait déjà le
résumé de la classe, et parce qu'une liste déterministe serait alors découvrable depuis `context.`.
Rejeté parce que cela introduit une seconde règle de résolution pour les mêmes opérandes, et parce que
son écriture canonique produirait silencieusement une recette mélangée impossible à rejouer, là où
aujourd'hui toute recette mélangée est écrite en toutes lettres par son auteur — sans dispenser
l'appelant de lier le générateur d'éléments au contexte pour que le résultat se rejoue.

### Rattacher les opérandes au contexte

Faire que `context.ListOf(item)` redirige les tirages de `item` vers la source du contexte. Rejeté parce
qu'un générateur est une recette immuable qui porte sa source, et qu'un `IAny<T>` étranger n'en porte
aucune à rediriger : aucun mécanisme ne le permet, à moins d'un second modèle de générateur.

### Changer la résolution d'une source mélangée pour un dictionnaire ou une composition

Résoudre vers *le premier opérande lié à un contexte* plutôt que vers *le premier opérande portant une
source*, pour qu'un générateur de valeurs lié au contexte gouverne un dictionnaire aux clés ambiantes.
Rejeté parce que cela modifierait la source dont tire une chaîne existante, donc ce qu'une graine rejoue
— le critère d'acceptation de l'issue l'exclut — et parce que la règle actuelle est la plus simple à
énoncer.

### Doter dès maintenant les collections d'un indice de rejeu

Qualifier le rejeu d'une collection comme l'est celui d'une composition, pour qu'une collection mélangée
dise que la graine signalée ne couvre qu'une partie de ses tirages. Envisagé parce que le manque est réel
et que cet enregistrement le nomme. Non retenu ici parce que c'est indépendant de l'emplacement des
fabriques, que c'est un changement de comportement avec son propre contrat de diagnostic, et que les
critères d'acceptation de l'issue excluent tout changement dans la résolution ou le signalement d'une
chaîne existante ; le point est laissé en action de suivi.

## Conséquences

### Positives

* La surface documentée de `AnyContext` et sa surface réelle concordent, et la documentation explique
  comment se construit une collection ou une composition déterministe et quel opérande gouverne un
  mélange, replis compris.
* Aucune nouvelle méthode publique, aucun changement de comportement, aucun changement dans ce qu'une
  chaîne existante rejoue.
* La règle que le test de convention applique est écrite là où un lecteur la trouve, et elle est
  consignée.

### Négatives

* Une collection déterministe s'écrit avec deux points d'entrée, `Any.ListOf(context.Int32())` ; le
  contexte ne la propose pas depuis `context.`. Sa découvrabilité repose sur la documentation du
  contexte.
* Le repli du dictionnaire vers la source de son générateur de valeurs ne joue que si le générateur de
  clés est étranger : un cas particulier qu'il faut expliquer au lecteur plutôt qu'un cas qu'il peut
  déduire.

### Risques

* Une collection mélangée — un `ContainingAny` sur une autre source, un générateur d'éléments étranger —
  signale une graine qui ne rejoue que son nombre d'éléments et leur ordre, sans indice pour le dire. La
  documentation nomme le cas ; l'action de suivi ci-dessous ferait que le générateur le nomme lui-même.
* Une future fabrique scalaire ajoutée à `Any` reste détectée par la convention ; un futur combinateur
  ajouté à `AnyContext` l'est aussi. Le risque tient dans la prose : une page ou un résumé qui
  promettrait à nouveau *les mêmes fabriques* sur le contexte. Le résumé du test de convention et les
  remarques du contexte renvoient tous deux à cet enregistrement, pour que la prochaine modification y
  trouve la règle.

## Actions de suivi

* La documentation de la classe du contexte, le guide de reproductibilité et la page du paquet, en
  anglais et en français, énoncent la règle et ses replis dans le même changement que celui qui la
  consigne.
* Décider si une collection doit porter un indice de rejeu qualifié, comme une composition, revient au
  mainteneur ; cet enregistrement se contente de nommer le manque.

## Références

* [ADR-0021](0021-serialize-draws-on-a-random-source.fr.md) — la reproductibilité est limitée à une
  séquence de tirages effectués sur une seule source, ce qui explique qu'une composition mélangée
  qualifie son indice de rejeu.
* [ADR-0005](0005-cap-any-combine-at-arity-eight.fr.md) — les sept arités qu'un miroir devrait porter.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — une question d'API
  publique sur un type livré se tranche, elle ne se rafistole pas.
* [Issue #184](https://github.com/Reefact/just-dummies/issues/184) — le manque, les deux voies
  possibles, et la demande d'un enregistrement.
