# ADR-0068 | Extraire JustDummies dans son propre dépôt

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0068-extract-justdummies-into-its-own-repository.md)

**Statut :** Proposé
**Proposé :** 2026-07-31
**Décideurs :** Reefact
**Supersède :** [ADR-0011](0011-host-dummies-as-a-standalone-package.fr.md) (uniquement son volet colocation — voir ci-dessous)

## Contexte

L'[ADR-0011](0011-host-dummies-as-a-standalone-package.fr.md) décidait deux choses à la fois, et une seule
est remplacée ici. Il décidait **ce qu'est JustDummies** — un package indépendant qui ne doit référencer
aucun projet FirstClassErrors — et **où il vit** — colocalisé dans `Reefact/first-class-errors`, pour
réutiliser l'infrastructure CI, packaging, release, SBOM, SourceLink et gouvernance de ce dépôt pendant que
l'API itérait vite et que ses premiers consommateurs étaient juste à côté.

Le premier volet tient et n'est pas remis en cause. Le second était explicitement provisoire : l'ADR-0011
écartait « créer immédiatement un dépôt séparé » pour des raisons de coût, non de principe, et actait que la
règle de non-référence existe précisément pour qu'« une extraction ultérieure reste mécanique plutôt
qu'architecturale ».

Les conditions qui justifiaient la colocation ont expiré :

* La bibliothèque a sa propre surface produit — 28 analyseurs de première partie (ADR-0044), un adaptateur
  xUnit v3 (ADR-0039), un banc de tests à deux suites (ADR-0040), un scaffolder spécifié, et un site produit
  `https://justdummies.io` que les packages annoncent déjà comme `PackageProjectUrl`.
* Sa cadence de publication n'est pas celle de FirstClassErrors. Partager un espace de noms de tags imposait
  un train `dum-v*` dont le seul objet était d'éviter la collision avec `lib-v*` et `cli-v*`.
* La colocation coûte désormais à l'hôte : `FirstClassErrors.Testing` ne peut pas exprimer sa dépendance par
  un `PackageReference` normal, et porte à la place un `ProjectReference` privé plus une cible de pack
  écrite à la main qui embarque `JustDummies.dll` dans son propre `lib/` — un contournement que l'ADR-0026
  n'acceptait que « jusqu'à ce que JustDummies soit publié ».
* Les issues, les pull requests et les exécutions CI de deux produits sans rapport partagent une seule file.

## Décision

JustDummies — la bibliothèque, ses analyseurs, son adaptateur xUnit, son banc de tests, sa documentation, ses
ADR et son scaffolder spécifié — vit dans **`Reefact/just-dummies`**, qui devient l'unique dépôt source du
produit. `Reefact/first-class-errors` devient consommateur des packages publiés.

Le déplacement préserve l'historique : le `main` de ce dépôt a été produit avec `git filter-repo` depuis
`Reefact/first-class-errors` au `SOURCE_CUTOFF_SHA = fbf523b86acebdd34ba0bbfd437683864be3cb9c`, en
conservant chaque auteur, date et message de commit, et en suivant les chemins à travers leurs noms
historiques.

## Conséquences

### Les hachages de commit diffèrent du dépôt source

Le filtrage réécrit chaque commit : aucun SHA ici ne correspond au SHA du même changement dans
`Reefact/first-class-errors`. La correspondance complète est commitée dans
[`../migration/commit-map.txt`](../migration/commit-map.txt) (1350 entrées), avec la spécification exacte des
chemins utilisée par le filtre.

**Les références d'issues et de pull requests dans les messages de commit historiques (`#123`, `Refs: #229`)
pointent vers `Reefact/first-class-errors`, pas vers ce dépôt.** Elles ont été délibérément laissées
intactes : les réécrire aurait falsifié des messages réellement écrits par leurs auteurs, et il n'existe pas
de cible correcte vers laquelle les réécrire. Tout numéro dans un commit antérieur au 2026-07-31 se lit comme
un numéro du dépôt source.

