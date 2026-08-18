# Notes de version — JustDummies, 0.x

Ce qui a changé pour vous, version par version, sur le train `lib`. Pour le registre technique complet — chaque contrainte, chaque cas limite, chaque ADR — voir [CHANGELOG.md](https://github.com/Reefact/just-dummies/blob/main/JustDummies/CHANGELOG.md).

## 0.1.0-preview.1 — 31 juillet 2026

_Première version publiée — JustDummies atteint nuget.org pour la première fois._

### ✨ Nouveautés

- **La surface de génération `Any`** — un DSL fluide produisant des valeurs de test arbitraires mais valides. Les contraintes expriment les invariants qu'une valeur doit satisfaire, jamais ce que le test vérifie. Scalaires, chaînes, collections, dictionnaires, ensembles, énumérations, GUID, types temporels et URI, plus la composition via `As`, `Combine` et `OrNull`.
- **Détection des conflits en échec rapide.** Des contraintes contradictoires sont refusées dès la déclaration, en nommant les deux côtés, plutôt que de boucler ou de tirer en silence une valeur qui ne satisfait ni l'une ni l'autre.
- **Reproductibilité.** `Any.Reproducibly` épingle une seed pour l'exécution et la rapporte quand le corps échoue, pour qu'un test rouge indique comment se rejouer ; `Any.ReproduciblyAsync` couvre les corps `async`, et `Any.UseSeed` ouvre une portée explicite.
- **28 analyseurs maison** (`JD001`–`JD028`), embarqués dans ce package, qui surveillent la frontière entre recette et valeur, là où le système de types ne peut pas l'atteindre seul.
- **Deux frameworks cibles.** `netstandard2.0` pour la portée la plus large, et `net8.0` qui ajoute les générateurs pour `DateOnly`, `TimeOnly`, `Int128`, `UInt128` et `Half`. Le plancher .NET Framework supporté est 4.7.2, et la CI exécute les suites dessus.
- **Durcissement du package** — SBOM SPDX embarqué, SourceLink, package de symboles, build déterministe, et une attestation de provenance sur l'artefact publié.
