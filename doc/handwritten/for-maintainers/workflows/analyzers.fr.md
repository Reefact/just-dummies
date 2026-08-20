# Workflow `analyzers`

🌍 🇬🇧 [English](analyzers.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/analyzers.yml`](../../../../.github/workflows/analyzers.yml)

## À quoi il sert

`JustDummies` livre 32 règles Roslyn (`JD001`–`JD032`) **embarqués dans le package NuGet**, sous
`analyzers/dotnet/cs`. Ils sont donc chargés par **le compilateur de chaque consommateur**, pas par
le nôtre, et c'est ce seul fait qui justifie ce workflow.

Un analyseur compilé contre un Roslyn plus récent que l'hôte du consommateur ne se comporte pas
mal : il **refuse de se charger**, avec `CS8032`, et toutes ses règles cessent silencieusement de
se déclencher. Rien dans un build ordinaire ne l'attrape : `ci` construit les analyseurs via une
`ProjectReference` sous le SDK moderne, ce qui n'est pas la façon dont un consommateur les
rencontre. Ce job est le seul endroit où l'artefact livré est chargé comme un consommateur le
charge : **depuis le package, par le plus vieux compilateur qu'on supporte**.

Le plancher est `4.8.0` — Roslyn 4.8, c'est-à-dire Visual Studio 2022 17.8 / le SDK .NET 8 —
déclaré une seule fois comme `RoslynFloorVersion` dans
[`Directory.Build.props`](../../../../Directory.Build.props)
([ADR-0001](../adr/0001-lock-the-analyzer-roslyn-floor.fr.md)).

## Quand il s'exécute

- Sur chaque **pull request ciblant `main`**, et sur chaque **push sur `main`**.
- À la demande via **`workflow_dispatch`**.

## Comment il s'exécute

Un seul job, `floor`, qui utilise délibérément **deux SDK** :

1. **Packager sous le SDK de release (.NET 10).** `dotnet pack` s'exécute depuis la racine du
   dépôt, donc le `global.json` racine sélectionne le SDK avec lequel `release.yml` publie —
   l'artefact testé est celui que reçoivent les consommateurs, analyseurs embarqués par la cible
   `_AddAnalyzerToPackage` de `JustDummies.csproj`. La version du package est
   `1.0.0-floorcheck.<run>.<tentative>`, une valeur que NuGet n'a jamais mise en cache, de sorte
   que l'étape suivante ne peut pas restaurer une copie périmée.
2. **Consommer sous le SDK plancher (.NET 8.0.100).**
   [`tools/floor-check`](../../../../tools/floor-check) porte un `global.json` imbriqué avec
   `rollForward: disable` ; la résolution du SDK dépend du répertoire courant, donc c'est le fait
   de construire *depuis ce répertoire* qui épingle le vieux compilateur. `FloorCheck.csproj` prend
   une `PackageReference` sur le `JustDummies` packagé — jamais une `ProjectReference`, qui
   court-circuiterait le package et ne prouverait rien.
3. **Prouver que les analyseurs se sont chargés.** Deux protections, pour deux échecs différents.
   Un chargement *tenté et échoué* lève `CS8032`, que `FloorCheck.csproj` élève en erreur : le build
   lui-même vire au rouge. Un chargement *jamais tenté* — le package livré sans son dossier
   `analyzers/dotnet/cs` — ne lève rien et laisserait le build au vert ; c'est ce que le grep final
   attrape. `-p:ReportAnalyzer=true -v detailed` fait émettre à Roslyn sa table de temps par
   analyseur, et l'étape y cherche un *type* d'analyseur pleinement qualifié.

   Les deux ne sont pas interchangeables : le message de `CS8032` nomme lui-même le type
   d'analyseur qu'il n'a pas pu créer, donc le grep seul ne saurait pas distinguer un chargement
   raté d'un chargement réussi. Il n'a pas à le faire — l'erreur élevée fait échouer l'étape avant
   que le grep ne s'exécute.

`tools/floor-check/Sample.cs` est la source qu'on donne à analyser au vieux compilateur. Ce n'est
pas une démonstration de la bibliothèque et cela ne doit pas le devenir : son unique rôle est
d'être du code que les règles ont une raison de regarder. Il doit aussi rester **propre** — un
diagnostic `JD` de sévérité Erreur fait échouer ce build, et cet échec serait indiscernable de
l'échec de chargement que le job cherche.

## Permissions & sécurité

`contents: read` seulement. Le workflow fait un checkout, un pack et un build ; il ne stocke aucun
secret et n'a besoin d'aucun périmètre en écriture.

`tools/floor-check/nuget.config` efface les sources héritées et route l'identifiant `JustDummies`
**exclusivement** vers le feed local. Ce n'est pas décoratif : `JustDummies` est publié sur
nuget.org, donc sans ce routage la restauration pourrait servir le package publié à la place de
celui que ce run vient de packager, et le job dogfooderait une release au lieu du changement testé.

## À manipuler avec précaution

- **L'étape de pack ne doit pas s'exécuter sous le SDK plancher.** Elle testerait un analyseur que
  personne ne livre, et figerait toute la bibliothèque en C# 12 (`LangVersion latest` sous le SDK 8).
- **`8.0.100`, pas `8.0.x`.** Une bande de fonctionnalités .NET 8 ultérieure embarque un Roslyn plus
  récent que 4.8, ce qui relèverait silencieusement le plancher que ce job mesure.
- **La version est épinglée exactement, jamais flottante.** Un `1.0.0-floorcheck-*` flottant
  résoudrait vers un `JustDummies` stable publié dès qu'il en existera un — NuGet classe une version
  stable au-dessus de toute préversion partageant sa racine.
- **`--no-incremental` et `-v detailed` sont tous deux porteurs.** Sans le premier, un build en cache
  ne produit aucune table d'analyseurs ; sans le second, la table n'atteint jamais le log. Dans les
  deux cas le grep échouerait pour une raison étrangère au chargement.
- **Le grep vise un type, pas le nom d'assembly.** `JustDummies.Analyzers` seul apparaît dans des
  lignes de build ordinaires (`-> ...dll`, chemins), donc le viser passerait même si rien ne s'était
  chargé. C'est la protection contre l'*absence* ; l'élévation de `CS8032` est celle contre
  l'*échec*. En retirer une laisse un trou.
- **`CS8032` et `AD0001` sont élevés en erreurs** dans `FloorCheck.csproj`, tandis que le cliquet
  d'avertissements du dépôt y est désactivé. Le vieux SDK émet légitimement des avertissements que
  les jambes .NET 10 ne voient jamais ; faire rougir ce job pour eux enterrerait son unique vrai
  signal.
- **Ce job n'est pas `tools/justdummies-check`.** Ils consomment le même package et vérifient des
  contrats différents : celui-là demande quel *asset* NuGet résout et construit donc sous le SDK
  moderne ; celui-ci demande si les *analyseurs* se chargent sur le plus vieux compilateur et
  épingle donc un vieux SDK. Aucun ne subsume l'autre.

## Le garde-fou rapide qui l'accompagne : le test `RoslynFloorTests`

Ce workflow prouve le contrat de bout en bout, et cela lui coûte un package et deux SDK.
[`RoslynFloorTests`](../../../../JustDummies.Analyzers.UnitTests/RoslynFloorTests.cs) en prouve une
version plus étroite en quelques millisecondes, dans l'exécution de tests ordinaire : il réfléchit
sur l'assembly d'analyseurs construite et échoue si une référence `Microsoft.CodeAnalysis*` est
plus récente que le plancher.

Les deux sont complémentaires, pas redondants. Le test attrape la régression *courante* — une
référence de package montée — à la vitesse de `dotnet test`, avant la CI. Le workflow attrape tout
ce que le test ne peut pas voir : un chemin `analyzers/dotnet/cs` cassé, un analyseur qui lève à
l'initialisation, une dépendance transitive qui n'échoue que sur le vieil hôte.

Le test lit le plancher depuis l'`AssemblyMetadata` de l'assembly d'analyseurs, que
`JustDummies.Analyzers.csproj` émet à partir du même `$(RoslynFloorVersion)` que l'épinglage du
package. Un test portant son propre littéral continuerait de passer après un déplacement de la
propriété ; le relire depuis les métadonnées rend l'épinglage et son garde-fou impossibles à
désynchroniser.

## Voir aussi

- [ADR-0001 — Lock the analyzer's Roslyn floor](../adr/0001-lock-the-analyzer-roslyn-floor.fr.md) —
  la décision que ce workflow fait respecter.
- [Référence d'implémentation des ADR](../specifications/adr-implementation-reference.fr.md) — les
  protections qui réalisent l'ADR-0001, et lesquelles existent ici.
- [`ci`](../../../../.github/workflows/ci.yml) *(pas encore de page de référence)* — où les
  analyseurs sont dogfoodés contre le code de ce dépôt, via des références de projet.
- [`justdummies`](../../../../.github/workflows/justdummies.yml) *(pas encore de page de
  référence)* — l'autre consommateur de l'artefact packagé, qui vérifie la sélection d'asset plutôt
  que le chargement des analyseurs.
