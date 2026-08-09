# Relevé de migration — extraction depuis `Reefact/first-class-errors`

Ce répertoire est la piste d'audit de l'extraction unique qui a créé ce dépôt. Il existe pour que
l'historique réécrit puisse être réconcilié plus tard avec le dépôt source, par un mainteneur qui
n'était pas là. Décision : [ADR-0044](../adr/0044-extract-justdummies-into-its-own-repository.fr.md).

## Faits

| | |
| --- | --- |
| Dépôt source | `Reefact/first-class-errors` |
| `SOURCE_CUTOFF_SHA` | `fbf523b86acebdd34ba0bbfd437683864be3cb9c` (son `main` au moment de l'extraction) |
| Dépôt cible | `Reefact/just-dummies` |
| `TARGET_ORIGINAL_SHA` | `ef85c8ffcb2cc6696a78d000cbe1cbc5027719dd` (son unique `Initial commit`, LICENSE seule) |
| Sauvegarde de la cible d'origine | branche `archive/pre-history-extraction` |
| Outil | `git filter-repo` 2.47.0, exécuté sur un clone neuf, jamais sur un clone de travail |
| Commits avant / après | 1350 → 420 |
| Commits de merge préservés | 156 |
| Date d'extraction | 31/07/2026 |

Le dépôt source n'a jamais été force-pushé ni modifié directement par cette migration.

## Fichiers présents ici

| Fichier | Ce que c'est |
| --- | --- |
| `filter-repo-paths.txt` | la spécification de chemins exacte utilisée par le filtre, commentaires compris |
| `commit-map.txt` | ancien SHA → nouveau SHA pour les 1350 commits source (`0000…` = commit abandonné) |
| `ref-map.txt` | ancienne ref → nouvelle ref |
| `suboptimal-issues.txt` | le rapport de `filter-repo` sur les empreintes de commit citées dans des messages et qui n'existent plus |

## Comment la frontière a été décidée

Une liste de chemins bâtie sur les seuls noms de répertoires aurait été fausse trois fois ; la
frontière a donc été dérivée de l'historique lui-même :

* **Chemins renommés.** Le produit s'appelait `Dummies` avant de s'appeler `JustDummies`. Le filtre
  liste donc `Dummies/`, `Dummies.UnitTests/`, `Dummies.Xunit/`, `Dummies.Xunit.UnitTests/`,
  `tools/dummies-check/`, `.github/workflows/dummies.yml` et
  `specifications/dummies-generation.{en,fr}.md` à côté de leurs noms actuels. Les omettre aurait
  tronqué l'historique au renommage.

* **ADR renumérotées.** Quatre décisions ont changé de numéro sur place — `0010→0011`, `0043→0044`,
  `0048→0049`, `0050→0051` — et une était un brouillon créé puis abandonné
  (`0023-prune-the-exotic-width-numeric-generators`, commit *« docs(dummies): drop ADR-0023 draft »* —
  0023 était le numéro propre à ce brouillon dans la base partagée de l'époque, et n'a rien à voir
  avec l'ADR-0023 que porte `Reefact/first-class-errors` aujourd'hui). La spécification liste **tous
  les chemins qu'a jamais occupés chaque décision**, appariés par slug plutôt que par numéro.

* **Fichiers sans « dummies » dans leur chemin.** Les 56 pages de documentation des analyzers
  (`doc/handwritten/for-users/analyzers/JD001…JD028.{en,fr}.md`) et environ 25 ADR portant sur le
  moteur de génération `Any` ne portent aucun marqueur de ce genre. Ils ont été trouvés en lisant
  chaque titre d'ADR et en cherchant dans le *contenu* des fichiers, non dans les chemins.

Notez que `git log -- <chemin>` applique une simplification d'historique et ne rapporte
silencieusement rien pour certains de ces chemins ; `git log --full-history` ou un balayage brut de
l'arbre de chaque commit est nécessaire pour les voir. `filter-repo` lui-même ne simplifie pas : les
chemins listés sont donc appariés quoi qu'il arrive.

## Ce qui a été délibérément laissé derrière

| Conservé dans `Reefact/first-class-errors` | Pourquoi |
| --- | --- |
| [ADR-0026 (first-class-errors)](https://github.com/Reefact/first-class-errors/blob/main/doc/handwritten/for-maintainers/adr/0026-rebase-testing-arbitrary-values-on-dummies.md) *Rebase the testing package's arbitrary values on JustDummies* | son sujet est `FirstClassErrors.Testing` |
| [ADR-0061 (first-class-errors)](https://github.com/Reefact/first-class-errors/blob/main/doc/handwritten/for-maintainers/adr/0061-run-the-justdummies-analyzers-on-the-repository-s-own-code.md) *Run the JustDummies analyzers on the repository's own code* | son sujet est le build de ce dépôt-là |
| [ADR-0006 (first-class-errors)](https://github.com/Reefact/first-class-errors/blob/main/doc/handwritten/for-maintainers/adr/0006-supply-arbitrary-test-values-from-a-seedable-source.md) *Supply arbitrary test values from a single seedable source* | son sujet est le paquet compagnon `FirstClassErrors.Testing` |
| l'historique d'`icon.png` | ses deux seuls commits sont motivés par FirstClassErrors ; le fichier a été copié dans le commit d'amorçage à la place, pour qu'un commit sans rapport ne devienne pas la racine de ce dépôt |

ADR-0003 et ADR-0007 lient les deux produits et existent donc dans les **deux** dépôts.

## Infrastructure réécrite, non extraite

`Directory.Build.props`, `Directory.Packages.props`, `build/PublicApiBaseline.props`,
`build/Net472TestFloor.props`, `JustDummies.sln`, `tools/trains.sh`, `tools/packaging/pack.sh` et la
plupart des workflows contenaient un contenu substantiel propre à FirstClassErrors. Ils ont été
recréés dans le commit d'amorçage plutôt que repris, pour que le build de ce dépôt décrive ce dépôt.

`build/sonar-profile.globalconfig` est le seul fichier repris à l'octet près : il est généré depuis un
profil qualité SonarCloud, et le garder inchangé maintient le build sur le même jeu de règles
qu'avant l'extraction. Régénérez-le depuis le projet SonarCloud propre à ce dépôt une fois ce projet
créé.

## Suites connues

* ~~**la publication de confiance nuget.org n'est pas configurée**~~ — fait. La page qui la décrit
  n'a jamais été un artefact de migration et a été déplacée vers
  [`workflows/nuget-trusted-publishing.fr.md`](../workflows/nuget-trusted-publishing.fr.md).
* **Sonar, Scorecard et les workflows pilotés par Claude** (`adr-check`, `changelog`,
  `dependabot-autofix`) ont été portés tels quels et réclament `SONAR_TOKEN` / `ANTHROPIC_API_KEY`
  ainsi qu'un projet SonarCloud de clé `reefact_just-dummies`. Ils sont rouges jusque-là, par choix
  explicite : les porter garde l'intention visible plutôt que de l'abandonner en silence.
* **`tools/analyzer-count-check` n'a pas été porté.** Il vérifie qu'un README annonce le bon nombre
  d'analyzers ; `JustDummies/README.nuget.md` ne fait aucune annonce de ce genre, la vérification
  n'avait donc ici aucun invariant à garder. À réintroduire si le README se met à annoncer les 28
  règles.
* **Une branche source porte du travail JustDummies non mergé** :
  `agent/extract-adr-specifications` dans `Reefact/first-class-errors` (31 commits d'avance sur son
  `main`, 4 fichiers JustDummies touchés). Elle n'était pas incluse dans la coupe et devra être
  recréée ici à la main si elle est toujours souhaitée.
