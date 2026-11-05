# `JustDummies.Xunit`

🌍 **Langues :**  
🇬🇧 [English](./justdummies-xunit.en.md) | 🇫🇷 Français (ce fichier)

L'adaptateur xUnit **v3**. Il n'apporte qu'une seule chose — un attribut `[Reproducible]` — et cette
seule chose supprime le besoin d'envelopper le moindre corps de test dans `Any.Reproducibly`.

## Installation

```bash
dotnet add package JustDummies.Xunit
```

Il dépend de `JustDummies` et de xUnit v3 — `xunit.v3` **4.0.2** ou ultérieure. Ce plancher est la
propre dépendance de l'adaptateur, `xunit.v3.extensibility.core`, et les paquets xUnit ne fonctionnent
qu'à une seule et même version : avec un `xunit.v3` plus ancien, la restauration tire le paquet
d'extensibilité au-dessus de celui que `xunit.v3.core` épingle, le signale (`NU1608`), puis le processus
de test plante au lieu d'exécuter le moindre test. L'avertissement est le seul indice : si vous le
voyez, montez `xunit.v3` au même niveau.

### Exécuter les tests

Le même plancher décide du comportement de `dotnet test`. À partir de 4.0.0, `xunit.v3` active par
défaut son intégration Microsoft.Testing.Platform v2, et sur le SDK .NET 10 cette intégration arrête un
`dotnet test` exécuté dans son mode VSTest par défaut, en affichant un message d'erreur demandant
d'activer explicitement Microsoft.Testing.Platform. Un projet en `xunit.v3` 3.x, où un simple
`dotnet test` fonctionnait, rencontre cette erreur dès qu'il monte `xunit.v3` pour adopter
l'adaptateur — et `dotnet test --project …` échoue plus tôt encore, avec `MSB1001: Unknown switch`,
parce que ce commutateur appartient au mode Microsoft.Testing.Platform. Activez ce mode depuis le
`global.json` placé à côté de votre solution :

```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

Si vous préférez ne pas toucher à `global.json`, deux voies restent ouvertes :

- exécuter le projet de test lui-même, qui est un exécutable : `dotnet run --project Tests` ;
- rester sur VSTest avec `-p:IsTestingPlatformApplication=false`, ou la même propriété dans le fichier
  projet. Le commutateur ne fait que désactiver l'application Microsoft.Testing.Platform : découvrir et
  exécuter les tests par VSTest exige toujours les références de paquets `xunit.runner.visualstudio` et
  `Microsoft.NET.Test.Sdk`, qu'un projet configuré pour Microsoft.Testing.Platform seul peut ne pas avoir.

La documentation officielle de xUnit, [Microsoft Testing Platform](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform),
décrit l'intégration et ce que chaque mode exige ; celle du SDK,
[Tester avec `dotnet test`](https://learn.microsoft.com/dotnet/core/testing/unit-testing-with-dotnet-test),
décrit les deux modes de `dotnet test` et les commutateurs que chacun accepte.

## Toute la surface

`ReproducibleAttribute`, dans l'espace de noms `JustDummies.Xunit`, avec une propriété `Seed` assignable. C'est tout — l'adaptateur est
volontairement mince, car tout ce dont il a besoin existe déjà dans la bibliothèque
([ADR-0018](../../for-maintainers/adr/0018-adapt-dummies-to-xunit-v3-through-a-companion-package.fr.md)).

## Utilisation

<!-- jd:declarations -->
```csharp
using JustDummies;
using JustDummies.Xunit;

using Xunit;

public sealed class OrderTests {

