# ADR-0086 | Lire les aides de garde de bibliothèques nommées, dans leurs deux orthographes

🌍 🇬🇧 [English](0086-read-the-guard-helpers-of-named-libraries.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-23
**Accepted:** 2026-08-23
**Decision Makers:** Reefact

> Les références de section (§N) pointent dans la [spécification de `dum`](../specifications/justdummies-tool.fr.md).

## Contexte

Le §9 nomme un résidu que l'ensemble fermé ne peut pas voir : une aide de garde qui
**retourne** la valeur qu'elle a vérifiée — `_name = Ensure.NotBlank(value);` — est
indiscernable d'une normalisation, donc silencieuse, pas même `unread guards`. La règle du
résultat-jeté du §5.3 a été façonnée par un faux positif mesuré dans l'autre sens : lire
chaque résultat utilisé comme un doute bloquait la compilation des constructeurs
normalisants ordinaires (`_name = value.Trim();`), et les Actions de suivi de l'ADR-0083
consignent ce coût comme un coût à ne pas porter.

L'audit d'architecture du 23/08/2026 a mesuré ce que ce résidu contient. La forme assignée
est l'**usage documenté** d'`Ardalis.GuardClauses` — l'un des paquets de garde les plus
téléchargés de NuGet, concentré dans les bases de code de modèle de domaine que cet outil
vise — et sa première occurrence dans un constructeur est une affectation à l'état, donc le
scan de tête s'y arrête (§5.3) : un constructeur à cinq paramètres entièrement gardé dans ce
style se lit comme cinq paramètres que personne n'a contraints, sous un récapitulatif qui
n'affiche aucun doute nulle part. Contre de tels constructeurs, les générateurs neutres
échouent environ un tirage sur deux pour une garde de signe et pratiquement chaque tirage
pour un pourcentage borné — un à deux ordres de grandeur au-dessus de la mesure de 594 sur
10 000 qui a justifié de construire le §5.3 (ADR-0060).

`CommunityToolkit.Diagnostics` porte la même classe d'aides dans une orthographe à retour
`void`, jetée, qui aujourd'hui gagne la marque et bloque le build — une confirmation par
paramètre et par scaffold pour des gardes dont le sens est documenté.

L'ensemble fermé lit déjà des appels d'aide par symbole résolu :
`ArgumentNullException.ThrowIfNull` et la famille arithmétique
d'`ArgumentOutOfRangeException` y sont entrés ainsi (ADR-0082, suites). Ce que le §5.3
refuse est une **liste de préfixes de noms bénis** — une supposition d'intention qu'aucun
lecteur ne pourrait reproduire. Une méthode documentée précise d'un paquet précis n'est pas
une supposition de préfixe : elle a une sémantique, la compilation résout son symbole, et le
corpus et l'oracle de tirage seedé de ce dépôt peuvent épingler cette sémantique en
appelant le vrai paquet à sa version épinglée — y compris le comportement aux bornes qu'un
nom n'énonce pas (l'audit a mesuré la garde de plage d'Ardalis inclusive aux deux bouts et
celle du Toolkit exclusive à son bout supérieur).

[ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.fr.md) garde le moteur
libre de références de paquets ; chaque symbole qu'il lit est résolu contre la compilation
du développeur lui-même.
[ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.fr.md), dans ses
Actions de suivi, nomme l'extension de l'ensemble fermé comme le remède précis aux faux
positifs de la marque, et le §16 réserve « une bibliothèque d'aides de type
Guard.Against » comme candidate.

[ADR-0085](0085-change-the-guard-reader-only-against-a-field-report.fr.md) ferme la surface
du §5.3 derrière une signature de signalement. Cette décision est la première à y entrer :
la mesure de l'audit est le signalement, et les formes du corpus et les cas du résolveur
qu'elle exige sont livrés avec le changement.

## Décision

L'ensemble fermé du §5.3 gagne les aides de validation documentées
d'`Ardalis.GuardClauses` et de `CommunityToolkit.Diagnostics`, reconnues par symbole résolu
dans leurs deux orthographes — jetée, et assignée à un champ ou une propriété — et une
affectation à l'état dont le côté droit est une aide reconnue de cet ensemble ne termine
plus le scan de tête.

