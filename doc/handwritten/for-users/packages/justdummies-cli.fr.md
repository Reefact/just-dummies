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
| `unread guards` | ce « quelque chose » : une garde que l'outil ne reconnaît pas, un helper dans lequel il ne voit pas, ou une garde qu'il lit sans pouvoir la situer — après une affectation du paramètre ou de l'état, testant une variable locale ou un champ calculé à partir du paramètre, ou sous quelque chose qui décide si elle s'exécute |
| `constraint unavailable` | une garde a été comprise, et ce générateur n'a aucun membre pour l'exprimer |
| `self-referencing` | le paramètre fait référence au type en cours de génération (par exemple, un nœud enfant ou un supérieur hiérarchique du même type) : il reste donc à compléter (`TODO`), car sa génération automatique provoquerait une récursion infinie. C'est à vous d'en fixer la profondeur. |
| `no source` | le type vient d'un package, il n'y avait donc aucun corps de constructeur à lire |
| `unavailable` | le générateur existe dans JustDummies, mais pas dans l'asset que votre projet résout |

**Un `TODO` n'est pas un échec.** L'outil émet un identifiant qui n'existe pas, si bien que *votre
propre build* signale ce qui n'a pas pu être inféré, à la ligne exacte, avec le type sous la main
([ADR-0060](../../for-maintainers/adr/0060-seed-generators-from-constructor-guards.fr.md)). Un
générateur qui tirerait discrètement une valeur plausible à cet endroit serait bien pire.

**`to verify` fonctionne pareil, et pour la même raison.** Là où votre constructeur délègue sa
validation à un helper, garde dans une forme que l'outil n'analyse pas, ou garde à un endroit dont il
ne peut pas répondre — après une affectation du paramètre ou de l'état, ou sous quelque chose qui décide si la garde
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

**Un `throw` décidé par quelque chose que l'outil ne sait pas suivre marque chaque paramètre.** L'outil
remonte une valeur à travers un nombre quelconque de copies — variables locales, champs, un argument
`out`, une déconstruction — jusqu'aux paramètres dont elle vient, et marque ceux-là. Là où continuer
demanderait de lire du code qu'il ne lit pas — une propriété dont le getter calcule sa valeur, une
méthode de l'objet en construction, l'objet lui-même passé à un helper —, il ne peut pas dire quels
paramètres le rejet implique ; il les marque donc tous plutôt qu'aucun
([ADR-0110](../../for-maintainers/adr/0110-mark-every-parameter-a-throw-may-depend-on.fr.md)).
Confirmez chaque recette comme ci-dessus.

