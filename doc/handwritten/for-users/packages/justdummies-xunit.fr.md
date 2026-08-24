# `JustDummies.Xunit`

🌍 **Langues :**  
🇬🇧 [English](./justdummies-xunit.en.md) | 🇫🇷 Français (ce fichier)

L'adaptateur xUnit **v3**. Il n'apporte qu'une seule chose — un attribut `[Reproducible]` — et cette
seule chose supprime le besoin d'envelopper le moindre corps de test dans `Any.Reproducibly`.

## Installation

```bash
dotnet add package JustDummies.Xunit
```

Il dépend de `JustDummies` et de xUnit v3.

## Toute la surface

`ReproducibleAttribute`, avec une propriété `Seed` assignable. C'est tout — l'adaptateur est
volontairement mince, car tout ce dont il a besoin existe déjà dans la bibliothèque
([ADR-0018](../../for-maintainers/adr/0018-adapt-dummies-to-xunit-v3-through-a-companion-package.fr.md)).

## Utilisation

<!-- jd:declarations -->
```csharp
public sealed class OrderTests {

    [Fact, Reproducible]
    public void A_20_percent_discount_takes_a_fifth_off_the_order() {
        // Arrange
        string anyReference = Any.String().StartingWith("ORD-").WithLength(12).Generate();
        string anyCustomer  = Any.String().Alpha().WithLengthBetween(1, 50).Generate();

        Order order = new Order(anyReference, anyCustomer, amount: 100m);

        // Act
        order.ApplyDiscount(20);

        // Assert
        Assert.Equal(80m, order.Total);
    }

}
```

L'attribut s'applique à trois niveaux, et le plus spécifique l'emporte pendant la durée d'un test :

<!-- jd:declarations -->
```csharp
// Sur une classe : chaque test qu'elle déclare est reproductible.
[Reproducible]
public sealed class OrderTests {

    [Fact]
    public void A_20_percent_discount_takes_a_fifth_off_the_order() {
        // Arrange
        string anyReference = Any.String().StartingWith("ORD-").WithLength(12).Generate();
        string anyCustomer  = Any.String().Alpha().WithLengthBetween(1, 50).Generate();

        Order order = new Order(anyReference, anyCustomer, amount: 100m);

        // Act
        order.ApplyDiscount(20);

        // Assert
        Assert.Equal(80m, order.Total);
    }

    // Sur une méthode, pour rejouer une graine rapportée — le niveau extérieur est restauré ensuite.
    [Fact, Reproducible(Seed = 1743029518)]
    public void A_100_percent_discount_clears_the_order() {
        // Arrange
        string anyReference = Any.String().StartingWith("ORD-").WithLength(12).Generate();
        string anyCustomer  = Any.String().Alpha().WithLengthBetween(1, 50).Generate();

        Order order = new Order(anyReference, anyCustomer, amount: 100m);

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
[assembly: Reproducible]
```

## Ce qu'il fait, précisément

Avant chaque **cas** de test, l'attribut ouvre la même portée de graine ambiante qu'utilise
`Any.Reproducibly`, en épinglant une graine fraîche — ou celle que vous avez fixée sur `Seed`. Après
le test, il ferme la portée et, **uniquement si le test a échoué**, écrit la graine dans la sortie du
test :

```text
[JustDummies] These arbitrary values were seeded with 1743029518. Reproduce this run with [Reproducible(Seed = 1743029518)].
```

Remarquez que le message nomme l'**attribut**, et non `Any.Reproducibly(seed, ...)`. Un test épinglé
depuis l'extérieur de son propre corps ne contient aucun appel de ce genre : le nommer enverrait le
lecteur chercher du code qui n'existe pas. L'adaptateur fournit son propre fragment de rejeu via la
seconde surcharge d'`Any.UseSeed` — la raison d'être de cette surcharge
([ADR-0017](../../for-maintainers/adr/0017-open-the-ambient-seed-scope-to-adapters.fr.md)).

Trois conséquences méritent d'être connues :

* **Une graine par cas de test**, si bien que chaque cas d'une `[Theory]` a la sienne au lieu d'en
  partager une.
* **Un test vert reste silencieux.** La graine est une aide au diagnostic, pas une sortie.
* **Les contextes `Any.WithSeed(...)` ne sont pas affectés.** Ce contexte est isolé par conception et
  ne tire pas de la source ambiante que cet attribut épingle.

## Rejouer un échec

Copiez la graine depuis la sortie du test en échec sur l'attribut, relancez, et les valeurs exactes
reviennent. Corrigez le défaut, puis **retirez l'épingle** — une graine versionnée retransforme un
test variable en test à un seul cas.

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
