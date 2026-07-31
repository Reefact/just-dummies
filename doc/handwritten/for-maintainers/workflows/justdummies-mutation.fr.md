# Workflow `justdummies-mutation`

🌍 🇬🇧 [English](justdummies-mutation.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/justdummies-mutation.yml`](../../../../.github/workflows/justdummies-mutation.yml)

## À quoi il sert

La couverture répond à *« cette ligne a-t-elle été exécutée par un test ? »*. Les
tests de mutation répondent à la question qui compte vraiment : *« un test
aurait-il remarqué quoi que ce soit si cette ligne avait été fausse ? »*.

[Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/) réécrit
la bibliothèque un petit changement à la fois — inverser une comparaison,
supprimer une instruction, renvoyer l'autre constante, retirer un bloc —, la
rebuilde, et relance la suite de tests contre chaque réécriture. Un **mutant** sur
lequel la suite passe encore est un **survivant** : un comportement que le code a
et que rien n'affirme. Un mutant tué, c'est un test qui fait son travail.

Ce workflow rend ce contrôle automatique pour les **trois composants
JustDummies** : `JustDummies`, son adaptateur xUnit v3 `JustDummies.Xunit`
([ADR-0018](../adr/0018-adapt-dummies-to-xunit-v3-through-a-companion-package.fr.md)),
et les analyseurs livrés dans le package
([ADR-0023](../adr/0023-ship-justdummies-analyzers.fr.md)). Sur une pull request,
il ne mute que les fichiers modifiés par celle-ci, pour l'adaptateur et les
analyseurs ; le générateur est mesuré par le seul balayage hebdomadaire
([ADR-0028](../adr/0028-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.fr.md)).
Le score est rapporté **sans bloquer le merge** — consultatif depuis
l'[ADR-0025](../adr/0025-make-the-per-pull-request-mutation-gate-advisory.fr.md),
parce que la sélection `--since` de Stryker, qui opère par *fichier*, fait suivre
le coût à la taille du fichier où atterrit un changement, et non à la taille du
changement. Le **balayage hebdomadaire** est le niveau imposé.

## Pourquoi un workflow séparé

Ce découpage est antérieur au dépôt. `JustDummies` est un package autonome et
agnostique des erreurs, qui ne référence volontairement pas `FirstClassErrors`
([ADR-0003](../adr/0003-host-dummies-as-a-standalone-package.fr.md)), et il était
destiné à un dépôt à lui. Découper le barrage de mutation le long de cette
frontière future *avant* le déménagement a fait de celui-ci **un déplacement de
fichier plutôt qu'une réécriture** — ce qui s'est vérifié : voir *Le déménagement
a eu lieu* plus bas.

Le découpage garde son utilité maintenant qu'il a eu lieu. Il donne à JustDummies
son propre check, **`JustDummies mutation gate`**, et une barre qui évolue
indépendamment de celle de n'importe quel autre dépôt. Sur les pull requests, il
est **consultatif**
([ADR-0025](../adr/0025-make-the-per-pull-request-mutation-gate-advisory.fr.md)) ;
le niveau imposé est le balayage complet hebdomadaire.

## Quand il s'exécute

- Sur chaque **pull request ciblant `main`** — cantonné au diff et **consultatif** :
  il rapporte le score du diff mais ne bloque jamais le merge
  ([ADR-0025](../adr/0025-make-the-per-pull-request-mutation-gate-advisory.fr.md)).
- **Chaque semaine** sur planification (lundi, 03h47 UTC) — le balayage complet, le
  **niveau imposé**.
- À la demande via **`workflow_dispatch`** — le balayage complet.

## Comment il s'exécute

Chaque composant muté a sa propre configuration Stryker sous
[`build/stryker/`](../../../../build/stryker/) : le projet à muter, les projets de
tests qui doivent tuer ses mutants, et les seuils. Rien de la politique
d'exécution ne vit uniquement dans le YAML, si bien que `dotnet stryker
--config-file build/stryker/justdummies.json` sur la machine d'un mainteneur barre
exactement comme la CI. Les trois sont
[`justdummies.json`](../../../../build/stryker/justdummies.json),
[`justdummies-xunit.json`](../../../../build/stryker/justdummies-xunit.json) et
[`justdummies-analyzers.json`](../../../../build/stryker/justdummies-analyzers.json).

Le moteur lui-même est épinglé dans
[`.config/dotnet-tools.json`](../../../../.config/dotnet-tools.json) et restauré
par `dotnet tool restore`. Cet épinglage est porteur : un Stryker plus récent
invente de nouveaux mutants, ce qui déplace tous les scores à lui seul.

### `changed` — le diff, sur chaque pull request

Une patte de matrice par composant dans le périmètre par PR. Chaque patte :

1. Fait un checkout avec **`fetch-depth: 0`** — le `--since` de Stryker diffe
   contre un commit, l'historique doit donc être là.
2. Résout le **point de fourche** (`git merge-base` entre la base de la pull
   request et `HEAD`), pas la pointe de la branche de base : celle-ci a pu avancer
   depuis que la branche a été tirée, et tout fichier modifié sur `main` entre
   temps serait autrement compté comme « modifié par cette pull request ».
3. Lance Stryker avec `--since:<point de fourche>`, de sorte que seuls les mutants
   **des fichiers touchés par cette pull request** sont testés.
4. Rend les mutants survivants — statut, fichier, ligne, nature de la réécriture —
   dans le résumé du run, pour qu'une patte en échec se diagnostique sans quitter
   la page du run.
5. Téléverse les rapports HTML et JSON en artefact — `if: always()`, parce que la
   vue HTML montre chaque survivant *dans sa source*, ce que le tableau de résumé
   ne peut pas faire.

Une patte dont la pull request n'a pas touché le projet ne sélectionne aucun
mutant, rapporte *« unable to calculate a mutation score »*, et sort en 0. C'est
un succès — et c'est le cas courant.

### `gate` — le check consultatif unique

Une matrice produit un check par patte. `gate` les regroupe sous un nom de check
stable — **`JustDummies mutation gate`** — pour que la protection de branche ait
une seule entrée à viser, au lieu de redéclarer les noms de pattes à chaque
changement de matrice.

Il est **consultatif**
([ADR-0025](../adr/0025-make-the-per-pull-request-mutation-gate-advisory.fr.md)) :
il rapporte l'agrégat des pattes de diff mais **ne fait jamais échouer la pull
request**. Un vrai échec de patte est remonté en `::warning::` à investiguer, et
un run annulé par un push qui le supplante est traité comme du bruit, pas comme un
échec. Il s'exécute en `if: always()` pour rapporter après une patte en échec *ou
annulée* plutôt que d'être sauté. Le niveau imposé est le balayage `full`
hebdomadaire, pas ce check.

### `full` — le balayage hebdomadaire

Les mêmes composants sans le filtre `--since`, et le générateur réintégré : tous
les mutants de tous les composants. Il est **consultatif par construction** —
`--break-at 0` désactive le seuil — parce que son travail est de publier une
tendance, pas de faire virer `main` au rouge un lundi matin sur du code que
personne n'a touché. Lisez-le dans le rapport HTML téléversé.

**Le seul point où ce workflow diffère d'un barrage à matrice pleine : la matrice
par PR compte deux pattes, pas trois.** Le générateur est balayé chaque semaine
mais **n'est pas** muté par pull request
([ADR-0028](../adr/0028-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.fr.md)).
Comme `--since` sélectionne par **fichier** changé et non par ligne changée, un
diff d'une centaine de lignes touchant l'une des grosses sources du générateur
entraîne ce fichier entier : mesuré à 844 mutants, encore en cours après une
heure, sans produire aucun score. Ce n'est pas un défaut de réglage — chaque
levier exposé par Stryker plafonne vers −36 % là où une telle patte aurait besoin
de −95 %, le sharding ne peut pas descendre sous un fichier, et les motifs
`mutate` limités aux lignes ne sélectionnent rien. L'adaptateur et les analyseurs
sont petits, terminent en quatre-vingt-dix secondes environ, et gardent leur
patte.

## Deux réglages qui n'en sont pas

`build/stryker/*.json` porte deux réglages qui ressemblent à de l'optimisation et
n'en sont pas. Les deux ont été établis par la mesure ; en changer un casse le
barrage en silence plutôt que de le ralentir.

### `"test-runner": "mtp"` — obligatoire, pas une préférence

Le **runner VSTest par défaut de Stryker ne fonctionne pas du tout sur ce banc de
tests.** Tous les projets de tests ici sont en xUnit v3, et un projet de tests
xUnit v3 *est* un exécutable que l'adaptateur VSTest lance en processus fils —
hors de portée des hooks in-process dont Stryker se sert à la fois pour capturer
la couverture et, surtout, pour **activer** le mutant. Le run se termine, rapporte
un nombre de tests plausible, et score **0 %** : tous les mutants reviennent
« survived », y compris des mutants qui cassent la suite de façon démontrable
quand la même modification est appliquée à la main. En amont :
[stryker-net#3117](https://github.com/stryker-mutator/stryker-net/issues/3117).

Le runner Microsoft Testing Platform lance l'exécutable de tests lui-même, donc le
mutant est activé et le score est réel. Stryker le marque **preview** et le dit à
chaque run ; cet avertissement est attendu ici, ce n'est pas une erreur de
configuration.

Si une future montée de version de Stryker fait s'effondrer tous les scores à
zéro, c'est la première chose à vérifier.

### `"coverage-analysis": "off"` — exactitude, pas vitesse

Stryker fait normalement une passe de couverture d'abord, pour que chaque mutant
ne relance que les tests qui l'atteignent. Sous le runner MTP, cette sélection est
encore incomplète
([stryker-net#3629](https://github.com/stryker-mutator/stryker-net/issues/3629)) :
des mutants que la suite tue *bel et bien* sont classés non couverts et comptés
contre le score. Mesuré en amont sur une population comparable, le même ensemble
score 75 % avec la sélection active et 100 % sans — et c'est le 100 % qui est le
vrai chiffre.

La désactiver coûte peu sur l'adaptateur et les analyseurs, dont les suites sont
rapides. Elle coûte davantage sur le générateur, qui est le plus gros composant
ici. C'est une raison de garder le générateur hors du chemin critique par PR — ce
que fait
l'[ADR-0028](../adr/0028-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.fr.md)
— pas une raison de réactiver une sélection qui rapporte le mauvais nombre.

## Le modèle de coût, et pourquoi le barrage est cantonné au diff

**Une exécution complète de la suite de tests du composant par mutant**, plus
environ deux minutes de coût fixe par patte (analyse de la solution, build,
exécution initiale des tests, génération des mutants).

`JustDummies` est le plus gros composant ici — quelques milliers de mutants — et
son balayage complet est donc le job le plus long que ce dépôt exécute. C'est
toute la raison pour laquelle le barrage est cantonné au diff plutôt qu'un
balayage complet par pull request.

Cela explique aussi deux choses qui surprennent :

- **La sélection se fait par *fichier* changé, pas par *ligne* changée.** Le
  `--since` de Stryker n'a pas de granularité à la ligne. Ajouter une ligne dans
  un gros fichier sélectionne **tous** les mutants de ce fichier, donc le barrage
  rapporte le score de mutation du fichier entier — pas seulement celui de ce qui
  a été ajouté. Sur les plus gros fichiers, c'est un job plus long et un score qui
  reflète une dette préexistante.
- **Une pull request qui n'ajoute que des tests sélectionne quand même des
  mutants**, par les fichiers de tests qu'elle a modifiés.

## D'où viennent les seuils

Chaque composant porte son propre `break` dans `build/stryker/*.json`, et les
valeurs diffèrent à dessein. Ce **ne sont pas** un avis sur ce qu'un composant
devrait valoir : une barre est fixée à partir du score de balayage complet mesuré
sur ce composant au moment où le barrage a été introduit, arrondi à la baisse,
avec un peu de marge pour le mutant équivalent occasionnel.

Cela fait du barrage un **cliquet**, pas une aspiration. Il dit *ne descendez pas
sous là où ce composant est déjà* — une barre qu'il franchit dès le premier jour,
si bien que le barrage ne démarre jamais au rouge, et qui ne fait jamais que
monter. Relever une valeur après qu'un balayage hebdomadaire a montré de la marge
est l'usage prévu ; en abaisser une devrait ressembler à une décision.

La conséquence à garder en tête : un composant nettement sous les 100 % a une
barre basse aujourd'hui, et une pull request touchant l'un de ses fichiers les
plus faibles peut quand même passer dessous. C'est le barrage qui fonctionne, pas
qui se trompe — le rapport dit quelle assertion manque.

## `JustDummies` n'a pas encore de seuil de score

Toutes les autres barres ont été fixées à partir d'un balayage complet mesuré sur
ce qu'elles barrent (ci-dessus). Pas celle de `JustDummies` : elle porte quelques
milliers de mutants sur une suite lourde, son balayage complet dépasse largement
l'heure, et **aucun score n'a été mesuré pour elle**. Plutôt que d'inventer un
chiffre, [`justdummies.json`](../../../../build/stryker/justdummies.json) met
`break` à **0** — le barrage sur le score est coupé pour ce seul composant.

C'est délibéré et c'est temporaire. La patte s'exécute toujours, échoue toujours
sur un build cassé ou une suite en échec, et liste toujours ses mutants survivants
dans le résumé du run ; ce qu'elle ne fait pas encore, c'est refuser une pull
request sur un score. **Le premier balayage hebdomadaire publie le chiffre sur
toute la bibliothèque** — c'est ce run que ce seuil attend. Lisez-le, et fixez
`break` à partir de là, exactement comme les autres barres l'ont été.

`JustDummies.Xunit` n'appelle pas cette réserve : elle est assez petite pour que
sa barre vienne d'un balayage complet comme les autres, et elle barre normalement.

La patte des analyseurs part elle aussi avec `break` à **0**, pour une autre
raison : ses survivants résiduels sont des mutants d'infrastructure d'analyseur et
de chaînes de descripteurs — elle rapporte donc au lieu de bloquer
([ADR-0023](../adr/0023-ship-justdummies-analyzers.fr.md)).

## Quand le survivant est un mutant équivalent

Parfois la réponse honnête est que le mutant ne peut pas être tué : la réécriture
ne change pas le comportement observable, donc aucun test ne pourrait faire la
différence. Écrire un test pour le poursuivre reviendrait à écrire un test qui
affirme un détail d'implémentation — pire que le trou.

Stryker accepte cette réponse dans la source, à côté du code, sous forme de
commentaire :

```csharp
// Stryker disable once Statement : the trace call has no observable effect
```

La forme est `// Stryker disable [once] <mutator|all> [: raison]`, avec
`// Stryker restore all` pour terminer un bloc non-`once`. Préférez `once`,
préférez nommer le mutateur plutôt qu'`all`, et donnez toujours la raison — une
exclusion non documentée est indiscernable d'un test manquant six mois plus tard.
N'y recourez qu'après avoir décidé que le mutant est réellement équivalent ;
abaisser un seuil pour faire taire un survivant cache tous les survivants à venir
avec lui.

## Permissions & sécurité

`contents: read` seulement. Le workflow fait un checkout, un build et lance des
tests ; il ne stocke aucun secret et n'a besoin d'aucun périmètre en écriture.

## Le déménagement a eu lieu

JustDummies a quitté `Reefact/first-class-errors` le 2026-07-31 et ce dépôt est celui
qui l'a accueilli. Ce que la migration a réellement fait, consigné ici parce que le
mode d'emploi qui occupait cette place le décrivait au futur :

- ce workflow a gardé son nom, `justdummies-mutation.yml`, plutôt que de devenir
  `mutation.yml` — il n'y a pas de second workflow de mutation ici dont il faudrait le
  distinguer, et le renommer aurait cassé l'entrée `JustDummies mutation gate` de la
  protection de branche sans rien apporter ;
- les trois configurations Stryker sont arrivées inchangées à l'exception du champ
  `solution`, qui nomme désormais `JustDummies.sln` ;
- [`.config/dotnet-tools.json`](../../../../.config/dotnet-tools.json), l'épinglage de
  Stryker, est arrivé aussi — mais pas au premier passage, ce qui a fait échouer les
  deux jambes de mutation sur `dotnet tool restore` jusqu'à sa restauration ;
- les sections partagées de la page `mutation` amont ont désormais été repliées dans
  celle-ci, qui est donc autonome. Elle ne renvoie plus à une page que ce dépôt ne
  possède pas.

## À manipuler avec précaution

- **`fetch-depth: 0` est obligatoire**, ce n'est pas une habitude. Un clone
  superficiel rend le point de fourche inatteignable et `--since` ne peut pas le
  résoudre.
- **`--since` veut une branche, un tag ou un vrai SHA de commit — `HEAD` est
  refusé.** `--since:HEAD` fait échouer tout le run avec *« No branch or tag or
  commit found with given target »*, ce pour quoi le workflow résout `git
  merge-base` en SHA d'abord au lieu de laisser passer une expression de révision.
- **Le cliquet d'avertissements de la CI n'a pas besoin d'être désactivé ici.**
  L'inquiétude est légitime — Stryker compile de la source *mutée*, et un mutant
  lève couramment un avertissement que l'original n'avait pas — mais à la mesure,
  `GITHUB_ACTIONS=true` ne change rien : Stryker compile les mutants via Roslyn
  avec ses propres options et n'hérite pas de `TreatWarningsAsErrors` de
  [`Directory.Build.props`](../../../../Directory.Build.props). Le nombre d'erreurs
  de compilation est identique cliquet activé ou non. Si un futur Stryker se
  mettait à l'honorer, les mutants se transformeraient silencieusement en erreurs
  de compilation au lieu d'être testés — c'est dans le compte du log de run que
  cela se verrait.
- **`if: always()` sur `gate` est porteur.** Retirez-le et `gate` est sauté dès
  qu'une patte échoue ou est annulée, donc il ne rapporte jamais l'agrégat —
  l'avertissement consultatif
  ([ADR-0025](../adr/0025-make-the-per-pull-request-mutation-gate-advisory.fr.md))
  serait silencieusement perdu exactement quand il y a quelque chose à dire.
- **La version de Stryker est épinglée dans le manifeste d'outils.** La monter est
  un acte délibéré : attendez-vous à ce que les scores bougent, et relisez les
  seuils.
- **Les seuils vivent dans `build/stryker/*.json`, pas dans le YAML.** C'est ce qui
  garde un run local et la CI d'accord. `break` est la valeur qui fait échouer le
  build ; `high`/`low` ne font que colorer le rapport.
- **Un survivant n'est pas automatiquement un bug**, et la réponse à un survivant
  équivalent est un commentaire `// Stryker disable once` avec une raison, jamais
  un seuil abaissé — voir *Quand le survivant est un mutant équivalent* plus haut.

## L'exécuter en local

```bash
dotnet tool restore
dotnet stryker --config-file build/stryker/justdummies.json
```

C'est le balayage complet du plus gros composant et cela prend un moment. Pour
reproduire ce que fait le barrage sur une branche :

```bash
dotnet stryker --config-file build/stryker/justdummies.json --since:$(git merge-base origin/main HEAD)
```

Les rapports atterrissent dans `StrykerOutput/` (ignoré par git) ; ouvrez
`reports/mutation-report.html`.

## Voir aussi

- [`justdummies`](../../../../.github/workflows/justdummies.yml) *(pas encore de
  page de référence)* — l'autre workflow cantonné à JustDummies : il prouve que les
  assets `netstandard2.0` et `net8.0` packagés se comportent bien sur leurs
  propres runtimes.
- [ADR-0022 — Gate pull requests on the mutation score of what they
  changed](../adr/0022-gate-pull-requests-on-the-mutation-score-of-the-diff.fr.md)
  — la décision que ce workflow met en œuvre.
- [ADR-0025 — Make the per-pull-request mutation gate
  advisory](../adr/0025-make-the-per-pull-request-mutation-gate-advisory.fr.md)
  — pourquoi le check rapporte au lieu de bloquer.
- [`mutation`](https://github.com/Reefact/first-class-errors/blob/main/doc/handwritten/for-maintainers/workflows/mutation.fr.md)
  dans `Reefact/first-class-errors` — la même machine pour les bibliothèques de ce
  dépôt-là. Conservé comme simple repère : cette page n'en dépend plus.
