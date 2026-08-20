# JustDummies

🌍 **Langues :**  
🇬🇧 [English](README.md) | 🇫🇷 Français (ce fichier)

|  |  |
| :-- | :-- |
| **Build** | [![ci](https://github.com/Reefact/just-dummies/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Reefact/just-dummies/actions/workflows/ci.yml) |
| **Qualité** | [![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=reefact_just-dummies&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=reefact_just-dummies) [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=reefact_just-dummies&metric=coverage)](https://sonarcloud.io/summary/new_code?id=reefact_just-dummies) |
| **Sécurité** | [![codeql](https://github.com/Reefact/just-dummies/actions/workflows/codeql.yml/badge.svg?branch=main)](https://github.com/Reefact/just-dummies/actions/workflows/codeql.yml) [![OpenSSF Best Practices](https://www.bestpractices.dev/projects/14006/badge)](https://www.bestpractices.dev/projects/14006) [![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/Reefact/just-dummies/badge)](https://securityscorecards.dev/viewer/?uri=github.com/Reefact/just-dummies) |
| **Paquet** | [![NuGet](https://img.shields.io/nuget/vpre/JustDummies?logo=nuget)](https://www.nuget.org/packages/JustDummies) ![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4) |
| **Projet** | [![License](https://img.shields.io/github/license/Reefact/just-dummies)](LICENSE) [![Conventional Commits](https://img.shields.io/badge/Conventional%20Commits-1.0.0-fe5196?logo=conventionalcommits&logoColor=white)](https://www.conventionalcommits.org) |

**Un DSL fluide pour générer des valeurs de test arbitraires mais valides : des dummies.**

## 🚨 Le problème

Tout test regorge de valeurs dont il ne se soucie pas.

```csharp
string reference = "ORD-12345678";
int    quantity  = 3;
```

Un lecteur ne peut pas savoir si `3` compte ou si `7` conviendrait. Chaque littéral a l'air également
porteur de sens, personne n'ose donc en changer un — et le test ne couvre jamais que ce cas-là. Un
défaut réclamant une autre forme d'entrée est un défaut que ce test ne trouvera jamais.

## ✅ La solution

Dites ce que la valeur doit **satisfaire**, et laissez la bibliothèque en tirer une qui convient :

```csharp
string reference = Any.String().StartingWith("ORD-").WithLength(12).Generate();
int    quantity  = Any.Int32().Between(1, 100).Generate();
Guid   id        = Any.Guid().NonEmpty().Generate();
```

Le test énonce désormais ses hypothèses. Tout le reste varie d'une exécution à l'autre, et c'est ce
qui lui fait trouver des choses.

Un appel `Any.*` renvoie un **générateur** — une recette immuable — et `.Generate()` en tire une
valeur. Un objet-valeur au contrat plus strict se construit en transformant un primitif contraint via
sa vraie fabrique :

```csharp
OrderReference orderRef = Any.String()
    .StartingWith("ORD-")
    .WithLength(12)
    .As(OrderReference.Create)
    .Generate();
```

**La seule règle qui compte :** une contrainte énonce un invariant du domaine, jamais ce que le test
affirme. Des contraintes contradictoires échouent immédiatement, avec un message nommant *les deux*
côtés.

## 📦 Installation

```bash
dotnet add package JustDummies
```

Aucune dépendance à l'exécution, et les 32 règles d'analyzer sont embarquées — elles se mettent à travailler dès
votre prochaine compilation.

## 🔁 Reproductible par construction

Des valeurs aléatoires dans les tests ne sont acceptables que si un échec peut être rejoué.
Enveloppez le corps du test :

```csharp
Any.Reproducibly(() => {
    decimal orderTotal = Any.Decimal().Between(0m, 10_000m).WithScale(2).Generate();

    Assert.InRange(Shipping.FeeFor(orderTotal), 0m, 4.90m);
});
```

Quand il passe au rouge — et seulement alors — la graine qui a produit l'exécution est rapportée :

```text
[JustDummies] These arbitrary values were seeded with 1743029518. Reproduce this run with Any.Reproducibly(1743029518, ...).
```

Recopiez ce nombre devant le corps. Même test, un argument de plus, et l'exécution exacte revient —
valeur pour valeur :

```csharp
Any.Reproducibly(1743029518, () => {
    // le même corps que ci-dessus ; seule la graine a été ajoutée
});
```

Corrigez le défaut, puis supprimez la graine pour que le test recommence à varier.

Avec xUnit v3, `[Reproducible]` remplace complètement l'enveloppe — voir
[l'adaptateur](doc/handwritten/for-users/packages/justdummies-xunit.fr.md). Depuis
`1.0.0-preview.1`, une graine se rejoue sur chaque version corrective et mineure d'une majeure,
garanti par un golden master
([ADR-0049](doc/handwritten/for-maintainers/adr/0049-replay-a-seed-across-patch-and-minor-versions.fr.md)).

## 🛠 En scaffolder un pour vos propres types

Écrire un `IAny<T>` à la main pour chacun de vos types métier, c'est la partie fastidieuse. `dum` en
écrit le premier jet :

```bash
dotnet tool install --global JustDummies.Cli
dum generate Order
```

```text
  reference  OrderReference  Any.String().NonEmpty().As(OrderReference.Create)  factory, guard
  customer   Customer        —                                                  TODO
  quantity   int             Any.Int32().Positive()                             guard

✓ AnyOrder.cs — 5 of 6 parameters inferred, 1 TODO.
```

Il lit votre compilation, resserre ce que les guards du constructeur lui disent
(`quantity <= 0` → `.Positive()`), et émet du code ordinaire qui vous appartient ensuite. Ce qu'il
n'a pas pu inférer, il le laisse sous forme d'un identifiant qui n'existe pas — si bien que c'est
**votre** build qui nomme le trou, à la ligne, au lieu qu'une valeur plausible soit tirée dans votre
dos.

→ [`JustDummies.Cli`](doc/handwritten/for-users/packages/justdummies-cli.fr.md)

## 📚 Documentation

**→ [Commencez par le guide en dix minutes](doc/handwritten/for-users/guides/getting-started.fr.md)**

| | |
| --- | --- |
| [Sommaire de la documentation](doc/handwritten/for-users/README.fr.md) | tout, organisé, en anglais et en français |
| [Concepts fondamentaux](doc/handwritten/for-users/guides/core-concepts.fr.md) | recette contre valeur, et la règle d'or |
| [Référence des générateurs](doc/handwritten/for-users/generators/README.fr.md) | chaque fabrique `Any.*` et ses contraintes |
| [Reproductibilité](doc/handwritten/for-users/guides/reproducibility.fr.md) | graines, portées et rejeu |
| [Composition](doc/handwritten/for-users/guides/composition.fr.md) | des dummies pour vos propres types |
| [Règles des analyzers](doc/handwritten/for-users/analyzers/README.fr.md) | une page par diagnostic |
| [Principes de conception](doc/handwritten/for-users/guides/design-principles.fr.md) | ce qu'il refuse volontairement, et pourquoi |

## 🧩 Paquets

| Paquet | Ce que c'est |
| --- | --- |
| [`JustDummies`](doc/handwritten/for-users/packages/justdummies.fr.md) | la bibliothèque, avec ses 32 règles embarquées |
| [`JustDummies.Xunit`](doc/handwritten/for-users/packages/justdummies-xunit.fr.md) | l'adaptateur xUnit v3 : `[Reproducible]` |
| [`JustDummies.DiagnosticCatalog`](doc/handwritten/for-users/packages/justdummies-diagnosticcatalog.fr.md) | les règles `JD001`–`JD032` en constantes vérifiées par le compilateur |
| [`JustDummies.Cli`](doc/handwritten/for-users/packages/justdummies-cli.fr.md) | `dum`, le scaffolder — outil global, jamais une référence de projet |

Les trois bibliothèques ciblent `netstandard2.0` ; `JustDummies` publie en plus un asset `net8.0`
avec les générateurs modernes (`DateOnly`, `TimeOnly`, `Int128`, `UInt128`, `Half`). Le plancher .NET
Framework supporté est **4.7.2**
([ADR-0007](doc/handwritten/for-maintainers/adr/0007-floor-the-library-on-net-framework-4-7-2.fr.md)).
`dum` est un outil et non une bibliothèque : il cible `net8.0` et roule vers l'avant, quelle que soit
la cible du projet qu'il analyse.

> **Préversion.** La surface publique est déclarée dans `PublicAPI.Unshipped.txt` : rien n'en est
> encore promis, et c'est une version stable qui la figera. Le contrat de graine fait exception —
> voir plus haut. Les versions présentes sur nuget.org ne sont pas répétées ici, car une copie de
> cette information est périmée dès le lendemain d'une publication : consultez
> [la fiche du paquet](https://www.nuget.org/packages/JustDummies).

## 🤝 Contribuer

Tickets et pull requests sont les bienvenus. Commencez par
[`CONTRIBUTING.fr.md`](doc/handwritten/for-users/CONTRIBUTING.fr.md) pour les conventions de commit
et les règles du banc de test, et par
[`SECURITY.fr.md`](doc/handwritten/for-users/SECURITY.fr.md) pour signaler une vulnérabilité.

```bash
dotnet build JustDummies.sln -c Release
dotnet test  JustDummies.sln -c Release
```

Le dépôt cible le SDK .NET 10 (épinglé dans `global.json`). Le matériel destiné aux mainteneurs —
décisions d'architecture, workflows, spécifications — se trouve sous
[`doc/handwritten/for-maintainers/`](doc/handwritten/for-maintainers/README.fr.md).

## 📜 Histoire et licence

Ce dépôt a été extrait de
[`Reefact/first-class-errors`](https://github.com/Reefact/first-class-errors) le 31/07/2026 avec
`git filter-repo`, en préservant auteurs, dates et messages de commit. **Les empreintes de commit
diffèrent donc du dépôt source, et les numéros de tickets ou de PR figurant dans les messages de
commit antérieurs à l'extraction renvoient à `Reefact/first-class-errors`.** Le relevé complet est
dans [`doc/handwritten/for-maintainers/migration/`](doc/handwritten/for-maintainers/migration/) ; la
décision est
[ADR-0044](doc/handwritten/for-maintainers/adr/0044-extract-justdummies-into-its-own-repository.fr.md).

Distribué sous licence [Apache 2.0](LICENSE).
