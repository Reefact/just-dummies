# Documentation mainteneur

🌍 **Langues :**  
🇬🇧 [English](./README.md) | 🇫🇷 Français (ce fichier)

Tout ce qu'il faut pour modifier ce dépôt, non pour l'utiliser. Si vous cherchez comment *écrire des
tests avec* JustDummies, c'est la [documentation utilisateur](../for-users/README.fr.md) qu'il vous
faut.

## Commencer ici

**Jamais travaillé dans ce dépôt ?** Lisez ces trois pages, dans l'ordre :

1. [Architecture](./architecture.fr.md) — ce qu'est chaque projet, comment un tirage circule
   réellement, et où va un changement d'un type donné.
2. [`CONTRIBUTING.fr.md`](../for-users/CONTRIBUTING.fr.md) — conventions de commit, branches, pull
   requests.
3. [Écrire les tests JustDummies](./WritingJustDummiesTests.fr.md) — à laquelle des deux suites
   appartient un nouveau test, et pourquoi la réponse n'est jamais « l'une ou l'autre ».

## Décisions d'architecture

Toute décision durable de ce dépôt est consignée, en anglais et en français, et reste lisible une
fois le code qui l'a mise en œuvre remanié.

| | |
| --- | --- |
| [Base de décisions](./adr/README.fr.md) | les 55 enregistrements, leurs conventions, et l'index |
| [Gabarit](./adr/template.fr.md) | la forme que prend un nouvel enregistrement |
| [Référence d'implémentation](./specifications/adr-implementation-reference.fr.md) | ce que chaque décision acceptée impose réellement, et où |
| [Workflow `adr-check`](./workflows/adr-check.fr.md) | la vérification consultative qui lit une pull request contre la base |

La règle qui gouverne la base est courte : **une ADR consigne une décision significative et durable —
une décision qu'un mainteneur futur questionnerait.** Le test : l'enregistrement survivrait-il à la
réécriture de sa propre implémentation ? La plupart des pull requests n'en demandent aucune ; la
*vérification* est l'habitude, l'*enregistrement* est l'exception.

Vous rédigez et proposez. Accepter, remplacer et déprécier appartiennent au mainteneur, exactement
comme aucun agent ne merge une pull request.

## Comment le dépôt est construit

| | |
| --- | --- |
| [Architecture](./architecture.fr.md) | les projets, le pipeline de tirage, où ajouter un générateur, un analyzer ou une règle |
| [Écrire les tests JustDummies](./WritingJustDummiesTests.fr.md) | la suite par l'exemple contre la suite par propriétés |
| [Ajouter un train de release](./AddingAReleaseTrain.fr.md) | comment un paquet obtient son propre train versionné |
| [L'outil `dum`](./specifications/justdummies-tool.fr.md) | spécifié et implémenté ; `dum generate` s'exécute — rien ne le publie encore |

## Workflows

La surface de CI, une page par workflow, toutes indexées dans
[le README des workflows](./workflows/README.fr.md).

| Workflow | Rôle |
| --- | --- |
| [`adr-check`](./workflows/adr-check.fr.md) | lit une pull request contre la base de décisions — consultatif |
| [`analyzers`](./workflows/analyzers.fr.md) | éprouve les analyzers publiés sur le plancher Roslyn |
| [`justdummies-mutation`](./workflows/justdummies-mutation.fr.md) | teste le diff par mutation — **rapporte, ne bloque jamais** |
| [`sonar`](./workflows/sonar.fr.md) | l'analyse SonarCloud |
| [`sonar-profile`](./workflows/sonar-profile.fr.md) | comment le jeu de règles du build dérive du profil qualité |
| [`nuget-trusted-publishing`](./workflows/nuget-trusted-publishing.fr.md) | comment une release est publiée, sans clé d'API stockée |

## Archives

Du matériel qui documente un état passé plutôt qu'une règle en vigueur. Utile quand une décision
paraît arbitraire tant qu'on ignore ce à quoi elle réagissait.

| | |
| --- | --- |
| [Audit d'architecture et de conception](./audit/2026-07-20-dummies-architecture-and-design-audit.fr.md) | une évaluation datée, 2026-07-20 — un instantané, pas une règle |
| [Relevé d'extraction](./migration/README.fr.md) | comment ce dépôt a été détaché de `Reefact/first-class-errors`, avec la carte des commits |

## Les conventions en un coup d'œil

Le détail vit dans les pages ci-dessus ; voici celles qui piègent un nouveau venu.

* **La documentation est bilingue et vérifiée.** Chaque page a un jumeau français portant les mêmes
  titres, les mêmes blocs de code et les mêmes marqueurs, dans le même ordre. Les exemples C# de la
  documentation utilisateur sont compilés à chaque build
  ([ADR-0055](./adr/0055-hold-the-user-documentation-to-contracts-the-build-checks.fr.md)).
* **La base de décisions nomme ses pages anglaises sans suffixe de langue** — `NNNN-slug.md` à côté
  de `NNNN-slug.fr.md` — tandis que toute autre page appariée utilise `.en.md`/`.fr.md`. Les deux
  sont gérées ; n'allez pas « corriger » l'une pour l'aligner sur l'autre.
* **Écrivez le type, jamais `var`.** Vérifié deux fois : par un hook sur l'édition, et par `IDE0008`,
  que la CI transforme en erreur
  ([ADR-0034](./adr/0034-enforce-the-style-rules-the-compiler-can-express.fr.md)).
* **Une règle supprimée se nomme par une constante de catalogue**, jamais par un littéral de chaîne
  ([ADR-0050](./adr/0050-name-a-suppressed-rule-through-a-catalogue-constant.fr.md)).
* **Rien n'impose de score de mutation.** La barrière par pull request rapporte et ne bloque pas
  ([ADR-0025](./adr/0025-make-the-per-pull-request-mutation-gate-advisory.fr.md)). N'affirmez pas
  qu'une pull request « a passé la barre de mutation » — il n'y en a aucune à passer.
* **Les pull requests atterrissent en rebase**
  ([ADR-0051](./adr/0051-land-pull-requests-by-rebase.fr.md)) : chaque commit d'une branche arrive
  donc seul sur `main`. Nettoyez l'historique avant de merger.

Les instructions destinées aux agents vivent dans [`AGENTS.md`](../../../AGENTS.md) et
[`CLAUDE.md`](../../../CLAUDE.md) ; elles reprennent ces règles là où un agent les rencontrera
vraiment.

---

[← README du dépôt](../../../README.fr.md) · [Documentation utilisateur](../for-users/README.fr.md)
