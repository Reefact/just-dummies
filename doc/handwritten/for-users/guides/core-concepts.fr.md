# Concepts fondamentaux

🌍 **Langues :**  
🇬🇧 [English](./core-concepts.en.md) | 🇫🇷 Français (ce fichier)

Cinq idées portent toute la bibliothèque. Une fois acquises, chaque générateur de la référence se
lit de la même façon, et les surprises cessent.

## Un générateur est une recette, pas une valeur

`Any.Int32()` ne donne pas un nombre. Il donne un `AnyInt32` — un objet décrivant quels nombres
seraient acceptables. Rien n'est tiré tant que `Generate()` n'est pas appelé, et chaque appel tire à
nouveau :

```csharp
AnyInt32 anyQuantity = Any.Int32().Between(1, 100);

int first  = anyQuantity.Generate();
int second = anyQuantity.Generate();

// first et second sont tous deux dans 1..100, et sont généralement différents.
```

C'est la distinction sur laquelle repose toute l'API, et la raison pour laquelle le paquet embarque
des analyzers : une recette et une valeur satisfont beaucoup des mêmes signatures, le compilateur ne
peut donc pas signaler qu'on les a confondues. Écrire `$"{Any.Int32()}"` compile parfaitement et
produit la chaîne `"JustDummies.AnyInt32"`. C'est le diagnostic
[JD005](../analyzers/JD005.fr.md), et il existe précisément parce que rien d'autre ne l'aurait
attrapé.

```mermaid
flowchart TD
    accTitle: Pourquoi un générateur est une recette et non une valeur
    accDescr: Any.Int32() renvoie un générateur d'un int quelconque. Between(1, 100) en renvoie un nouveau, et MultipleOf(5) encore un autre. Appeler Generate() deux fois sur ce dernier générateur donne deux valeurs différentes, 45 et 70.
    F["Any.Int32()"] -->|"renvoie"| G1["générateur<br/><i>un int quelconque</i>"]
    G1 -->|".Between(1, 100)"| G2["générateur<br/><i>un int dans 1..100</i>"]
    G2 -->|".MultipleOf(5)"| G3["générateur<br/><i>un multiple de 5 dans 1..100</i>"]
    G3 -->|".Generate()"| V["45"]
    G3 -->|".Generate()"| V2["70"]
    style G1 fill:#e8eaf6,stroke:#3f51b5,color:#1a237e
    style G2 fill:#e8eaf6,stroke:#3f51b5,color:#1a237e
    style G3 fill:#e8eaf6,stroke:#3f51b5,color:#1a237e
    style V fill:#e8f5e9,stroke:#43a047,color:#1b5e20
    style V2 fill:#e8f5e9,stroke:#43a047,color:#1b5e20
```

## Les générateurs sont immuables

Une contrainte ne modifie jamais le générateur sur lequel elle est appelée. Elle en renvoie un
**nouveau**, porteur d'une exigence de plus, et laisse l'original exactement tel qu'il était :

```csharp
AnyString anyCode     = Any.String().Alpha().WithLength(8);
AnyString anyUpperCode = anyCode.InUpperCase();

string mixed = anyCode.Generate();      // 8 lettres, casse quelconque
string upper = anyUpperCode.Generate(); // 8 lettres, majuscules
```

Deux conséquences en découlent, toutes deux utiles.

On peut **partager un générateur librement** — le placer dans un champ `static readonly`, le passer
à une méthode utilitaire, en dériver dix variantes — sans risquer que la contrainte d'un appelant ne
déborde sur celle d'un autre.

Et une contrainte dont on jette le résultat ne fait rien du tout. C'est une vraie erreur, facile à
commettre quand une chaîne est répartie sur plusieurs lignes ; elle a donc son propre diagnostic,
[JD006](../analyzers/JD006.fr.md) :

<!-- jd:allow=JD006 -->
```csharp
AnyString anyReference = Any.String().WithLength(12);

anyReference.StartingWith("ORD-"); // JD006 : le résultat est jeté, le préfixe est donc perdu

string reference = anyReference.Generate(); // 12 caractères, sans préfixe
```

## `IAny<T>` est la couture sur laquelle tout se compose

Tout générateur implémente `IAny<T>`, dont l'unique membre est `Generate()`. Cette seule interface
permet de faire circuler, de stocker et de combiner des générateurs sans que le code receveur ait à
savoir quel type concret les a produits :

```csharp
static List<T> ThreeOf<T>(IAny<T> generator) {
    return [generator.Generate(), generator.Generate(), generator.Generate()];
}

List<int>    quantities = ThreeOf(Any.Int32().Between(1, 100));
List<string> references = ThreeOf(Any.String().StartingWith("ORD-").WithLength(12));
```

C'est aussi la monnaie d'échange de l'API de composition : `Any.ListOf`, `Any.Combine`, `.As(...)`
et `.OrNull()` prennent et renvoient tous des `IAny<T>`. Voir
[Composition](./composition.fr.md) pour ce que cela permet.

## Une contrainte énonce un invariant, jamais une assertion

C'est la règle qui décide si un test utilisant des dummies vaut quelque chose.

Une contrainte existe pour décrire **ce que le domaine garantit sur la valeur**. Elle ne doit jamais
être ajoutée pour faire passer une assertion. Prenons un test sur une règle disant que les frais de
port sont offerts au-delà d'un seuil :

```csharp
// Anti-patron : la contrainte a été choisie pour rendre l'assertion vraie.
decimal orderTotal = Any.Decimal().GreaterThan(100m).Generate();

Assert.Equal(0m, Shipping.FeeFor(orderTotal));
```