### Le train `dum-v*` est remplacé par trois trains

Aucun tag `dum-v*` n'a jamais été poussé — le train existait dans `release.yml` sans avoir jamais servi — le
renommage n'a donc rien écarté. Ce dépôt publie sur `lib-v*` (JustDummies), `xunit-v*` (JustDummies.Xunit) et
`cli-v*` (le scaffolder `dum`, câblé avant son implémentation).

Séparer l'adaptateur sur son propre train introduit un risque que le train unique n'avait pas :
`JustDummies.Xunit` porte un `ProjectReference` vers `JustDummies`, donc `dotnet pack` estampille sa
dépendance à la version en cours d'empaquetage. Publier `xunit-v0.2.0` alors que la bibliothèque est à
`lib-v0.1.0` livrerait un adaptateur exigeant une version de bibliothèque jamais publiée.
`tools/packaging/pack.sh` refuse un tel pack en exigeant que la version de dépendance estampillée corresponde
à un tag `lib-v*` existant.

### FirstClassErrors garde sa copie jusqu'à la première publication

Cette extraction n'a rien supprimé de `Reefact/first-class-errors`. Quatre de ses projets référencent
JustDummies, et l'un d'eux — `FirstClassErrors.Testing` — le *livre*. Retirer la source là-bas exige un
package `JustDummies` restaurable sur nuget.org, qui n'existe pas encore. La bascule est préparée, non
exécutée ; la décision correspondante de ce côté-là supersède le contournement d'embarquement de l'ADR-0026.

### Deux décisions restent dans FirstClassErrors

L'[ADR-0026](0026-rebase-testing-arbitrary-values-on-dummies.fr.md) et l'ADR-0061 ont FirstClassErrors pour
sujet bien qu'ils concernent JustDummies : l'un acte pourquoi `FirstClassErrors.Testing` a rebasé ses valeurs
arbitraires sur cette bibliothèque, l'autre pourquoi ce dépôt exécute ces analyseurs sur son propre code. Ils
ne sont pas repris ici. L'[ADR-0011](0011-host-dummies-as-a-standalone-package.fr.md) et
l'[ADR-0022](0022-floor-the-library-on-net-framework-4-7-2.fr.md) sont le cas inverse — ils lient les deux
produits — et existent donc dans les deux dépôts.

### Les numéros d'ADR sont préservés, et la séquence est trouée

Les ADR repris ont gardé leur numéro : l'ensemble de ce dépôt est donc 0011, 0013, 0015, 0020, 0022, 0025,
0030–0033, 0035–0042, 0044, 0045, 0047–0054, 0058, 0059, 0063–0066. Les renuméroter aurait cassé chaque
référence croisée dans les textes acceptés et dans le dépôt source. Les nouveaux ADR continuent à partir de
0068 — au-dessus du plus haut de FirstClassErrors — pour qu'un numéro ne désigne jamais deux décisions
différentes selon le dépôt.

## Alternatives considérées

### Conserver la colocation et publier depuis FirstClassErrors

Considérée parce qu'elle ne change rien et que l'infrastructure fonctionne déjà. Rejetée parce qu'elle
conserve tous les coûts listés ci-dessus — le contournement de DLL embarquée, l'espace de tags partagé, la
file d'issues partagée — et que chacun croît avec le produit au lieu de décroître.

### Démarrer le nouveau dépôt par un import écrasé

Considérée parce que c'est trivial et que cela produit un commit racine propre. Rejetée parce qu'elle
jetterait l'attribution et le raisonnement de 420 commits : les ADR de ce dépôt citent des commits qui
expliquent *pourquoi* un générateur se comporte comme il le fait, et un écrasement laisserait ces citations
dans le vide.

### Forker `Reefact/first-class-errors`

Rejetée parce qu'un fork emporte tout l'historique et tout l'arbre de travail de FirstClassErrors, et que
GitHub continuerait de présenter le résultat comme un dérivé d'un dépôt dont ce produit est censé se
distinguer.