## Justification

**La masse d'échec se situe au-dessus de celle qui a justifié la fonctionnalité.**
L'ADR-0060 a construit la lecture des gardes sur une flakiness mesurée d'un sur dix-sept ;
l'idiome assigné des bibliothèques de garde fait échouer les tirages d'un sur deux à un par
tirage, silencieusement, sur les bases de code pour lesquelles l'outil a été écrit. Le
silence est la seule issue que cette base traite comme pire que le blocage (ADR-0083), et
c'est la plus grande surface silencieuse que le moteur ait.

**Une méthode nommée est une sémantique, pas une supposition.** Le refus que le §5.3
consigne vise la reconnaissance des validateurs à leur orthographe ; cette décision
reconnaît deux bibliothèques à ce que leurs symboles résolus sont documentés et mesurés
faire. C'est le socle sur lequel les aides de levée de la BCL se tiennent déjà — élargir
l'ensemble, pas affaiblir sa discipline.

**Mesuré, ou hors de la table.** La sémantique de chaque aide retenue — quelles valeurs
elle rejette, l'inclusivité de chaque borne, le fait qu'elle retourne son entrée
inchangée — est épinglée par des tests qui appellent le vrai paquet à sa version épinglée,
et par des formes du corpus dont les générateurs émis tirent contre les vrais
constructeurs. Une aide dont la table ne peut pas porter la sémantique ainsi n'est pas
approximée : une méthode non retenue d'une bibliothèque reconnue se lit comme une garde
dont le moteur ne peut pas répondre — la marque, pas le silence, et pas une supposition.
Les mesures aux bornes de l'audit sont l'exemple permanent de la raison d'être de cette
règle : les deux bibliothèques sont en désaccord sur l'admissibilité de la borne haute
d'une plage, et une ligne de table écrite de mémoire aurait été fausse avec assurance sur
l'une des deux.

**La forme assignée n'est lue que là où ses deux faits sont certains.** Une aide reconnue
assignée à un champ ou une propriété valide le paramètre et range le résultat, sans écrire
aucun paramètre — les instructions en dessous restent donc la validation de tête du
constructeur, et le scan peut continuer là où il s'arrêtait. Assigné au paramètre lui-même,
le même appel est une écriture de paramètre que les règles de placement refusent de lire
au-delà (§5.3) ; une telle instruction est marquée plutôt que lue, ce qui convertit le
silence d'aujourd'hui en confirmation sans toucher à la couche de placement. Le faux
positif de normalisation ne peut pas revenir : `Trim` n'est pas dans la table, et une
affectation dont le côté droit n'est pas reconnu par la table termine le scan exactement
comme aujourd'hui.

**La reconnaissance lie la version du développeur lui-même.** Sous l'ADR-0063 le moteur
résout l'aide contre la compilation qu'on lui tend : un projet qui ne référence aucune des
deux bibliothèques ne paie rien, et un qui les référence est lu contre l'assembly avec
lequel il compile réellement.

## Alternatives considérées

### Laisser le résidu au développeur

Envisagée parce que le §9 le nomme déjà, et que l'audit a trouvé le moteur actuel sûr.

Rejetée sur la mesure : l'idiome est l'usage documenté du paquet de garde dominant dans le
segment cible de l'outil lui-même, et son mode d'échec est le test flaky silencieux que ce
produit existe pour éliminer — à un taux bien au-dessus de celui qui a justifié la lecture
des gardes.

### Lire tout appel assigné sur un paramètre comme un doute

Envisagée parce qu'elle n'exige aucune connaissance de bibliothèque et convertit tout le
résidu en confirmations.

Rejetée parce qu'elle a déjà été tentée et retirée : elle lisait chaque constructeur
normalisant comme un doute, et les Actions de suivi de l'ADR-0083 consignent ce coût comme
inacceptable. La règle structurelle du résultat-jeté demeure ; seules les sémantiques
connues de la table la franchissent.

### Un fichier de configuration nommant les validateurs de l'équipe

