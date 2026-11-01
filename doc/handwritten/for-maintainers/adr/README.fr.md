# Enregistrements de décisions d'architecture

Enregistrements datés des décisions significatives — leur contexte, l'option
retenue et les conséquences. Une ADR est un journal historique : une fois
acceptée, elle n'est pas modifiée sur place ; une décision se réexamine en
écrivant une **nouvelle** ADR qui remplace l'ancienne, et le statut de
l'ancienne passe à *Superseded* avec un lien vers celle qui lui succède.

## D'où viennent ces décisions

Ce dépôt a été extrait de
[`Reefact/first-class-errors`](https://github.com/Reefact/first-class-errors) le
31/07/2026 ([ADR-0044](0044-extract-justdummies-into-its-own-repository.fr.md)).
Presque toutes les décisions ci-dessous y ont été acceptées avant que ce dépôt
n'existe, et la colonne **Origine** dit comment chacune est arrivée ici :

* **déplacé** — la décision a voyagé avec le code qu'elle décrit. Elle ne sera
  plus dans `Reefact/first-class-errors` une fois le nettoyage de ce dépôt-là
  atterri, et cette base est son unique foyer. 34 décisions.
* **adopté** — la décision gouverne le build, la CI ou les conventions de ce
  dépôt, mais son enregistrement reste vivant dans `Reefact/first-class-errors`
  aussi, car ce dépôt l'applique encore. Les deux copies sont désormais
  indépendantes : chaque côté peut remplacer la sienne sans toucher à l'autre.
  9 décisions.
* **consigné ici** — décidé dans ce dépôt, de son propre chef. 80 décisions.

Les numéros appartiennent à cette base, attribués dans l'ordre où les décisions
ont été consignées en amont
([ADR-0045](0045-renumber-the-decision-base.fr.md)) ; chaque ADR porte aussi son
ancien numéro dans son en-tête, et la colonne **Origine** répète la
correspondance. **Les messages de commit n'ont jamais été réécrits** : un message
de commit antérieur au 31/07/2026 cite donc le numéro qu'avait la décision dans
`Reefact/first-class-errors` — `docs: draft ADR-0010 hosting Dummies as a
standalone package` désigne l'ADR-0003 de ce dépôt. L'historique lui-même a été
réécrit le 08/08/2026
([ADR-0053](0053-rewrite-the-published-history-to-a-single-line.fr.md)), ce qui a
changé tous les identifiants de commit mais a repris chaque message à l'octet
près.

## Références à l'autre dépôt

Certaines décisions d'ici citent des ADR restées dans
`Reefact/first-class-errors` et qui ne gouvernent que ce produit-là — pourquoi
son paquet de test s'est appuyé sur cette bibliothèque, pourquoi son build
exécute ces analyzers, le plancher d'exécution de l'outillage. Ces citations
s'écrivent **`ADR-00NN (first-class-errors)`** et ne figurent pas dans le tableau
ci-dessous. Le qualificatif est porteur : un `ADR-0006` nu désigne l'ADR-0006 de
*cette* base, qui est une décision différente de celle que ce numéro désigne
là-bas.

## Quand écrit-on une ADR ?

Chaque pull request est contrôlée au regard de cette base — au moment où de
nouvelles décisions entrent dans le code
([ADR-0002](0002-check-every-pull-request-against-the-adr-base.fr.md)). La
plupart des pull requests n'embarquent aucune décision d'architecture et
n'ajoutent aucune ADR ; c'est le contrôle qui est obligatoire, pas l'artefact. Le
test du « significatif » : *si l'implémentation changeait mais que la décision
tenait, l'ADR ne devrait pas avoir à être modifiée.* Une nouvelle décision est
**consignée** ici, une décision qui en remplace une autre s'écrit comme une ADR
**qui la remplace**, et un changement qui **contredit** une ADR acceptée est
porté à l'attention du mainteneur. La procédure pour les agents — rédiger en
*Proposed*, ne jamais basculer un statut unilatéralement — est dans
[`AGENTS.md`](../../../../AGENTS.md).

## Une ADR est un enregistrement de décision, pas une spécification

Une ADR capture une **décision et le raisonnement qui la porte** — non la façon
dont elle est implémentée. La mécanique d'implémentation (code, configuration,
YAML, options exactes, fragments XML ou de commandes, parcours garde par garde)
vit dans le code et dans la documentation de référence vers laquelle l'ADR
pointe, jamais dans l'ADR elle-même. En particulier, **la Justification est un
argumentaire, pas un document de conception** : si un paragraphe explique
*comment quelque chose est construit* plutôt que *pourquoi la décision est
juste*, il appartient à la documentation de référence, et l'ADR y renvoie.

## Conventions de fichiers

* Une décision par fichier, nommé `NNNN-resume-en-kebab-case.md`.
* Chaque ADR existe en **anglais et en français** : `NNNN-....md` et
  `NNNN-....fr.md`, liées entre elles dans leur en-tête. Le fichier anglais fait
  foi.
* L'en-tête porte **une ligne datée par état réellement atteint** par la
  décision, et aucune date n'est jamais écrasée
  ([ADR-0036](0036-keep-one-dated-line-per-state-an-adr-reached.fr.md)).
