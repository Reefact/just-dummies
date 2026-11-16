# Notes de version — JustDummies.Xunit, 1.x

Ce qui a changé pour vous, version par version, sur le train `xunit`. Pour le registre technique complet — chaque contrainte, chaque cas limite, chaque ADR — voir [CHANGELOG.md](https://github.com/Reefact/just-dummies/blob/main/JustDummies.Xunit/CHANGELOG.md).

## 1.0.0-rc.1 — 16 novembre 2026

_La release candidate de la 1.0 : un test en échec rapporte désormais exactement une graine, celle qui le rejoue — ou aucune, s'il n'a rien tiré. Requiert `JustDummies` 1.0.0-rc.1, qui porte le `SeedScope.HasDrawn` que l'adaptateur lit._

### 🙌 Améliorations

- **Le plancher `xunit.v3` est affiché : 4.0.2 ou plus.** Le README du paquet et la [page de l'adaptateur](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-xunit.fr.md) le nomment désormais et disent ce que fait une version inférieure — un avertissement `NU1608`, puis un processus de test qui plante.
- **Exécuter les tests sur le SDK .NET 10 est documenté.** Le README et la page de l'adaptateur donnent l'activation par `global.json` de Microsoft.Testing.Platform, et les deux alternatives, pour l'erreur sur laquelle `dotnet test` s'arrête une fois `xunit.v3` relevé au plancher.
- **La page de l'adaptateur explique comment conserver une graine.** Une graine de rejeu fonctionne à tout niveau, et une graine qui a révélé un défaut reste exécutable à la demande avec `[Fact(Explicit = true)]` et `--explicit only`.

### 🐛 Corrections

- **`[Reproducible]` ne rapporte plus de graine pour un test en échec qui n'a rien tiré.** Un test qui échoue sur un littéral n'envoie plus chercher une valeur tirée qui n'existait pas.
- **Des niveaux `[Reproducible]` imbriqués rapportent une seule graine, celle qui rejoue l'échec.** Quand l'assembly, la classe et le test sont tous marqués, seule la graine la plus interne est affichée.
- **Un test qui expire reçoit sa graine.** `[Fact(Timeout = ...)]` perdait la ligne de graine ; elle est désormais écrite quand xUnit annule le test, tant que la sortie l'accepte encore.
- **La ligne de rejeu qu'imprime un test en échec compile là où on la colle.** Elle est désormais pleinement qualifiée, `[JustDummies.Xunit.Reproducible(Seed = N)]`, et n'exige plus `using JustDummies.Xunit;`.

## 1.0.0-preview.2 — 2 septembre 2026

_Un changement de licence que chaque consommateur devrait lire._

### ⚠️ Changements cassants

- **JustDummies.Xunit est désormais sous licence [PolyForm Internal Use 1.0.0](https://github.com/Reefact/just-dummies/blob/main/LICENSE), et non plus Apache 2.0 — source disponible, pas open source.** Vous pouvez lire, construire, modifier et exécuter l'adaptateur pour vos propres opérations internes ou celles de votre entreprise ; vous ne pouvez pas redistribuer le logiciel. Les versions déjà publiées sur NuGet ne sont pas concernées et conservent la licence sous laquelle elles ont été livrées. Les contributions sont désormais régies par un [Contributor Agreement](https://github.com/Reefact/just-dummies/blob/main/CONTRIBUTOR_AGREEMENT.md).

## 1.0.0-preview.1 — 7 août 2026

_Première version publiée — l'adaptateur xUnit v3 atteint nuget.org pour la première fois, au numéro même de la bibliothèque plutôt qu'à `0.1.0` : c'est l'adaptateur offert pour JustDummies 1.0, pas une esquisse antérieure._

### ✨ Nouveautés

- **`[Reproducible]`** — marquez un test, une classe ou un assembly, et les valeurs arbitraires tirées par son corps proviennent d'une seed épinglée, rapportée uniquement quand le test échoue. Supprime l'enrobage `Any.Reproducibly` par test, sans changer la façon dont les valeurs sont générées.
- **Un package séparé, délibérément.** `JustDummies` lui-même reste libre de toute dépendance à un framework de test ; le pont xUnit vit ici et porte la seule dépendance que la bibliothèque ne peut pas porter.
- **Durcissement du package** — SBOM SPDX embarqué, SourceLink, package de symboles, build déterministe, et une attestation de provenance sur l'artefact publié.

### 🙌 Améliorations

- Nécessite xUnit v3 et un package `JustDummies` publié depuis ce dépôt. Les deux trains évoluent indépendamment ([ADR-0047](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0047-declare-the-adapters-library-dependency-independently.md)) : un correctif propre à l'adaptateur est publié seul, sans attendre une nouvelle version de la bibliothèque.
