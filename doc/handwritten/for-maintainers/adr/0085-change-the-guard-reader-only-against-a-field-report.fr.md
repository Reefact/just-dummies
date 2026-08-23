# ADR-0085 | Ne changer le lecteur de gardes que contre un signalement du terrain

🌍 🇬🇧 [English](0085-change-the-guard-reader-only-against-a-field-report.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-23
**Accepted:** 2026-08-23
**Decision Makers:** Reefact

> Les références de section (§N) pointent dans la [spécification de `dum`](../specifications/justdummies-tool.fr.md).

## Contexte

Le §5.3 a deux moitiés. Une table fermée d'idiomes de garde reconnus fait correspondre à
chaque forme reconnue exactement une contrainte
([ADR-0060](0060-seed-generators-from-constructor-guards.fr.md)). Une couche de placement
décide si une garde reconnue peut être attribuée à la valeur tirée — aucune écriture de son
paramètre n'a pu s'exécuter, rien ne décide si elle s'exécute, rien au-dessus ne peut la
sauter — et répond à chacune de ces questions avec un défaut orienté refus : une
construction que la couche ne modélise pas coûte une contrainte, jamais une contrainte
fausse ([ADR-0084](0084-place-a-guard-by-syntax-reach-not-a-control-flow-graph.fr.md)).

Le fichier qui porte les deux moitiés est passé de 375 lignes à 1 323 en à peu près
quarante et une heures (pull requests #105 à #119), après douze jours sans un changement. La
séquence est documentée dans ces pull requests :
[ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.fr.md) a fait de
chaque marque `unread guards` un coût de compilation ; l'élargissement qui a suivi (#113)
mettait en œuvre les propres suites de l'ADR-0082 sous cette pression, sans aucun
signalement extérieur pour le demander ; chaque élargissement a exposé une question de
placement que le lecteur plus étroit n'avait jamais à se poser ; #117 et #119 ont payé
cette facture de sûreté. L'ADR-0082 et l'ADR-0083 nomment chacun cette traction dans leurs
sections Risques — « l'attraction vers la propagation est exactement ce que l'ADR-0046
existe pour contrer ».

Un audit d'architecture du 23/08/2026 a passé toute la surface en revue et mesuré les deux
directions de changement à zéro preuve de terrain. Aucun constructeur d'une base de code
réelle n'a été signalé que les règles actuelles refusent et dont l'auteur s'en soit ému —
le propre décompte de l'ADR-0084. Aucun incident de maintenance, défaut ou confusion n'a
davantage été imputé à la complexité de la couche de placement. L'audit a aussi soumis à
l'épreuve une simplification de la couche de placement et l'a trouvée non sûre telle que
spécifiée sur deux classes constructives — les écritures atteintes par une fonction locale
appelée au-dessus de sa déclaration, et un `goto` arrière — toutes deux démontrées par des
sondes exécutées contre le plancher Roslyn épinglé, toutes deux tenues aujourd'hui par des
mécanismes qui raisonnent hors de la position des instructions. Réparée, la candidate ne
supprimait plus que vingt à quarante lignes exécutables nettes sur 542.

L'ADR-0084 gouverne déjà une frontière de cette surface avec une signature de réouverture
écrite : à quoi ressemble un signalement recevable, combien il en existe (zéro), et le
remède à appliquer d'abord s'il en arrive un. Rien d'équivalent ne gouverne le reste du
§5.3.

Ce dépôt est développé pour l'essentiel par des sessions d'agents
([ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md), Contexte).
L'épisode de quarante et une heures montre ce que produit l'argumentation au cas par cas à
cette vélocité : chaque pas individuel était argumenté et sûr, et la somme fut un
quadruplement que personne n'a décidé.

## Décision

La surface de lecture des gardes du §5.3 — la table d'idiomes reconnus comme les règles de
placement — ne change que contre un signalement du terrain conforme à une signature écrite,
la marque `unread guards` restant la réponse permanente pour tout ce que la surface ne
couvre pas.

## Justification

**Les deux directions sont à zéro preuve, donc l'état décidé-et-testé gagne par défaut.**
L'élargissement n'a aucun signalement extérieur qui le demande ; le rétrécissement n'a
aucun incident imputé à la mécanique qu'il retirerait. Une surface dans cet état est
terminée jusqu'à ce qu'une preuve arrive, et une procédure d'entrée écrite est ce qui
convertit la pression future en preuve plutôt qu'en quarante et une heures de plus.

**Le cliquet a besoin d'un ancrage extérieur.** L'ADR-0083 a couplé la marque à un coût de
compilation, ce qui rend visible chaque idiome lisible-mais-non-lu, ce qui invite à
élargir, ce qui crée des obligations de placement — une boucle que cette base a déjà
parcourue une fois, documentée dans ses propres enregistrements. Exiger que le déclencheur
vienne de l'extérieur de la boucle est la seule coupe qui la brise sans affaiblir aucun de
ses maillons.

**C'est l'ADR-0046 appliqué au rythme de changement du moteur lui-même.** Borner l'effort,
nommer la frontière, refuser bruyamment à son bord. La frontière ici n'est pas ce que le
lecteur émet — cela a toujours été borné — mais la vitesse à laquelle, et sur la demande de
qui, son mécanisme peut croître. Refuser un changement est une décision qui doit être
argumentée exactement comme en faire un (ADR-0046, Risques) ; la signature est la forme
permanente de cet argument.

**La marque est une sortie conçue, pas un échec à faire disparaître par l'ingénierie.** Un
paramètre marqué `unread guards` garde la meilleure proposition du moteur sous une ligne
que le développeur supprime une fois (ADR-0083). Chaque lacune que la signature laisse
ouverte finit là — visible, confirmable, sûre — et c'est pourquoi la surface peut se
permettre de rester immobile.

**La signature lie symétriquement.** Une suppression dans la surface — retirer un mécanisme
de placement, abandonner un idiome — exige un signalement aussi : un défaut imputé au code
qu'elle retirerait, ou un coût de maintenance mesuré. La propre candidate de l'audit est le
précédent : argumentée depuis des décomptes de lignes, elle a échoué contre la mesure ; les
deux mécanismes qu'elle aurait supprimés portaient chacun une propriété de sûreté.

## Alternatives considérées

### Laisser la surface ouverte, gouvernée au cas par cas par l'ADR-0046

Envisagée parce que l'ADR-0046 élève déjà la barre pour l'élargissement comme pour le
rétrécissement, et que chaque étape de #105–#119 l'a cité.

Rejetée sur le résultat mesuré : chaque étape a tenu la barre individuellement et la somme
a quadruplé le fichier en deux jours, poussée par une pression que le processus lui-même
avait créée. L'ADR-0046 fixe le niveau d'exigence de l'argument ; il ne fixe aucune
condition d'entrée, et à la vélocité des agents les arguments n'arrêtent jamais d'arriver.

### Simplifier la couche de placement maintenant

Envisagée parce que la recommandation intermédiaire de l'audit lui-même était un
remplacement des trois mécanismes de placement par une règle unique, et que le décompte de
lignes plaidait pour elle.

Rejetée sur la mesure adversariale de l'audit : non sûre telle que spécifiée sur deux
classes au plancher Roslyn épinglé ; vingt à quarante lignes exécutables nettes une fois
réparée ; une spécification réécrite contre ses propres arguments consignés, en deux
langues ; un enregistrement accepté la veille rendu périmé ; et une entrée de changelog
annonçant que des gardes lues jusqu'ici bloquent désormais des builds — une régression sans
signalement derrière elle.

### Ne geler que la moitié placement et laisser la table ouverte

Envisagée parce que la table est bon marché à la ligne et que la couche de placement est là
où le coût se concentre.

Rejetée parce que l'histoire court dans l'autre sens : c'est l'élargissement de la table
qui a créé les obligations de placement (#113 → #117, #119), et les Actions de suivi de
l'ADR-0083 dirigent chaque future plainte de faux positif d'abord vers la table. Une table
ouverte sur une couche de placement gelée rejoue la même boucle et interdit d'en payer la
facture.

## Conséquences

### Positives

* La boucle d'élargissement exige désormais une preuve extérieure à elle-même ; le prochain
  #113 arrive avec un constructeur attaché, ou n'arrive pas.
* Un futur argument de simplification a une procédure à satisfaire au lieu d'un audit à
  refaire, et la seule coupe pré-validée est consignée.
* Les deux instruments que ce dépôt possède déjà — le corpus et l'oracle de tirage seedé —
  deviennent le banc de qualification de tout changement proposé, dans les deux directions.

### Négatives

* Un élargissement réellement utile sans signalement encore existant attend d'en avoir un.
  L'attente est bornée par la marque : le cas qu'il servirait finit en une confirmation
  d'une ligne, pas en un tirage faux silencieux.
* Chaque changement du §5.3 porte désormais un coût de procédure — un signalement, une
  forme du corpus, des cas du résolveur — même quand le changement lui-même est petit.

### Risques

* La signature pourrait être satisfaite rituellement — un signalement fabriqué sur
  commande. L'exigence de la forme du corpus l'atténue : elle doit démontrer le problème
  avant le changement, contre le vrai moteur, et l'acceptation reste au mainteneur.
* Un lecteur pourrait lire cet enregistrement comme interdisant les corrections de défaut.
  Il ne le fait pas : une contrainte fausse rapportée comme inférée est un défaut de
  correction, hors de toute borne que cette base pose (ADR-0046), et sa mesure est son
  signalement.

## Actions de suivi

* **Ce qu'est un signalement recevable.** Un changement du §5.3 n'est recevable que
  lorsque tout ce qui suit l'accompagne : un signalement nommant un constructeur réel —
  d'une base de code du terrain, ou un défaut mesuré par le corpus et l'oracle de tirage
  de ce dépôt — l'idiome qu'il utilise, et ce que le moteur en a fait ; une forme du
  corpus qui le reproduit, ajoutée avant le changement et démontrant le problème sans
  lui ; et des cas de la suite du résolveur pour ce que le changement lit ou refuse
  (ADR-0060, Actions de suivi).
* **Le remède, pris dans l'ordre.** D'abord : aucun changement — la marque
  `unread guards` y répond déjà. Ensuite : étendre la table fermée d'une sémantique
  nommée, documentée, mesurée. Puis, pour le placement : nommer de nouveaux cas dans le
  parcours syntaxique, en gardant demander-entier dessous (le remède pré-engagé de
  l'ADR-0084). Enfin : un autre modèle d'analyse — non rejeté, et disponible exactement
  comme l'ADR-0084 le conditionne : quand un vrai besoin est démontré **et** que
  l'alternative est réellement plus simple que d'étendre le parcours qu'elle remplace.
* **La seule coupe pré-validée**, consignée pour ne pas être re-dérivée : si la couche de
  placement doit un jour maigrir, la coupe que la revue adversariale de l'audit a validée
  comme sûre est la liste blanche des constructions englobantes
  (`using`/`lock`/`checked`/`unsafe`/`finally` et le `else` terminal), dont les lectures
  protègent des formes à fréquence de terrain quasi nulle ; tout le reste de la couche a
  été mesuré porteur.
* **Ce que cet enregistrement ne conditionne jamais.** Une contrainte fausse émise comme
  inférée est un défaut de correction : la correction n'est pas ce qui se borne
  (ADR-0046), et un tel défaut porte son signalement par définition.
* [ADR-0086](0086-read-the-guard-helpers-of-named-libraries.fr.md) est le premier
  changement à entrer par cette procédure ; son signalement, ses formes de corpus et ses
  cas de résolveur y sont listés.
* Fermer l'issue #112, dont #117 a corrigé la substance en remplaçant son esquisse.

## Références

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — la règle que
  cet enregistrement applique à la croissance du moteur lui-même.
* [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) — l'ensemble fermé, et
  l'exigence de test par ajout que cet enregistrement généralise.
* [ADR-0082](0082-answer-for-the-finished-chain-not-each-constraint.fr.md) /
  [ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.fr.md) — les
  enregistrements dont les sections Risques nomment la boucle que cet enregistrement coupe.
* [ADR-0084](0084-place-a-guard-by-syntax-reach-not-a-control-flow-graph.fr.md) —
  l'instrument de signature de réouverture que cet enregistrement étend à toute la surface.
* §5.3, §5.6, §9 de la spécification ; pull requests #105, #113, #117, #118, #119.
