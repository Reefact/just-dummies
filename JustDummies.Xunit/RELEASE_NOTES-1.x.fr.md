# Notes de version — JustDummies.Xunit, 1.x

Ce qui a changé pour vous, version par version, sur le train `xunit`. Pour le registre technique complet — chaque contrainte, chaque cas limite, chaque ADR — voir [CHANGELOG.md](https://github.com/Reefact/just-dummies/blob/main/JustDummies.Xunit/CHANGELOG.md).

## 1.0.0-preview.1 — 7 août 2026

_Première version publiée — l'adaptateur xUnit v3 atteint nuget.org pour la première fois, au numéro même de la bibliothèque plutôt qu'à `0.1.0` : c'est l'adaptateur offert pour JustDummies 1.0, pas une esquisse antérieure._

### ✨ Nouveautés

- **`[Reproducible]`** — marquez un test, une classe ou un assembly, et les valeurs arbitraires tirées par son corps proviennent d'une seed épinglée, rapportée uniquement quand le test échoue. Supprime l'enrobage `Any.Reproducibly` par test, sans changer la façon dont les valeurs sont générées.
- **Un package séparé, délibérément.** `JustDummies` lui-même reste libre de toute dépendance à un framework de test ; le pont xUnit vit ici et porte la seule dépendance que la bibliothèque ne peut pas porter.
- **Durcissement du package** — SBOM SPDX embarqué, SourceLink, package de symboles, build déterministe, et une attestation de provenance sur l'artefact publié.

### 🙌 Améliorations

- Nécessite xUnit v3 et un package `JustDummies` publié depuis ce dépôt. Les deux trains évoluent indépendamment ([ADR-0047](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0047-declare-the-adapters-library-dependency-independently.md)) : un correctif propre à l'adaptateur est publié seul, sans attendre une nouvelle version de la bibliothèque.