**Ce que l'outil ne voit toujours pas reste silencieux.** Quelques formes rejettent une valeur sans que
l'outil y voie le moindre rejet, et ne reçoivent donc aucune marque : un validateur qui *retourne* la
valeur vérifiée (`_name = Ensure.Valid(name);`), une copie locale confiée à un helper (`var v = value;
Validate(v);`), un lambda qui capture le paramètre, et un helper que l'outil ne reconnaît pas appelé
après la première affectation de l'état. Si votre constructeur valide ainsi, relisez vous-même la
recette de ce paramètre. La liste complète est le §9 de la
[spécification d'`any`](../../for-maintainers/specifications/justdummies-tool.fr.md).

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

Le générateur créé par `any` propose deux surcharges `With…` pour chaque paramètre. Elles répondent à
deux questions différentes : commencez par déterminer celle que pose votre test.

Les exemples ci-dessous utilisent un `Shipment` qui n'accepte qu'une `Route` nationale. Cette règle
relève de l'expédition : une route, en elle-même, peut très bien traverser une frontière. Ils
s'appuient sur les générateurs `AnyShipment` et `AnyRoute`, créés avec `any` et accessibles via
`--entry-point any`.

### Toute la valeur, ou seulement une partie ?

Si votre test ne s'intéresse qu'à **une partie** d'une valeur, passez une lambda à la méthode. Elle
reçoit la base valide du paramètre, et vous n'indiquez que ce que vous y ajoutez :

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

La route reste arbitraire, et nationale : le test n'a pas besoin de savoir qu'une expédition l'exige,
et il n'en dit rien. Le nom `anyLongShipment` indique à la fois qu'il s'agit d'une valeur générée
(`any`) et que le test fixe sa distance (`Long`)
([Arbitraire ne veut pas dire dummy](../guides/core-concepts.fr.md#arbitraire-ne-veut-pas-dire-dummy)).
Le paramètre de la lambda, lui, s'appelle `r` et non `anyRoute` : ce n'est pas une valeur tirée mais un
générateur déjà préparé par la base, et le préfixe `any` est réservé à ce qui a été tiré.

Si votre test s'intéresse à la valeur **entière** — à une route précise —, fixez-la :

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

Deux lambdas auraient effectué deux tirages indépendants, sans garantir des routes identiques. Une
valeur fixée ignore elle aussi la base, mais c'est vous qui la choisissez, et elle ne varie pas : si
elle enfreint une règle, le test échoue dès sa première exécution, puis à chacune des suivantes.

### Où se trouve la base valide

Dans le fichier généré, chaque paramètre dispose d'une fabrique nommée `AnyValid…()`. Le constructeur
du générateur y prend sa valeur par défaut, et chaque lambda part d'elle : c'est le seul endroit où la
base de ce paramètre est définie.

C'est aussi là que vous écrivez une règle que l'outil n'a pas pu appliquer. Ici, `Shipment` vérifie
sur sa route une condition que `any` n'a pas su lire ; l'outil a donc marqué le paramètre `to verify`,
et la base est devenue :

<!-- jd:declarations -->
```csharp
public sealed partial class AnyShipment {

    private static AnyRoute AnyValidRoute() {
        // A shipment only accepts a domestic route; a route on its own may cross a border.
        return new AnyRoute().WithDomestic(true);
    }

}
```

Écrite à cet endroit, la règle s'applique à tous les tests qui tirent une expédition, avec ou sans
lambda. Si elle change, il suffit de la modifier là : aucun test ne la répète, aucun n'est donc à
retrouver.

### Ce qu'un affinage permet, et ce qu'il ne permet pas

* **Il restreint une valeur simple.** Sur un nombre, une chaîne, une date ou une collection, la lambda
  ajoute des contraintes à celles de la base, et la bibliothèque les vérifie ensemble :
  `WithParcels(p => p.LessThanOrEqualTo(10))` tire un nombre entre 1 et 10, puisque la base impose déjà
  `Positive()`. Un affinage qui contredit la base, comme `p => p.Negative()`, est refusé dès cette
  ligne, à chaque exécution, par une exception qui cite `Positive()` et `Negative()`.
* **Il fixe une partie d'une valeur composée.** Sur une route, la lambda appelle les méthodes `With…`
  d'`AnyRoute`, et ce que vous fixez remplace ce que la base prévoyait pour cette partie.
  `r => r.WithDomestic(false)` est accepté par la route mais refusé par l'expédition, à chaque
  exécution puisque la valeur est fixée. Une lambda garantit son point de départ, pas la valeur à
  laquelle vous aboutissez.
* **Le dernier appel l'emporte**, comme pour toute méthode `With…`. Un second `WithParcels(…)` repart
  de la base au lieu de s'ajouter au premier.
* **Il n'élargit jamais la base.** Aucune lambda ne peut produire une étiquette vide quand la base
  impose `NonEmpty()`.

### Quand la lambda ne suffit pas

Aucune méthode `With…` n'accepte un générateur qui remplacerait la base : c'est ainsi qu'un test
perdait une règle sans que la compilation le signale. `WithParcels(Any.Int32().LessThanOrEqualTo(10))`
ne compile plus, et c'est voulu : cet appel tirait parfois zéro ou un nombre négatif, que l'expédition
refusait lors de certaines exécutions seulement.

Cette surcharge servait cinq besoins. Chacun dispose d'une autre solution.

**Des valeurs déterministes hors d'un test.** Au lieu de passer, paramètre par paramètre, les
générateurs d'un contexte `Any.WithSeed(…)`, ouvrez la portée ambiante. Elle s'applique à tous les
tirages, y compris celui de la route :

```csharp
using (Any.UseSeed(20261027)) {
    List<Shipment> anyShipments = Any.ListOf(Any.Shipment()).WithCount(50).Generate();
}
```

**Des éléments de collection tirés autrement.** Une lambda peut modifier la collection elle-même —
`l => l.WithCount(3)` — mais pas la façon d'en tirer les éléments. Si tous les tests ont besoin
d'étiquettes alphabétiques, cette règle relève de la base : modifiez `AnyValidLabels()`. Si un seul
test en a besoin, ajoutez vous-même la surcharge (voir plus bas) et passez
`Any.ListOf(Any.String().Alpha())`.

**Une valeur parmi quelques-unes.** `Any.OneOf(lyonParis, parisLille)` signifie généralement que le
résultat dépend de la valeur tirée : c'est alors une propriété, testée sur un seul tirage. Fixez la
valeur sur laquelle porte le test. Si le choix n'a vraiment aucune importance, ajoutez la surcharge.

**Parfois `null`.** Une lambda ne peut pas l'exprimer : elle reçoit un `AnyDecimal` et doit en
renvoyer un, alors que `OrNull()` renvoie un générateur de `decimal?`. Pour obtenir *toujours* `null`,
un appel suffit ; le cast permet de distinguer la valeur d'une lambda :

```csharp
Shipment anyUndeclaredShipment = Any.Shipment().WithDeclaredValue((decimal?)null).Generate();
```

Pour obtenir `null` *de temps en temps*, ajoutez la surcharge.

**Plus que ce que la base autorise.** Si le domaine accepte une étiquette vide, c'est la base qui est
erronée : corrigez `AnyValidLabels()`. Si un seul test a besoin précisément d'une étiquette vide,
fixez-la.

### Ajouter la surcharge vous-même

Le fichier généré vous appartient et la classe est `partial`. Ajoutez à côté un fichier contenant la
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

Le test exprime alors directement son intention :

```csharp
Shipment anyShipment = Any.Shipment()
    .WithDeclaredValue(Any.Decimal().Positive().OrNull())
    .Generate();
```

Trois points à connaître sur ce fichier :

1. Il appelle le constructeur privé du fichier généré, en passant chaque champ à sa place — comme le
   fait chaque méthode `With…` générée.
2. `any generate Shipment --force` ne réécrit que `AnyShipment.cs` : votre fichier est conservé quand
   vous régénérez.
3. Si le constructeur de `Shipment` change, la régénération modifie ce constructeur privé, et votre
   méthode ne compile plus, avec une erreur sur sa propre ligne. Elle ne devient jamais fausse à votre
   insu.

Enfin, gardez à l'esprit que cette surcharge **remplace** la base. Toute règle que `AnyValid…()`
impose à ce paramètre doit aussi être respectée par le générateur que vous passez — c'est pourquoi
l'outil ne l'écrit pas à votre place. Décision :
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
