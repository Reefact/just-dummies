# Référence des workflows CI/CD

🌍 🇬🇧 [English](README.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur. Elle décrit les workflows GitHub Actions qui
> construisent, vérifient et publient JustDummies. Elle **ne fait pas** partie de
> la documentation utilisateur sous `doc/handwritten/for-users/`.

## Ce que c'est

Chaque workflow sous [`.github/workflows/`](../../../../.github/workflows/) porte
une bonne dose d'intention qu'un « nettoyage » casse facilement : une permission
étroite à dessein, un ordre d'étapes qui garde contre une panne précise, une
version gelée pour une raison produit. Les fichiers de workflow tiennent
eux-mêmes la justification ligne à ligne dans leurs commentaires — ces
commentaires sont la source de vérité la plus proche du code. **Ces pages sont la
couche pédagogique au-dessus :** à quoi sert chaque workflow, quand et comment il
s'exécute, et la poignée de choses à ne pas changer sans avoir compris pourquoi.

Lisez la page d'un workflow avant d'y toucher. Quand la page et le YAML sont en
désaccord, c'est le YAML qui gagne — et la page qu'il faut corriger.

**Sept workflows ont une page pour l'instant**, et le tableau ci-dessous dit
lequel. Les autres y figurent quand même : un index qui les omettrait laisserait
croire qu'ils n'existent pas. Leurs commentaires YAML sont, pour l'heure, toute
la documentation qu'ils ont.

## Les conventions transverses

Quelques décisions sont partagées par tous les workflows. Elles sont documentées
une fois ici plutôt que répétées sur chaque page. Chacune a été vérifiée contre
les workflows tels qu'ils sont, pas reprise sur parole :

- **Les actions sont épinglées par SHA de commit, pas par tag.** Un tag comme
  `@v4` peut être déplacé par son propriétaire vers du nouveau code ; un SHA de 40
  caractères, non. Chaque `uses:` épingle donc un SHA, avec le tag lisible en
  commentaire de fin de ligne (`# v4`). Quand vous montez une action, changez **les
  deux**. Compté : 47 `uses:` épinglés par SHA, et un qui ne l'est pas —
  `contributor-agreement` épingle `actions/github-script@v9` par tag.
- **Les `permissions:` partent en lecture seule et s'élargissent par job.** Le
  bloc au niveau du workflow est le moindre privilège dont il a besoin
  (généralement `contents: read`) ; un job qui doit écrire quelque chose (téléverser
  du SARIF, publier une release, activer l'auto-merge) redéclare un bloc
  `permissions:` qui ajoute *uniquement* ce périmètre. N'élargissez jamais le bloc
  de tête pour satisfaire un job. Un job qui n'a besoin de **rien** fait
  l'inverse : il déclare `permissions: {}` — le mapping vide explicite, puisqu'un
  `permissions:` nu est un null et non une map vide.
- **Chaque job pose un `timeout-minutes`.** Le défaut GitHub est de six heures ;
  une étape bloquée retiendrait un runner tout ce temps. Compté : 24 jobs, 23 avec
  un plafond — tous sauf celui de `contributor-agreement`, qui n'en pose aucun.
  Chacun est réglé à quelques fois la durée observée, avec un commentaire à côté.
- **`concurrency` annule les runs supplantés.** Pousser deux fois sur la même
  branche ou la même PR annule le run en vol. La seule exception est `release`, qui
  met `cancel-in-progress: false` — on ne veut jamais annuler une publication à
  moitié faite.
- **Les scanners de sécurité tournent aussi chaque semaine.** `codeql` et
  `scorecard` repassent sur du code inchangé pour que les requêtes et contrôles
  nouvellement livrés s'appliquent même sans push.
- **Les forks ne lisent pas les secrets.** Les workflows qui en ont besoin
  (`sonar`) détectent une PR de fork et se sautent plutôt que d'échouer : GitHub
  n'expose pas les secrets du dépôt à une PR issue d'un fork.
- **Ce sont les checks requis qui barrent vraiment.** Plusieurs workflows
  (`dependency-review`, `dependabot-automerge`) ne font que *signaler* ou
  *activer* — ils ne mergent rien d'eux-mêmes. Ce qui bloque réellement un mauvais
  merge, c'est la configuration de protection de branche sur `main` qui marque ces
  checks comme **requis**. C'est un réglage de dépôt, pas quelque chose qu'un
  workflow peut imposer pour lui-même.

## Les workflows

### Build & qualité

| Workflow | Rôle |
| --- | --- |
| `ci` | Build et tests de la solution sur Linux et Windows, avec couverture, plus la patte du plancher .NET Framework 4.7.2. Le barrage principal. |
| `justdummies` | Prouve que les assets `netstandard2.0` et `net8.0` packagés se comportent bien sur les runtimes qui les chargent réellement — la patte que le projet de tests net10 ne peut pas exercer. |
| [`justdummies-mutation`](justdummies-mutation.fr.md) | Tests de mutation des trois composants avec Stryker.NET — un check consultatif sur ce qu'une PR change, plus le balayage complet hebdomadaire. Publie des comptes par statut, jamais un score (ADR-0093). |
| [`genany-sweep`](genany-sweep.fr.md) | Hebdomadaire : le balayage génératif du moteur de scaffolding — ~3 600 domaines gardés issus d'un produit d'axes déclaré, chacun scaffoldé, compilé, analysé et tiré. L'instrument qui trouve des défauts ; une tranche couvrante tourne à chaque build. |
| `stryker-xunit-v3-watch` | Hebdomadaire : signale le moment où Stryker.NET corrige son bug de découverte de tests xUnit v3, que rien d'autre ici ne remarquerait jamais. Rapporte sur la PR #148 ; ne merge et ne rouvre rien. |
| [`analyzers`](analyzers.fr.md) | Charge les analyseurs embarqués depuis l'artefact packagé sous le plus vieux compilateur supporté (Roslyn 4.8), ce qu'un build ordinaire ne fait jamais. |
| [`sonar`](sonar.fr.md) | Analyse SonarQube Cloud — quality gate et remontée de couverture. |
| [`sonar-profile`](sonar-profile.fr.md) | Hebdomadaire : échoue quand la liste de règles Sonar C# commitée a dérivé du quality profile SonarCloud. Rapporte, ne répare jamais. |
| `sonar-gate` | Nocturne : lit le verdict du Quality Gate SonarCloud et échoue quand il est rouge. |
| `commit-lint` | Impose la convention Conventional Commits sur chaque commit d'une PR, avec le script du hook local. |
| `lint` | shellcheck et actionlint sur les fichiers que le compilateur C# ne voit jamais — les scripts POSIX et les définitions de workflow. |
| [`adr-check`](adr-check.fr.md) | Consultatif, sur déclenchement manuel : confronte une branche à la base ADR (décision nouvelle / supersession / conflit). Ne bloque jamais. |

### Sécurité & chaîne d'approvisionnement

| Workflow | Rôle |
| --- | --- |
| `codeql` | Analyse statique GitHub CodeQL pour C#, résultats sur le tableau de bord code-scanning. |
| `dependency-review` | Bloque une PR qui introduit une dépendance vulnérable connue. Exige que le graphe de dépendances du dépôt soit activé. |
| `scorecard` | OpenSSF Scorecard — note la posture de sécurité du dépôt. |

### Release

| Workflow | Rôle |
| --- | --- |
| [`release`](nuget-trusted-publishing.fr.md) | Construit, atteste et publie les packages NuGet sur un tag de version de l'un des quatre trains (`lib-v*`, `xunit-v*`, `catalog-v*`, `cli-v*`). **Pousser un tel tag publie**, et une version publiée est immuable — la page liée couvre la configuration trusted publishing de nuget.org qu'il exige et comment répéter sans publier. (Page en anglais seulement.) |
| `release-dryrun` | Répète en continu la part sans effet de bord de la release (pack + SBOM) sur chaque PR et chaque push. |
| `changelog` | Rédige la section `[Unreleased]` du changelog d'un train à partir des PR mergées, sur déclenchement manuel, et ouvre une PR de relecture. |

### Maintenance des dépendances

| Workflow | Rôle |
| --- | --- |
| `dependabot-automerge` | Active l'auto-merge sur les montées patch/mineures de Dependabot ; laisse les majeures à un humain. |
| `dependabot-autofix` | Claude trie une PR Dependabot en échec et pousse un correctif à faible risque. Ne merge jamais. |

## Documentation mainteneur liée

- [`tools/trains.sh`](../../../../tools/trains.sh) — la source de vérité unique des
  trains de release, que `release`, `release-dryrun` et `changelog` lisent tous.
- [Écrire les tests JustDummies](../WritingJustDummiesTests.fr.md) — à laquelle des
  deux suites appartient un nouveau test.
- [`CONTRIBUTING.md`](../../for-users/CONTRIBUTING.fr.md) — les conventions de commit et de PR
  que le workflow `commit-lint` vérifie.
