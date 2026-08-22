# ADR-0082 | Répondre de la chaîne finie, pas de chaque contrainte lue

🌍 🇬🇧 [English](0082-answer-for-the-finished-chain-not-each-constraint.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-22
**Accepted:** 2026-08-22
**Decision Makers:** Reefact

> Les références de section (§N) pointent vers la [spécification `dum`](../specifications/justdummies-tool.fr.md).

## Contexte

Le moteur de scaffolding dérive des contraintes d'un ensemble fermé de clauses de garde de
constructeur ([ADR-0060](0060-seed-generators-from-constructor-guards.fr.md)) et écrit les
survivantes sur le generator que la table de base a choisi pour le type du paramètre (§5.2, §5.3).

Jusqu'ici il les écrivait une à une. La composition posait une seule question — deux contraintes
fixent-elles la même borne, et une borne inférieure est-elle au-dessus d'une supérieure — et émettait
ce qui passait. Le modèle de contrainte compte six sortes de borne ; cette question en lisait deux.

La table de base sème aussi des contraintes qui lui sont propres. Un paramètre `string` est tiré non
vide parce qu'un type de domaine l'exige massivement, et cette semence est composée à côté de celles
issues des gardes (§5.2). C'est le défaut du moteur, pas quelque chose que le développeur a écrit.

Cinq formes ont été mesurées contre le moteur livré. Une taille exacte à côté d'une borne qui
l'exclut, et une contrainte de signe contre une borne opposée, produisaient des chaînes que la
bibliothèque refuse à la construction. Une garde exigeant une chaîne vide produisait une chaîne
contredisant la semence de la table de base, sans que rien du développeur soit en cause. Deux gardes
bornant le même côté étaient toutes deux écartées, perdant un invariant lu correctement. Et un
plancher avec un plafond produisait l'écriture en deux bornes que `JD031` nomme, dans chacune des
familles que le moteur émet.

L'[ADR-0058](0058-leave-the-scaffolded-file-open-to-the-analyzers.fr.md) enregistre que les analyzers
servent de filet à cette classe de défaut, et énonce que la collision sur un même axe est *la seule
façon* dont l'émetteur peut produire une chaîne que la bibliothèque rejette. Cette affirmation s'est
révélée incomplète : sur les cinq formes, quatre ne levaient aucun diagnostic et n'étaient visibles
qu'en construisant le generator émis et en y tirant.

L'[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) borne ce que ce dépôt
tente et exige un refus nommé à cette frontière ; il ne dit pas où se situe la frontière pour une
chaîne que le moteur a lui-même assemblée.

## Décision

Le moteur de scaffolding répond de ce que dit la chaîne finie, en conciliant les contraintes qu'il a
lues comme un seul intervalle sur une table fixe de bornes plutôt qu'en les émettant l'une après
l'autre.

## Justification

**Une contrainte que le développeur n'a jamais écrite, c'est ce que le moteur a assemblé.** Chaque
garde était lue correctement dans chacune des formes mesurées ; ce qui parvenait au développeur était
la *combinaison*, et personne n'en répondait. Un outil qui lit cinq invariants correctement et émet
un generator qui lève n'a pas été prudent, il a été absent — et la défaillance atterrit dans la suite
de tests du développeur, c'est-à-dire dans la fragilité même que cette fonctionnalité existe pour
supprimer (ADR-0060).

**Concilier n'est pas deviner.** Deux gardes qui lèvent toutes deux forment une conjonction : une
valeur doit satisfaire les deux, donc la borne la plus serrée est la seule chose qu'elles puissent
vouloir dire ensemble, et les écarter toutes deux jetait un invariant que le moteur avait déjà
compris. La même lecture règle le reste — une taille exacte est un plancher et un plafond sur une
seule valeur, un signe est une arête à zéro, la non-vacuité est un plancher à un — si bien que cinq
défauts d'apparence distincte sont une seule question posée cinq fois.

**Un défaut doit céder devant une déclaration.** Le raffinement de la table de base est une opinion
sur ce qu'un paramètre `string` veut d'ordinaire ; une garde est ce que ce constructeur-ci énonce. Là
où les deux ne peuvent tenir ensemble, un seul peut avoir tort, et ce n'est pas celui que le
développeur a écrit. Les traiter en pairs faisait fabriquer au moteur une contradiction qu'il
signalait ensuite comme celle du développeur, ce qui est pire que chacune des deux moitiés.

**Écrire ce qu'il a lu n'est pas obéir à une règle.** L'écriture en deux bornes est légale, documentée
et décomposable à dessein ([ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.fr.md)), et
`JD031` la signale comme information, non comme faute. Le moteur émet la forme d'intervalle parce
qu'on lui a dit un intervalle, non parce qu'une règle l'a demandé — et le même raisonnement excuse
`JD030` sur la même sortie, là où le domaine n'a énoncé aucune longueur et où le moteur n'en inventera
pas. Un diagnostic informationnel sur du code émis relit l'intention du moteur ; il ne la révoque pas.

**La frontière est une table, et la nommer est ce qui garde l'ADR-0046 intact.** De l'arithmétique
d'intervalles sur un ensemble fixe de bornes, plus les deux domaines d'élément que les analyzers
tranchent déjà, est un travail borné au coût connu — ni propagation de contraintes, ni solveur. Tout
ce qui est au-delà reste refusé, et un refus est signalé plutôt qu'approximé : là où les contraintes
conciliées n'admettent aucune valeur, ou nomment une taille que la bibliothèque ne produira pas, le
paramètre garde son generator neutre et le récapitulatif le dit.

**Le filet ne peut pas tenir lieu de test.** La couverture de l'ADR-0058 est réelle et elle s'est
déclenchée ici, mais quatre formes sur cinq y étaient muettes, et rien dans la suite ne surveillait la
cinquième. Une classe de défaut que son propre filet ne voit qu'en partie doit être mesurée
directement, en tirant depuis ce qui a été émis.

## Alternatives envisagées

##### Laisser la composition en l'état et laisser les analyzers en signaler le résultat

Envisagée parce que l'ADR-0058 fait déjà analyser le fichier scaffoldé, et parce qu'un diagnostic dans
l'éditeur du développeur est un vrai signal délivré à un vrai moment.

Rejetée sur la mesure. Quatre des cinq formes ne lèvent rien du tout, donc le filet attrape une partie
de la classe et ne dit rien du reste ; et là où il se déclenche, il se déclenche sur l'écran du
développeur à propos d'un fichier que l'outil vient d'écrire, ce qui dépense son attention sur
l'erreur de l'outil.

##### Faire exécuter les analyzers par le moteur sur la chaîne qu'il vient de composer

Envisagée parce qu'elle n'exige aucun second modèle des contraintes : les règles existent déjà, elles
sont déjà livrées, et le moteur pourrait les interroger directement plutôt que de raisonner lui-même
sur des bornes.

Rejetée pour trois raisons. Le moteur ne peut pas référencer le package
([ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.fr.md)), il devrait donc charger les
analyzers du consommateur — ce qui ferait dépendre le fichier émis de la version de la bibliothèque
que ce projet résout, et briserait l'identité octet pour octet que le §8.1 promet. Les règles sont
muettes sur quatre des cinq formes, elle ne suffirait donc même pas. Et réparer une chaîne en relisant
des diagnostics est un mécanisme que personne ne peut raisonner, ce que l'ADR-0046 refuse précisément.

##### Refuser toute chaîne que le moteur ne peut pas concilier entièrement

Envisagée parce que refuser est la réponse correcte la moins chère et celle que l'ADR-0046 privilégie,
et qu'elle n'exige aucune arithmétique d'intervalles.

Rejetée parce que c'est ce que le moteur faisait déjà, et que c'est ainsi que l'invariant a été perdu :
deux gardes bornant le même côté ne sont pas inconciliables, et les écarter jetait un fait que le
moteur avait lu correctement. Le refus est juste là où rien ne survit, pas là où quelque chose survit.

## Conséquences

### Positives

* Une chaîne que la bibliothèque refuse à la construction n'est plus émise pour aucune forme que le
  corpus couvre, et ce corpus est la première fixture de ce dépôt à porter des gardes.
* Le moteur énonce un invariant une fois. Un plancher à huit dit déjà non vide, et les deux étaient
  écrits.
* Le récapitulatif distingue une contrainte appliquée d'une contrainte seulement lue, ce que le modèle
  précédent ne savait pas exprimer.

### Négatives

* Le moteur porte un second modèle de ce que la bibliothèque accepte, et une copie de son plafond de
  taille productible, puisque l'ADR-0063 interdit de demander. Les deux sont tenus à l'original par
  des tests plutôt que par le système de types.
* La sortie change pour des gardes qui composaient déjà : un plancher et un plafond émettent
  désormais un appel au lieu de deux, et une semence redondante disparaît. Toute attente enregistrée
  bouge avec.

### Risques

* La table est une frontière que quelqu'un voudra élargir. Chaque élargissement est une nouvelle
  affirmation sur ce que le moteur sait décider, et l'attrait de la propagation est exactement ce que
  l'ADR-0046 existe pour tenir à distance.
* Deux domaines d'élément sont tranchés — un booléen et les membres déclarés d'un enum. Un domaine
  indémontrable ne doit jamais être traité comme petit, sous peine de refuser une chaîne légale.

## Actions de suivi

* Étendre l'ensemble fermé du §5.3 aux membres d'exclusion d'enum que la bibliothèque porte déjà, ce
  qui ferait passer la dernière forme du corpus de refusée à tirée.
* Relire les règles informationnelles que le corpus assume chaque fois que l'une est ajoutée, la liste
  étant un jugement sur les intentions du moteur plutôt que sur la sévérité.

## Références

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — la frontière que ceci
  affine pour une chaîne que le moteur a assemblée lui-même.
* [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) — ce qui est lu, et pourquoi le lire
  compte.
* [ADR-0058](0058-leave-the-scaffolded-file-open-to-the-analyzers.fr.md) — la couverture dont ceci
  montre qu'elle est un filet partiel et non complet.
* [ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.fr.md) — pourquoi le moteur
  modélise au lieu de demander.
* [ADR-0076](0076-let-a-declared-maximum-steer-the-size-draw.fr.md) — le plafond de production que le
  moteur reproduit.
* [ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.fr.md) — pourquoi l'écriture en deux
  bornes est une information et non une faute.
