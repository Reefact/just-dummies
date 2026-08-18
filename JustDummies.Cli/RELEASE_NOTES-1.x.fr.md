# Notes de version — dum (JustDummies.Cli), 1.x

Ce qui a changé pour vous, version par version, sur le train `cli`. Pour le registre technique complet — chaque contrainte, chaque cas limite, chaque ADR — voir [CHANGELOG.md](https://github.com/Reefact/just-dummies/blob/main/JustDummies.Cli/CHANGELOG.md).

## 1.1.0-beta.1 — 13 août 2026

_Une version mineure, additive de bout en bout : trois nouvelles options, et aucun comportement existant n'a changé. `dum generate Order` écrit toujours exactement ce qu'il écrivait en 1.0.0-beta.1, octet pour octet._

### ✨ Nouveautés

- **`--entry-point`** — un scaffold peut désormais aussi écrire un point d'entrée, pour atteindre un générateur comme ceux de la bibliothèque. `any` émet un membre d'extension C# 14, vous donnant `Any.Order()` à côté de `Any.Int32()` ; `static:<Name>` émet une racine `partial` que vous possédez, vous donnant `Dummies.Order()`, sans aucune exigence de version de langage. Par défaut : `none` ([ADR-0070](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0070-emit-an-entry-point-on-request-as-a-file-of-its-own.md)).
- **`--entry-point-namespace`** — place le fichier du point d'entrée dans un espace de noms qui lui est propre, distinct de celui du générateur.
- **`--format json`** — une exécution se rapporte comme un seul document JSON sur stdout au lieu du résumé console, pour un appelant qui est un script plutôt qu'un lecteur. Porte ce que le code de sortie ne peut pas — `summary.openParameters`, et une ligne par paramètre avec son expression et sa provenance. Les codes de sortie eux-mêmes ne bougent pas ([ADR-0071](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0071-report-a-run-as-data-without-moving-the-exit-codes.md)).
- **`dum.json`** — un fichier optionnel à côté du projet fournit des valeurs par défaut pour `output`, `namespace`, `entryPoint`, `entryPointNamespace` et `format`. La ligne de commande gagne toujours ([ADR-0072](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0072-read-project-defaults-from-a-file-the-command-line-overrides.md)).

### 🙌 Améliorations

- Quand un scaffold écrit deux fichiers, il écrit désormais les deux ou aucun — un `Any{Type}.Entry.cs` déjà présent refuse le scaffold entier, et `--force` couvre les deux.
- Le résumé console nomme maintenant l'appel que le point d'entrée a ouvert.

### 🐛 Corrections de bugs

- **`--namespace ""` et ses quatre équivalents ne pointent plus vers un conseil obsolète** maintenant que `dum.json` peut fixer la même option — le refus pointe désormais vers le fichier.
- **Un type de paramètre hors de tout espace de noms n'émet plus de `using` qui ne compile pas.** Le cas le plus fréquent : un paramètre dont le type n'a pas pu être résolu.

## 1.0.0-beta.1 — 10 août 2026

_Première version publiée — `dum` atteint nuget.org pour la première fois, en implémentant la spécification du scaffolder dans son intégralité. Une **beta**, pas une preview : un outil ne porte aucun socle d'API publique, sa surface étant la ligne de commande plutôt qu'un ensemble de types, et cette surface n'a pas encore été éprouvée par un projet hors de ce dépôt._

### ✨ Nouveautés

- **`dum generate <Type>`** — écrit le générateur de dummy d'un type, une fois, comme du code ordinaire que vous possédez ensuite.
- **Résolution.** Un paramètre de constructeur devient un générateur via la table de base, puis les clauses de garde du constructeur lui-même (`quantity <= 0` → `.Positive()`), puis la composition via une factory ou un `Any{Type}` déjà scaffoldé.
- **Un paramètre non résolu reste ouvert, bruyamment** — émis comme un identifiant qui n'existe pas, pour que votre propre build le signale à la ligne, avec le type en main ([ADR-0060](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0060-seed-generators-from-constructor-guards.md)).
- **Un résumé console** indiquant d'où vient chaque expression — table de base, garde, factory, générateur réutilisé, ou rien.
- **`--project`, `--output`, `--namespace`, `--force`, `--dry-run`**, et rien d'autre.
- **Durcissement du package** — SBOM SPDX embarqué, SourceLink, package de symboles, build déterministe, et une attestation de provenance sur l'artefact publié.

### 🙌 Améliorations

- Nécessite le package `JustDummies` dans le projet analysé. Aucune dépendance vers lui n'est déclarée dans aucun sens — chaque symbole de la bibliothèque est résolu par son nom dans votre compilation, exactement comme le font les analyseurs ([ADR-0063](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0063-give-the-scaffolder-no-dependency-on-the-package.md)), si bien que les versions de l'outil et de la bibliothèque ne peuvent jamais diverger.
