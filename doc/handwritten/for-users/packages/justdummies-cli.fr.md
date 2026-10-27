# `JustDummies.Cli`

🌍 **Langues :**  
🇬🇧 [English](./justdummies-cli.en.md) | 🇫🇷 Français (ce fichier)

`any` écrit le générateur de valeurs arbitraires de l'un de vos types — `AnyOrder` pour `Order` —
**une fois**, sous forme de code ordinaire que vous possédez et modifiez. Ce n'est pas un générateur de source et il ne s'exécute pas à la
compilation : il lit votre compilation, émet un fichier, et s'efface.

## Installation

```bash
dotnet tool install --global JustDummies.Cli
```

Le paquet installe une seule commande, `any`. Contrairement aux trois bibliothèques, vous ne le
référencez jamais depuis un projet : c'est un outil, pas une dépendance.

## Ce qu'il produit

Lancez-le depuis votre projet de **test** : c'est là que le fichier a sa place, et là que le type est
atteignable.

```text
$ any generate Order

Analyzing Shop.Domain.Order
  constructor Order(OrderReference, Customer, int, OrderStatus, IReadOnlyList<string>, DateTime)

  reference  OrderReference         new AnyOrderReference()              AnyX
  customer   Customer               new AnyCustomer()                    AnyX
  quantity   int                    Any.Int32().Positive()               guard
  status     OrderStatus            Any.Enum<OrderStatus>()
  tags       IReadOnlyList<string>  Any.ListOf(Any.String().NonEmpty())
  placedAt   DateTime               Any.DateTime()

✓ AnyOrder.cs — 6 of 6 parameters inferred.
```

