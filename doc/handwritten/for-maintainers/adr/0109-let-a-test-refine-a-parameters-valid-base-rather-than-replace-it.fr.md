# ADR-0109 | Laisser un test affiner la base valide d'un paramètre plutôt que la remplacer

🌍 🇬🇧 [English](0109-let-a-test-refine-a-parameters-valid-base-rather-than-replace-it.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-10-27
**Accepted:** 2026-10-27
**Decision Makers:** Reefact

Supersède l'[ADR-0096](0096-emit-a-parameter-inline-whenever-it-has-nothing-to-report.fr.md).

> Les renvois de section (§N) pointent vers la [spécification de `any`](../specifications/justdummies-tool.fr.md).

## Contexte

Un `AnyX` scaffoldé tire chaque paramètre de constructeur d'une recette que le moteur a inférée : une
ligne de la table de base resserrée par les gardes du constructeur (§5.2, §5.3), ou, pour un paramètre
composé, le générateur que ce type possède (§5.4, [ADR-0089](0089-draw-a-composed-parameter-through-the-generator-its-type-owns.fr.md)).
Cette recette est la **base valide** du paramètre : ce que le constructeur accepte.

L'endroit où la base est écrite dépend de ce que le paramètre a à signaler
([ADR-0096](0096-emit-a-parameter-inline-whenever-it-has-nothing-to-report.fr.md)) : un paramètre
dont une garde a resserré la chaîne, ou que retient l'un des deux marqueurs bloquants des §5.5 et
§5.6, reçoit une fabrique privée que le constructeur appelle ; tout autre est écrit en ligne, comme
un appel dans l'initialiseur du constructeur, parce qu'une méthode enveloppant cet appel ne dirait
rien que l'appel ne dise.

Une règle peut appartenir au type composant plutôt qu'au type composé. Un `Shipment` peut n'accepter
qu'une `Route` nationale, alors qu'une `Route` seule peut être internationale. Le moteur ne sait pas
appliquer une telle règle à un paramètre composé : il marque le paramètre (§5.6), et le développeur
écrit la règle dans le fichier, qui lui appartient
([ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.fr.md)).

Chaque paramètre portait deux surcharges `With` : l'une fixant une valeur, l'autre prenant un
`IAny<T>` qui **remplace** la recette. Un test qui tient à une partie d'une valeur — la distance d'une
route, une borne haute sur une quantité — ne pouvait l'atteindre que par la seconde, en construisant
un générateur de zéro. Ce générateur ne porte pas la base : le test doit répéter la règle, ou la perd.

Un test qui la perd compile toujours, et tire : il passe chaque fois que le tirage satisfait la règle
par hasard, et échoue aux autres exécutions, pour une raison sans rapport avec ce qu'il vérifie. Un
test qui n'échoue qu'à certaines exécutions est l'échec que la bibliothèque existe pour éliminer.

Les générateurs de la bibliothèque sont des recettes immuables, chaque contrainte renvoyant une
nouvelle instance ([ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.fr.md)). Sur un
générateur de la bibliothèque, les contraintes s'additionnent, et deux contraintes qui se contredisent
sont refusées là où elles sont déclarées, le message nommant les deux côtés. Une contrainte peut
renvoyer un autre type de générateur que celui sur lequel elle est appelée.

L'analyseur qui signale une contrainte jetée, `JD006`, exempte un appel qui forme tout le corps d'une
lambda passée en argument, pour qu'un test qui affirme un refus puisse y écrire l'appel.

La justification d'[ADR-0061](0061-draw-from-the-ambient-context-and-hold-no-state.fr.md) s'appuyait
sur la surcharge `IAny<T>` pour servir un développeur qui tire depuis un contexte isolé
`Any.WithSeed`, paramètre par paramètre ; la portée ambiante, `Any.UseSeed`, atteint chaque tirage du
générateur émis, paramètres composés compris. La justification
d'[ADR-0064](0064-never-draw-null-for-a-nullable-parameter.fr.md) s'appuyait sur la même surcharge
comme moyen, pour un test, d'opter pour un générateur qui tire parfois `null`.

La bibliothèque fait correspondre un `AnyX` scaffoldé à la Creation Method anonyme et paramétrée
d'*xUnit Test Patterns* (`core-concepts`) : ses méthodes `With` en sont les paramètres.

## Décision

Chaque paramètre résolu d'un générateur scaffoldé est tiré d'une fabrique unique qui porte sa base
valide, et un test en fixe la valeur ou affine cette base par une lambda, sans jamais la remplacer.

## Justification

Le défaut n'était pas qu'un test puisse construire un générateur ; c'était qu'en construire un
revenait à repartir de zéro. Une Creation Method existe pour qu'un test n'ait pas à savoir construire
un objet valide, y compris les objets dont il dépend. La surcharge de remplacement faisait refluer ce
savoir dans chaque test qui paramétrait une valeur : une règle de construction écrite une fois dans le
générateur était recopiée dans chacun de ces tests, où elle paraît aussi délibérée que la valeur dont
le test parle — et où l'oublier compile. Affiner part de la base, au contraire : le test ne dit que ce
qui lui importe, et la règle garde son adresse unique.

Une fabrique unique est ce qui rend cette adresse réelle. La recette par défaut et l'affinage doivent
partir du même endroit, sinon le développeur qui resserre l'une laisse l'autre lâche. Cet endroit doit
être celui que le développeur modifie déjà : la base écrite par le moteur, ou la règle qu'il a écrite
là où le moteur le lui demandait. Un affinage partant d'un générateur neuf compilerait, se lirait
comme s'il partait d'une base valide, et reproduirait l'échec intermittent dans le code généré au lieu
du test.

C'est aussi pourquoi chaque paramètre résolu a désormais une fabrique, et pourquoi cette décision
supersède l'ADR-0096. L'ADR-0096 avait raison : une méthode enveloppant un seul appel ne disait rien
que l'appel ne dise — tant que le constructeur était son seul appelant. Avec un second appelant, la
fabrique dit la chose qu'un lecteur a le plus besoin de trouver : où est la base du paramètre, pour le
défaut comme pour chaque affinage. Un paramètre en ligne dans l'initialiseur laisserait à l'affinage
soit une seconde copie de cet appel, soit rien d'où partir.

La lambda renvoie le générateur affiné plutôt que de modifier celui qu'elle reçoit, parce que les
générateurs sont immuables et qu'un `With` mutable contredirait le contrat que le type émis implémente
(ADR-0057). Elle garde aussi visible la seule erreur qui reste possible : une lambda qui jette une
contrainte doit le faire par une instruction dans un bloc, que `JD006` signale, alors qu'une forme
mutable jetterait l'affinage entier précisément sous la forme que `JD006` exempte.

Sur une feuille, affiner est plus sûr que répéter, pas seulement plus court : les contraintes de la
bibliothèque s'additionnent, donc un affinage qui contredit la base échoue là où il est écrit, en
nommant les deux côtés, à chaque exécution. C'est la ligne de correction de la bibliothèque appliquée
au site d'appel ([ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md)).

Supprimer la surcharge de remplacement plutôt que la garder à côté de celle d'affinage, c'est ce qui
fait de l'échec une erreur de compilation. Avec les deux, rien au site d'appel ne les distingue, et
celle qui perd la règle reste celle vers laquelle un lecteur se tourne. Les besoins qu'elle servait
ont tous un autre chemin : fixer une valeur couvre un test qui tient à la valeur entière, `null`
compris ; la portée de seed ambiante couvre le déterminisme hors d'un test mieux que les générateurs de
contexte paramètre par paramètre, puisqu'elle atteint aussi les paramètres composés ; et le reste —
tirer parmi quelques valeurs choisies, une collection dont les éléments demandent une autre recette,
un paramètre nullable tiré `null` de temps en temps — est assez rare pour que le développeur ajoute la
surcharge dans son propre fichier partiel, où elle devient un choix fait pour un paramètre plutôt que
le défaut offert sur tous.

La surcharge de valeur reste. Un test qui tient à la valeur entière doit pouvoir le dire, et une valeur
fixée qui viole une règle échoue à chaque exécution plutôt qu'à certaines, ce qui est l'échec visible
que cette décision recherche.

## Alternatives considérées

### Un `Action<T>` de configuration, la forme du patron options

Envisagé parce que c'est la forme familière de la configuration imbriquée en .NET, et qu'elle permet à
une lambda à bloc d'enchaîner des ajustements sans rien renvoyer.

Rejeté parce que les générateurs émis sont immuables : l'appel naturel compile et jette l'ajustement
en silence, sous la seule forme que `JD006` exempte. Rendre `With` mutable rouvrirait ADR-0057.

### Faire partir la lambda d'un `new AnyX()` neuf

Envisagé parce que cela ne demande aucune fabrique pour un paramètre composé, et garde la forme émise
d'ADR-0089.

Rejeté parce que cela ignore la base écrite par le développeur. La lambda remettrait au test une valeur
arbitraire tout en se lisant comme si elle partait d'une base valide.

### Garder les paramètres en ligne de l'ADR-0096, et répéter leur appel dans la surcharge d'affinage

Envisagé parce que cela garde le fichier émis aussi court qu'avant, une fabrique seulement là où un
paramètre a quelque chose à signaler.

Rejeté parce que cela écrit la base deux fois dans un même fichier. Un développeur qui modifie
l'appel dans l'initialiseur du constructeur laisse la copie de l'affinage telle quelle, ce qui est la
dérive qu'une adresse unique existe pour empêcher.

### Partir de la recette courante, après un appel `With` antérieur

Envisagé parce que des affinages successifs se composeraient alors.

Rejeté parce que le champ devrait garder le type concret du générateur, et qu'une fois une valeur
fixée il ne reste aucun générateur d'où partir. Toutes les autres méthodes `With` laissent le dernier
appel l'emporter, et celle-ci aussi.

### Garder la surcharge de remplacement à côté de celle d'affinage

Envisagé parce que cela laisse compiler tous les appels existants.

Rejeté parce que la surcharge qui perd la règle resterait à une frappe de celle qui la garde, sans rien
au site d'appel pour les distinguer.

### Garder la surcharge de remplacement sur les seuls paramètres nullables

Envisagé parce qu'un type valeur nullable tiré `null` de temps en temps n'a aucun chemin par une lambda
qui doit renvoyer le générateur qu'elle reçoit.

Rejeté parce que le fichier partiel couvre ce besoin en quelques lignes, et qu'une seule forme pour
tous les paramètres se lit et s'enseigne plus simplement qu'une forme qui dépend du type du paramètre.

### Les paramètres composés seulement

Envisagé parce que la règle du type composant est l'endroit où le problème a d'abord été vu.

Rejeté parce qu'une feuille a le même défaut : remplacer un `Positive()` issu d'une garde le perd
exactement comme remplacer une base composée perd une règle écrite par le développeur.

### Faire descendre un contexte isolé à travers les générateurs

Envisagé parce que cela laisserait `Any.WithSeed` atteindre chaque paramètre, le cas que la surcharge
de remplacement servait.

Rejeté parce que cela demande une seconde copie de chaque recette et renverse ADR-0061, alors que la
portée ambiante `Any.UseSeed` atteint déjà tout l'arbre.

## Conséquences

### Positives

* Un test ne dit que ce qui lui importe ; une règle qu'un paramètre doit satisfaire est écrite une
  fois, dans le générateur, et la changer là change tous les tests à la fois.
* L'appel qui remplace la base et perd la règle ne compile plus.
* Sur une feuille, un affinage qui contredit la base est refusé là où il est écrit, à chaque exécution.
* La surface émise ne grandit pas : chaque paramètre garde deux surcharges `With`.

### Négatives

* Chaque paramètre résolu porte une fabrique, donc le fichier émis est plus long. Cela renverse la
  décision de l'ADR-0096, et la conséquence de l'ADR-0089 selon laquelle un paramètre composé ne porte
  aucune méthode.
* Un paramètre composé dont le générateur n'existe pas encore est nommé à plusieurs endroits du
  fichier, donc le build signale le type manquant plus d'une fois.
* Tirer parmi des valeurs choisies, changer la recette des éléments d'une collection et tirer `null`
  de temps en temps demandent une surcharge que le développeur écrit dans son fichier partiel.
* Les fichiers scaffoldés avant cette décision gardent la surcharge de remplacement ; un nouveau
  scaffold la retire, et un appel qui l'utilise encore cesse alors de compiler.
* Les justifications d'ADR-0061 et d'ADR-0064 nomment la surcharge de remplacement ; leurs décisions
  tiennent, mais les chemins qu'elles décrivaient passent désormais par la portée ambiante, la
  surcharge de valeur et le fichier partiel.

### Risques

* Un développeur qui comptait sur la surcharge de remplacement peut lire son absence comme une
  régression. La documentation doit enseigner chaque chemin restant, pas à pas.
* Un affinage garantit son point de départ, pas son résultat : une lambda qui écrase ce que la base a
  fixé pour une valeur composée est le choix du test, et peut encore produire une valeur que le
  constructeur composant refuse.

## Actions de suivi

* Enseigner les deux surcharges et chaque chemin que servait celle de remplacement sur la page
  `justdummies-cli`.
* Guetter un retour du terrain sur un besoin que les chemins restants servent mal ; la liste des
  besoins jugés rares est ci-dessus.

## Références

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — borner l'ambition, jamais la correction.
* [ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.fr.md) — le fichier appartient au développeur, partiel compris.
* [ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.fr.md) — des générateurs immuables.
* [ADR-0061](0061-draw-from-the-ambient-context-and-hold-no-state.fr.md) — le contexte ambiant.
* [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.fr.md) — ne jamais tirer null par défaut.
* [ADR-0089](0089-draw-a-composed-parameter-through-the-generator-its-type-owns.fr.md) — le générateur que possède un type composé.
* [ADR-0096](0096-emit-a-parameter-inline-whenever-it-has-nothing-to-report.fr.md) — l'émission en ligne que cet enregistrement supersède.
* Issue [#273](https://github.com/Reefact/just-dummies/issues/273).
