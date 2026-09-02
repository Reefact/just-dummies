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
* **consigné ici** — décidé dans ce dépôt, de son propre chef. 51 décisions.

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
  de l'enregistrement superséedé devient `Superseded by` suivi d'un lien vers le
  successeur ; rien d'autre ne change chez lui : ses dates restent, et son corps
  n'est jamais retouché, car une décision acceptée est une pièce d'histoire et
  non un brouillon. Le successeur porte une ligne `Supersède l'…` sous son
  en-tête, qui pointe en retour. Chaque côté pointe le jumeau de sa propre
  langue. Le couple de référence est
  [ADR-0029](0029-let-a-size-maximum-cap-without-steering-the-draw.fr.md) et
  [ADR-0076](0076-let-a-declared-maximum-steer-the-size-draw.fr.md).
* La citation d'un enregistrement superséedé ne se repinne **pas**. Sa ligne de
  statut nomme le successeur, et c'est cela qu'un lecteur suit ; une citation ne
  bouge que si ce qu'elle affirme a cessé d'être vrai.
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
| [ADR-0023](0023-ship-justdummies-analyzers.fr.md) | Fournir des analyseurs JustDummies de première partie, et garder avec eux la surface asynchrone reproductible | Accepted | déplacé · FCE ADR-0044 |
| [ADR-0024](0024-guard-public-and-internal-arguments-against-null.fr.md) | Garder contre le null les arguments publics et internes, imposé par une convention par réflexion | Superseded by ADR-0041 | déplacé · FCE ADR-0045 |
| [ADR-0025](0025-make-the-per-pull-request-mutation-gate-advisory.fr.md) | Rendre la porte de mutation par pull request consultative | Accepted | adopté · FCE ADR-0046 |
| [ADR-0026](0026-measure-justdummies-mutation-against-the-unit-suite-only.fr.md) | Mesurer la mutation de JustDummies contre la seule suite unitaire déterministe | Accepted | déplacé · FCE ADR-0047 |
| [ADR-0027](0027-guarantee-a-generated-regex-value-matches-by-bounded-redraw.fr.md) | Garantir qu'une valeur regex générée matche son pattern, par redraw borné | Accepted | déplacé · FCE ADR-0048 |
| [ADR-0028](0028-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.fr.md) | Retirer le générateur JustDummies de la matrice de mutation par pull request | Accepted | déplacé · FCE ADR-0049 |
| [ADR-0029](0029-let-a-size-maximum-cap-without-steering-the-draw.fr.md) | Laisser un maximum de taille plafonner sans piloter le tirage, et plafonner une taille explicitement demandée | Superseded by ADR-0076 | déplacé · FCE ADR-0050 |
| [ADR-0030](0030-filter-the-datetimeoffset-pool-by-the-declared-offset.fr.md) | Filtrer le pool DateTimeOffset par le décalage déclaré | Accepted | déplacé · FCE ADR-0051 |
| [ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.fr.md) | Tirer les nombres arbitraires dans une magnitude ordinaire | Accepted | déplacé · FCE ADR-0052 |
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
| [ADR-0052](0052-publish-the-jd-rules-as-a-first-party-catalogue.fr.md) | Publier les règles JD comme catalogue first-party, et y lire les descripteurs | Accepted | consigné ici |
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
| [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.fr.md) | Ne jamais tirer null pour un paramètre nullable | Accepted | consigné ici |
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
| [ADR-0092](0092-run-every-mutation-leg-from-its-own-source-project.fr.md) | Lancer chaque jambe de mutation depuis son propre projet source | Proposed | consigné ici |
| [ADR-0093](0093-publish-mutation-statuses-not-a-score.fr.md) | Publier des statuts de mutation, pas un score | Proposed | consigné ici |
| [ADR-0094](0094-lift-a-nullable-value-type-rather-than-deriving-it.fr.md) | Lever un type valeur nullable plutôt que le dériver | Proposed | consigné ici |