`AnyOrder.cs` est une `partial class` implémentant `IAny<Order>`, avec deux méthodes `With…` par
paramètre du constructeur — l'une pour fixer sa valeur, l'autre pour affiner sa base valide
([Ajuster un paramètre](#ajuster-un-paramètre)). Il vous appartient dès cet instant : lisez-le,
modifiez-le, commitez-le.

Ce qu'il tire est arbitraire — d'où `Any`, et d'où le nom de l'outil. Une valeur que vous fixez par une
méthode `With…` n'est pas tirée mais choisie ; que l'objet qui la porte reste un dummy dépend de ce
que le résultat de votre test dépende ou non de cette valeur, et non de l'appel : voir
[Arbitraire ne veut pas dire dummy](../guides/core-concepts.fr.md#arbitraire-ne-veut-pas-dire-dummy).

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
private static AnyString AnyValidValue() {
    // TODO(any): 'string value' may be guarded by something the scaffolder could not read (§9).
    //   This is the scaffolder's best generator for the type; verify it honours the real invariant,
    //   or replace it, then delete the line below.
    _ = TODO_verify_the_generator_for_value;

    return Any.String().NonEmpty();
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
any generate OrderReference
any generate Order --force
```

Le récapitulatif ne le répète pas — un fichier qui ne build pas n'est pas un silence. La seule forme
composée qu'il laisse à un `TODO` est un type générique, parce que `AnyRepository` nommerait aussi
mal `Repository<Order>` que `Repository<Line>`.

## L'atteindre comme `Any.Order()`

`new AnyOrder()` est la façon d'atteindre le generator, et elle marche toujours. Si vous préférez que
les deux moitiés d'un bloc d'arrangement se lisent pareil — `Any.Int32()` sur une ligne et
`Any.Order()` sur la suivante — demandez un point d'entrée :

```bash
any generate Order --entry-point any
```

Cela écrit un second fichier, `AnyOrder.Entry.cs`, à côté du generator :

<!-- jd:skip -->
```csharp
Order anyPendingOrder = Any.Order().WithStatus(OrderStatus.Pending).Generate();
```

Il utilise un membre d'extension C# 14, donc le projet doit compiler en C# 14 ; en deçà, `any` le dit
et s'arrête plutôt que de vous donner discrètement autre chose. Si vous ne pouvez pas relever la
version de langage — ou si vous préférez que la racine soit la vôtre — nommez-en une :

```bash
any generate Order --entry-point static:Arbitrary    # Arbitrary.Order()
```

Cette forme n'exige aucun C# 14. La racine est `partial` et chaque type apporte son propre fichier,
donc `any generate Order Customer Invoice --entry-point static:Arbitrary` vous donne `Arbitrary.Order()`,
`Arbitrary.Customer()` et `Arbitrary.Invoice()` sans qu'aucun fichier soit écrit deux fois.

Par défaut le point d'entrée est déclaré à côté du generator, ce qui ne coûte aucun import à vos
tests. `--entry-point-namespace` déplace ce fichier — et lui seul — pour qu'une racine unique
rassemble des types de plusieurs namespaces :

```bash
any generate Order --entry-point static:Arbitrary --entry-point-namespace Shop.Tests.Arbitrary
```

Le generator, lui, ne bouge pas, et `AnyOrder.cs` est identique octet pour octet quelle que soit la
valeur demandée. Une racine nommée `Any` est refusée : une classe statique de ce nom dans votre propre
projet masquerait `JustDummies.Any` pour tout son namespace, et `Any.Int32()` cesserait de compiler —
ce que `--entry-point any` existe précisément pour éviter. Décision :
[ADR-0070](../../for-maintainers/adr/0070-emit-an-entry-point-on-request-as-a-file-of-its-own.fr.md).

## Ajuster un paramètre

Chaque paramètre d'un generator scaffoldé vient avec deux méthodes `With…`. Elles répondent à deux
questions différentes, et la première chose à se demander est laquelle votre test pose.

Les exemples ci-dessous utilisent un `Shipment` qui n'accepte qu'une `Route` nationale — une règle qui
appartient à l'expédition, puisqu'une route seule peut très bien franchir une frontière — et les
`AnyShipment` et `AnyRoute` que `any` a scaffoldés pour eux, atteints par `--entry-point any`.

### La valeur entière, ou une partie seulement ?

Si votre test tient à **une partie** d'une valeur, donnez une lambda à la méthode. Elle reçoit la base
valide de ce paramètre, et vous n'énoncez que ce que vous y ajoutez :

```csharp
// Arrange
Shipment anyLongShipment = Any.Shipment()
    .WithRoute(r => r.WithDistanceKm(240))
    .Generate();

// Act
bool needsRestStop = anyLongShipment.NeedsRestStop;

// Assert
Assert.True(needsRestStop);
```

La route reste arbitraire, et reste nationale : le test n'a pas à savoir qu'une expédition en exige
une, donc il ne le dit pas. Deux noms portent cette lecture. `anyLongShipment` dit les deux moitiés —
`any` pour ce qui a été tiré, `Long` pour ce que le test a choisi
([Arbitraire ne veut pas dire dummy](../guides/core-concepts.fr.md#arbitraire-ne-veut-pas-dire-dummy)).
Le paramètre de la lambda s'appelle `r`, pas `anyRoute` : c'est un generator déjà préparé par la base,
pas une valeur tirée, et `any` ne nomme que ce qui a été tiré.

Si votre test tient à la valeur **entière** — quelle route, exactement — fixez-la :

```csharp
// Arrange
Route lyonParis = new Route("Lyon", "Paris", 465, domestic: true);

Shipment anyShipmentOnLyonParis      = Any.Shipment().WithRoute(lyonParis).Generate();
Shipment anyOtherShipmentOnLyonParis = Any.Shipment().WithRoute(lyonParis).Generate();

// Act
bool together = anyShipmentOnLyonParis.CanTravelWith(anyOtherShipmentOnLyonParis);

// Assert
Assert.True(together);
```

Deux lambdas auraient tiré deux routes différentes. Une valeur fixée contourne elle aussi la base, mais
elle est la vôtre, et elle est fixe : si elle viole une règle, le test échoue dès sa première
exécution, et à chacune des suivantes.

### Où vit la base valide

Chaque paramètre a une fabrique dans le fichier généré, nommée `AnyValid…()`. Le constructeur du
generator tire d'elle, et chaque lambda part d'elle : c'est l'adresse unique de ce paramètre.

C'est aussi là que vous écrivez une règle que l'outil n'a pas su appliquer. Ici, `Shipment` vérifie
sur sa route quelque chose que `any` n'a pas su lire ; il a donc marqué le paramètre `to verify`, et la
base est devenue :

<!-- jd:declarations -->
```csharp
public sealed partial class AnyShipment {

    private static AnyRoute AnyValidRoute() {
        // A shipment only accepts a domestic route; a route on its own may cross a border.
        return new AnyRoute().WithDomestic(true);
    }

}
```

Écrite là une fois, la règle vaut pour chaque test qui tire une expédition, avec ou sans lambda.
Changée là, elle change pour tous à la fois — aucun test ne l'a répétée, donc aucun test n'est à
retrouver.

### Ce qu'un affinage peut faire, et ce qu'il ne peut pas

* **Il resserre une feuille.** Sur un nombre, une chaîne, une date ou une collection, la lambda ajoute
  des contraintes à la base, et la bibliothèque les vérifie ensemble :
  `WithParcels(p => p.LessThanOrEqualTo(10))` tire de 1 à 10, parce que la base est déjà
  `Positive()`. Un affinage qui contredit la base — `p => p.Negative()` — est refusé à cette ligne, à
  chaque exécution, par une exception qui nomme à la fois `Positive()` et `Negative()`.
* **Il fixe une partie d'une valeur composée.** Sur une route, la lambda appelle les propres méthodes
  `With…` d'`AnyRoute`, et ce que vous fixez remplace ce que la base avait fixé pour cette partie.
  `r => r.WithDomestic(false)` est accepté par la route et refusé par l'expédition — à chaque
  exécution, puisque la valeur est fixée. Une lambda garantit d'où elle part, pas où vous l'emmenez.
* **Le dernier appel l'emporte**, comme pour chaque `With…`. Un second `WithParcels(…)` repart de la
  base ; il ne s'ajoute pas au premier.
* **Il n'élargit jamais la base.** Aucune lambda ne peut tirer une étiquette vide quand la base dit
  `NonEmpty()`.

### Quand la lambda ne suffit pas

Aucune méthode `With…` ne prend un generator pour le mettre à la place de la base : c'était ainsi
qu'un test perdait une règle tout en compilant. `WithParcels(Any.Int32().LessThanOrEqualTo(10))` ne
compile plus — et ne doit pas compiler, puisqu'il tirait zéro et des nombres négatifs que l'expédition
refuse à certaines exécutions, pas à toutes.

Cinq besoins passaient par cette surcharge. Chacun a un autre chemin.

**Des valeurs déterministes hors d'un test.** Plutôt que de passer les generators d'un contexte
`Any.WithSeed(…)` paramètre par paramètre, ouvrez la portée ambiante. Elle atteint chaque tirage, y
compris celui de la route :

```csharp
using (Any.UseSeed(20261027)) {
    List<Shipment> anyShipments = Any.ListOf(Any.Shipment()).WithCount(50).Generate();
}
```

**Les éléments d'une collection, tirés autrement.** Une lambda peut changer la collection —
`l => l.WithCount(3)` — mais pas la recette de ses éléments. Si chaque test a besoin d'étiquettes
alphabétiques, cette règle appartient à la base : changez `AnyValidLabels()`. Si un seul test en a
besoin, ajoutez la surcharge vous-même (ci-dessous) et passez `Any.ListOf(Any.String().Alpha())`.

**Une valeur parmi quelques-unes choisies.** `Any.OneOf(lyonParis, parisLille)` signifie d'ordinaire
que le résultat dépend de celle qui est tirée — une propriété, testée sur un seul tirage. Fixez celle
dont parle le test. Si cela n'a vraiment aucune importance, ajoutez la surcharge.

**Parfois `null`.** Une lambda ne peut pas le dire : elle reçoit un `AnyDecimal` et doit en rendre
un, alors que `OrNull()` renvoie un generator de `decimal?`. *Toujours* `null` tient en un appel, le
cast distinguant la valeur d'une lambda :

```csharp
Shipment anyUndeclaredShipment = Any.Shipment().WithDeclaredValue((decimal?)null).Generate();
```

*Parfois* `null` est la surcharge que vous ajoutez.

**Plus que la base ne permet.** Si le domaine accepte une étiquette vide, c'est la base qui est
fausse : corrigez `AnyValidLabels()`. Si un seul test a besoin d'une étiquette vide précisément,
fixez-la.

### Ajouter la surcharge vous-même

Le fichier généré est le vôtre, et la classe est `partial`. Ajoutez à côté un fichier qui porte la
surcharge dont vous avez besoin :

<!-- jd:skip -->
```csharp
// AnyShipment.Custom.cs: written by you, never touched by `any generate --force`.
public sealed partial class AnyShipment {

    public AnyShipment WithDeclaredValue(IAny<decimal?> generator) {
        return new AnyShipment(_route, _parcels, _labels, generator);
    }

}
```

Le test dit alors ce qu'il veut dire :

```csharp
Shipment anyShipment = Any.Shipment()
    .WithDeclaredValue(Any.Decimal().Positive().OrNull())
    .Generate();
```

Trois choses à savoir sur ce fichier :

1. Il appelle le constructeur privé du fichier généré, chaque champ à sa place — le même appel que
   fait chaque `With…` généré.
2. `any generate Shipment --force` ne réécrit que `AnyShipment.cs`, donc votre fichier survit à un
   nouveau scaffold.
3. Si le constructeur de `Shipment` change, un nouveau scaffold change ce constructeur privé, et votre
   méthode cesse de compiler à sa propre ligne. Elle ne se trompe jamais en silence.

Et une chose à retenir : cette surcharge **remplace** la base. Quelle que soit la règle que
`AnyValid…()` garde pour ce paramètre, le generator que vous passez doit la garder aussi — c'est
pourquoi l'outil ne l'écrit pas pour vous. Décision :
[ADR-0109](../../for-maintainers/adr/0109-let-a-test-refine-a-parameters-valid-base-rather-than-replace-it.fr.md).

## Rendre compte à un script

`any` rend compte à un lecteur par défaut. `--format json` rend compte à un script à la place — un
document JSON sur stdout, et rien d'autre dessus :

```bash
any generate Order Customer Invoice --format json > report.json
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

Les options qui décrivent votre projet plutôt que cette invocation ont leur place dans un `any.json`
**à côté de votre fichier projet**, commité avec lui :

```json
{ "output": "./Arbitrary", "entryPoint": "static:Arbitrary", "entryPointNamespace": "Shop.Tests.Arbitrary" }
```

`any generate Order` vous donne alors ce que la longue ligne de commande aurait donné. Cinq clés sont
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

`any generate Order Customer Invoice` en scaffolde plusieurs. Ils sont traités indépendamment, et le
code de sortie est le pire d'entre eux : `0` un fichier écrit (TODO compris), `1` un scaffolding qui
a échoué, `2` une instruction que l'outil n'a pas pu lire — une ligne de commande, ou un `any.json`.

## Il ne référence jamais JustDummies

L'outil résout chaque symbole de la bibliothèque **par son nom, contre votre compilation**, et ne
déclare aucune dépendance vers elle
([ADR-0063](../../for-maintainers/adr/0063-give-the-scaffolder-no-dependency-on-the-package.fr.md)).
L'outil et la bibliothèque versionnent donc indépendamment, et `any` ne peut pas entraîner une montée
de version de JustDummies dans votre projet. Si un générateur n'existe pas dans l'asset que vous
résolvez, il le dit plutôt que d'émettre un appel qui ne compilera pas.

## Prérequis

Le paquet [`JustDummies`](./justdummies.fr.md) dans le projet analysé — sans lui rien ne peut être
résolu, et `any` le dit plutôt que d'émettre quoi que ce soit.

L'outil lui-même cible **.NET 8** et roule vers l'avant : n'importe quel runtime plus récent que vous
avez installé l'exécute.

---

[← Paquets](./README.fr.md) · [Sommaire de la documentation](../README.fr.md)