    [Fact, Reproducible]
    public void A_20_percent_discount_takes_a_fifth_off_the_order() {
        // Arrange
        OrderReference anyReference    = Any.String().StartingWith("ORD-").WithLength(12).As(OrderReference.Create).Generate();
        string         anyCustomerName = Any.String().Alpha().WithLengthBetween(1, 50).Generate();

        Order order = new Order(anyReference, anyCustomerName, amount: 100m);

        // Act
        order.ApplyDiscount(20);

        // Assert
        Assert.Equal(80m, order.Total);
    }

}
```

L'attribut s'applique à trois niveaux, et le plus spécifique l'emporte pendant la durée d'un test. Sa graine est
celle qu'utilise chaque tirage du corps du test : c'est donc aussi la seule rapportée quand le test échoue —
un niveau extérieur n'écrit rien, puisque rejouer sa graine ne reproduirait rien :

<!-- jd:declarations -->
```csharp
using JustDummies;
using JustDummies.Xunit;

using Xunit;

// Sur une classe : chaque test qu'elle déclare est reproductible.
[Reproducible]
public sealed class OrderTests {

    [Fact]
    public void A_20_percent_discount_takes_a_fifth_off_the_order() {
        // Arrange
        OrderReference anyReference    = Any.String().StartingWith("ORD-").WithLength(12).As(OrderReference.Create).Generate();
        string         anyCustomerName = Any.String().Alpha().WithLengthBetween(1, 50).Generate();

        Order order = new Order(anyReference, anyCustomerName, amount: 100m);

        // Act
        order.ApplyDiscount(20);

        // Assert
        Assert.Equal(80m, order.Total);
    }

    // Sur une méthode, pour rejouer une graine rapportée — le niveau extérieur est restauré ensuite.
    [Fact, Reproducible(Seed = 1743029518)]
    public void A_100_percent_discount_clears_the_order() {
        // Arrange
        OrderReference anyReference    = Any.String().StartingWith("ORD-").WithLength(12).As(OrderReference.Create).Generate();
        string         anyCustomerName = Any.String().Alpha().WithLengthBetween(1, 50).Generate();

        Order order = new Order(anyReference, anyCustomerName, amount: 100m);

        // Act
        order.ApplyDiscount(100);

        // Assert
        Assert.Equal(0m, order.Total);
    }

}
```

Il s'applique aussi à un assembly entier, forme à privilégier quand la reproductibilité doit être le
défaut d'une suite. Placez ceci en tête de n'importe quel fichier du projet de test, avant toute
déclaration de namespace ou de type :

<!-- jd:skip -->
```csharp
using JustDummies.Xunit;

[assembly: Reproducible]
```

## Ce qu'il fait, précisément

Avant chaque **cas** de test, l'attribut ouvre la même portée de graine ambiante qu'utilise
`Any.Reproducibly`, en épinglant une graine fraîche — ou celle que vous avez fixée sur `Seed`. Après
le test, il ferme la portée et, **uniquement si le test a échoué**, écrit la graine dans la sortie du
test :

```text
[JustDummies] These arbitrary values were seeded with 1743029518. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 1743029518)].
```

Remarquez que le message nomme l'**attribut**, et non `Any.Reproducibly(seed, ...)`. Un test épinglé
depuis l'extérieur de son propre corps ne contient aucun appel de ce genre : le nommer enverrait le
lecteur chercher du code qui n'existe pas. L'adaptateur fournit son propre fragment de rejeu via la
seconde surcharge d'`Any.UseSeed` — la raison d'être de cette surcharge
([ADR-0017](../../for-maintainers/adr/0017-open-the-ambient-seed-scope-to-adapters.fr.md)).

Quatre conséquences méritent d'être connues :

* **Une graine par cas de test**, si bien que chaque cas d'une `[Theory]` a la sienne au lieu d'en
  partager une.
* **Un test vert reste silencieux.** La graine est une aide au diagnostic, pas une sortie.
* **Un test qui expire reçoit lui aussi sa graine.** `[Fact(Timeout = ...)]` fait échouer le test et
  ferme sa sortie avant que le hook *after* ne s'exécute : la graine est donc écrite au moment où
  xUnit annule le test — une valeur tirée qui fait boucler le code est précisément le cas où vous en
  avez besoin. Un `Dispose` ou un `DisposeAsync` qui lève après un corps de test réussi n'est pas
  rapporté : le corps qui a tiré les valeurs a bien réussi.
* **Les contextes `Any.WithSeed(...)` ne sont pas affectés.** Ce contexte est isolé par conception et
  ne tire pas de la source ambiante que cet attribut épingle.

## Rejouer un échec

Copiez la graine depuis la sortie du test en échec sur l'attribut, relancez, et les valeurs exactes
reviennent. Corrigez le défaut, puis **retirez l'épingle** — une graine versionnée retransforme un
test variable en test à un seul cas.

La graine rejoue au niveau où vous la placez. Sur la méthode en échec, c'est le plus étroit :
`[Fact, Reproducible(Seed = 1743029518)]`. Sur sa classe, chaque test de la classe prend cette graine. Au
niveau de l'assembly, modifiez l'attribut déjà présent, `[assembly: Reproducible(Seed = 1743029518)]` :
un second `[assembly: Reproducible]` est une erreur de compilation, CS0579, puisque l'attribut n'en
admet qu'un par cible.

Une graine qui révèle un défaut pas encore corrigé peut rester exécutable à la demande sans tourner à
chaque build : marquez le test explicite, et xUnit le saute sauf si on le demande.

<!-- jd:declarations -->
```csharp
using JustDummies;
using JustDummies.Xunit;

