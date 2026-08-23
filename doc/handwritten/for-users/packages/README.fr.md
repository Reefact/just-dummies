# Paquets

🌍 **Langues :**  
🇬🇧 [English](./README.md) | 🇫🇷 Français (ce fichier)

Quatre paquets sont publiés depuis ce dépôt. Trois sont des bibliothèques que vous référencez, et la
plupart des projets n'ont besoin que de l'une d'elles ; le quatrième est un outil en ligne de
commande que vous installez globalement et ne référencez jamais.

| Paquet | Ce que c'est | En ai-je besoin ? |
| --- | --- | --- |
| [`JustDummies`](./justdummies.fr.md) | la bibliothèque, avec ses 33 règles embarquées | **Oui** — c'est le produit |
| [`JustDummies.Xunit`](./justdummies-xunit.fr.md) | l'adaptateur xUnit v3 : `[Reproducible]` | Seulement avec xUnit v3, et seulement si vous voulez l'attribut |
| [`JustDummies.DiagnosticCatalog`](./justdummies-diagnosticcatalog.fr.md) | les règles `JD001`–`JD033` en constantes vérifiées par le compilateur | Seulement pour supprimer une règle sans littéral de chaîne |
| [`JustDummies.Cli`](./justdummies-cli.fr.md) | `dum`, le scaffolder : écrit le générateur d'un de vos types | Seulement pour en scaffolder un — outil global, jamais une référence |

## Comment ils s'articulent

```mermaid
flowchart TD
    accTitle: Comment les quatre paquets s'articulent
    accDescr: JustDummies, la bibliothèque, embarque les 33 règles sous analyzers/dotnet/cs. JustDummies.Xunit dépend de la bibliothèque. JustDummies.DiagnosticCatalog nomme ces règles sans en dépendre, et dum, le scaffolder, émet du code appelant la bibliothèque.
    L["JustDummies<br/><i>la bibliothèque</i>"] -->|"embarque"| A["33 règles<br/><i>analyzers/dotnet/cs</i>"]
    X["JustDummies.Xunit<br/><i>[Reproducible]</i>"] -->|"dépend de"| L
    C["JustDummies.DiagnosticCatalog<br/><i>JustDummiesRule.JD0NN</i>"]
    C -.->|"nomme les règles de"| A
    D["dum<br/><i>le scaffolder</i>"] -.->|"émet du code appelant"| L
    style L fill:#e8eaf6,stroke:#3f51b5,color:#1a237e
    style X fill:#e8f5e9,stroke:#43a047,color:#1b5e20
    style C fill:#fff8e1,stroke:#f9a825,color:#e65100
    style D fill:#fce4ec,stroke:#d81b60,color:#880e4f
```

`JustDummies` se suffit à lui-même : il ne prend aucune dépendance à l'exécution. Les analyzers
voyagent **à l'intérieur**, si bien qu'ajouter le paquet suffit à les obtenir.

`JustDummies.Xunit` dépend de la bibliothèque et de xUnit v3. `JustDummies.DiagnosticCatalog` est
autonome — il ne porte aucun générateur, seulement les identifiants de règles.

`dum` est le cas à part : il ne référence **rien**. Il résout chaque symbole de la bibliothèque par
son nom, contre votre propre compilation, si bien que le code qu'il écrit appelle JustDummies sans
que l'outil en dépende jamais
([ADR-0063](../../for-maintainers/adr/0063-give-the-scaffolder-no-dependency-on-the-package.fr.md)).

## Installation

```bash
dotnet add package JustDummies

# seulement avec xUnit v3
dotnet add package JustDummies.Xunit

# seulement pour nommer une règle dans un [SuppressMessage]
dotnet add package JustDummies.DiagnosticCatalog

# outil global, pas une référence de projet
dotnet tool install --global JustDummies.Cli
```

Les versions présentes sur nuget.org ne sont pas répétées ici, car une copie de cette information est
périmée dès le lendemain d'une publication : consultez plutôt
[la fiche du paquet](https://www.nuget.org/packages/JustDummies).

## Frameworks cibles

Les trois bibliothèques ciblent **`netstandard2.0`**, ce qui leur donne leur portée. `dum` est un
outil et non une bibliothèque : la portée ne s'y applique pas de la même façon. Il cible **`net8.0`**
et roule vers l'avant sur n'importe quel runtime plus récent que vous avez installé, quelle que soit
la cible du projet qu'il analyse.

`JustDummies` publie en plus un asset **`net8.0`** portant les générateurs des types qui n'existent
pas en deçà — `DateOnly`, `TimeOnly`, `Int128`, `UInt128` et `Half`. Un projet ciblant .NET 8 ou
au-delà résout cet asset et obtient ces fabriques ; un projet en deçà résout l'asset
`netstandard2.0`, où elles sont simplement absentes.

Le plancher .NET Framework supporté est **4.7.2**, et la CI y exécute les suites
([ADR-0007](../../for-maintainers/adr/0007-floor-the-library-on-net-framework-4-7-2.fr.md)).

## Une note sur la stabilité

La surface publique est déclarée dans `PublicAPI.Unshipped.txt` et non dans
`PublicAPI.Shipped.txt` : rien n'en est encore promis, et c'est une version stable qui la figera.

Le **contrat de graine** fait exception, et il est déjà promis : depuis `1.0.0-preview.1`, une graine
donnée tire les mêmes valeurs sur chaque version corrective et mineure d'une version majeure,
garanti par un golden master
([ADR-0049](../../for-maintainers/adr/0049-replay-a-seed-across-patch-and-minor-versions.fr.md)).

---

[← Sommaire de la documentation](../README.fr.md)
