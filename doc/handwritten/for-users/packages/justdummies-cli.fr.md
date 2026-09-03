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

  reference  OrderReference         new AnyOrderReference()              AnyX
  customer   Customer               new AnyCustomer()                    AnyX
  quantity   int                    Dummy.Int32().Positive()               guard
  status     OrderStatus            Dummy.Enum<OrderStatus>()
  tags       IReadOnlyList<string>  Dummy.ListOf(Dummy.String().NonEmpty())
  placedAt   DateTime               Dummy.DateTime()

✓ DummyOrder.cs — 6 of 6 parameters inferred.
```

`DummyOrder.cs` est une `partial class` implémentant `IDummy<Order>`, avec une méthode `With…` par
paramètre du constructeur. Il vous appartient dès cet instant : lisez-le, modifiez-le, commitez-le.

## Ce que dit la dernière colonne

C'est tout l'intérêt du récapitulatif, pas une décoration — elle sépare ce qui a été **inféré** de ce
qui a été **deviné** :

| Mot | Signification |
| --- | --- |
| *(vide)* | directement issu de la table de base pour ce type |
| `guard` | un guard du constructeur l'a resserré (`quantity <= 0` → `.Positive()`) |
| `AnyX` | tiré par le generator que ce type possède |
| `TODO` | rien n'a pu être inféré ; le fichier nomme ce qu'il reste à faire |
| `to verify` | un générateur *a bien* été inféré, mais quelque chose près de ce paramètre n'a pas pu être lu — vérifiez-le |
| `unread guards` | ce « quelque chose » : une garde que l'outil ne reconnaît pas, un helper dans lequel il ne voit pas, ou une garde qu'il lit sans pouvoir la situer — sous une écriture du paramètre, ou sous quelque chose qui décide si elle s'exécute |
| `constraint unavailable` | une garde a été comprise, et ce générateur n'a aucun membre pour l'exprimer |
| `no source` | le type vient d'un package, il n'y avait donc aucun corps de constructeur à lire |
| `unavailable` | le générateur existe dans JustDummies, mais pas dans l'asset que votre projet résout |

**Un `TODO` n'est pas un échec.** L'outil émet un identifiant qui n'existe pas, si bien que *votre
propre build* signale ce qui n'a pas pu être inféré, à la ligne exacte, avec le type sous la main
([ADR-0060](../../for-maintainers/adr/0060-seed-generators-from-constructor-guards.fr.md)). Un
générateur qui tirerait discrètement une valeur plausible à cet endroit serait bien pire.

**`to verify` fonctionne pareil, et pour la même raison.** Là où votre constructeur délègue sa
validation à un helper, garde dans une forme que l'outil n'analyse pas, ou garde à un endroit dont il
ne peut pas répondre — sous une écriture du paramètre, ou sous quelque chose qui décide si la garde
s'exécute —, il ne peut pas promettre que la recette inférée honore votre véritable invariant — alors il écrit cette recette comme base de
travail et ajoute au-dessus une ligne qui ne compile pas :

<!-- jd:skip -->
```csharp
private static IDummy<string> AnyValidValue() {
    // TODO(dum): 'string value' may be guarded by something dum could not read (§9).
    //   This is dum's best generator for the type; verify it honours the real invariant,
    //   or replace it, then delete the line below.
    _ = TODO_verify_the_generator_for_value;

    return Dummy.String().NonEmpty();
}
```

Gardez la recette ou remplacez-la, supprimez cette seule ligne, et c'est réglé. L'alternative — un
fichier qui compile et tire une valeur que votre constructeur rejette lors d'une exécution
ultérieure — est l'échec qui coûte le plus cher, parce qu'il surgit loin de sa cause
([ADR-0083](../../for-maintainers/adr/0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.fr.md)).

## À travers un graphe d'agrégats

Un type de domaine est tiré par le generator qu'il possède — `new AnyOrderReference()` — là où vit
la recette de ce type. Rien ne la redérive à chaque site qui le compose, donc aucun fichier ne porte
sa propre copie d'un invariant susceptible de dériver du constructeur qu'il décrit
([ADR-0089](../../for-maintainers/adr/0089-draw-a-composed-parameter-through-the-generator-its-type-owns.fr.md)).

Ce nom est écrit **que vous l'ayez scaffoldé ou non**, si bien que scaffolder un agrégat d'abord
vous donne un fichier qui nomme ce qui lui manque encore :

```text
error CS0246: The type or namespace name 'AnyOrderReference' could not be found
```

Soit la liste de travail, à la ligne qui en a besoin :

```bash
dum generate OrderReference
dum generate Order --force
```

Le récapitulatif ne le répète pas — un fichier qui ne build pas n'est pas un silence. La seule forme
composée qu'il laisse à un `TODO` est un type générique, parce que `AnyRepository` nommerait aussi
mal `Repository<Order>` que `Repository<Line>`.

## L'atteindre comme `Dummy.Order()`

`new DummyOrder()` est la façon d'atteindre le generator, et elle marche toujours. Si vous préférez que
les deux moitiés d'un bloc d'arrangement se lisent pareil — `Dummy.Int32()` sur une ligne et
`Dummy.Order()` sur la suivante — demandez un point d'entrée :

```bash
dum generate Order --entry-point dummy
```

Cela écrit un second fichier, `DummyOrder.Entry.cs`, à côté du generator :

<!-- jd:skip -->
```csharp
Order order = Dummy.Order().WithStatus(OrderStatus.Pending).Generate();
```

Il utilise un membre d'extension C# 14, donc le projet doit compiler en C# 14 ; en deçà, `dum` le dit
et s'arrête plutôt que de vous donner discrètement autre chose. Si vous ne pouvez pas relever la
version de langage — ou si vous préférez que la racine soit la vôtre — nommez-en une :

```bash
dum generate Order --entry-point static:Dummies    # Dummies.Order()
```

Cette forme n'exige aucun C# 14. La racine est `partial` et chaque type apporte son propre fichier,
donc `dum generate Order Customer Invoice --entry-point static:Dummies` vous donne `Dummies.Order()`,
`Dummies.Customer()` et `Dummies.Invoice()` sans qu'aucun fichier soit écrit deux fois.

Par défaut le point d'entrée est déclaré à côté du generator, ce qui ne coûte aucun import à vos
tests. `--entry-point-namespace` déplace ce fichier — et lui seul — pour qu'une racine unique
rassemble des types de plusieurs namespaces :

```bash
dum generate Order --entry-point static:Dummies --entry-point-namespace Shop.Tests.Dummies
```

Le generator, lui, ne bouge pas, et `DummyOrder.cs` est identique octet pour octet quelle que soit la
valeur demandée. Une racine nommée `Dummy` est refusée : une classe statique de ce nom dans votre propre
projet masquerait `JustDummies.Dummy` pour tout son namespace, et `Dummy.Int32()` cesserait de compiler —
ce que `--entry-point dummy` existe précisément pour éviter. Décision :
[ADR-0070](../../for-maintainers/adr/0070-emit-an-entry-point-on-request-as-a-file-of-its-own.fr.md).

## Rendre compte à un script

`dum` rend compte à un lecteur par défaut. `--format json` rend compte à un script à la place — un
document JSON sur stdout, et rien d'autre dessus :

```bash
dum generate Order Customer Invoice --format json > report.json
```

Il existe parce que le code de sortie ne peut pas tout dire. Un fichier écrit avec des `TODO` ouverts
est un **succès** — c'est tout le design — donc `0` se lit pareil que tous les paramètres aient
résolu ou qu'un tiers d'entre eux non. Le rapport, lui, le dit :

```json
{
  "summary": { "scaffolded": 3, "failed": 0, "openParameters": 2, "parametersToVerify": 1 }
}
```

Les deux compteurs sont distincts parce qu'ils décrivent des états différents : un paramètre ouvert
n'a aucune expression, un paramètre à vérifier en a une et réclame quand même votre regard. Chaque
ligne de paramètre énonce les deux — `"resolved"` et `"requiresVerification"` — de sorte que les
compteurs se vérifient contre les lignes au lieu d'être crus sur parole.

Chaque résultat porte le type, les fichiers écrits et où ils sont allés, chaque paramètre avec son
expression et sa provenance, le point d'entrée s'il y en a un, et les avertissements. Une exécution
arrêtée avant son premier scaffold produit un document elle aussi, dont le `refusal` dit pourquoi —
de sorte que stdout porte toujours exactement un document. Tout ce qui est écrit pour une personne
continue d'aller sur stderr, ce qui laisse à `2>/dev/null` un tuyau propre.

Les codes de sortie sont inchangés : ceci ajoute un canal plutôt que d'en redéfinir un. Décision :
[ADR-0071](../../for-maintainers/adr/0071-report-a-run-as-data-without-moving-the-exit-codes.fr.md).

## Fixer les défauts une fois

Les options qui décrivent votre projet plutôt que cette invocation ont leur place dans un `dum.json`
**à côté de votre fichier projet**, commité avec lui :

```json
{ "output": "./Dummies", "entryPoint": "static:Dummies", "entryPointNamespace": "Shop.Tests.Dummies" }
```

`dum generate Order` vous donne alors ce que la longue ligne de commande aurait donné. Cinq clés sont
lues — `output`, `namespace`, `entryPoint`, `entryPointNamespace`, `format` — et **la ligne de commande
l'emporte toujours** sur chacune, de sorte qu'une invocation peut différer sans éditer le fichier.
`--force` et `--dry-run` n'en font pas partie : elles disent à quoi sert cette exécution, pas ce
qu'est le projet.

Une clé non lue est **refusée**, en la nommant — un défaut que vous croyez en vigueur et qui ne l'est
pas est pire que pas de fichier du tout. Un `output` relatif est résolu depuis le dossier du projet,
donc il veut dire la même chose d'où que vous lanciez l'outil. Décision :
[ADR-0072](../../for-maintainers/adr/0072-read-project-defaults-from-a-file-the-command-line-overrides.fr.md).

## Options

| Option | Défaut | Signification |
| --- | --- | --- |
| `--project <chemin>` | l'unique `*.csproj` du répertoire courant | projet dont la compilation est analysée |
| `--output <dossier>` | le répertoire courant | où le fichier est écrit |
| `--namespace <ns>` | le namespace du type visé | namespace du type émis |
| `--entry-point <v>` | `none` | émet en plus un point d'entrée : `none`, `static:<Name>` ou `any` |
| `--entry-point-namespace <ns>` | le namespace du type émis | namespace du seul fichier de point d'entrée |
| `--force` | inactif | écrase un fichier existant — les deux fichiers, quand il y en a deux |
| `--dry-run` | inactif | imprime le fichier sur la sortie standard ; n'écrit rien |
| `--format <f>` | `human` | comment l'exécution rend compte : `human` ou `json` |

`dum generate Order Customer Invoice` en scaffolde plusieurs. Ils sont traités indépendamment, et le
code de sortie est le pire d'entre eux : `0` un fichier écrit (TODO compris), `1` un scaffolding qui
a échoué, `2` une instruction que l'outil n'a pas pu lire — une ligne de commande, ou un `dum.json`.

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
