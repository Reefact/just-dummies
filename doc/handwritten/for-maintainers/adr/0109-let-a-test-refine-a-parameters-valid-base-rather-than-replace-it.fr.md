# ADR-0109 | Laisser un test affiner la base valide d'un paramètre plutôt que la remplacer

🌍 🇬🇧 [English](0109-let-a-test-refine-a-parameters-valid-base-rather-than-replace-it.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-10-27
**Accepted:** 2026-10-27
**Decision Makers:** Reefact

Supersède l'[ADR-0096](0096-emit-a-parameter-inline-whenever-it-has-nothing-to-report.fr.md).

> Les renvois de section (§N) pointent vers la [spécification de `any`](../specifications/justdummies-tool.fr.md).

## Contexte

Un `AnyX` scaffoldé tire chaque paramètre du constructeur à partir d'une recette inférée par le
moteur : une ligne de la table de base, resserrée par les gardes du constructeur (§5.2, §5.3), ou,
pour un paramètre composé, le générateur propre à son type (§5.4, [ADR-0089](0089-draw-a-composed-parameter-through-the-generator-its-type-owns.fr.md)). Cette recette constitue
la **base valide** du paramètre : ce que le constructeur accepte.

L'endroit où cette base est écrite dépend de ce que le paramètre a à signaler ([ADR-0096](0096-emit-a-parameter-inline-whenever-it-has-nothing-to-report.fr.md)). Un paramètre
dont une garde a resserré la chaîne, ou que bloque l'un des deux marqueurs des §5.5 et §5.6, reçoit
une fabrique privée appelée par le constructeur ; tous les autres sont écrits directement dans
l'initialiseur du constructeur, sous la forme d'un appel unique, car une méthode qui ne ferait
qu'envelopper cet appel n'apporterait rien.

Une règle peut appartenir au type composant plutôt qu'au type composé. Un `Shipment` peut n'accepter
qu'une `Route` nationale, alors qu'une `Route` prise isolément peut être internationale. Le moteur ne
sait pas appliquer une telle règle à un paramètre composé : il marque le paramètre (§5.6), et le
développeur écrit la règle dans le fichier, qui lui appartient ([ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.fr.md)).

Chaque paramètre disposait de deux surcharges `With` : l'une fixait une valeur, l'autre acceptait un
`IAny<T>` qui **remplaçait** la recette. Un test qui ne s'intéressait qu'à une partie d'une valeur —
la distance d'une route, une borne maximale pour une quantité — n'avait d'autre choix que la seconde,
et devait construire un générateur de toutes pièces. Ce générateur ne reprend pas la base : le test
doit répéter la règle, ou il la perd.

Un test qui la perd compile quand même, et tire des valeurs : il réussit chaque fois que le tirage
respecte la règle par hasard, et échoue lors des autres exécutions, pour une raison sans rapport avec
ce qu'il vérifie. Or un test qui n'échoue que de temps en temps est précisément ce que la bibliothèque
cherche à éliminer.

Les générateurs de la bibliothèque sont des recettes immuables : chaque contrainte renvoie une
nouvelle instance ([ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.fr.md)). Sur un générateur de la bibliothèque, les contraintes se cumulent, et
deux contraintes contradictoires sont refusées dès leur déclaration, avec un message qui cite les
deux. Une contrainte peut renvoyer un type de générateur différent de celui sur lequel elle est
appelée.

L'analyseur qui signale une contrainte dont le résultat est ignoré, `JD006`, ne signale pas un appel
qui constitue à lui seul le corps d'une lambda passée en argument, afin qu'un test qui vérifie un
refus puisse écrire l'appel à cet endroit.

La justification de l'[ADR-0061](0061-draw-from-the-ambient-context-and-hold-no-state.fr.md) comptait sur la surcharge `IAny<T>` pour un développeur qui tire ses
valeurs d'un contexte isolé `Any.WithSeed`, paramètre par paramètre ; la portée ambiante,
`Any.UseSeed`, couvre tous les tirages du générateur émis, y compris ceux des paramètres composés. La
justification de l'[ADR-0064](0064-never-draw-null-for-a-nullable-parameter.fr.md) comptait sur la même surcharge pour permettre à un test d'utiliser un
générateur qui renvoie parfois `null`.

La bibliothèque fait correspondre un `AnyX` scaffoldé à la Creation Method anonyme et paramétrée
d'*xUnit Test Patterns* (`core-concepts`) : ses méthodes `With` en sont les paramètres.

## Décision

Chaque paramètre résolu d'un générateur scaffoldé est tiré d'une fabrique unique qui contient sa base
valide ; un test fixe sa valeur ou affine cette base au moyen d'une lambda, mais ne la remplace jamais.

## Justification

Le problème n'était pas qu'un test puisse construire un générateur, mais qu'en construire un
l'obligeait à repartir de zéro. Une Creation Method existe pour qu'un test n'ait pas à savoir
construire un objet valide, ni les objets dont celui-ci dépend. Avec la surcharge de remplacement,
chaque test qui paramétrait une valeur était obligé de connaître et de répéter ces règles de
construction : une règle écrite une seule fois dans le générateur se retrouvait recopiée dans chacun
de ces tests, où elle paraît aussi délibérée que la valeur réellement testée — et où l'oublier ne
provoque aucune erreur de compilation. Affiner part au contraire de la base : le test n'exprime que
ce qui l'intéresse, et la règle reste définie à un seul endroit.

C'est la fabrique unique qui garantit cet endroit unique. La recette par défaut et l'affinage doivent
partir de la même base, faute de quoi le développeur qui renforce l'une laisse l'autre inchangée. Et
cette base doit se trouver là où le développeur intervient déjà : la base écrite par le moteur, ou la
règle qu'il a ajoutée là où le moteur le lui demandait. Un affinage partant d'un générateur neuf
compilerait, laisserait croire qu'il part d'une base valide, et ne ferait que déplacer l'échec
intermittent du test vers le code généré.

C'est aussi pourquoi chaque paramètre résolu dispose désormais d'une fabrique, et pourquoi cette
décision remplace l'ADR-0096. L'ADR-0096 avait raison de considérer qu'une méthode enveloppant un seul
appel n'apportait rien, tant que le constructeur était son unique appelant. Dès qu'un second appelant
existe, la fabrique indique ce qu'un lecteur a le plus besoin de trouver : l'emplacement de la base du
paramètre, pour la valeur par défaut comme pour chaque affinage. Un paramètre écrit directement dans
l'initialiseur obligerait l'affinage soit à dupliquer cet appel, soit à partir de rien.

La lambda renvoie le générateur affiné au lieu de modifier celui qu'elle reçoit, car les générateurs
sont immuables et une méthode `With` qui modifierait l'instance contredirait le contrat implémenté
par le type émis (ADR-0057). Ce choix rend aussi visible la seule erreur encore possible : pour
ignorer une contrainte, une lambda doit l'écrire comme une instruction dans un bloc, ce que `JD006`
signale, alors qu'une forme mutable ferait disparaître tout l'affinage, précisément sous la forme
que `JD006` ne signale pas.

Pour une valeur simple, affiner est plus sûr que répéter, et pas seulement plus court : les
contraintes de la bibliothèque se cumulent, si bien qu'un affinage qui contredit la base échoue là où
il est écrit, à chaque exécution, avec un message qui cite les deux contraintes. C'est l'exigence de
correction de la bibliothèque, appliquée au site d'appel ([ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md)).

Supprimer la surcharge de remplacement, plutôt que de la conserver à côté de la surcharge
d'affinage, est ce qui transforme l'échec en erreur de compilation. Si les deux coexistaient, rien au
site d'appel ne permettrait de les distinguer, et celle qui perd la règle resterait celle vers
laquelle un lecteur se tourne naturellement. Les besoins qu'elle couvrait disposent tous d'une autre
solution : fixer une valeur couvre le test qui s'intéresse à la valeur entière, `null` compris ; la
portée de seed ambiante couvre le déterminisme hors d'un test mieux que des générateurs de contexte
passés paramètre par paramètre, puisqu'elle atteint aussi les paramètres composés ; enfin, les autres
cas — tirer parmi quelques valeurs choisies, tirer autrement les éléments d'une collection, tirer
`null` de temps en temps pour un paramètre nullable — sont assez rares pour que le développeur ajoute
la surcharge dans son propre fichier partiel. Elle y devient un choix fait pour un paramètre précis,
et non un comportement proposé par défaut pour tous.

La surcharge par valeur est conservée. Un test qui s'intéresse à la valeur entière doit pouvoir le
dire, et une valeur fixée qui enfreint une règle échoue à chaque exécution, et non de temps en
temps : c'est exactement l'échec visible que recherche cette décision.

## Alternatives considérées

### Un `Action<T>` de configuration, sur le modèle du patron options

Envisagé parce que c'est la forme habituelle de la configuration imbriquée en .NET, et qu'une lambda
à bloc peut alors enchaîner plusieurs ajustements sans rien renvoyer.

Rejeté parce que les générateurs émis sont immuables : l'appel le plus naturel compile, mais
l'ajustement est perdu sans avertissement, sous la forme même que `JD006` ne signale pas. Rendre
`With` mutable remettrait en cause l'ADR-0057.

### Faire partir la lambda d'un nouveau `new AnyX()`

Envisagé parce qu'aucune fabrique ne serait alors nécessaire pour un paramètre composé, ce qui
préserverait la forme émise décrite par l'ADR-0089.

Rejeté parce que la base écrite par le développeur serait ignorée. La lambda fournirait au test une
valeur arbitraire tout en laissant croire qu'elle part d'une base valide.

### Conserver les paramètres écrits en ligne de l'ADR-0096 et répéter leur appel dans la surcharge d'affinage

Envisagé parce que le fichier émis resterait aussi court qu'avant, avec une fabrique uniquement pour
les paramètres qui ont quelque chose à signaler.

Rejeté parce que la base serait écrite deux fois dans le même fichier. Le développeur qui modifie
l'appel dans l'initialiseur du constructeur laisserait la copie de l'affinage inchangée : c'est
précisément la divergence qu'un emplacement unique doit empêcher.

### Partir de la recette courante, après un appel `With` antérieur

Envisagé parce que des affinages successifs pourraient alors se combiner.

Rejeté parce que le champ devrait conserver le type concret du générateur, et qu'une fois une valeur
fixée, il ne reste plus de générateur d'où partir. Pour toutes les autres méthodes `With`, c'est le
dernier appel qui l'emporte ; il en va de même pour celle-ci.

### Conserver la surcharge de remplacement à côté de celle d'affinage

Envisagé parce que tous les appels existants continueraient de compiler.

Rejeté parce que la surcharge qui perd la règle resterait à portée de main de celle qui la conserve,
sans que rien au site d'appel permette de les distinguer.

### Conserver la surcharge de remplacement pour les seuls paramètres nullables

Envisagé parce qu'un type valeur nullable tiré `null` de temps en temps ne peut pas passer par une
lambda qui doit renvoyer le générateur qu'elle reçoit.

Rejeté parce que le fichier partiel couvre ce besoin en quelques lignes, et qu'une forme identique
pour tous les paramètres est plus simple à lire et à enseigner qu'une forme qui dépend du type du
paramètre.

### Limiter la décision aux paramètres composés

Envisagé parce que c'est sur la règle d'un type composant que le problème est d'abord apparu.

Rejeté parce qu'une valeur simple présente le même défaut : remplacer un `Positive()` issu d'une
garde le fait disparaître exactement comme remplacer une base composée fait disparaître une règle
écrite par le développeur.

### Propager un contexte isolé à travers les générateurs

Envisagé parce que `Any.WithSeed` pourrait alors atteindre chaque paramètre, ce qui était le cas
couvert par la surcharge de remplacement.

Rejeté parce qu'il faudrait une seconde copie de chaque recette et revenir sur l'ADR-0061, alors que
la portée ambiante `Any.UseSeed` atteint déjà l'ensemble de l'arbre.

## Conséquences

### Positives

* Un test n'exprime que ce qui l'intéresse ; une règle qu'un paramètre doit respecter est écrite une
  seule fois, dans le générateur, et la modifier à cet endroit modifie tous les tests à la fois.
* L'appel qui remplace la base et perd la règle ne compile plus.
* Pour une valeur simple, un affinage qui contredit la base est refusé là où il est écrit, à chaque
  exécution.
* La surface émise n'augmente pas : chaque paramètre conserve deux surcharges `With`.

### Négatives

* Chaque paramètre résolu dispose d'une fabrique, ce qui allonge le fichier émis. Cela revient sur la
  décision de l'ADR-0096, ainsi que sur la conséquence de l'ADR-0089 selon laquelle un paramètre
  composé ne porte aucune méthode.
* Un paramètre composé dont le générateur n'existe pas encore est nommé à plusieurs endroits du
  fichier : le build signale donc le type manquant plusieurs fois.
* Tirer parmi des valeurs choisies, tirer autrement les éléments d'une collection et tirer `null` de
  temps en temps nécessitent une surcharge que le développeur écrit dans son fichier partiel.
* Les fichiers scaffoldés avant cette décision conservent la surcharge de remplacement ; une nouvelle
  génération la supprime, et tout appel qui l'utilise encore cesse alors de compiler.
* Les justifications des ADR-0061 et ADR-0064 mentionnent la surcharge de remplacement ; leurs
  décisions restent valables, mais les solutions qu'elles décrivaient passent désormais par la portée
  ambiante, la surcharge par valeur et le fichier partiel.

### Risques

* Un développeur qui utilisait la surcharge de remplacement peut percevoir sa disparition comme une
  régression. La documentation doit présenter chaque solution restante, étape par étape.
* Un affinage garantit son point de départ, pas son résultat : une lambda qui écrase ce que la base
  avait fixé pour une valeur composée relève du choix du test, et peut encore produire une valeur que
  le constructeur du type composant refuse.

## Actions de suivi

* Présenter sur la page `justdummies-cli` les deux surcharges, ainsi que chaque solution qui prend le
  relais de la surcharge de remplacement.
* Rester attentif aux retours du terrain sur un besoin que les solutions restantes couvriraient mal ;
  la liste des besoins jugés rares figure ci-dessus.

## Références

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — borner l'ambition, jamais la correction.
* [ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.fr.md) — le fichier appartient au développeur, partiel compris.
* [ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.fr.md) — des générateurs immuables.
* [ADR-0061](0061-draw-from-the-ambient-context-and-hold-no-state.fr.md) — le contexte ambiant.
* [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.fr.md) — ne jamais tirer null par défaut.
* [ADR-0089](0089-draw-a-composed-parameter-through-the-generator-its-type-owns.fr.md) — le générateur que possède un type composé.
* [ADR-0096](0096-emit-a-parameter-inline-whenever-it-has-nothing-to-report.fr.md) — l'émission en ligne que cette décision remplace.
* Issue [#273](https://github.com/Reefact/just-dummies/issues/273).
