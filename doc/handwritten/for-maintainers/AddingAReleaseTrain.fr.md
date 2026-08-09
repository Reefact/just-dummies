# Ajouter un train de release

🌍 🇬🇧 [English](AddingAReleaseTrain.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — comment ajouter un nouveau paquet versionné
> indépendamment (un « train de release ») à JustDummies. Ne fait pas partie
> de la documentation utilisateur sous `doc/`.

## Ce qu'est un train

Un **train de release** est un paquet (ou un groupe de paquets versionnés en
lockstep) qui se version et se publie sur son propre préfixe de tag. Il y en a trois
aujourd'hui :

| Train | Préfixe de tag | Scopes | Paquet(s) | Changelog |
| --- | --- | --- | --- | --- |
| `lib` | `lib-v*` | `core`, `analyzers` (plus les historiques `dummies`, `justdummies`) | JustDummies, analyzers embarqués dedans | `JustDummies/CHANGELOG.md` |
| `xunit` | `xunit-v*` | `xunit` | JustDummies.Xunit (l'adaptateur xUnit v3) | `JustDummies.Xunit/CHANGELOG.md` |
| `cli` | `cli-v*` | `cli` | `dum`, le scaffolder (squelette en place, pas encore publié) | `JustDummies.Cli/CHANGELOG.md` |

Le mapping train → (préfixe, scopes, paquet, fichier changelog) vit à **un seul
endroit**, [`tools/trains.sh`](../../../tools/trains.sh), que le générateur de notes de
release, le collecteur de changelog et le workflow changelog *sourcent* tous. Les
autres édits ci-dessous n'existent que parce que les triggers et les entrées de
choix des workflows GitHub, la liste fermée des scopes du commit-lint, et la logique
de packaging **ne peuvent pas** être pilotés par ce fichier — ils sont statiques par
nature.

> **Scope vs train.** Ajouter un *scope* à un train **existant** (un nouveau
> composant qui ship dans `lib`, par exemple) est bien plus léger : ajoutez le scope
> à la ligne de ce train dans `trains.sh` et à la liste du commit-lint (étapes 1–2
> ci-dessous), et c'est tout. La checklist complète n'est nécessaire que lorsque le
> nouveau paquet obtient son **propre préfixe de tag**.

## L'unique édit de données

**1. Ajoutez une ligne à [`tools/trains.sh`](../../../tools/trains.sh).** Une ligne, séparée
par des barres verticales :

```
<id>|<préfixe-de-tag>|<scopes csv>|<fichier changelog>|<libellé du paquet>
```

p. ex. `docs|docs-v|gendoc|tools/gendoc/CHANGELOG.md|le générateur de documentation`.
C'est tout ce dont `release-notes.sh`, `collect-prs.sh` et l'étape « Resolve
component » du workflow changelog ont besoin — ils prennent le nouveau train sans
autre changement. Les scopes doivent être un sous-ensemble de la liste du commit-lint
(étape suivante).

## Les édits statiques imposés par GitHub et la tooling

**2. Scopes de commit** — si le train introduit de nouveaux scopes, ajoutez-les à la
liste fermée de [`tools/commit-lint/lint-commit-message.sh`](../../../tools/commit-lint/lint-commit-message.sh)
(`SCOPES` **et** `SCOPES_HUMAN`) et au tableau des scopes de
[`CONTRIBUTING.md`](../for-users/CONTRIBUTING.fr.md). Sans ça, les commits du nouveau composant
échouent au check commit-lint.

**3. [`.github/workflows/release.yml`](../../../.github/workflows/release.yml)** — trois
endroits :
- la liste des triggers de tag : ajoutez `- '<préfixe>*.*.*'` (p. ex. `- 'docs-v*.*.*'`) ;
- le choix `component` du `workflow_dispatch` : ajoutez `- <id>` ;
- le `case` de résolution de version sur `REF_NAME` : ajoutez
  `<préfixe>*) COMPONENT="<id>"; VERSION="${REF_NAME#<préfixe>}" ;;`.

Il y en avait un quatrième : une liste blanche `lib|xunit|cli)` validant le composant résolu.
Elle a disparu — l'entrée du `workflow_dispatch` est désormais vérifiée par `require_train`
contre `trains.sh`, donc l'étape 1 la couvre. Elle a été supprimée parce que c'est justement
celle que cette checklist n'a pas protégée : `catalog` a été ajouté partout ailleurs et pas là,
et `catalog-v1.0.0-preview.1` est mort dessus. La leçon n'est pas « lire la checklist plus
attentivement » ; c'est qu'un édit dont une checklist doit se souvenir a sa place dans
`trains.sh` dès que c'est possible.

**4. [`tools/packaging/pack.sh`](../../../tools/packaging/pack.sh)** — ajoutez une branche
`<id>)` sélectionnant quels projets ce train packe. C'est une logique de packaging
réellement spécifique au train (quel `.csproj` packer, si un package de symboles est
livré), donc elle n'est pas pilotée par `trains.sh`.

**5. [`.github/workflows/release-dryrun.yml`](../../../.github/workflows/release-dryrun.yml)** —
les deux étapes de répétition codent la liste des trains en dur : ajoutez
`tools/packaging/pack.sh "$DRYRUN_VERSION" <id>` à l'étape *Pack with SBOM* et
`tools/packaging/release-notes.sh <id> HEAD` à l'étape *Rehearse release notes*.
Sans cela, le packaging et les notes du nouveau train ne sont jamais exercés avant
une vraie release — rien n'échoue, ils ne sont simplement jamais lancés, ce qui
annule tout l'intérêt du dry-run.

**6. [`.github/workflows/changelog.yml`](../../../.github/workflows/changelog.yml)** —
ajoutez `- <id>` aux `options` du choix `component` du `workflow_dispatch`. (Le
workflow lit tout le reste du train depuis `trains.sh`.)

## Ce qui se fait tout seul

- [`tools/packaging/release-notes.sh`](../../../tools/packaging/release-notes.sh),
  [`tools/changelog/collect-prs.sh`](../../../tools/changelog/collect-prs.sh) et l'étape
  « Resolve component » du workflow changelog lisent la nouvelle ligne directement —
  aucun édit.
- Le **fichier changelog du train est créé au premier run de rédaction**
  (`merge-unreleased.sh` pose le préambule Keep a Changelog si le fichier manque).
  Vous pouvez le pré-créer à la main pour une première pull request plus propre, mais
  ce n'est pas obligatoire.

## Vérifier

- **Convention de commit :** faites un commit sous un nouveau scope et confirmez que
  `tools/commit-lint/lint-commit-message.sh` l'accepte (le hook local et le workflow
  `commit-lint` le partagent).
- **Notes de release :** lancez `tools/packaging/release-notes.sh <id> <préfixe>0.0.0 HEAD`
  en local ; il doit lister les commits de ce train et rien des autres trains.
- **Packaging :** après l'édit de l'étape 5, le workflow
  [`release-dryrun`](../../../.github/workflows/release-dryrun.yml) packe et répète les notes du
  nouveau train sur chaque PR — vérifiez que son log liste le nouveau train. Ou
  lancez `tools/packaging/pack.sh 0.0.0-dry.1 <id>` en local.
- **Changelog :** une fois mergé sur la branche par défaut, déclenchez le workflow
  [`changelog`](../../../.github/workflows/changelog.yml) pour le nouveau composant et relisez la
  pull request rédigée.

## En rapport

- [`tools/trains.sh`](../../../tools/trains.sh) — la source unique de vérité autour de
  laquelle ce runbook est construit.
- Les pages des workflows [`changelog`](../../../.github/workflows/changelog.yml) et
  [`release`](../../../.github/workflows/release.yml).
- [`CONTRIBUTING.fr.md`](../for-users/CONTRIBUTING.fr.md) — les scopes Conventional Commit.
