# `JustDummies.Cli`

🌍 **Langues :**  
🇬🇧 [English](./justdummies-cli.en.md) | 🇫🇷 Français (ce fichier)

`dum` écrit le générateur de dummy pour l'un de vos types, **une fois**, sous forme de code ordinaire
que vous possédez et modifiez. Ce n'est pas un générateur de source et il ne s'exécute pas à la
compilation : il lit votre compilation, émet un fichier, et s'efface.

## Installation

```bash
dotnet tool install --global JustDummies.Cli
```

Le paquet installe une seule commande, `dum`. Contrairement aux trois bibliothèques, vous ne le
référencez jamais depuis un projet : c'est un outil, pas une dépendance.

## Ce qu'il produit

Lancez-le depuis votre projet de **test** : c'est là que le fichier a sa place, et là que le type est
atteignable.

```text
$ dum generate Order

Analyzing Shop.Domain.Order
  constructor Order(OrderReference, Customer, int, OrderStatus, IReadOnlyList<string>, DateTime)

  reference  OrderReference         Any.String().NonEmpty().As(OrderReference.Create)  factory, guard
  customer   Customer               —                                                  TODO
  quantity   int                    Any.Int32().Positive()                             guard
  status     OrderStatus            Any.Enum<OrderStatus>()
  tags       IReadOnlyList<string>  Any.ListOf(Any.String().NonEmpty())
  placedAt   DateTime               Any.DateTime()

✓ AnyOrder.cs — 5 of 6 parameters inferred, 1 TODO.
  The file will not compile until you resolve it. That is deliberate.
```

`AnyOrder.cs` est une `partial class` implémentant `IAny<Order>`, avec une méthode `With…` par
paramètre du constructeur. Il vous appartient dès cet instant : lisez-le, modifiez-le, commitez-le.

## Ce que dit la dernière colonne

C'est tout l'intérêt du récapitulatif, pas une décoration — elle sépare ce qui a été **inféré** de ce
qui a été **deviné** :

| Mot | Signification |
| --- | --- |
| *(vide)* | directement issu de la table de base pour ce type |
| `guard` | un guard du constructeur l'a resserré (`quantity <= 0` → `.Positive()`) |
| `factory` | composé via une fabrique statique (`.As(OrderReference.Create)`) |
| `AnyX` | un générateur que vous aviez déjà scaffoldé a été réutilisé |
| `TODO` | rien n'a pu être inféré ; le fichier nomme ce qu'il reste à faire |
| `unavailable` | le générateur existe dans JustDummies, mais pas dans l'asset que votre projet résout |

**Un `TODO` n'est pas un échec.** L'outil émet un identifiant qui n'existe pas, si bien que *votre
propre build* signale ce qui n'a pas pu être inféré, à la ligne exacte, avec le type sous la main
([ADR-0060](../../for-maintainers/adr/0060-seed-generators-from-constructor-guards.fr.md)). Un
générateur qui tirerait discrètement une valeur plausible à cet endroit serait bien pire.

## À travers un graphe d'agrégats

`customer` est ouvert ci-dessus parce que `AnyCustomer` n'existe pas encore. Scaffoldez-le,
recompilez, puis relancez :

```bash
dum generate Customer
dotnet build
dum generate Order --force
```

La ligne se referme sur `new AnyCustomer()`. Ce parcours en deux temps est la façon prévue de
traverser un graphe d'agrégats : l'outil ne compose que ce qu'il voit déjà dans votre compilation.

## Options

| Option | Défaut | Signification |
| --- | --- | --- |
| `--project <chemin>` | l'unique `*.csproj` du répertoire courant | projet dont la compilation est analysée |
| `--output <dossier>` | le répertoire courant | où le fichier est écrit |
| `--namespace <ns>` | le namespace du type visé | namespace du type émis |
| `--force` | inactif | écrase un fichier existant |
| `--dry-run` | inactif | imprime le fichier sur la sortie standard ; n'écrit rien |

`dum generate Order Customer Invoice` en scaffolde plusieurs. Ils sont traités indépendamment, et le
code de sortie est le pire d'entre eux : `0` un fichier écrit (TODO compris), `1` un scaffolding qui
a échoué, `2` une ligne de commande illisible.

## Il ne référence jamais JustDummies

L'outil résout chaque symbole de la bibliothèque **par son nom, contre votre compilation**, et ne
déclare aucune dépendance vers elle
([ADR-0063](../../for-maintainers/adr/0063-give-the-scaffolder-no-dependency-on-the-package.fr.md)).
L'outil et la bibliothèque versionnent donc indépendamment, et `dum` ne peut pas entraîner une montée
de version de JustDummies dans votre projet. Si un générateur n'existe pas dans l'asset que vous
résolvez, il le dit plutôt que d'émettre un appel qui ne compilera pas.

## Prérequis

Le paquet [`JustDummies`](./justdummies.fr.md) dans le projet analysé — sans lui rien ne peut être
résolu, et `dum` le dit plutôt que d'émettre quoi que ce soit.

L'outil lui-même cible **.NET 8** et roule vers l'avant : n'importe quel runtime plus récent que vous
avez installé l'exécute.

---

[← Paquets](./README.fr.md) · [Sommaire de la documentation](../README.fr.md)
