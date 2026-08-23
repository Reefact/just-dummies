# ADR-0084 | Placer une garde par portée syntaxique, non par un graphe de flot de contrôle

🌍 🇬🇧 [English](0084-place-a-guard-by-syntax-reach-not-a-control-flow-graph.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-23
**Accepted:** 2026-08-23
**Decision Makers:** Reefact

> Les références de section (§N) pointent vers la [spécification `dum`](../specifications/justdummies-tool.fr.md).

## Contexte

Le §5.3 lit les gardes de tête d'un constructeur et resserre le generator en conséquence. Une garde
dit quelque chose de la valeur que le generator tire exactement lorsqu'aucune écriture de son
paramètre n'a pu s'exécuter là où elle se trouve ; le moteur pose donc cette question de chaque garde
avant de la lire, et marque le paramètre `unread guards` quand la réponse est oui — ce qui bloque la
compilation ([ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.fr.md)).

La question se répond en deux moitiés. **Quelles écritures existent** va à l'analyse de flot de
données du compilateur sur une région syntaxique, qui répond pour toutes les orthographes à la
fois — une déconstruction, un argument `out`, un local `ref` aliasant le paramètre — y compris celles
que personne n'a pensé à lister. **Où elles se situent** se répond en remontant depuis la garde et en
collectant les régions qui ont fini : les instructions au-dessus d'elle à chaque niveau
d'imbrication, la condition de chaque `if` sous lequel elle se trouve, les arguments d'un
`: this(…)` ou d'un `: base(…)`, et — pour tout autre construct — ce construct entier.

Cette dernière partie est la règle, et non un repli pour les formes que personne n'a listées ; sa
sûreté tient à une propriété : une région qui est un surensemble ne peut qu'ajouter des refus,
jamais en retirer un. Un construct que personne n'a énuméré coûte donc une contrainte, jamais une
contrainte fausse, et la règle tient pour les constructs que C# n'a pas encore fait naître. Le §5.3
nomme le prix sans détour — une garde à l'intérieur d'un `try`, d'un `switch` ou d'un `using` dont le
construct n'écrit le paramètre qu'*après* cette garde est refusée alors qu'elle était lisible.

Roslyn expose son propre modèle de cette question exacte,
`Microsoft.CodeAnalysis.FlowAnalysis.ControlFlowGraph`. Il est disponible en dessous du plancher
Roslyn épinglé du moteur, et il est livré dans `Microsoft.CodeAnalysis.Common`, déjà une dépendance
transitive : l'adopter n'ajouterait aucune référence de paquet. Il a été évalué pendant la
construction de la règle de placement et n'a pas été retenu ; jusqu'à cet enregistrement, ce
raisonnement n'existait que dans un message de commit.

Quatre faits bornent ce que son adoption pourrait apporter.

**Le gain atteignable est plus étroit que le prix ne le laisse croire.** Une instruction de tête
n'est lue comme chaîne de gardes que si c'est un `if`. Un `try`, un `switch` ou un `using` est traité
par un autre chemin, qui marque tout paramètre qu'il mentionne comme `unread guards` dès que
l'instruction contient un `throw` où que ce soit — sans consulter la règle de placement du tout. Un
`if (…) { throw … }` à l'intérieur de ces constructs est donc déjà refusé pour une raison étrangère.
Ce que le placement pourrait rattraper est l'intersection de quatre conditions : un assistant de
levée reconnu, à l'intérieur d'un tel construct, où le construct ne porte aucun `throw`, et où le
même paramètre est écrit plus loin dans ce même construct.

**Roslyn ne modélise pas l'entrée dans un gestionnaire d'exception comme une branche ordinaire.** Une
région `finally` est atteinte par un mécanisme que le graphe décrit à part des successeurs de ses
blocs, et un bloc situé dedans n'a aucune arête de prédécesseur par où remonter.

**La version de Roslyn du moteur est un contrat de chargement.**
L'[ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.fr.md) compile le moteur
contre le plancher pour qu'un compilateur hôte puisse le charger en processus ; l'outil lui-même
héberge un compilateur plus récent, et un IDE en hébergerait encore un autre. L'analyse de flot de
données sur une région source répond pareil sous tous. La façon dont un graphe de flot de contrôle
découpe ses blocs, et le nœud syntaxique auquel une opération synthétisée est attribuée, sont des
détails d'implémentation d'un Roslyn donné plutôt qu'un contrat versionné.

**Rien ne mesure ce prix aujourd'hui.** Aucun test de la suite du moteur ne changerait de verdict si
le placement devenait plus précis, aucune forme du corpus gardé n'en a besoin, et aucun constructeur
issu d'un vrai code n'a été signalé que la règle refuse et dont l'auteur s'en soit plaint.

Les observations sur Roslyn ci-dessus ont été lues dans les métadonnées disponibles là où la question
a été étudiée, un assemblage 5.x, plutôt qu'obtenues en exécutant un graphe au plancher épinglé.

## Décision

Le placement d'une garde se répond depuis la portée syntaxique — les régions qui ont fini quand la
garde est évaluée, tout construct dont l'ordre n'est pas lu étant interrogé entier — et non depuis le
graphe de flot de contrôle de Roslyn.

## Justification

**Ce qui se joue est la direction du défaut, pas la précision.** Le parcours syntaxique répond d'un
construct non modélisé en l'interrogeant entier, ce qui sur-approxime et donc refuse. Un ensemble
d'atteignabilité répond d'une arête qu'il ne porte pas par le silence, et le silence se lit *aucune
écriture n'a tourné* — la seule réponse qui transforme une garde que le moteur ne sait pas placer en
une garde qu'il émet. Oublier un cas cesserait de coûter une contrainte pour coûter une contrainte
fausse, rapportée comme inférée. C'est l'axe sur lequel se tiennent l'[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md)
et l'ADR-0083, et il se tranche avant toute question de précision.

**La formulation qui éviterait d'énumérer les genres d'opération est non sûre sur la forme même pour
laquelle la règle existe.** Garder l'analyse de flot de données pour *quelles écritures existent* et
n'utiliser le graphe que pour l'*atteignabilité* est la seule formulation qui ne réintroduit pas
l'énumération des orthographes. Mais parce que l'entrée dans un gestionnaire n'est pas une branche
ordinaire, un bloc situé dans un `finally` n'a aucun prédécesseur par où remonter, et la formulation
conclut que rien n'a précédé une garde qui s'y trouve. `try { … } finally { … }` — la forme qui a
motivé la règle, épinglée par un test et nommée au §5.3 — lirait une borne sur une valeur que le
constructeur avait déjà remplacée.

**Une version sûre n'est pas une version plus petite.** La rendre correcte suppose de décrire les
régions de gestion que le graphe tient délibérément à l'écart de ses successeurs, et de le faire
comme un point fixe plutôt que comme un parcours. Quatre cas syntaxiques deviennent quatre règles de
région plus ce point fixe, et la liste des formes à refuser d'emblée devient porteuse comme
interroger-entier n'a jamais eu à l'être — la même analyse de cas, déplacée sur un modèle moins
familier, avec un défaut sûr qui n'est plus gratuit.

**La stabilité est ici un contrat de chargement, pas une préférence.** Le moteur est lu par le Roslyn
que l'hôte fournit. Une garde lue d'une façon sous le compilateur d'un IDE et d'une autre sous celui
de l'outil serait une classe de défaut que ce dépôt n'a pas aujourd'hui, et la moitié de la question
qui dépendrait nouvellement de l'abaissement est celle qu'une montée de version peut déplacer sans le
dire.

**Le gain n'atteint pas les cas pour lesquels l'outil existe.** Ce qu'il achèterait se situe à quatre
raretés de profondeur, et rien de tout cela n'est le constructeur ordinaire que le scaffold est censé
aider. Face à un coût déjà payé sous forme d'une marque que son auteur lève une fois, la réponse par
défaut de l'ADR-0046 s'applique : borner l'effort, nommer la frontière, et laisser le dernier mot au
développeur.

## Alternatives envisagées

##### L'atteignabilité par le graphe, l'analyse de flot de données gardée sur la syntaxe

Envisagée parce que c'est la seule formulation qui tient *quelles écritures existent* à l'écart de
l'énumération des genres d'opération, et parce que c'est ce vers quoi un lecteur de la règle actuelle
irait d'abord.

Rejetée parce qu'elle est non sûre sur `try`/`finally`, comme ci-dessus, et parce que ramener un bloc
à une région que le compilateur acceptera d'analyser n'est pas bien défini : un bloc de base ne
couvre pas un morceau de source contigu. Là où une opération abaissée est attribuée à son instruction
englobante, la région est de nouveau un surensemble et rien n'est gagné ; là où elle est attribuée à
un fragment, la région est un sous-ensemble, et une écriture orthographiée hors de ce fragment passe
inaperçue. Laquelle des deux survient est une propriété de l'abaissement d'un Roslyn donné, non de la
règle.

##### Détecter les écritures directement depuis les opérations du graphe

Envisagée parce que c'est la façon évidente d'utiliser un graphe dont les blocs portent des
opérations.

Rejetée parce qu'elle suppose d'énumérer les genres d'opération qui écrivent — une affectation
simple, une incrémentation, un argument lié `out`, et le reste — soit la forme énumérer-les-
orthographes que l'analyse de flot de données a précisément remplacée, et dont on a déjà mesuré
qu'elle manquait une déconstruction, un argument `out` et un local `ref`.

##### Modéliser explicitement les régions de gestion, par-dessus le graphe

Envisagée parce que c'est la version qui serait effectivement correcte.

Rejetée sur le rapport coût/bénéfice plutôt que sur la sûreté : c'est plus de code que le parcours
qu'elle remplace, exprimé contre un modèle qui a moins de lecteurs, et chacun de ses trous est
invisible dans la source — là où un trou du parcours syntaxique est un `case` dont un lecteur voit
qu'il manque.

##### Ajouter des cas au parcours syntaxique à la place

Envisagée parce que l'essentiel de ce qu'un graphe achèterait est atteignable en nommant quelques
constructs de plus — ne rendre que l'expression de ressource d'un `using`, que l'expression de
gouverne d'un `switch`, et pour un `try` les régions depuis lesquelles un gestionnaire est atteint —
tout en gardant *l'interroger entier* en dessous, de sorte que la direction du défaut reste intacte
et que chaque trou reste visible.

Non rejetée, et délibérément non faite : c'est le remède si le prix est un jour payé par quelqu'un,
et c'est celui qui laisse cette décision intacte plutôt que de la renverser. La faire maintenant
ajouterait des cas pour des formes que personne n'a signalées.

## Conséquences

### Positives

* La propriété qui rend la règle sûre — une région surensemble ne fait qu'ajouter des refus — est
  conservée, et avec elle la garantie qu'un construct que le moteur ne modélise pas coûte une
  contrainte plutôt que d'en produire une fausse.
* Le moteur continue de répondre pareil sous chaque Roslyn qu'un hôte peut fournir, parce que la
  seule question de flot qu'il pose porte sur une région source.
* Le raisonnement du refus siège désormais là où un mainteneur le trouvera, au lieu du message d'un
  commit que personne n'aura l'idée d'aller chercher.

### Négatives

* Le prix du §5.3 demeure : une garde à l'intérieur d'un `try`, d'un `switch` ou d'un `using` dont le
  construct n'écrit le paramètre qu'après reste refusée bien qu'elle fût lisible, et son auteur
  confirme le generator à la main.
* Un lecteur qui connaît Roslyn continuera d'arriver au graphe de flot de contrôle comme à l'outil
  évident pour cette question, et trouvera désormais un enregistrement qui dit non plutôt que d'en
  redécouvrir les raisons.

### Risques

* Le rétrécissement qui rend le gain petit — une garde `if` dans ces constructs étant refusée
  ailleurs, avant que le placement ne soit consulté — est une propriété de la façon dont la lecture
  des gardes est aujourd'hui stratifiée, non une décision. Si cette stratification change, le gain
  grandit et cet enregistrement devrait être repesé plutôt que cité.
* Les observations sur la façon dont le graphe modélise l'entrée dans un gestionnaire ont été lues
  sur un Roslyn plus récent que le plancher. Une version qui exposerait cette entrée comme une arête
  ordinaire retirerait l'objection de sûreté, mais pas les trois autres.

## Actions de suivi

* **Ce qui rouvrirait la question.** L'un de : un constructeur issu d'un vrai code que la règle refuse
  et dont l'auteur s'en est plaint — le compte est aujourd'hui zéro ; une seconde question du moteur
  ayant besoin du même graphe, un graphe amorti sur plusieurs questions étant un autre calcul qu'un
  graphe bâti pour celle-ci seule ; ou la preuve que l'entrée dans un gestionnaire est une arête
  ordinaire au plancher.
* **La signature à laquelle confronter un signalement.** Un signalement ne rouvre cette décision que
  s'il porte sur un assistant de levée reconnu appelé sur le paramètre, situé dans un `try`, un
  `catch`, un `finally`, une section de `switch`, un `using` ou un `lock`, où ce construct ne porte
  aucun `throw`, et où le même paramètre est écrit plus loin dans ce même construct — la forme étant
  que la garde s'exécute avant l'écriture sur tous les chemins, et se voit refusée quand même :

  ```csharp
  public Order(int quantity) {
      try {
          ArgumentOutOfRangeException.ThrowIfNegative(quantity);
          quantity = checked(quantity * Lot);
      } catch (OverflowException) {
          quantity = Lot;
      }

      this.quantity = quantity;
  }
  ```

  Un signalement hors de cette signature est refusé pour une autre raison et cet enregistrement n'est
  pas sa réponse.
* **Le remède s'il est rouvert** est la quatrième alternative ci-dessus — nommer les constructs dans
  le parcours, en gardant *l'interroger entier* comme défaut en dessous — avant que le graphe ne soit
  reconsidéré.

## Références

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — la réponse par défaut
  à *« le générateur devrait-il traiter ce cas aussi ? »*, que cet enregistrement applique.
* [ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.fr.md) — ce que coûte une
  garde refusée, et pourquoi la direction du défaut est toute la question.
* [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) — la lecture de gardes que ceci
  place.
* [ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.fr.md) — le contrat de
  chargement qui fait d'un détail d'implémentation de Roslyn une question de compatibilité.
* §5.3 et §9 de cette spécification.