* Le statut vaut *Proposed*, *Accepted*, *Superseded* ou *Deprecated*.
* Une **supersession** s'écrit des deux côtés et n'ajoute aucune date. Le statut
  de l'enregistrement remplacé devient `Superseded by` suivi d'un lien vers le
  successeur ; rien d'autre ne change chez lui : ses dates restent, et son corps
  n'est jamais retouché sur le fond, car une décision acceptée est une pièce d'histoire et
  non un brouillon. Le successeur porte une ligne `Supersède l'…` sous son
  en-tête, qui pointe en retour. Chaque côté pointe le jumeau de sa propre
  langue. Le couple de référence est
  [ADR-0029](0029-let-a-size-maximum-cap-without-steering-the-draw.fr.md) et
  [ADR-0076](0076-let-a-declared-maximum-steer-the-size-draw.fr.md).
* La citation d'un enregistrement remplacé ne se repinne **pas**. Sa ligne de
  statut nomme le successeur, et c'est cela qu'un lecteur suit ; une citation ne
  bouge que si ce qu'elle affirme a cessé d'être vrai. Écrire le successeur à côté
  est une correction éditoriale, et sur une page utilisateur c'est obligatoire :
  une page sous `doc/handwritten/for-users/` nomme le successeur sur la même
  ligne, ce que vérifie la suite de documentation
  ([ADR-0115](0115-allow-an-editorial-correction-to-an-accepted-adr.fr.md)).
* Un enregistrement accepté reçoit des corrections **éditoriales** — une coquille,
  un lien mort ou déplacé, la mise en forme, un jumeau français réaligné sur
  l'anglais, le successeur nommé à côté d'une citation — et jamais un changement
  de fond, qui passe par un nouvel enregistrement. Une retouche est éditoriale
  quand l'enregistrement, lu à la date de son acceptation, dit exactement ce qu'il
  disait avant
  ([ADR-0115](0115-allow-an-editorial-correction-to-an-accepted-adr.fr.md)).
* La citation d'une ADR qui ne vit que dans l'autre dépôt se qualifie
  `ADR-00NN (first-class-errors)` ; un numéro non qualifié désigne toujours cette
  base.

## Commencer ici

