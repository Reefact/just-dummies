# ADR-0060 | Amorcer les generators sur les gardes du constructeur, et laisser le reste en erreur de compilation

🌍 🇬🇧 [English](0060-seed-generators-from-constructor-guards.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

> Les renvois de section (§N) pointent vers la [spécification de `dum`](../specifications/justdummies-tool.fr.md), le document dont cet enregistrement a été extrait.

## Contexte

Les generators non contraints tirent tout leur domaine : celui des chaînes produit de zéro à seize
caractères, donc il peut retourner la chaîne vide, et celui des entiers tire tout l'intervalle,
négatifs compris (§14.5).

Les constructeurs métier rejettent couramment une partie de ce domaine.

Cela a été mesuré sur une vraie fabrique validante de ce dépôt : un generator de chaînes non
contraint composé dessus a levé 594 fois sur 10 000 tirages, et 557 lors d'une reprise
indépendante — environ une fois sur dix-sept, le taux que prédit un tirage non contraint sur les
longueurs de 0 à 16 (§17).

Les clauses de garde en tête de constructeur sont l'idiome de validation dominant dans le code que
ce tool vise.

Le tool dispose du corps du constructeur en source pour tout type de la solution du développeur, et
n'en dispose pas pour un type venant d'un package.

Certains invariants ne sont pas exprimés comme des gardes du tout — validation déléguée à une
méthode auxiliaire, à une bibliothèque de gardes, ou règle portant sur deux paramètres.

Le développeur lance le tool et ouvre le fichier obtenu dans la même minute.

## Décision

Le moteur dérive les contraintes d'un ensemble clos de clauses de garde de constructeur reconnues,
et émet un identifiant inexistant pour tout paramètre dont il ne peut pas inférer le generator.

## Justification

Sans lecture des gardes, la sortie par défaut du tool n'est pas seulement imprécise, elle est
nuisible : elle fabrique, dans la suite de tests du développeur, l'échec intermittent que la
bibliothèque existe pour éliminer. Un échec sur dix-sept est pire que pas d'outil du tout, parce qu'il
discrédite la bibliothèque à l'instant du premier usage.

Un ensemble clos et syntaxique borne le risque. Lire des gardes n'est pas inférer une intention ;
chaque forme reconnue se projette sur exactement une contrainte, et tout ce qui est hors de
l'ensemble est ignoré. L'appariement conservateur — un paramètre, aucune composition booléenne, des
opérandes constants — sous-signale plutôt qu'il ne se trompe, ce qui est le bon biais ici : une
contrainte manquante donne une valeur que le constructeur peut rejeter et un échec visible, tandis
qu'une contrainte fausse donne une valeur qui exerce mal le test en silence.

Pour les paramètres qui restent non résolus, une erreur de compilation est le signal le moins cher
disponible. Le développeur est dans le fichier, venant de lancer le tool ; le compilateur nomme le
paramètre dans son propre message, et ce message atteint aussi bien l'éditeur, la liste d'erreurs
que l'intégration continue. Un signal délivré plus tard coûte plus, et un signal jamais délivré
coûte le plus.

Publier un fichier qui ne compile pas n'est défendable qu'à cause de [ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.fr.md). Un outil qui possède sa
sortie ne le pourrait pas ; un outil qui remet un squelette le peut, et énoncer le manque
franchement est plus honnête qu'un fichier qui compile et échoue plus tard.

## Alternatives considérées

##### Des generators neutres, tout le resserrement laissé au développeur

Considérée parce qu'elle fait que le tool n'affirme rien qu'il ne puisse prouver, ce qui est
séduisant pour une bibliothèque bâtie sur la précision.

Écartée sur la mesure. La sortie par défaut échouerait par intermittence pour la plupart des
constructeurs validants, ce qui est le mode d'échec le plus coûteux disponible et celui que la
bibliothèque a été construite pour supprimer.

##### Une exception à l'exécution pour les paramètres non résolus

Considérée parce que le fichier compile alors, ce qui est plus avenant à première vue.

Écartée parce qu'elle reporte le signal au-delà du moment où le développeur regarde le fichier, et
convertit un manque de scaffolding en un test en échec dont la cause est une ligne qu'il n'a jamais
lue.

##### Omettre du recipe le paramètre non résolu

Considérée parce que c'est la plus élégante des trois : le generator exigerait simplement du
développeur qu'il fournisse ce paramètre.

Écartée parce qu'elle est silencieuse. Le generator devient partiellement utilisable sans le dire,
et le manque remonte comme un null ou un défaut au fond d'un test.

##### Un fichier de déclaration associant des types à leur construction

Considérée parce qu'elle permettrait au développeur d'enseigner le tool une fois pour toutes,
couvrant des invariants qu'aucune garde n'exprime, et rendrait la composition correcte pour les
value objects en général plutôt que pour les seuls gardés.

Écartée pour la première version parce qu'elle convertit le tool en système de conventions, ce qui
contredit la règle de conception voulant que rien ne soit configuré avant le premier usage. Laissée
ouverte au §16.

## Conséquences

**Positives.** Le défaut émis fonctionne pour l'idiome de validation dominant. Les paramètres non
résolus sont impossibles à manquer.

**Négatives.** Un fichier scaffoldé peut ne pas compiler tant qu'il n'est pas édité, ce qui
surprendra quiconque attend d'un scaffolding qu'il produise du code fonctionnel. Les invariants hors
de l'ensemble reconnu donnent toujours des valeurs que le constructeur rejette.

**Risques.** L'ensemble reconnu peut apparier une garde dont il se méprend sur le sens, produisant
une contrainte fausse plutôt qu'absente — le seul résultat pire que de ne rien inférer. Atténué par
les conditions d'appariement conservatrices et la règle de conflit sur le même axe ; le test sur le
code du dépôt (§12) est le contrôle le plus susceptible de l'attraper, parce qu'il fait tourner
l'émetteur sur du code écrit pour d'autres raisons.

## Actions de suivi

* Tout ajout à l'ensemble de gardes reconnues demande un cas dans la suite du résolveur et, quand
  c'est possible, une occurrence dans le test sur le code du dépôt.

## Références

* §5.3, §5.5, §9, §14.5, §17 de cette spécification.

---