Le test ne prouve plus rien sur le seuil — il prouve que le code est d'accord avec la contrainte que
le test a lui-même inventée. Pire : le jour où le seuil passe à 200, ce test passe toujours.

Le réflexe, à ce stade, est de relâcher la contrainte et de calculer l'attendu à partir de la valeur
tirée. N'en faites rien : cela échoue de la même façon, et y ajoute un défaut propre.

```csharp
// Toujours faux, d'une manière qui a l'air soigneuse.
decimal orderTotal = Any.Decimal().Between(0m, 10_000m).WithScale(2).Generate();

decimal expected = orderTotal > 100m ? 0m : 4.90m;   // la règle, recopiée dans le test

Assert.Equal(expected, Shipping.FeeFor(orderTotal));
```

Ce test affirme que `Shipping.FeeFor` est d'accord avec une seconde copie de `Shipping.FeeFor`
écrite dans le corps du test : lui aussi survit donc au passage du seuil à 200. Et `orderTotal`
n'a jamais été un dummy : les frais sont exactement ce qu'il décide, ce qui en fait une donnée
qui intervient dans ce que le test vérifie.

La version honnête écrit le seuil noir sur blanc, des deux côtés :

```csharp
// Le seuil est ce dont ces tests parlent : il est donc épelé plutôt que tiré.
Assert.Equal(0m,    Shipping.FeeFor(150m));   // au-dessus : offerts
Assert.Equal(4.90m, Shipping.FeeFor(50m));    // en dessous : facturés
```

Remarquez ce qui *ne figure pas* dans cet exemple : un dummy. Ce test n'en a aucun, et n'en a pas
besoin — chaque valeur qu'il manipule est une valeur dont il parle. Un dummy apparaîtrait dès lors
qu'il faudrait calculer les frais pour une commande entière, dont la règle ne consulte ni la
référence ni le client. **Prenez un dummy quand une valeur doit être là et ne doit pas compter ;
quand la valeur est le sujet, écrivez-la en littéral.**

Deux tests plutôt qu'un, c'est la forme à attendre ici : si vous ne pouvez pas exprimer le test sans
contraindre la valeur tirée à la forme de l'assertion, c'est que cette valeur n'est pas un dummy, et
ce qu'il vous faut est un littéral de chaque côté de la frontière.

## Les valeurs sont construites, pas filtrées

Quand une chaîne déclare plusieurs contraintes, JustDummies ne tire **pas** au hasard en
recommençant jusqu'à ce que quelque chose convienne. Il construit une valeur qui satisfait toute la
spécification par construction. Une exécution de `Any.Int32().Between(1, 100).MultipleOf(7)` choisit
parmi les multiples de sept de cet intervalle ; elle ne lance pas les dés en espérant tomber dessus.

C'est pourquoi des contraintes contradictoires ne bouclent pas. Elles sont refusées, avec un message
nommant **les deux** côtés du conflit :

<!-- jd:allow=JD023 -->
```csharp
// Lève ConflictingAnyConstraintException — le message nomme les deux bornes.
int impossible = Any.Int32().GreaterThan(100).LessThan(10).Generate();
```

Quelques contraintes ne peuvent pas être honorées par construction : exclure des valeurs d'un
intervalle continu, satisfaire une expression régulière, remplir une collection d'éléments
distincts. Celles-là utilisent un retirage **borné** : un nombre fixe de tentatives, après quoi le
tirage échoue bruyamment et de façon reproductible plutôt que de boucler indéfiniment.
[Erreurs et conflits](./errors-and-conflicts.fr.md) décrit à quoi cela ressemble et comment y
réagir.

```mermaid
flowchart LR
    accTitle: Les valeurs sont construites pour satisfaire les contraintes, jamais filtrées
    accDescr: On demande aux contraintes déclarées si elles admettent une valeur. Sinon, une ConflictingAnyConstraintException nomme les deux côtés. Si oui, une valeur qui les satisfait toutes est construite, et c'est la valeur tirée.
    D["contraintes déclarées"] --> C{"admettent-elles<br/>une valeur ?"}
    C -->|non| X["ConflictingAnyConstraintException<br/><i>nommant les deux côtés</i>"]
    C -->|oui| B["construire une valeur<br/>qui les satisfait toutes"]
    B --> V["la valeur tirée"]
    style X fill:#ffebee,stroke:#e53935,color:#b71c1c
    style V fill:#e8f5e9,stroke:#43a047,color:#1b5e20
```

## Ce que « arbitraire mais valide » ne promet pas

La bibliothèque garantit une chose avec précision : une valeur tirée satisfait **toutes les
contraintes déclarées sur le site d'appel**. Être clair sur ce qu'elle ne promet *pas* est ce qui la
rend prévisible.

* **Aucune garantie de distribution.** Un tirage est arbitraire : ni uniforme, ni adverse, ni réglé
  pour trouver les cas limites. Si une frontière précise compte pour votre test, écrivez-la en
  littéral.
* **Aucun rétrécissement (*shrinking*).** Ce n'est pas une bibliothèque de test à base de
  propriétés. Un échec se rejoue exactement via sa graine, il n'est pas réduit à un contre-exemple
  minimal.
* **Aucun graphe d'objet complet.** Il n'existe pas d'`Any.Object<T>()` qui réfléchirait sur votre
  type pour le remplir. C'est vous qui composez la valeur, et c'est ce qui la garde valide selon vos
  règles plutôt que selon une convention devinée par la bibliothèque.
* **Une valeur par `Generate()`.** La couverture vient de l'exécution fréquente de la suite avec des
  graines qui varient, pas d'un appel qui explorerait un espace.

Ces limites sont volontaires et argumentées dans
[Principes de conception](./design-principles.fr.md).

---

[← Sommaire de la documentation](../README.fr.md)
