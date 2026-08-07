# Workflow `sonar`

🌍 🇬🇧 [English](sonar.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/sonar.yml`](../../../../.github/workflows/sonar.yml)

## À quoi il sert

`sonar` exécute l'analyse SonarQube Cloud : il alimente le **Quality Gate** et la
métrique de **couverture** affichés par les deux badges SonarCloud du README.
C'est la vue analyse-statique-plus-couverture du code, hébergée hors de GitHub.

## Quand il s'exécute

- À chaque **push sur `main`**.
- À chaque **pull request visant `main`** — **sauf les PR issues de forks et les
  exécutions déclenchées par Dependabot** (voir plus bas).
- À la demande via **`workflow_dispatch`**.

## Comment il s'exécute

Un seul job, `analyze`, sous Linux :

1. Checkout avec **`fetch-depth: 0`** — historique complet, pour que Sonar puisse
   attribuer les problèmes via `git blame` et distinguer le code neuf de
   l'ancien.
2. Installation de .NET **et de Java 17** — le SonarScanner for .NET tourne sur la
   JVM.
3. `dotnet-sonarscanner begin` → **build** → test avec couverture →
   `dotnet-sonarscanner end`.

## Permissions & sécurité

`contents: read` seulement. La décoration des PR (les commentaires Sonar en
ligne) est livrée par la **GitHub App SonarQube Cloud**, pas par le token de ce
workflow, donc aucun `pull-requests: write` n'est requis ici. L'analyse
s'authentifie avec le secret `SONAR_TOKEN`.

## À manipuler avec précaution

- **Le build doit se trouver *entre* `begin` et `end`.** Le scanner s'accroche à
  MSBuild pour observer la compilation ; il ne peut pas analyser une sortie
  pré-construite ou `--no-build`. Ne réordonnez pas ces étapes et n'ajoutez pas
  `--no-build` au build d'analyse.
- **Le build d'analyse désactive volontairement le cliquet de warnings.** Il
  passe `-p:TreatWarningsAsErrors=false -p:MSBuildTreatWarningsAsErrors=false`. Le
  scanner a besoin que la compilation **aille au bout** pour collecter les
  diagnostics `SonarAnalyzer` et les uploader dans `end` ; un warning de règle
  Sonar promu en erreur ferait échouer le build avant que les résultats ne soient
  remontés. Le cliquet reste imposé par [`ci`](../../../../.github/workflows/ci.yml) sur les deux branches OS
  — c'est ça le barrage, pas cette branche d'analyse.
- **Le garde-fou sur les secrets illisibles est nécessaire, pas optionnel.** Le
  `if` du job saute l'analyse pour les deux exécutions qui ne peuvent pas lire
  `SONAR_TOKEN`, parce que chacune échouerait sur un secret absent plutôt que sur
  un vrai problème :
  - **Les PR issues de forks** — `… head.repo.full_name == github.repository`.
    Une PR de fork ne reçoit jamais les secrets de ce dépôt.
  - **Les exécutions déclenchées par Dependabot** —
    `github.actor != 'dependabot[bot]'`. GitHub les traite comme des exécutions
    de fork : elles lisent le magasin distinct des **secrets Dependabot**, donc
    `secrets.SONAR_TOKEN` arrive comme chaîne vide et `dotnet-sonarscanner begin`
    s'arrête sur *« The format of the analysis property sonar.token= is
    invalid »*. Recopier le token dans le magasin Dependabot est l'autre
    correctif possible, et il est **écarté** : il confierait le token d'analyse
    aux exécutions les moins fiables du dépôt pour analyser une montée de
    version. La condition s'appuie sur `github.actor` plutôt que sur l'auteur de
    la PR parce que le secret retenu suit celui qui a *déclenché* l'exécution :
    un humain qui pousse sur une branche Dependabot retrouve les secrets, et
    cette exécution analyse normalement.

  Les branches internes au dépôt (le flux contributeur normal) tournent
  normalement.
- **`fetch-depth: 0` compte.** Un checkout superficiel casserait la détection de
  code neuf et l'attribution par blame de Sonar.

## En rapport

- [`ci`](../../../../.github/workflows/ci.yml) — produit la même forme de couverture OpenCover via le
  `coverage.runsettings` partagé, et c'est là que le cliquet de warnings est
  réellement imposé.
