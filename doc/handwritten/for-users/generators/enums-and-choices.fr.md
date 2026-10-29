# Énumérations et choix

🌍 **Langues :**  
🇬🇧 [English](./enums-and-choices.en.md) | 🇫🇷 Français (ce fichier)

Quatre générateurs couvrent le cas où la valeur provient d'un **ensemble connu** plutôt que d'un
intervalle : les énumérations, les viviers explicites, les éléments d'une collection existante, et
les booléens.

## Énumérations

`Any.Enum<TEnum>()` tire l'un des membres déclarés sur le type :

```csharp
OrderStatus anyStatus    = Any.Enum<OrderStatus>().Generate();
OrderStatus anyNotDraft  = Any.Enum<OrderStatus>().DifferentFrom(OrderStatus.Draft).Generate();
OrderStatus anyOpenState = Any.Enum<OrderStatus>().Except(OrderStatus.Shipped, OrderStatus.Cancelled).Generate();
OrderStatus anyTerminal  = Any.Enum<OrderStatus>().OneOf(OrderStatus.Shipped, OrderStatus.Cancelled).Generate();
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
Permissions anySingle = Any.Enum<Permissions>().Generate();

// N'importe quelle combinaison : Read | Delete, Read | Write | Delete, ...
Permissions anyCombined = Any.Enum<Permissions>().AllowingCombinations().Generate();

// L'une des deux combinaisons que vous nommez : une liste blanche est le vivier, rien à activer.
Permissions anyWritable = Any.Enum<Permissions>()
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
string  anyCurrency = Any.OneOf("EUR", "USD", "GBP").Generate();
int     anyHttpPort = Any.OneOf(80, 443, 8080).Generate();
decimal anyVatRate  = Any.OneOf(0.055m, 0.10m, 0.20m).Generate();

// Un vivier se restreint comme le reste.
string anyNotEuro = Any.OneOf("EUR", "USD", "GBP").DifferentFrom("EUR").Generate();
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

string anyCurrency = Any.ElementOf(currencies).Generate();
```

Deux surcharges existent, pour `IReadOnlyList<T>` et `IEnumerable<T>`. Le compilateur choisit la plus
spécifique dès que le type le permet, car une liste s'indexe tandis qu'une séquence générale doit
être parcourue ; les deux sont prises en charge pour qu'une méthode utilitaire à `yield` ou une requête
LINQ fonctionne sans `.ToList()` sur le site d'appel.

```csharp
List<OrderStatus>       open      = [OrderStatus.Draft, OrderStatus.Submitted];
IEnumerable<OrderStatus> lazyOpen = open.Where(status => status != OrderStatus.Draft);

OrderStatus anyFromList     = Any.ElementOf(open).Generate();
OrderStatus anyFromSequence = Any.ElementOf(lazyOpen).Generate();
```

Un vivier vide n'admet aucune valeur et est refusé, plutôt que de renvoyer une valeur par défaut.

### Le piège ne concerne que `Any.OneOf`

L'avertissement ci-dessus concerne le `Any.OneOf<T>` **de premier niveau**, où `T` est inféré et peut
se lier silencieusement à la collection elle-même. Le `OneOf` d'un **constructeur typé** n'a aucun `T`
à inférer : il accepte les deux formes, et le piège disparaît :

```csharp
List<int> allowedPorts = [80, 443, 8080];

int anyPort = Any.Int32().OneOf(allowedPorts).Generate();
```

Ce n'est pas le même appel que `Any.ElementOf(allowedPorts)`, et c'est pourquoi les deux existent.
`ElementOf` renvoie un générateur de vivier qui n'expose que `Except` et `DifferentFrom`, alors qu'un
`OneOf` typé renvoie le constructeur : les contraintes propres au type peuvent donc continuer de
restreindre l'ensemble.

```csharp
List<decimal> rates = [0.055m, 0.10m, 0.20m];

decimal anyStandardRate = Any.Decimal().OneOf(rates).GreaterThan(0.09m).Generate();
```

Tout générateur qui propose `OneOf` propose les deux formes : cette uniformité est la règle, et un
test la vérifie. Une conséquence mérite d'être connue : une `string` étant un `IEnumerable<char>`, la
forme séquence de `Any.Char()` en accepte une directement, et elle se lit comme la liste des
caractères autorisés :

```csharp
char anySeparator = Any.Char().OneOf("-_.").Generate();
```

## Booléens

```csharp
bool anyFlag       = Any.Boolean().Generate();
bool anyAlways     = Any.Boolean().True().Generate();
bool anyNever      = Any.Boolean().False().Generate();
bool anyNotTheSame = Any.Boolean().DifferentFrom(true).Generate();
```

`True()` et `False()` existent pour qu'un site d'appel qui fige le drapeau se lise comme ceux qui ne
le figent pas, ce qui compte dans un test où trois dummies sur quatre varient et un seul non.

`Any.Boolean().True().DifferentFrom(true)` viderait le domaine, et est refusé avec un message disant
exactement cela.

---

[← Référence des générateurs](./README.fr.md) · [Sommaire de la documentation](../README.fr.md)