Envisagée parce qu'elle couvrirait aussi les aides de garde maison, ce que cette décision
ne fait pas.

Rejetée par l'ADR-0060 pour la première version et inchangée ici : elle convertit l'outil
en système de convention et contredit la règle selon laquelle rien ne se configure avant le
premier usage. Les aides maison gardent la réponse d'aujourd'hui — la marque là où la forme
est visible, le résidu du §9 là où elle ne l'est pas — et le §16 garde la question ouverte.

### Sonder le générateur émis en tirant au moment du scaffold

Envisagée parce qu'une sonde empirique attraperait ce résidu et tous les autres, sans
aucune connaissance de bibliothèque.

Non retenue ici : elle détecte un générateur faux mais ne peut pas en semer un correct,
donc elle complète la lecture plutôt qu'elle ne la remplace ; et exécuter le code du
développeur au moment du scaffold est une décision à part entière, laissée ouverte.

### Retenir les surfaces complètes des deux bibliothèques, plages et gardes de format comprises

Envisagée parce que la complétude minimiserait les déclenchements de la marque.

Rejetée là où la mesure s'arrête : une garde de format ou de prédicat n'a aucune contrainte
que la table puisse porter, et une borne dont le moteur ne peut pas épingler la sémantique
serait une contrainte supposée — l'issue que l'ADR-0060 nomme pire qu'aucune. Le reste non
retenu gagne la marque, qui est la sortie conçue.

## Conséquences

### Positives

* L'idiome de garde DDD dominant se lit, dans l'orthographe que sa propre documentation
  enseigne ; l'amplificateur des cinq générateurs neutres disparaît, car une
  garde-affectation reconnue ne termine plus le scan.
* L'orthographe jetée du Toolkit passe d'une confirmation par paramètre à une lecture.
* Une aide non retenue d'une bibliothèque reconnue passe du silence à une confirmation —
  strictement plus honnête dans les deux directions.

### Négatives

* Le moteur porte la connaissance de deux contrats tiers — identités de méthodes,
  inclusivités de bornes, retourne-son-entrée — tenue aux paquets par des tests plutôt que
  par le système de types, le même compromis que l'ADR-0082 a accepté pour la surface de la
  bibliothèque elle-même sous l'ADR-0063.
* Deux références de paquets de test seulement entrent dans le projet de test du moteur,
  épinglées, pour que le corpus puisse appeler ce dont il répond.

### Risques

* Une version future de l'une ou l'autre bibliothèque pourrait changer la sémantique d'une
  méthode retenue ; la reconnaissance serait alors fausse avec assurance dans cette ligne
  jusqu'à ce que les tests de version épinglée soient montés et re-mesurés. L'ensemble
  retenu est délibérément petit et les contrats sont l'identité documentée des
  bibliothèques ; une montée de version qui fait échouer les tests aux bornes est
  l'alarme.
* Chaque bibliothèque supplémentaire demandera à entrer par analogie. La signature de
  l'ADR-0085 est la porte : un signalement, une forme du corpus, une sémantique mesurée —
  ou la marque demeure.

## Actions de suivi

* Le §5.3 de la spécification porte les lignes retenues et la règle de la forme assignée ;
  le corpus porte les formes ; la suite du résolveur porte les cas par ligne — le tout dans
  le changement que cet enregistrement accompagne.
* Si des aides de garde maison doivent un jour être lues, c'est la question du fichier de
  déclaration du §16, et elle rentre par la signature de l'ADR-0085.

## Références

* [ADR-0085](0085-change-the-guard-reader-only-against-a-field-report.fr.md) — la procédure
  d'entrée que cette décision est la première à satisfaire.
* [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) — l'ensemble fermé, et
  pourquoi une contrainte fausse pèse plus qu'une contrainte manquante.
* [ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.fr.md) — pourquoi la
  reconnaissance se résout contre la compilation du développeur.
* [ADR-0082](0082-answer-for-the-finished-chain-not-each-constraint.fr.md) — le précédent
  de la connaissance miroir tenue par des tests.
* [ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.fr.md) — ce que
  coûte la marque, et ses suites nommant l'extension de la table comme remède.
* §5.3, §9, §16 de la spécification.