Un enregistrement ne porte pas sur une fonctionnalité mais sur la façon dont
toute question de fonctionnalité est tranchée :
[**ADR-0046 — Borner l'ambition du générateur, jamais sa correction**](0046-bound-the-generators-ambition-never-its-correctness.fr.md).
Sept des décisions ci-dessous bornent chacune une surface ou un effort ; l'ADR-0046 est la règle
qu'elles partagent, et c'est la réponse par défaut à *« le générateur devrait-il traiter ce cas
aussi ? »*. Lisez-la avant les autres. Son numéro est tardif parce que
[les numéros sont ici des poignées stables, non un ordre de lecture](0045-renumber-the-decision-base.fr.md)
et qu'elle a été décidée le 01/08/2026 — l'index porte l'ordre, la numérotation porte l'identité.

## Envisagé, non retenu

Certaines options sont refusées à dessein, et chaque refus porte la condition qui le rouvrirait.
Elles sont listées ici pour qu'un signalement venu du terrain rencontre l'enregistrement plutôt
qu'une page blanche — le mode de défaillance d'un « pas maintenant » est que personne ne se souvienne
que l'option existait le jour où elle compterait.

| Option | Enregistrement | Ce qui la rouvrirait |
|---|---|---|
| Le graphe de flot de contrôle de Roslyn pour le placement d'une garde | [ADR-0084](0084-place-a-guard-by-syntax-reach-not-a-control-flow-graph.fr.md) | Un constructeur issu d'un vrai code que la règle de placement refuse et dont l'auteur s'en est plaint, une seconde question du moteur ayant besoin du même graphe, ou l'entrée dans un gestionnaire d'exception s'avérant une arête ordinaire au plancher Roslyn. L'enregistrement porte la signature à laquelle confronter un signalement. |
| Une règle signalant une valeur tirée qui atteint une assertion | [Essai UX, AN-C1](../../../../tools/ux-trial/REPORT.fr.md#an-c1) | Un moyen de reconnaître une assertion sans dépendre d'aucune bibliothèque d'assertion : sinon la règle serait liée aux bibliothèques qu'elle nomme, dont la bibliothèque elle-même ne dépend jamais. |
| Une contrainte de jour ouvré ou de jour de la semaine sur les dates | [Essai UX, CORE-C3](../../../../tools/ux-trial/REPORT.fr.md#core-c3) | Des retours du terrain, au-delà des deux domaines de l'essai, demandant un filtre de jour de la semaine ; les calendriers fériés sont des données métier et restent hors périmètre ([ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md)). |
| Un membre tirant un `Guid` version 4 (RFC 9562) | [Essai UX, CORE-C5](../../../../tools/ux-trial/REPORT.fr.md#core-c5) | Plus d'une base de code qui vérifie `Guid.Version` ; un seul domaine l'a rencontré, et un masque via `.As(...)` le couvre. |
| Un marqueur public permettant à un générateur écrit à la main de déclarer qu'il tire depuis la graine ambiante | [Essai UX, CORE-C10](../../../../tools/ux-trial/REPORT.fr.md#core-c10) | Un utilisateur ayant besoin du conseil de rejeu complet pour son propre générateur ; #313 a déjà retiré le message trompeur, et un marqueur ne pourrait pas prouver que le générateur tire via `Any`. |
| Un point d'extension `partial` qui survit à `any generate --force` | [Essai UX, CLI-E1](../../../../tools/ux-trial/REPORT.fr.md#cli-e1) ; [Essai UX 2, CLI-8](../../../../tools/ux-trial/REPORT-2.fr.md#cli-8) | Un changement du §1 de la spécification d'`any` : le fichier scaffoldé appartient au développeur, donc le modifier est l'usage normal et `--force` jette ces modifications par conception. Le second essai a rencontré la perte dans 15 contextes ; la réponse a été `OrderedPair` et `Then` dans le cœur, générés là où c'est simple (#404, #405), pas une histoire de régénération. |
| Une règle signalant au build une construction regex non prise en charge | [Essai UX, AN-C5](../../../../tools/ux-trial/REPORT.fr.md#an-c5) | Des motifs refusés atteignant régulièrement la CI ; le refus à l'exécution nomme la construction, sa position et la réécriture (#351), et la règle embarquerait une copie du parseur. |
| Un rapport d'échec qui liste les valeurs tirées | [Essai UX 2, XU-2](../../../../tools/ux-trial/REPORT-2.fr.md#xu-2) | Un échec que le rejeu de la graine ne reproduit pas sous débogueur ; le rejeu redonne déjà chaque valeur, exactement. |
| Une portée qui retient les valeurs tirées pour garder distincts les tirages suivants | [Essai UX 2, CORE-2](../../../../tools/ux-trial/REPORT-2.fr.md#core-2) | Un cas qu'un ensemble tenu par le test, passé à `Except(IEnumerable<T>)` (#393), ne sait pas exprimer ; un générateur à état s'épuise et élargit l'ambition ([ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md)). |
| Vérifier d'avance la cardinalité de la clé d'un `DistinctBy` | [Essai UX 2, CORE-3](../../../../tools/ux-trial/REPORT-2.fr.md#core-3) | Une clé dont la bibliothèque pourrait connaître le domaine sans lire à travers la lambda de sélection ; en attendant, le retirage borné échoue bruyamment avec sa graine. |
| `any` qui lit une garde `a.Equals(b)` pour émettre un retirage | [Essai UX 2, CORE-4](../../../../tools/ux-trial/REPORT-2.fr.md#core-4) | Des retours terrain montrant que `DifferentFrom` sur tout `IAny<T>` (#395), écrit au point d'appel, ne suffit pas. |
| Une fabrique qui choisit entre plusieurs générateurs | [Essai UX 2, CORE-12](../../../../tools/ux-trial/REPORT-2.fr.md#core-12) | Des choix entre assez de générateurs pour que `Then` (#396) cesse de bien se lire. |
| `Any.SubsetOf(items)`, et un `SetOf(item, n)` à taille positionnelle | [Essai UX 2, CORE-7](../../../../tools/ux-trial/REPORT-2.fr.md#core-7) | Un sous-ensemble que `SetOf(ElementOf(items)).WithCount(n)` n'exprime pas ; une taille reste une contrainte nommée, et le second argument de `SetOf` est un comparer. |
| Restreindre un `AnyPattern` (`StartingWith` et consorts) | [Essai UX 2, CORE-9](../../../../tools/ux-trial/REPORT-2.fr.md#core-9) | Un besoin qu'une recette nommée reformulant le motif plus étroit ne couvre pas ; restreindre revient à intersecter des langages réguliers ([ADR-0008](0008-generate-strings-from-a-home-grown-regular-subset.fr.md), [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md)). |
| Un décalage général sur `MultipleOf` (`CongruentTo`) | [Essai UX 2, CORE-11](../../../../tools/ux-trial/REPORT-2.fr.md#core-11) | Des retours terrain demandant un reste autre que les impairs que couvre `Odd()` (#401). |
| Un `OrderedTriple` pour trois valeurs liées par un ordre | [Essai UX 2, CLI-8](../../../../tools/ux-trial/REPORT-2.fr.md#cli-8) | Un retour de terrain que la paire et un `Then` tirant la valeur du milieu ne servent pas ([ADR-0126](0126-draw-two-values-tied-by-an-order.fr.md)) ; une paire `TimeOnly` attend un retour qui dise quelle lecture de l'ordre il lui faut. |
| Une règle entre éléments successifs d'une collection | [Essai UX 2, CORE-13](../../../../tools/ux-trial/REPORT-2.fr.md#core-13) | Un cas que ni `Ordered`/`OrderedBy` (#360) ni le tirage de listes ordonnées appariées n'expriment, rencontré de façon répétée ; la règle générale est de la propagation de contraintes ([ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md)). |
| Une répartition logarithmique des tirages flottants | [Essai UX 2, CORE-14](../../../../tools/ux-trial/REPORT-2.fr.md#core-14) | Une plage déclarée sur de nombreuses décades qui doit atteindre les plus basses, au-delà de déclarer une plage plus étroite ; avec une borne à 0, une échelle logarithmique tire quelque 300 décades sous 1, et choisir une distribution élargit l'ambition. |
| `any` qui dérive une relation générale entre deux paramètres | [Essai UX 2, CLI-8](../../../../tools/ux-trial/REPORT-2.fr.md#cli-8) | Une relation autre qu'un ordre (#404, #405) assez fréquente pour avoir une construction exacte ; le cas général est un solveur ([ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md)). |
| Une commande `any accept` qui supprime les blocs TODO vérifiés | [Essai UX 2, CLI-24](../../../../tools/ux-trial/REPORT-2.fr.md#cli-24) | Un changement du §1 de la spécification d'`any` : l'outil ne touche plus jamais un fichier scaffoldé. |
| Raffiner la recette des éléments d'une collection à travers la lambda de raffinement | [Essai UX 2, CLI-26](../../../../tools/ux-trial/REPORT-2.fr.md#cli-26) | Des raffinements d'éléments assez fréquents pour que les surcharges du fichier partiel deviennent une charge ; chaque builder de collection devrait porter le type exact de son générateur d'élément. |

## Index

| ADR | Titre | Statut | Origine |
|---|---|---|---|
| [ADR-0001](0001-lock-the-analyzer-roslyn-floor.fr.md) | Verrouiller le plancher Roslyn de l'analyseur | Accepted | adopté · FCE ADR-0001 |
| [ADR-0002](0002-check-every-pull-request-against-the-adr-base.fr.md) | Contrôler chaque pull request au regard de la base d'ADR | Accepted | adopté · FCE ADR-0004 |
| [ADR-0003](0003-host-dummies-as-a-standalone-package.fr.md) | Héberger JustDummies comme package autonome dans ce dépôt | Accepted | déplacé · FCE ADR-0011 |
| [ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md) | Contrôler les collections distinctes par la cardinalité, sinon par un tirage borné | Accepted | déplacé · FCE ADR-0013 |
| [ADR-0005](0005-cap-any-combine-at-arity-eight.fr.md) | Plafonner Any.Combine à l'arité huit | Accepted | déplacé · FCE ADR-0015 |
| [ADR-0006](0006-materialize-dummies-only-through-generate.fr.md) | Matérialiser les dummies uniquement via Generate() | Accepted | déplacé · FCE ADR-0020 |
| [ADR-0007](0007-floor-the-library-on-net-framework-4-7-2.fr.md) | Fixer le plancher .NET Framework de la bibliothèque à 4.7.2 | Accepted | déplacé · FCE ADR-0022 |
| [ADR-0008](0008-generate-strings-from-a-home-grown-regular-subset.fr.md) | Générer les chaînes qui matchent depuis un sous-ensemble régulier maison | Accepted | déplacé · FCE ADR-0025 |
| [ADR-0009](0009-draw-arbitrary-strings-from-an-explicit-terminal-set.fr.md) | Tirer des chaînes arbitraires depuis un ensemble de valeurs explicite et terminal | Superseded by ADR-0033 | déplacé · FCE ADR-0030 |
| [ADR-0010](0010-name-any-factories-after-their-clr-type.fr.md) | Nommer les fabriques scalaires de Any d'après leur type CLR | Accepted | déplacé · FCE ADR-0031 |
| [ADR-0011](0011-draw-arbitrary-values-from-an-explicit-top-level-pool.fr.md) | Tirer des valeurs arbitraires depuis un pool de choix explicite et de premier niveau | Accepted | déplacé · FCE ADR-0032 |
| [ADR-0012](0012-meet-string-exclusions-with-a-bounded-redraw.fr.md) | Traiter les exclusions de chaînes par un tirage borné | Accepted | déplacé · FCE ADR-0033 |
| [ADR-0013](0013-require-a-scope-on-the-version-driving-commit-types.fr.md) | Exiger un scope sur les types de commit qui pilotent la version | Accepted | adopté · FCE ADR-0034 |
| [ADR-0014](0014-enforce-structural-any-conflicts-at-compile-time.fr.md) | Détecter les conflits structurels de Any à la compilation, ceux dépendant de la valeur à l'exécution | Accepted | déplacé · FCE ADR-0035 |
| [ADR-0015](0015-draw-lattice-constrained-scalars-on-the-grid.fr.md) | Tirer les scalaires contraints à un réseau sur la grille | Accepted | déplacé · FCE ADR-0036 |
| [ADR-0016](0016-vary-the-datetimeoffset-offset-dimension.fr.md) | Faire varier la dimension d'offset de DateTimeOffset | Superseded by ADR-0030 | déplacé · FCE ADR-0037 |
| [ADR-0017](0017-open-the-ambient-seed-scope-to-adapters.fr.md) | Ouvrir la portée de graine ambiante aux adaptateurs de framework de test | Accepted | déplacé · FCE ADR-0038 |
| [ADR-0018](0018-adapt-dummies-to-xunit-v3-through-a-companion-package.fr.md) | Adapter JustDummies à xUnit v3 via un package compagnon | Accepted | déplacé · FCE ADR-0039 |
| [ADR-0019](0019-split-the-justdummies-test-bed-between-example-and-property-suites.fr.md) | Répartir le banc de test de JustDummies entre une suite par l'exemple et une suite par propriétés | Accepted | déplacé · FCE ADR-0040 |
| [ADR-0020](0020-draw-flag-enum-combinations-behind-an-opt-in.fr.md) | Tirer les combinaisons d'enums de drapeaux derrière un opt-in | Accepted | déplacé · FCE ADR-0041 |
| [ADR-0021](0021-serialize-draws-on-a-random-source.fr.md) | Sérialiser les tirages sur une source aléatoire, et borner la reproductibilité à la séquence de tirages | Accepted | déplacé · FCE ADR-0042 |
| [ADR-0022](0022-gate-pull-requests-on-the-mutation-score-of-the-diff.fr.md) | Conditionner les pull requests au score de mutation de ce qu'elles modifient | Accepted | adopté · FCE ADR-0043 |
| [ADR-0023](0023-ship-justdummies-analyzers.fr.md) | Fournir des analyseurs JustDummies maison, et garder avec eux la surface asynchrone reproductible | Accepted | déplacé · FCE ADR-0044 |
| [ADR-0024](0024-guard-public-and-internal-arguments-against-null.fr.md) | Garder contre le null les arguments publics et internes, imposé par une convention par réflexion | Superseded by ADR-0041 | déplacé · FCE ADR-0045 |
| [ADR-0025](0025-make-the-per-pull-request-mutation-gate-advisory.fr.md) | Rendre la porte de mutation par pull request consultative | Accepted | adopté · FCE ADR-0046 |
| [ADR-0026](0026-measure-justdummies-mutation-against-the-unit-suite-only.fr.md) | Mesurer la mutation de JustDummies contre la seule suite unitaire déterministe | Accepted | déplacé · FCE ADR-0047 |
| [ADR-0027](0027-guarantee-a-generated-regex-value-matches-by-bounded-redraw.fr.md) | Garantir qu'une valeur regex générée matche son pattern, par redraw borné | Accepted | déplacé · FCE ADR-0048 |
| [ADR-0028](0028-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.fr.md) | Retirer le générateur JustDummies de la matrice de mutation par pull request | Accepted | déplacé · FCE ADR-0049 |
| [ADR-0029](0029-let-a-size-maximum-cap-without-steering-the-draw.fr.md) | Laisser un maximum de taille plafonner sans piloter le tirage, et plafonner une taille explicitement demandée | Superseded by ADR-0076 | déplacé · FCE ADR-0050 |
| [ADR-0030](0030-filter-the-datetimeoffset-pool-by-the-declared-offset.fr.md) | Filtrer le pool DateTimeOffset par le décalage déclaré | Accepted | déplacé · FCE ADR-0051 |
| [ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.fr.md) | Tirer les nombres arbitraires dans une magnitude ordinaire | Superseded by ADR-0097 | déplacé · FCE ADR-0052 |
| [ADR-0032](0032-unify-discrete-generation-in-one-ordinal-space.fr.md) | Unifier la génération discrète dans un espace ordinal unique, avec un moteur dédié seulement là où le substrat arithmétique l'impose | Accepted | déplacé · FCE ADR-0053 |
| [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.fr.md) | Décider la surface de contraintes d'un générateur par constructif contre rejectif, et non par terminalité | Accepted | déplacé · FCE ADR-0054 |
| [ADR-0034](0034-enforce-the-style-rules-the-compiler-can-express.fr.md) | Faire appliquer par le compilateur les règles de style qu'il sait exprimer, et laisser le DotSettings faire autorité pour les autres | Accepted | adopté · FCE ADR-0055 |
| [ADR-0035](0035-state-the-coding-rules-where-an-agent-can-act-on-them.fr.md) | Énoncer les règles de codage là où un agent peut les appliquer, et les vérifier à l'édition | Accepted | adopté · FCE ADR-0056 |
| [ADR-0036](0036-keep-one-dated-line-per-state-an-adr-reached.fr.md) | Garder une ligne datée par état atteint par une ADR, et n'en écraser aucune | Accepted | adopté · FCE ADR-0057 |
| [ADR-0037](0037-suppress-ca1510-while-the-netstandard-floor-stands.fr.md) | Supprimer CA1510 tant que le plancher antérieur à .NET 6 tient | Accepted | déplacé · FCE ADR-0058 |
| [ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.fr.md) | Garder la frontière recette/valeur avec des analyseurs là où le système de types ne l'atteint pas | Accepted | déplacé · FCE ADR-0059 |
| [ADR-0039](0039-derive-the-build-rule-set-from-the-quality-profile.fr.md) | Dériver le jeu de règles Sonar du build depuis le profil qualité | Accepted | adopté · FCE ADR-0062 |
| [ADR-0040](0040-throw-the-library-s-own-exceptions-through-named-factories.fr.md) | Lever les exceptions de la bibliothèque via des factories nommées | Accepted | déplacé · FCE ADR-0063 |
| [ADR-0041](0041-exempt-the-whole-failure-reporting-path-from-the-null-guard-convention.fr.md) | Exempter tout le chemin de report d'échec de la convention de garde null | Accepted | déplacé · FCE ADR-0064 |
| [ADR-0042](0042-carry-a-declared-constraint-as-a-value-object.fr.md) | Porter une contrainte déclarée comme objet-valeur, non comme son texte rendu | Accepted | déplacé · FCE ADR-0065 |
| [ADR-0043](0043-declare-a-value-object-and-enforce-its-identity.fr.md) | Déclarer un objet-valeur par un attribut, et faire respecter son identité par convention | Accepted | déplacé · FCE ADR-0066 |
| [ADR-0044](0044-extract-justdummies-into-its-own-repository.fr.md) | Extraire JustDummies dans son propre dépôt | Accepted | consigné ici |
| [ADR-0045](0045-renumber-the-decision-base.fr.md) | Renuméroter la base de décisions en une séquence contiguë | Accepted | consigné ici |
| [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) | Borner l'ambition du générateur, jamais sa correction | Accepted | consigné ici |
| [ADR-0047](0047-declare-the-adapters-library-dependency-independently.fr.md) | Déclarer la dépendance de l'adaptateur à la bibliothèque indépendamment de la version packagée | Accepted | consigné ici |
| [ADR-0048](0048-publish-only-from-a-commit-that-is-on-main.fr.md) | Ne publier qu'à partir d'un commit présent sur main | Accepted | consigné ici |
| [ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.fr.md) | Rejouer une graine à travers les versions patch et mineures | Accepted | consigné ici |
| [ADR-0050](0050-name-a-suppressed-rule-through-a-catalogue-constant.fr.md) | Nommer une règle supprimée par une constante de catalogue, pas par une chaîne littérale | Accepted | consigné ici |
| [ADR-0051](0051-land-pull-requests-by-rebase.fr.md) | Intégrer les pull requests par rebase | Accepted | consigné ici |
| [ADR-0052](0052-publish-the-jd-rules-as-a-first-party-catalogue.fr.md) | Publier les règles JD comme catalogue maison, et y lire les descripteurs | Accepted | consigné ici |
| [ADR-0053](0053-rewrite-the-published-history-to-a-single-line.fr.md) | Réécrire l'historique publié en une seule ligne, et y porter les tags de release | Accepted | consigné ici |
| [ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.fr.md) | Ne tirer que des valeurs valides depuis un builder typé, et ne rien juger dans un pool fourni par l'appelant | Accepted | consigné ici |
| [ADR-0055](0055-hold-the-user-documentation-to-contracts-the-build-checks.fr.md) | Tenir la documentation utilisateur à des contrats que le build vérifie | Accepted | consigné ici |
| [ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.fr.md) | Scaffolder le generator une fois et confier le fichier au développeur | Accepted | consigné ici |
| [ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.fr.md) | Faire du generator émis un `IAny<T>` de plein droit | Accepted | consigné ici |
| [ADR-0058](0058-leave-the-scaffolded-file-open-to-the-analyzers.fr.md) | Laisser le fichier scaffoldé ouvert aux analyzers JustDummies | Accepted | consigné ici |
| [ADR-0059](0059-emit-only-members-resolved-in-the-target-compilation.fr.md) | N'émettre que des membres résolus dans la compilation cible | Accepted | consigné ici |
| [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) | Amorcer les generators sur les gardes du constructeur, et laisser le reste en erreur de compilation | Accepted | consigné ici |
| [ADR-0061](0061-draw-from-the-ambient-context-and-hold-no-state.fr.md) | Tirer du contexte ambiant et ne détenir aucun état | Accepted | consigné ici |
| [ADR-0062](0062-emit-the-generator-into-the-target-types-namespace.fr.md) | Émettre le generator dans le namespace du type cible | Accepted | consigné ici |
| [ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.fr.md) | Ne donner au scaffolder aucune dépendance sur le package JustDummies | Accepted | consigné ici |
| [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.fr.md) | Ne jamais tirer null pour un paramètre nullable | Superseded by ADR-0117 | consigné ici |
| [ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.fr.md) | Garder le moteur de scaffolding chargeable par un hôte Roslyn | Accepted | consigné ici |
| [ADR-0066](0066-load-msbuild-from-the-sdk-never-from-the-tool.fr.md) | Charger MSBuild depuis le SDK installé, jamais depuis les fichiers de l'outil | Accepted | consigné ici |
| [ADR-0067](0067-report-a-filtered-pool-through-an-explicit-interface.fr.md) | Rendre compte d'un pool filtré par une interface implémentée explicitement, et n'avertir de rien | Accepted | consigné ici |
| [ADR-0068](0068-carry-the-pool-inspection-wherever-a-caller-supplies-the-values.fr.md) | Porter l'inspection de pool partout où l'appelant fournit les valeurs, et nulle part ailleurs | Accepted | consigné ici |
| [ADR-0069](0069-answer-a-cardinality-bound-under-the-comparer-that-will-use-it.fr.md) | Répondre à une borne de cardinalité sous le comparateur qui s'en servira | Accepted | consigné ici |
| [ADR-0070](0070-emit-an-entry-point-on-request-as-a-file-of-its-own.fr.md) | Émettre un point d'entrée à la demande, dans un fichier à lui | Accepted | consigné ici |
| [ADR-0071](0071-report-a-run-as-data-without-moving-the-exit-codes.fr.md) | Rendre compte d'une exécution en données sans déplacer les codes de sortie | Accepted | consigné ici |
| [ADR-0072](0072-read-project-defaults-from-a-file-the-command-line-overrides.fr.md) | Lire les défauts de projet dans un fichier que la ligne de commande surcharge | Accepted | consigné ici |
| [ADR-0073](0073-layer-the-agent-instructions-by-when-they-are-needed.fr.md) | Étager les instructions destinées aux agents selon le moment où elles servent | Accepted | consigné ici |
| [ADR-0074](0074-draft-a-releases-github-notes-by-hand-and-refuse-without-them.fr.md) | Rédiger à la main les notes GitHub d'une release à partir du changelog, et refuser sans elles | Accepted | consigné ici |
| [ADR-0075](0075-draw-characters-from-the-whole-of-ascii.fr.md) | Tirer les caractères dans tout l'ASCII, et ne rétrécir que par une famille nommée | Accepted | consigné ici |
| [ADR-0076](0076-let-a-declared-maximum-steer-the-size-draw.fr.md) | Laisser un maximum déclaré piloter le tirage de taille | Accepted | consigné ici |
| [ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.fr.md) | Admettre une règle JD qui signale une écriture correcte, bornée par un équivalent nommé exact | Superseded by ADR-0080 | consigné ici |
| [ADR-0078](0078-own-a-bound-declared-twice-as-one-rule.fr.md) | Confier à une seule règle la borne déclarée deux fois, et en retirer JD024 | Accepted | consigné ici |
| [ADR-0079](0079-constrain-what-a-dummy-draws-never-the-literals-it-was-given.fr.md) | Contraindre ce qu'un dummy tire, jamais les littéraux qu'on lui a donnés | Accepted | consigné ici |
| [ADR-0080](0080-admit-a-rule-that-names-a-resolved-ambiguity.fr.md) | Admettre une règle JD qui nomme une ambiguïté que la bibliothèque tranche, à côté de l'équivalent plus court | Accepted | consigné ici |
| [ADR-0081](0081-declare-one-top-level-type-per-file.fr.md) | Déclarer un seul type de premier niveau par fichier, via un analyseur de style tiers | Accepted | consigné ici |
| [ADR-0082](0082-answer-for-the-finished-chain-not-each-constraint.fr.md) | Répondre de la chaîne finie, pas de chaque contrainte lue | Accepted | consigné ici |
| [ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.fr.md) | Bloquer la compilation sur une garde que le moteur ne peut pas cautionner | Accepted | consigné ici |
| [ADR-0084](0084-place-a-guard-by-syntax-reach-not-a-control-flow-graph.fr.md) | Placer une garde par portée syntaxique, non par un graphe de flot de contrôle | Accepted | consigné ici |
| [ADR-0085](0085-change-the-guard-reader-only-against-a-field-report.fr.md) | Ne changer le lecteur de gardes que contre un signalement du terrain | Accepted | consigné ici |
| [ADR-0086](0086-read-the-guard-helpers-of-named-libraries.fr.md) | Lire les aides de garde de bibliothèques nommées, dans leurs deux orthographes | Accepted | consigné ici |
| [ADR-0087](0087-check-a-documented-count-against-its-source-not-its-translation.fr.md) | Vérifier un décompte documenté contre sa source, non contre sa traduction | Accepted | consigné ici |
| [ADR-0088](0088-state-the-whitespace-guard-with-a-member-of-its-own.fr.md) | Énoncer la garde de blancheur avec un membre à elle | Accepted | consigné ici |
| [ADR-0089](0089-draw-a-composed-parameter-through-the-generator-its-type-owns.fr.md) | Tirer un paramètre composé par le generator que son type possède | Accepted | consigné ici |
| [ADR-0090](0090-exempt-dependabot-from-the-contributor-agreement.fr.md) | Exempter Dependabot de l'accord de contribution, uniquement sur son propre commit signé | Accepted | consigné ici |
| [ADR-0091](0091-draw-a-half-from-the-values-it-can-represent.fr.md) | Tirer un `Half` parmi les valeurs qu'il sait représenter | Accepted | consigné ici |
| [ADR-0092](0092-run-every-mutation-leg-from-its-own-source-project.fr.md) | Lancer chaque job de mutation depuis son propre projet source | Accepted | consigné ici |
| [ADR-0093](0093-publish-mutation-statuses-not-a-score.fr.md) | Publier des statuts de mutation, pas un score | Accepted | consigné ici |
| [ADR-0094](0094-lift-a-nullable-value-type-rather-than-deriving-it.fr.md) | Lever un type valeur nullable plutôt que le dériver | Accepted | consigné ici |
| [ADR-0095](0095-read-the-assigned-null-check-as-a-guard-idiom-too.fr.md) | Lire aussi le null-check assigné comme un idiome de garde | Accepted | consigné ici |
| [ADR-0096](0096-emit-a-parameter-inline-whenever-it-has-nothing-to-report.fr.md) | Émettre un paramètre en ligne dès qu'il n'a rien à rapporter | Superseded by ADR-0109 | consigné ici |
| [ADR-0097](0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.fr.md) | Faire suivre les bornes déclarées par la fenêtre de magnitude ordinaire | Superseded by ADR-0117 | consigné ici |
| [ADR-0098](0098-offer-every-oneof-in-both-shapes.fr.md) | Offrir chaque OneOf sous les deux formes, params et séquence | Accepted | consigné ici |
| [ADR-0099](0099-offer-oneof-wherever-the-generator-would-otherwise-build.fr.md) | Offrir OneOf partout où le générateur construirait sinon la valeur | Accepted | consigné ici |
| [ADR-0100](0100-identify-a-value-set-by-its-values.fr.md) | Identifier un ensemble de valeurs déclaré par ses valeurs, non par l'appel qui l'a déclaré | Accepted | consigné ici |
| [ADR-0101](0101-accept-a-non-finite-exclusion-as-a-no-op.fr.md) | Accepter l'exclusion d'une valeur non finie sur un générateur flottant comme une opération sans effet | Accepted | consigné ici |
| [ADR-0102](0102-let-a-collection-or-composition-inherit-its-operands-source.fr.md) | Laisser une collection ou une composition hériter de la source de ses opérandes plutôt que de celle d'un contexte | Accepted | consigné ici |
| [ADR-0103](0103-run-the-suites-on-microsoft-testing-platform.fr.md) | Exécuter les suites de tests sur Microsoft.Testing.Platform | Accepted | consigné ici |
| [ADR-0104](0104-count-null-as-one-additional-value-in-an-optional-generators-domain.fr.md) | Compter `null` comme une valeur supplémentaire dans le domaine d'un générateur optionnel | Accepted | consigné ici |
| [ADR-0105](0105-accept-an-undeclared-enum-exclusion-as-a-no-op.fr.md) | Accepter l'exclusion d'une valeur non déclarée sur un enum comme une opération sans effet | Accepted | consigné ici |
| [ADR-0106](0106-test-the-analyzers-against-the-run-time-in-a-sibling-suite.fr.md) | Tester les analyseurs contre l'exécution dans une suite sœur | Accepted | consigné ici |
| [ADR-0107](0107-own-a-constraint-repeated-identically-as-one-rule.fr.md) | Confier à une seule règle la contrainte répétée à l'identique, et en retirer JD024 | Accepted | consigné ici |
| [ADR-0108](0108-name-the-scaffolder-after-what-it-draws.fr.md) | Nommer le scaffolder d'après ce qu'il tire, non d'après le rôle d'une valeur | Accepted | consigné ici |
| [ADR-0109](0109-let-a-test-refine-a-parameters-valid-base-rather-than-replace-it.fr.md) | Laisser un test affiner la base valide d'un paramètre plutôt que la remplacer | Accepted | consigné ici |
| [ADR-0110](0110-mark-every-parameter-a-throw-may-depend-on.fr.md) | Marquer tout paramètre dont un throw peut dépendre, et tous là où la piste se perd | Accepted | consigné ici |
| [ADR-0111](0111-own-an-undefined-enum-value-as-jd017.fr.md) | Confier à JD017 la valeur d'enum non définie d'un ensemble de valeurs, et en retirer JD014 | Accepted | consigné ici |
| [ADR-0112](0112-report-a-test-asserting-a-refusal-like-any-other-code.fr.md) | Signaler un test qui vérifie un refus comme tout autre code, et le laisser faire taire la règle explicitement | Accepted | consigné ici |
| [ADR-0113](0113-check-a-distinct-collections-cardinality-when-the-specification-is-whole.fr.md) | Contrôler la cardinalité d'une collection distincte quand la spécification est complète | Accepted | consigné ici |
| [ADR-0114](0114-refuse-a-local-datetime-bound.fr.md) | Refuser une borne `DateTime` `Local` plutôt que la lire en ticks | Accepted | consigné ici |
| [ADR-0115](0115-allow-an-editorial-correction-to-an-accepted-adr.fr.md) | Permettre une correction éditoriale d'une ADR acceptée, jamais un changement de fond | Accepted | consigné ici |
| [ADR-0116](0116-accumulate-a-parameters-refinements-on-its-current-recipe.fr.md) | Accumuler les affinages d'un paramètre sur sa recette courante | Proposed | consigné ici |
| [ADR-0117](0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.fr.md) | Tirer une valeur limite que le type permet une fois sur dix | Accepted | consigné ici |
| [ADR-0118](0118-return-a-literal-through-any-fixed.fr.md) | Renvoyer un littéral par Any.Fixed | Accepted | consigné ici |
| [ADR-0119](0119-draw-a-value-that-depends-on-another-draw.fr.md) | Tirer une valeur qui dépend d'un autre tirage | Accepted | consigné ici |
| [ADR-0120](0120-report-a-draw-made-inside-a-composition-lambda.fr.md) | Signaler un tirage fait dans une lambda de composition | Accepted | consigné ici |
| [ADR-0121](0121-exclude-a-value-from-any-generator-by-redrawing.fr.md) | Exclure une valeur de tout générateur en retirant | Accepted | consigné ici |
| [ADR-0122](0122-accept-an-empty-exclusion-as-the-identity.fr.md) | Accepter une exclusion vide comme l'identité | Accepted | consigné ici |
| [ADR-0123](0123-draw-a-decimal-on-a-steps-grid.fr.md) | Tirer un décimal sur la grille d'un pas | Accepted | consigné ici |
| [ADR-0124](0124-arrange-a-drawn-list-in-ascending-order.fr.md) | Ranger une liste tirée dans l'ordre croissant | Accepted | consigné ici |
| [ADR-0125](0125-let-a-time-of-day-window-cross-midnight.fr.md) | Laisser une fenêtre horaire franchir minuit | Accepted | consigné ici |
| [ADR-0126](0126-draw-two-values-tied-by-an-order.fr.md) | Tirer deux valeurs liées par un ordre | Accepted | consigné ici |
