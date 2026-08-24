# Énumérations et choix

🌍 **Langues :**  
🇬🇧 [English](./enums-and-choices.en.md) | 🇫🇷 Français (ce fichier)

Quatre générateurs couvrent le cas où la valeur provient d'un **ensemble connu** plutôt que d'un
intervalle : les énumérations, les viviers explicites, les éléments d'une collection existante, et
les booléens.

## Énumérations

`Any.Enum<TEnum>()` tire l'un des membres déclarés sur le type :

```csharp
OrderStatus status    = Any.Enum<OrderStatus>().Generate();
OrderStatus notDraft  = Any.Enum<OrderStatus>().DifferentFrom(OrderStatus.Draft).Generate();
OrderStatus openState = Any.Enum<OrderStatus>().Except(OrderStatus.Shipped, OrderStatus.Cancelled).Generate();
OrderStatus terminal  = Any.Enum<OrderStatus>().OneOf(OrderStatus.Shipped, OrderStatus.Cancelled).Generate();
```

Le tirage reste à l'intérieur des membres déclarés. Il n'invente jamais de valeur numérique non
déclarée, bien que le CLR l'autoriserait — un dummy qui le ferait testerait votre `switch` contre un
état que votre domaine ne possède pas.

Les exclusions qui vident l'univers sont refusées nommément, et l'analyzer
[JD017](../analyzers/JD017.fr.md) signale les cas constants dès la compilation.

## Énumérations de drapeaux

Pour une énumération `[Flags]`, un tirage ordinaire produit toujours **un membre déclaré**. Élargir
ce tirage se demande explicitement :

```csharp
// Un membre déclaré : None, Read, Write ou Delete.
Permissions single = Any.Enum<Permissions>().Generate();

// N'importe quelle combinaison : Read | Delete, Read | Write | Delete, ...
Permissions combined = Any.Enum<Permissions>().AllowingCombinations().Generate();

// L'une des deux combinaisons que vous nommez : une liste blanche est le vivier, rien à activer.
Permissions writable = Any.Enum<Permissions>()
                          .OneOf(Permissions.Read | Permissions.Write, Permissions.Write | Permissions.Delete)
                          .Generate();
```

Ce caractère explicite est délibéré
([ADR-0020](../../for-maintainers/adr/0020-draw-flag-enum-combinations-behind-an-opt-in.fr.md)). Un
attribut `[Flags]` dit que les membres *peuvent* se combiner, non que toute valeur de votre domaine
le fait ; et un générateur qui combinerait automatiquement changerait silencieusement ce que tirent
les tests existants le jour où quelqu'un ajoute l'attribut. Demander les combinaisons tient en un
appel, et cet appel dit sur place que les combinaisons font partie de ce que ce test couvre.

Ce que cette demande explicite tranche, c'est ce que parcourt un tirage *ordinaire*, et rien d'autre :
nommer une combinaison dans `OneOf` ne demande rien de tel, puisqu'une liste blanche est le vivier
lui-même et qu'y écrire `Read | Write` revient à demander cette valeur exacte. Une valeur qu'aucun OU
de membres déclarés ne produit — `(Permissions)16` sur l'énumération ci-dessus — est refusée dans les
deux cas, et signalée par [JD017](../analyzers/JD017.fr.md).

## Viviers explicites

`Any.OneOf` tire uniformément parmi les valeurs que vous listez :

```csharp
string  currency = Any.OneOf("EUR", "USD", "GBP").Generate();
int     httpPort = Any.OneOf(80, 443, 8080).Generate();
decimal vatRate  = Any.OneOf(0.055m, 0.10m, 0.20m).Generate();

// Un vivier se restreint comme le reste.
string notEuro = Any.OneOf("EUR", "USD", "GBP").DifferentFrom("EUR").Generate();
```

Deux erreurs sont assez fréquentes pour avoir leur propre diagnostic.

**Lister deux fois la même constante** fusionne le doublon : le vivier est plus petit qu'il n'y
paraît, et la valeur répétée ne pèse rien de plus — [JD025](../analyzers/JD025.fr.md).

**Passer des générateurs au lieu de valeurs** déduit un vivier de *recettes* : le tirage renvoie
alors un générateur et non une valeur — [JD012](../analyzers/JD012.fr.md). Utilisez `Any.Combine`
si vous vouliez composer.

## Éléments d'une collection existante

`Any.OneOf` prend un `params T[]` : lui passer un **tableau** s'étend donc normalement et fait ce que
vous attendez. Lui passer toute autre collection, non : `T` se lie au type de la collection
elle-même, et le vivier se réduit à un seul élément — cette collection :

<!-- jd:allow=JD013 -->
```csharp
List<string> currencies = ["EUR", "USD", "GBP"];

// JD013 : un vivier d'un seul élément, qui est la liste.
IAny<List<string>> wrong = Any.OneOf(currencies);
```

`Any.ElementOf` est celui qui tire *dans* la collection, quel qu'en soit le type :

```csharp
List<string> currencies = ["EUR", "USD", "GBP"];

string currency = Any.ElementOf(currencies).Generate();
```

Deux surcharges existent, pour `IReadOnlyList<T>` et `IEnumerable<T>`. Le compilateur choisit la plus
spécifique dès que le type le permet, car une liste s'indexe tandis qu'une séquence générale doit
être parcourue ; les deux sont supportées pour qu'une méthode utilitaire à `yield` ou une requête
LINQ fonctionne sans `.ToList()` sur le site d'appel.

```csharp
List<OrderStatus>       open      = [OrderStatus.Draft, OrderStatus.Submitted];
IEnumerable<OrderStatus> lazyOpen = open.Where(status => status != OrderStatus.Draft);

OrderStatus fromList     = Any.ElementOf(open).Generate();
OrderStatus fromSequence = Any.ElementOf(lazyOpen).Generate();
```

Un vivier vide n'admet aucune valeur et est refusé, plutôt que de renvoyer une valeur par défaut.

## Booléens

```csharp
bool flag       = Any.Boolean().Generate();
bool always     = Any.Boolean().True().Generate();
bool never      = Any.Boolean().False().Generate();
bool notTheSame = Any.Boolean().DifferentFrom(true).Generate();
```

`True()` et `False()` existent pour qu'un site d'appel qui fige le drapeau se lise comme ceux qui ne
le figent pas, ce qui compte dans un test où trois dummies sur quatre varient et un seul non.

`Any.Boolean().Except(true, false)` viderait le domaine, et est refusé avec un message disant
exactement cela.

---

[← Référence des générateurs](./README.fr.md) · [Sommaire de la documentation](../README.fr.md)
