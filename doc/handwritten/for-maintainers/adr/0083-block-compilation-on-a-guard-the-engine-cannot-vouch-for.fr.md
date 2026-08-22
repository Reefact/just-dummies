# ADR-0083 | Bloquer la compilation sur une garde que le moteur ne peut pas cautionner

🌍 🇬🇧 [English](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-22
**Accepted:** 2026-08-22
**Decision Makers:** Reefact

> Les références de section (§N) pointent vers la [spécification `dum`](../specifications/justdummies-tool.fr.md).

## Contexte

[ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) lit un ensemble clos d'idiomes de
garde de constructeur reconnus et, pour un paramètre dont il ne peut inférer aucun generator du
tout, émet un identifiant qui n'existe pas pour que le fichier ne compile pas tant que le
développeur n'a pas agi.

Le contexte de cette même décision nomme un second manque : « certains invariants ne s'expriment
pas du tout sous forme de gardes — validation déléguée à un helper, une librairie de gardes, ou une
règle portant sur deux paramètres ». Pour ce manque, la réponse retenue était différente — le
paramètre garde le generator neutre de la table de base, et le récapitulatif le marque `unread
guards` (§9) — parce qu'à l'époque rien ne distinguait ce paramètre d'un paramètre ne portant
aucun invariant.

La lecture des gardes s'est élargie depuis. Une instruction de tête qui atteint un paramètre par un
appel que l'ensemble reconnu n'analyse pas — un appel à un helper sans aucun `if` autour, une garde
de taille dont la constante dépasse ce que la bibliothèque produira, une garde de compte au-delà de
ce qu'une ligne d'élément peut tirer — est désormais lue et marquée `unread guards` elle aussi, là
où elle passait auparavant inaperçue ou, pour les cas de taille et de compte, était déjà marquée
mais laissait encore le generator neutre sans aucun commentaire au-delà de cette marque.

Le generator neutre gardé pour un paramètre `unread guards` peut violer l'invariant que la garde
abandonnée énonçait. Pour certaines des formes du corpus de test des gardes
(`JustDummies.GenAny.UnitTests`), ce n'est pas occasionnel : un plancher au-delà du plafond
producible de la bibliothèque, ou un compte au-delà de ce qu'une ligne d'élément d'une petite
énumération peut tirer de valeurs distinctes, signifie que le generator ne peut jamais satisfaire le
constructeur du domaine — chaque tirage échoue, pas seulement une fraction d'entre eux.

Un fichier dans cet état compile proprement, passe la revue, et est committé. L'échec qu'il finit
par produire — un constructeur de domaine qui lève sur une valeur que l'auteur du scaffold n'a
jamais écrite — surgit plus tard, dans une run de test différente, indiscernable pour qui le
rencontre d'un test flaky ordinaire.

[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) exige un refus de premier
ordre à la frontière de ce que le moteur peut décider, plutôt qu'une valeur produite par un
mécanisme que personne ne peut raisonner.

[ADR-0082](0082-answer-for-the-finished-chain-not-each-constraint.fr.md) réconcilie les contraintes
qu'une lecture de garde a produites et montre que les analyseurs propres de la bibliothèque et la
composition propre du moteur sont chacun un filet partiel : plusieurs des formes du corpus ne lèvent
aucun diagnostic et ne sont visibles qu'en construisant le generator émis et en tirant depuis lui.
Le raisonnement de cette même décision énonce, pour une chaîne au-delà de ce que la table peut
réconcilier, que « le paramètre garde son generator neutre et le récapitulatif le dit » — une
affirmation sur la chaîne écrite, pas sur la compilation du fichier ; cette décision répond à la
question qu'ADR-0082 laissait ouverte.

Séparément, sur cette même branche, le fichier émis donne désormais à la recette de chaque
paramètre sa propre fabrique privée statique, appelée par le constructeur public par son nom plutôt
qu'inlinée au point d'appel (§4.2). La fabrique d'un paramètre peut donc porter plus qu'un
identifiant : une expression qui fonctionne peut se trouver dans la même méthode qu'une ligne qui
bloque la compilation jusqu'à ce que le développeur la regarde, ce pour quoi le point d'appel à
expression unique d'origine d'ADR-0060 n'avait pas de place.

## Décision

Un paramètre portant `unread guards` bloque la compilation de la même façon qu'un paramètre non
résolu le fait déjà, avec son generator inféré gardé comme base de travail de la fabrique, sous la
ligne qui la bloque.

## Raisonnement

**Un generator qui compile et échoue parfois est un résultat pire qu'un generator qui ne compile
jamais.** ADR-0060 a déjà pesé ce compromis pour le paramètre sans aucun generator : un signal à la
compilation coûte dix secondes au développeur là où un signal différé coûte bien davantage. Un
paramètre marqué `unread guards` fait face au même choix exactement : la seule différence est
qu'un generator existe, ce qui ne dit rien sur sa sûreté.

**Le mécanisme existe déjà ; ceci étend où il s'applique, pas ce qu'il est.** Le procédé
d'identifiant-qui-n'existe-pas d'ADR-0060 est réutilisé sans changement. Ce qui change, c'est le
second cas qu'il couvre désormais, si bien que les deux états — aucun generator inféré, et un
generator inféré mais non cautionné — sont traités par un seul mécanisme qu'un développeur n'a à
apprendre qu'une fois.

**Le refactor en fabriques est ce qui rend la base digne d'être gardée.** Le mécanisme d'ADR-0060
abandonnait la question d'une base de travail avec tout le reste, parce que son point d'appel n'
avait nulle part où en mettre une. Une méthode fabrique nommée en a une : la ligne bloquante et la
proposition se trouvent ensemble, si bien que le développeur relit la meilleure tentative de dum au
lieu d'en écrire une de rien, une fois qu'il a supprimé une seule ligne.

**Ceci n'élargit pas ce que le moteur tente.** ADR-0046 borne l'ambition du generator, pas son
honnêteté sur une frontière qu'il a déjà nommée. `unread guards` marque déjà le paramètre comme un
paramètre que le moteur n'a pas pu entièrement prendre en compte ; refuser plus fort à une frontière
déjà déclarée est le refus qu'ADR-0046 demande, pas une nouvelle inférence sur le sens de la garde.

**Les deux filets existants sont prouvés partiels.** ADR-0082 a mesuré que les analyseurs de la
bibliothèque et la réconciliation propre du moteur sont chacun silencieux sur une partie de cette
classe de défaut — plusieurs formes ne lèvent rien et ne sont visibles qu'en tirant depuis le
generator construit. Un signal qui se déclenche avant même que le fichier ne soit exécuté referme
exactement l'écart que ces deux-là laissent ouvert.

## Alternatives envisagées

##### Laisser la note du récapitulatif comme seul signal

Envisagée parce qu'elle existe déjà, ne coûte rien à garder, et nomme précisément la provenance.

Rejetée parce qu'une ligne de récapitulatif est facile à manquer et ne porte aucune application à la
compilation — le même argument qu'ADR-0060 avait déjà opposé à une réponse purement informative pour
son propre cas, plus étroit.

##### Une exception à l'exécution levée là où le generator est construit

Envisagée parce que le fichier compilerait alors, ce qui semble plus amical à première vue.

Rejetée pour la raison qu'ADR-0060 a déjà rejetée pour son propre cas : elle reporte le signal
au-delà du moment où le développeur regarde le fichier, transformant un manque de scaffolding en un
échec de test dont la cause est une ligne que personne ne lit à ce moment-là.

##### Distinguer un abandon prouvé sûr d'un doute véritablement incertain, et ne bloquer que le second

Envisagée parce que certaines gardes abandonnées ne peuvent en réalité pas être violées — un plafond
au-delà du plafond producible de la bibliothèque n'est abandonné que parce que la plage propre du
generator s'y trouve déjà, si bien que rien de ce que le generator neutre tire ne peut de toute façon
faire échouer le constructeur du domaine.

Rejetée parce que décider « sûr » demande un raisonnement que le moteur ne fait nulle part ailleurs :
comparer le sens d'une contrainte abandonnée aux bornes propres du generator est exactement la
propagation de contraintes qu'ADR-0046 refuse de construire. Une règle unique qui signifie la même
chose partout vaut mieux qu'une règle plus étroite achetée avec un solveur.

##### Refuser de scaffolder tout le type dès qu'un seul paramètre nécessite une vérification

Envisagée parce que c'est la règle la plus simple et qu'elle ne demande aucun mécanisme par
paramètre.

Rejetée pour la raison qu'ADR-0082 a déjà rejetée pour le refus trop large équivalent dans son
propre cas : elle jette une proposition que le moteur a correctement établie pour chaque autre
paramètre, à cause d'un doute sur un seul.

## Conséquences

### Positives

* Un paramètre marqué `unread guards` ne peut plus atteindre une suite de tests committée en
  portant un generator susceptible de violer l'invariant qu'il a abandonné ; l'échec se déplace au
  moment où le fichier est écrit, ce qui aligne cette garantie sur celle qu'ADR-0060 donne déjà à
  l'autre cas non résolu.
* Un domaine qu'aucun generator ne peut satisfaire du tout — un plancher au-delà du plafond
  producible, un compte au-delà des valeurs distinctes d'une ligne d'élément — ne « construit »
  plus simplement ; le fichier le dit avant que quiconque ne l'exécute.
* Le récapitulatif compte cet état séparément d'un paramètre ouvert (`to verify`, pas `TODO`), si
  bien qu'un lecteur ou un script peut distinguer « rien n'a été inféré » de « quelque chose l'a
  été, et n'est pas cautionné ».

### Négatives

* Certains paramètres marqués ont un abandon prouvé inoffensif — une garde de plafond dont la borne
  dépasse le plafond producible ne peut jamais être violée par la plage plus étroite propre du
  generator — et ceux-ci bloquent désormais aussi la compilation plutôt que d'être distingués d'un
  doute véritable.
* Un scaffold portant plusieurs paramètres `unread guards` a désormais besoin de plus d'une ligne
  supprimée avant de compiler, là où il compilait auparavant sans y toucher.

### Risques

* Tout élargissement de ce qui compte comme `unread guards` porte désormais un coût à la
  compilation plutôt qu'une note de récapitulatif ; les deux décisions sont donc couplées : ce que
  la marque signifie a été tranché ici, ce qui la mérite l'est au §5.3, et un changement là-bas
  n'est plus un simple changement de formulation.
* Une décision ultérieure de distinguer un abandon prouvé sûr d'un doute véritable devrait réviser
  cette décision, pas seulement son implémentation.

## Actions de suivi

* Si le taux de faux positifs sur des bases de code réelles s'avère élevé, resserrer ce qui compte
  comme `unread guards` est le correctif — pas assouplir cette décision, qui ne décide que de ce
  qui se passe une fois cette marque posée. **Déjà appliqué une fois** : un appel dont le résultat
  est *utilisé* lisait comme un doute tout constructeur normalisant ordinaire
  (`_name = value.Trim();`), et le §5.3 exige désormais que le résultat soit jeté. Ce coût était
  nommé sous cette rubrique plutôt que sous Négatives parce qu'on s'attendait à l'assumer ; ce ne
  fut pas le cas.
* Étendre l'ensemble clos de gardes reconnues du §5.3, déjà une action de suivi d'ADR-0082, réduit
  la fréquence à laquelle `unread guards` se déclenche du tout, ce qui est le remède plus précis au
  même coût.

## Références

* [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) — le mécanisme que ceci étend à un
  second cas.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — la frontière dans
  laquelle ceci reste.
* [ADR-0082](0082-answer-for-the-finished-chain-not-each-constraint.fr.md) — la réconciliation en
  aval de laquelle ceci se place, et la mesure que les deux filets existants sont partiels.
* [ADR-0058](0058-leave-the-scaffolded-file-open-to-the-analyzers.fr.md) — le filet des analyseurs
  montré partiel par le même corpus.
* §4.2, §5.3, §5.5, §5.6, §6, §9 de cette spécification.