using Xunit;

public sealed class DiscountDefectTests {

    [Fact(Explicit = true), Reproducible(Seed = 1743029518)]
    public void A_20_percent_discount_takes_a_fifth_off_the_order() {
        // Arrange
        OrderReference anyReference    = Any.String().StartingWith("ORD-").WithLength(12).As(OrderReference.Create).Generate();
        string         anyCustomerName = Any.String().Alpha().WithLengthBetween(1, 50).Generate();

        Order order = new Order(anyReference, anyCustomerName, amount: 100m);

        // Act
        order.ApplyDiscount(20);

        // Assert
        Assert.Equal(80m, order.Total);
    }

}
```

Lancez-le avec `dotnet test --explicit only` en mode Microsoft.Testing.Platform, ou
`dotnet run --project Tests -- --explicit only`. Cela reste une graine épinglée : [JD019](../analyzers/JD019.fr.md),
s'il est activé, la signale, et l'épingle part une fois le défaut corrigé.

## Les arguments d'attribut et C#

Deux limites rencontrées autour de `[InlineData]` relèvent de C#, ni de xUnit ni de cette bibliothèque.
Un `decimal` n'est pas une constante que C# peut placer dans un attribut (CS0182) : passez-le sous forme de
chaîne, `[InlineData("12.50")]`, que xUnit v3 convertit pour un paramètre `decimal`. Et un substitut isolé
dans un argument chaîne d'attribut parvient au test sous la forme U+FFFD, le caractère de remplacement :
les arguments d'attribut sont stockés en UTF-8, qui ne peut pas contenir la moitié d'une paire.

## Ce que les analyzers vous diront

| Règle | Situation |
| --- | --- |
| [JD010](../analyzers/JD010.fr.md) | `[Reproducible]` sur une méthode que xUnit ne traite pas comme un test — elle n'épingle rien et ressemble exactement à la forme qui marche |
| [JD007](../analyzers/JD007.fr.md) | une valeur tirée dans le **constructeur** d'une classe `[Reproducible]`, que xUnit exécute avant l'ouverture de la portée : la graine rapportée ne la rejoue donc pas |
| [JD008](../analyzers/JD008.fr.md) | le fournisseur de données d'une théorie qui tire à la découverte, avant qu'aucune graine ne soit épinglée |

## Si vous utilisez xUnit v2

Cet adaptateur ne cible que la **v3**. En v2, utilisez `Any.Reproducibly(() => { ... })` dans le
corps du test : vous obtenez la même portée épinglée et le même rapport de graine, au prix d'une
lambda d'enveloppe. Voir [Reproductibilité](../guides/reproducibility.fr.md).

---

[← Paquets](./README.fr.md) · [Sommaire de la documentation](../README.fr.md)
