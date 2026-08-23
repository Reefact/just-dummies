# Tool JustDummies (`dum`) — spécification v1.0

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](justdummies-tool.md)

**Statut :** spécification, implémentée. `JustDummies.GenAny` et `JustDummies.Cli` existent et
portent les contraintes projet des §10 et §13. Écrites : la §3, la ligne de commande, et la résolution de type
du §3.2 ; la §4, le fichier émis ; **toute la §5** — choix du constructeur, table de base, guards, composition et
paramètre ouvert — ainsi que la provenance que rapporte la §6 ; la §6, le récapitulatif console ; les codes de
sortie et l'avertissement de masquage de la §7 ; et tout le pipeline du §11.1, si bien que `dum generate` ouvre un
vrai projet et écrit un vrai fichier. L'exemple travaillé du §4.1 est produit de bout en bout depuis sa propre
source, au caractère près. Le train de release `cli` l'empaquette et asserte la D9 sur le paquet produit
(§13.6). **Publié :** `cli-v1.1.0-beta.1`, après la première release du train `cli-v1.0.0-beta.1` — une beta
parce qu'un outil ne prend aucune baseline d'API publique (§13.4) : ce qu'une version engage ici, c'est la
ligne de commande de la §3, et aucun projet hors de ce dépôt ne l'a encore éprouvée.
**Remplace :** la pré-spécification de travail 0.1 (jamais commitée)

---

## 0. Comment lire ce document

Cette spécification est **autonome à dessein**. Elle a été écrite alors que JustDummies vivait
encore dans `Reefact/first-class-errors`, pour que rien ici ne dépende d'une lecture faite là-bas ;
le déménagement a eu lieu depuis, et la propriété tient toujours.

* **§1–§9, c'est le produit.** Ce que le tool fait, ce qu'il émet, et pourquoi. Lire le §2
  d'abord : douze décisions portent tout le reste. Le §5 est la partie difficile et la seule qui
  comporte un vrai risque de conception.
* **§10–§12, c'est la construction.** Deux projets, le contrat entre eux, et le plan de tests.
* **§13, c'est le contrat de portabilité.** Tout ce dont le tool a besoin *de son dépôt hôte*,
  énoncé en exigences plutôt qu'en chemins. Si JustDummies a déménagé, commencer ici.
* **§14, c'est la référence.** Chaque fait sur la bibliothèque JustDummies dont dépend cette
  spécification, inliné, avec la commande pour le redériver. Rien dans les §1–§12 n'exige de lire
  la source de la bibliothèque pour être vérifié.
* **§15, c'est le raisonnement.** Onze enregistrements de décision, désormais entrés dans la base
  d'ADR de ce dépôt et indexés ici. À lire quand on veut savoir *pourquoi*, ou quand on est tenté
  de revenir sur une décision du §2.
* **§16, c'est la frontière de la v1.0.** Ce qui est reporté, et ce qui a été abandonné net.
* **§17, ce sont les preuves.** Le squelette émis du §4.1 a été compilé et exécuté contre la vraie
  bibliothèque, et les deux affirmations contestées ont été mesurées. Le §17.2 dit comment tout
  rejouer.

Tout dans ce document est **décidé**, sauf ce qui figure au §16 (reporté) ou ce qui est
explicitement marqué ouvert. Aucune question ouverte ne bloque l'implémentation.

---

## 1. Ce qu'est `dum`

`dum` est un **scaffolder**, pas un générateur de code.

À partir d'un type du code du développeur, il écrit **un fichier C#, une fois**, contenant un
generator nommé et composable pour ce type. Dès que le fichier est écrit il appartient au
développeur : il le lit, le modifie, le commite, et ne relance jamais le tool dessus.

```console
$ cd Shop.Tests
$ dum generate Order
✓ AnyOrder.cs
```

```csharp
Order order = new AnyOrder()
    .WithStatus(OrderStatus.Pending)
    .Generate();
```

La distinction avec un *générateur* est toute la position produit, et elle règle d'un coup
l'essentiel de la conception :

* il n'y a pas de dérive, parce qu'il n'y a rien à maintenir synchronisé — le fichier est celui du
  développeur, pas celui du tool ;
* il n'y a donc **ni verbe `check`, ni source generator, ni scénario de régénération** ;
* le tool a le droit de laisser le fichier **inachevé**, parce que l'achever est la moitié du
  marché qui revient au développeur.

La proposition de valeur reste distincte de celle de la bibliothèque : la **bibliothèque** rend les
valeurs valides ; le **tool** rend le test concis.

### 1.1 Les règles de conception auxquelles ce document répond

1. **Extrêmement simple à utiliser.** L'invocation nominale tient en un verbe et un nom de type,
   depuis le répertoire où le fichier atterrira, sans fichier de configuration et sans option.
2. **Bon marché aux deux bouts.** Rien à configurer avant le premier usage ; rien à configurer à
   chaque usage.
3. **Générer tout ce qui peut l'être, et rien de plus.** Là où le tool ne peut pas savoir, il le
   dit dans le fichier et dans la console, et rend la main sur le squelette.
4. **Le nommage est figé en v1.0.** `Order` devient `AnyOrder`, point. Le renommage
   (`OrderFactory`, un préfixe personnalisé) est pour la v1.1+ et le §16 en réserve la forme pour
   que la v1.0 ne la bloque pas.

---

## 2. Décisions

Ce sont les décisions porteuses. Les douze sont couvertes par les onze enregistrements de décision du
§15 — contexte, argument, alternatives écartées, conséquences ; D5 et D6 en partagent un. Cette
table en est l'index ; elle ne porte aucun argument propre.

| # | Décision | Pourquoi, en une ligne |
|---|---|---|
| **D1** | Scaffolder une fois ; le fichier appartient au développeur. | Supprime d'un coup la dérive, le `check` et la question du source generator. |
| **D2** | Le type émis implémente `IAny<T>` et est **immuable**. | Composabilité, et réarmement des analyzers `JustDummies.Usage` sur le type émis. |
| **D3** | Le fichier émis n'est **pas** marqué comme code généré. | Les 33 analyzers exemptent le code généré ; le marquer rendrait le fichier aveugle. |
| **D4** | Ne jamais émettre un membre non résolu dans la compilation cible. | Une règle couvre le clivage de TFM, la baseline d'API publique, l'écart de version et l'arithmétique non signée. |
| **D5** | Lire les clauses de garde du constructeur pour amorcer chaque generator. | Sans cela le code émis produit des valeurs que le constructeur rejette. |
| **D6** | Un paramètre non résolu est émis comme **erreur de compilation**. | Le développeur est déjà dans le fichier ; un soulignement rouge est le signal le moins cher. |
| **D7** | Le generator émis tire du contexte **ambiant** et ne détient aucun état. | La résolution au tirage rend la garantie du §8.2 gratuite ; un état capturé exigerait une règle de cycle de vie. |
| **D8** | Le type émis vit dans le **namespace du type cible**. | Zéro friction au site d'appel — et la cause unique du risque de masquage du §7. |
| **D9** | Le tool ne prend **aucune dépendance sur le package JustDummies**. | Résolution par nom de métadonnée, comme les analyzers — l'écart de version devient structurellement impossible. |
| **D10** | Ne jamais émettre `.OrNull()`. | Un dummy aléatoirement `null` est précisément l'instabilité que la bibliothèque existe pour supprimer. |
| **D11** | Le **moteur de scaffolding est une bibliothèque séparée** au plancher Roslyn ; la CLI est une coquille. | Le second consommateur plausible du moteur est un refactoring IDE, qui n'est pas une CLI et ne peut pas charger un assembly `net8.0`. |
| **D12** *(v1.1)* | Un point d'entrée est **optionnel**, émis dans un fichier à lui. | Le fichier du generator ne change jamais, donc le plancher du §4.4 reste sa propriété et `new Any{Type}()` continue de fonctionner. |

---

## 3. Surface de commande

Le tool est distribué comme .NET tool dont la commande est **`dum`**.

```console
dotnet tool install --global JustDummies.Cli
dum generate <Type> [<Type>...] [options]
```

`generate` est le seul verbe de la v1.0.

| Option | Défaut | Signification |
|---|---|---|
| `--project <path>` | l'unique `*.csproj` du répertoire courant | Projet dont la compilation est analysée. |
| `--output <dir>` | le répertoire courant | Où le fichier est écrit. |
| `--namespace <ns>` | le namespace du type cible (D8) | Namespace du type émis. |
| `--entry-point <v>` *(v1.1)* | `none` | Émet en plus un point d'entrée : `none`, `static:<Name>` ou `any` (§4.5). |
| `--entry-point-namespace <ns>` *(v1.1)* | le namespace du type émis | Namespace du seul fichier de point d'entrée. |
| `--force` | inactif | Écrase un fichier existant. |
| `--dry-run` | inactif | Affiche le fichier sur stdout ; n'écrit rien. |
| `--format <f>` *(v1.1)* | `human` | Comment l'exécution rend compte : `human` ou `json` (§6.1). |

C'est toute la surface. Pas de `init`, pas de `list`, pas de `--all`, et — par D1 — pas de `check`.
Le §16 liste ce qui est délibérément reporté. Il **y a** un fichier de configuration depuis la v1.1,
et uniquement pour des défauts : §3.3.

### 3.1 Où le tool est lancé

Depuis le **projet de test**, parce que c'est là que le fichier va. Le projet de test référence le
projet de production, donc `Order` est atteignable depuis sa compilation, et le défaut de
`--output` place `AnyOrder.cs` à côté des tests qui l'utilisent.

Résolution de `--project` : si exactement un `*.csproj` se trouve dans le répertoire courant, il
est retenu ; s'il n'y en a aucun ou plusieurs, échec avec un message nommant les candidats et
pointant `--project`.

### 3.2 Résolution du type cible

`Order` est cherché, dans l'ordre :

1. par nom de métadonnée complet, si l'argument contient un `.` (`Shop.Domain.Order`) ;
2. par nom simple parmi les types source de la compilation et les assemblies référencées.

Un type **imbriqué** s'écrit comme un développeur le taperait — `dum generate Order.Line` — et le
moteur le traduit pour la recherche, où le séparateur est `+` et non `.`
(`Shop.Domain.Order+Line`). Passer la forme pointée telle quelle à une recherche par nom de
métadonnée ne renvoie rien, ce qui signalerait comme absent un type bien réel. Le generator émis est
un type de premier niveau dans le namespace englobant, nommé d'après le seul type imbriqué :
`AnyLine`.

Zéro correspondance → erreur, avec les noms les plus proches par distance d'édition. Plus d'une →
erreur, avec les noms complets, en demandant lequel. Les deux sortent en `1`.

### 3.3 Défauts de projet *(v1.1)*

Un `dum.json` optionnel **à côté du fichier projet** fixe ce que la ligne de commande répéterait
sinon. Décision :
[ADR-0072](../adr/0072-read-project-defaults-from-a-file-the-command-line-overrides.fr.md).

```json
{ "output": "./Dummies", "entryPoint": "static:Dummies", "entryPointNamespace": "Shop.Tests.Dummies" }
```

Il lit cinq clés — `output`, `namespace`, `entryPoint`, `entryPointNamespace`, `format` — une par
option qui est une propriété du projet plutôt que d'une invocation. `--force` et `--dry-run` n'en sont
pas : elles disent à quoi sert cette exécution-ci.

**La ligne de commande l'emporte toujours**, et elle l'emporte simplement en étant déjà là : une
valeur que le développeur a tapée est non nulle, et rien de ce que le fichier fournit n'en écrase une.
C'est toute la règle de précédence, et elle tient en une phrase exprès.

**Une clé que le fichier ne lit pas est refusée**, en la nommant et en listant celles qui sont lues.
Un défaut que quelqu'un croit en vigueur et qui ne l'est pas est un état pire que l'absence de
fichier. La clé `naming` que réserve le §16 est refusée par cette règle elle aussi, tant que `--name`
et `--pattern` n'existent pas pour lui donner un sens.

**Un `output` relatif est enraciné dans le dossier du projet**, pas dans le dossier courant. Un chemin
tapé sur la ligne de commande est relatif à l'endroit où il a été tapé ; un chemin commité dans ce
fichier doit vouloir dire la même chose d'où que l'outil soit lancé, sinon ce n'est pas un défaut.

L'état fusionné est validé par les règles auxquelles répond la ligne de commande, de sorte qu'une
valeur venue de ce fichier est refusée pour les mêmes raisons qu'une valeur tapée, et dans les mêmes
mots. Chaque refus ici est un `2` : rien n'a été scaffoldé, et ce qui n'a pas pu être lu est une
instruction.

---

## 4. Le fichier émis

### 4.1 Exemple complet

Cet exemple n'est pas une esquisse : il a été compilé et exécuté contre la vraie bibliothèque
(§17).

Source analysée :

```csharp
namespace Shop.Domain;

public sealed class Order {

    public Order(OrderReference reference, Customer customer, int quantity,
                 OrderStatus status, IReadOnlyList<string> tags, DateTime placedAt) {
        if (reference is null) { throw new ArgumentNullException(nameof(reference)); }
        if (quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantity)); }
        ...
    }

}

public sealed class OrderReference {

    public static OrderReference Create(string value) {
        if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException(...); }
        ...
    }

}
```

`dum generate Order`, avec `AnyCustomer` déjà scaffoldé dans le projet, émet :

```csharp
// Scaffolded by dum (JustDummies). This file is yours: read it, edit it, commit it.
// `dum generate Order --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Order" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins it; to draw from an isolated
///     <c>Any.WithSeed(...)</c> context, pass that context's generators through the
///     <c>With…</c> overloads.
/// </summary>
public sealed partial class AnyOrder : IAny<Order> {

    private readonly IAny<OrderReference>        _reference;
    private readonly IAny<Customer>              _customer;
    private readonly IAny<int>                   _quantity;
    private readonly IAny<OrderStatus>           _status;
    private readonly IAny<IReadOnlyList<string>> _tags;
    private readonly IAny<DateTime>              _placedAt;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyOrder()
        : this(reference: ReferenceFactory(),
               customer:  CustomerFactory(),
               quantity:  QuantityFactory(),
               status:    StatusFactory(),
               tags:      TagsFactory(),
               placedAt:  PlacedAtFactory()) { }

    private static IAny<OrderReference> ReferenceFactory() {
        return Any.String().NonEmpty().As(OrderReference.Create);
    }

    private static IAny<Customer> CustomerFactory() {
        return new AnyCustomer();
    }

    private static IAny<int> QuantityFactory() {
        return Any.Int32().Positive();
    }

    private static IAny<OrderStatus> StatusFactory() {
        return Any.Enum<OrderStatus>();
    }

    private static IAny<IReadOnlyList<string>> TagsFactory() {
        return Any.ListOf(Any.String().NonEmpty());
    }

    private static IAny<DateTime> PlacedAtFactory() {
        return Any.DateTime();
    }

    private AnyOrder(IAny<OrderReference>        reference,
                     IAny<Customer>              customer,
                     IAny<int>                   quantity,
                     IAny<OrderStatus>           status,
                     IAny<IReadOnlyList<string>> tags,
                     IAny<DateTime>              placedAt) {
        _reference = reference;
        _customer  = customer;
        _quantity  = quantity;
        _status    = status;
        _tags      = tags;
        _placedAt  = placedAt;
    }

    /// <summary>Pins <c>reference</c> to a fixed value.</summary>
    public AnyOrder WithReference(OrderReference value) {
        return WithReference(new FixedValue<OrderReference>(value));
    }

    /// <summary>Draws <c>reference</c> from <paramref name="generator" />.</summary>
    public AnyOrder WithReference(IAny<OrderReference> generator) {
        return new AnyOrder(generator, _customer, _quantity, _status, _tags, _placedAt);
    }

    // ... une telle paire par paramètre ...

    /// <summary>Produces one arbitrary <see cref="Order" />.</summary>
    public Order Generate() {
        return new Order(_reference.Generate(),
                         _customer.Generate(),
                         _quantity.Generate(),
                         _status.Generate(),
                         _tags.Generate(),
                         _placedAt.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
```

### 4.2 Règles de forme

* `public sealed partial class Any{Type} : IAny<{Type}>`. `partial` pour que les membres propres du
  développeur vivent dans un fichier voisin et survivent à un `--force`.
* Un `private readonly IAny<TParam> _param;` par paramètre du constructeur, dans l'ordre de
  déclaration.
* Un **constructeur public sans paramètre** portant la recette inférée, écrit avec des arguments
  nommés pour que le lecteur associe chaque appel à son paramètre sans compter.
* Une **fabrique privée statique** par paramètre — `{Param}Factory()` — logeant sa recette.
  L'initialiseur du constructeur public les appelle par leur nom plutôt que d'inliner chaque
  chaîne ; le TODO d'un paramètre non résolu (§5.5) vit dans sa propre fabrique, pas au point
  d'appel.
* Un **constructeur privé complet** réalisant la copie.
* Par paramètre, **deux** surcharges `With{Param}` retournant une nouvelle instance :
  `With{Param}(TParam value)` et `With{Param}(IAny<TParam> generator)`.
  La surcharge par valeur est l'ergonomique ; la surcharge par generator est ce qui maintient la
  composition possible, et c'est pourquoi passer `Any.String().StartingWith("ORD-")` ne devient pas
  une erreur `JD011`/`JD012`.
* `public {Type} Generate()` appelant le constructeur avec le `Generate()` de chaque champ.
* Le helper privé imbriqué `FixedValue<TValue>`. Justification : il accepte `null` (ce que
  `Any.OneOf(value)` refuse) et ne consomme aucun tirage de la source ambiante, donc épingler un
  paramètre ne décale pas les valeurs tirées pour les autres (§14.5). Il est imbriqué et privé,
  donc un nombre quelconque de fichiers scaffoldés coexistent. *(Si `Any.Fixed<T>(value)` est un
  jour ajouté à la bibliothèque, le helper pourra disparaître — voir §15.)*
* Casse de `With{Param}` : le nom du paramètre, première lettre en majuscule, culture invariante.
  Un paramètre nommé `_id` ou `@class` est normalisé en retirant le `_`/`@` de tête.

**Le cas dégénéré a sa propre forme.** Un constructeur sans paramètre (§5.1) fait s'effondrer tout
ce qui précède : un seul constructeur public sans paramètre, aucun champ, aucun constructeur privé,
aucune méthode `With`, aucun helper `FixedValue`, et `Generate()` retournant `new {Type}()`. Émettre
les deux constructeurs sans condition leur donnerait la même signature et échouerait en `CS0111` —
vérifié. Le résultat vaut quand même d'être généré : `Any{Type}` est un `IAny<T>`, donc il se
compose dans `Any.ListOf(...)`, `Any.Combine(...)` et le reste, ce qu'un simple `new {Type}()` ne
fait pas.

### 4.3 Règles d'en-tête

Exactement trois lignes de commentaire, comme ci-dessus. **Aucun horodatage et aucune version du
tool** : les deux feraient dépendre le contenu d'autre chose que du type analysé, si bien que tout
scaffold suivant une montée de version produirait un diff parasite. Le déterminisme est une
exigence dure (§8.1).

### 4.4 Niveau de langage

Le code émis n'utilise aucune construction plus récente que **C# 7.3** : pas de `var` (cela se lit
mieux dans un squelette), pas de `new` typé par la cible, pas de records, pas d'expressions
`switch`, pas de namespace à portée de fichier sauf si le fichier du type cible en utilise déjà un.
Le fichier atterrit dans le projet du développeur et doit compiler au `LangVersion` de ce projet.

La seule exception est la forme du namespace, copiée sur le style de déclaration du type cible pour
que le fichier émis ressemble à ses voisins.

Le plancher est une propriété de **ce** fichier. Le fichier de point d'entrée du §4.5 peut se voir
demander une construction plus récente, et le dit dans son propre en-tête ; c'est un fichier séparé
précisément pour que le plancher ne bouge pas ici.

### 4.5 Le fichier de point d'entrée *(v1.1)*

`new AnyOrder()` est la façon d'atteindre un generator scaffoldé, et elle le reste. `--entry-point`
demande un **second** fichier à côté, portant une fabrique, pour que le generator s'atteigne aussi
comme ceux de la bibliothèque — `Any.Int32()` sur une ligne et `Any.Order()` sur la suivante.
Décision : [ADR-0070](../adr/0070-emit-an-entry-point-on-request-as-a-file-of-its-own.fr.md).

| Valeur | Ce qui est émis | Écriture |
|---|---|---|
| `none` *(défaut)* | rien | — |
| `static:<Name>` | `public static partial class <Name>` portant une fabrique | `Dummies.Order()` |
| `any` | `extension(Any)` portant une fabrique statique | `Any.Order()` |

**Le fichier du generator ne change pas.** `Any{Type}.cs` est identique octet pour octet sous les
trois valeurs, donc `new Any{Type}()` continue de fonctionner et le plancher du §4.4 n'est pas
touché. Ce qui est ajouté l'est à côté, dans `Any{Type}.Entry.cs`.

**Une part par scaffold, jamais un fichier partagé.** La racine statique est `partial`, et chaque
scaffold écrit sa propre part. Rien n'est lu pour être réécrit, donc le §8.1 tient et D1 n'est pas
discrètement renversée : `dum generate Order Customer Invoice --entry-point static:Dummies` écrit six
fichiers et aucun deux fois.

**`any` exige C# 14, et le framework cible n'a rien à y voir.** Un membre d'extension statique
compile pour une cible `netstandard2.0` aussi bien que pour `net10.0` ; ce qu'il exige, c'est le
`LangVersion` du projet. Un projet en deçà de C# 14 est refusé, pas rétrogradé (§7).

**`static:Any` est refusé.** C# résout un nom de type simple dans le namespace englobant avant tout
`using`, donc une classe statique nommée `Any` dans le projet du développeur masque
`JustDummies.Any` au lieu de la compléter, et `Any.Int32()` cesse de compiler (`CS0117`). C'est à cela
que sert `any`, et c'est un autre mécanisme.

**Le point d'entrée peut se déplacer seul.** `--entry-point-namespace` place le fichier de point
d'entrée et rien d'autre ; le generator reste dans le namespace que D8 lui donne, donc aucun site
d'appel ne paie d'import pour lui. Déplacer le point d'entrée est ce qui rend une racine unique
atteignable à travers plusieurs namespaces, et cela ouvre le namespace du generator dans le fichier
émis. `--namespace` déplace toujours le generator, et emmène le point d'entrée avec lui sauf si cette
option en décide autrement.

**Règles de forme.** Trois lignes d'en-tête comme au §4.3, nommant l'option qui a écrit le fichier.
Une fabrique publique statique nommée d'après le seul type cible — `Order.Line` scaffolde `AnyLine` et
s'atteint par `Line()`. Elle rend le generator, jamais une valeur : le contraindre par `With…` et
appeler `Generate()` appartiennent au développeur, exactement comme avec `new Any{Type}()`. Le fichier
`static:<Name>` n'utilise aucune construction plus récente que C# 7.3 ; le fichier `any` exige C# 14
et rien de plus.

Un type cible dont le nom propre est celui de la racine choisie émet un membre nommé comme sa classe
englobante, ce qui ne compile pas (`CS0542`). C'est bruyant au build du développeur, comme le
paramètre ouvert du §5.5, et le remède est un autre nom de racine.

---

## 5. Résolution — comment un paramètre devient un generator

Pour chaque paramètre, le moteur produit une expression de type `IAny<TParam>`, ou échoue et marque
le paramètre non résolu.

### 5.1 Choix du constructeur

1. Constructeurs d'instance publics, le plus de paramètres d'abord ; égalité départagée par l'ordre
   source. La signature retenue est toujours affichée (§6).
2. Si le type n'a **aucun** constructeur accessible mais expose une fabrique statique reconnue
   (§5.4) retournant lui-même, cette fabrique est utilisée et `Generate()` l'appelle.
3. Un constructeur sans paramètre donne un `AnyOrder` valide et trivial, sans méthode `With`.
4. Les records positionnels fonctionnent sans traitement particulier — leur constructeur primaire
   est un constructeur public ordinaire. Les membres `init` et `required` sont **hors périmètre**
   (§16) — et hors périmètre est un **refus**, jamais un silence : un type dont le constructeur
   retenu laisse un membre `required` non assigné est refusé (§7), parce que l'alternative est un
   fichier annonçant `1 of 1 parameters inferred` puis faisant échouer la compilation du
   développeur en `CS9035`. Un constructeur marqué `[SetsRequiredMembers]` les assigne, et est
   scaffoldé comme n'importe quel autre.
5. Un constructeur ayant un paramètre `ref` ou `out` n'est **pas éligible** : `Generate()` passe des
   arguments par valeur, et un tel site d'appel échoue en `CS1620` — vérifié. L'ignorer et
   considérer le candidat suivant ; s'il n'en reste aucun, le type est non résolu (§7). `in`
   convient, un argument par valeur s'y lie.
6. **Trouver un constructeur n'est pas la même question que pouvoir l'appeler.** Un type
   **abstrait** déclare des constructeurs publics et ne peut pas être instancié (`CS0144`) ; un
   type **générique** — ou imbriqué dans un générique — ne peut pas même être nommé, puisque rien
   n'en fournit l'argument de type (`CS0246`). Les deux sont refusés avant que quoi que ce soit ne
   soit écrit (§7). La vérification est ici plutôt que dans l'émetteur : un fichier que le
   développeur ne peut pas compiler, écrit sous un récapitulatif affirmant que tous les paramètres
   ont été inférés, est pire que pas de fichier du tout.

### 5.2 La table de base

Chaque entrée est soumise à D4 : le membre n'est émis que s'il se résout dans la compilation.

| Type du paramètre | Émission |
|---|---|
| `string` | `Any.String().NonEmpty()` |
| `bool` | `Any.Boolean()` |
| `sbyte` `byte` `short` `ushort` `int` `uint` `long` `ulong` | `Any.SByte()` … `Any.UInt64()` |
| `float` `double` `decimal` | `Any.Single()` / `Any.Double()` / `Any.Decimal()` |
| `char` | `Any.Char()` |
| `Guid` | `Any.Guid().NonEmpty()` |
| `DateTime` `DateTimeOffset` `TimeSpan` | `Any.DateTime()` / `Any.DateTimeOffset()` / `Any.TimeSpan()` |
| `DateOnly` `TimeOnly` `Int128` `UInt128` `Half` | la fabrique correspondante — **asset `net8.0` uniquement**, D4 tranche |
| tout `enum E` | `Any.Enum<E>()` |
| `Uri` | `Any.Uri().Web()` |
| `T[]` | `Any.ArrayOf(<T>)` |
| `List<T>` `IReadOnlyList<T>` `IList<T>` `ICollection<T>` `IReadOnlyCollection<T>` | `Any.ListOf(<T>)` |
| `IEnumerable<T>` | `Any.SequenceOf(<T>)` |
| `HashSet<T>` `ISet<T>` | `Any.SetOf(<T>)` |
| `Dictionary<K,V>` `IDictionary<K,V>` `IReadOnlyDictionary<K,V>` | `Any.DictionaryOf(<K>, <V>)` |
| `T?` où `T` est un type référence | le generator de `T` inchangé — **jamais** `.OrNull()` (D10) |
| `T?` où `T` est un type valeur | `<generator de T>.As(value => (T?)value)` — **jamais** `.OrNull()` (D10) |
| un type ayant un `AnyT` scaffoldé dans la compilation | `new AnyT()` (§5.4) |
| un type ayant une fabrique statique reconnue à un paramètre | `<generator du paramètre>.As(T.Create)` (§5.4) |
| tout le reste | non résolu (§5.5) |

Trois remarques sur la table.

**`Any.String().NonEmpty()`, pas `Any.String()`.** Sans contrainte, `Any.String()` peut retourner
la chaîne vide (§14.5). Un paramètre de constructeur de type `string` dans un type métier est
massivement requis non vide, et un défaut qui échoue par intermittence — environ une fois sur
dix-sept quand le §17 l'a mesuré, une fois sur mille sous l'étendue plus large fixée depuis par
l'ADR-0076 — est exactement l'instabilité que la bibliothèque existe pour supprimer. Le taux a
bougé ; le défaut, non. Même raisonnement pour `Any.Guid().NonEmpty()`.

**Les collections reposent sur la covariance — les types valeur, non.** `IAny<out T>` est
covariante, donc `Any.ListOf(...)`, de type `IAny<List<T>>`, est directement affectable à un champ
de type `IAny<IReadOnlyList<T>>` ; aucun adaptateur n'est nécessaire pour les lignes d'interface, et
il en va de même pour `HashSet<T>`/`ISet<T>` et `Dictionary<K,V>`/`IReadOnlyDictionary<K,V>`.

La variance en C# ne s'applique qu'aux conversions de **référence**, d'où la différence entre les
deux lignes nullables. `IAny<string>` est un `IAny<string?>` et ne demande rien ; `IAny<int>`
n'est **pas** un `IAny<int?>`, donc un paramètre `int?` exige le saut explicite
`.As(value => (int?)value)`. S'y tromper est la façon la plus probable de produire une table qui ne
compile pas — les lignes réservées à `net8.0` sont elles aussi des types valeur.

**Les generators d'éléments récursent.** `IReadOnlyList<OrderLine>` résout son élément par cette
même table, et devient donc `Any.ListOf(new AnyOrderLine())` quand `AnyOrderLine` existe. La
récursion est limitée à une profondeur de 3 et protégée contre les cycles ; dépasser l'une ou
l'autre rend le paramètre non résolu.

### 5.3 Clauses de garde

C'est la fonctionnalité qui justifie de construire le tool plutôt que de faire un template.

Quand le corps du constructeur (ou de la fabrique) est **disponible en source** — ce qui est le cas
pour tout type de la solution du développeur, et ne l'est pas pour un type venant d'un package
NuGet — le moteur lit ses clauses de garde de tête et resserre le generator en conséquence.

Une instruction n'est une garde que si **toutes** les conditions suivantes tiennent. La règle est
délibérément conservatrice, à l'image des analyzers de la bibliothèque, qui préfèrent sous-signaler
plutôt que se tromper :

* c'est un `if` dont le corps lève inconditionnellement ;
* elle apparaît avant la première affectation à un champ ou une propriété ;
* sa condition mentionne **exactement un** paramètre et ne contient ni `&&` ni `||` ;
* aucune écriture de ce paramètre n'a pu s'exécuter avant l'endroit où elle se trouve ;
* tout autre opérande est une constante de compilation.

**Un `else` n'arrête pas la lecture.** Une branche `else` ne dit que ce qui se passe quand sa propre
condition est fausse — exactement le cas où les branches précédentes ont déjà laissé passer la
valeur — donc elle ne peut jamais affaiblir ce qu'elles rejettent : `if (v < 0) { throw … } else { … }`
lit toujours `v < 0`, comme si le `else` était absent. Une chaîne `else if` se lit de la même façon,
branche après branche, tant que **chaque branche précédant celle en cours de lecture lève elle aussi
inconditionnellement** : `if (a < 0) { throw … } else if (b > 100) { throw … }` lit les deux, parce
qu'atteindre le test de `b` ne présuppose que le rejet déjà fait par la branche de `a`. Dès qu'une
branche ne lève pas inconditionnellement, la lecture s'arrête là : `if (a < 0) { a = 0; } else if
(b > 100) { throw … }` ne lit ni l'une ni l'autre, parce qu'atteindre le test de `b` présuppose
désormais aussi `a >= 0` — une règle inter-paramètres, exactement le cas que cette section refuse
déjà de lire. Cette branche, et tout ce qui suit, est marquée `unread guards` plutôt que passée sous
silence.

**La réassignation d'un paramètre met fin à la lecture de ce paramètre, et d'aucun autre.** Seule
une affectation à un champ ou une propriété met fin au parcours des gardes de tête : un constructeur
qui réécrit un paramètre puis le garde voyait donc ces gardes lues comme des bornes sur la valeur
tirée. C'était le seul cas où le moteur se trompait avec assurance plutôt que d'être aveugle — tous
les autres manques que nomme le §9 sont des gardes qu'il ne voit pas ; ici il voyait la garde, la
lisait correctement, et l'attribuait à une valeur que le constructeur avait déjà remplacée.
`if (percent < 0) { throw … } percent = 100 - percent; if (percent < 0) { throw … }` donnait
`.GreaterThanOrEqualTo(0)` sur un domaine réel de 0 à 100, rapporté comme inféré, et un tirage d'un
million levait dans le constructeur, sans sentinelle et sans rien à regarder. Une garde écrite
**sous** une réassignation de son propre paramètre est donc marquée `unread guards` (§9) plutôt que
lue, et une garde écrite **au-dessus** tient toujours : elle est vraie de la valeur tirée, et la
jeter aussi coûterait une contrainte pour rien. La règle est cantonnée au paramètre écrit : arrêter
le parcours purement et simplement jetterait les gardes de tous les *autres* paramètres du
constructeur, échangeant une contrainte à ne pas lire contre plusieurs à lire.

**Quelles écritures existent, c'est au compilateur qu'on le demande ; où elles se situent, c'est à
l'exécution.** Les deux moitiés sont des refus de deviner. Une liste des orthographes — `=`, les
formes composées, `++`, `--` — se lit comme complète et ne l'est pas :
`(percent, rate) = (100 - percent, rate)` écrit via un tuple dont le côté gauche ne résout vers
aucun paramètre, `int.TryParse(text, out percent)` écrit sans la moindre affectation, et un local
`ref` fait alias du paramètre sous un autre nom. Les trois ont été mesurés lus comme des bornes sur
la valeur tirée. La question passe donc par l'analyse de flot de données, qui répond pour toutes les
orthographes d'un coup, y compris celles auxquelles personne n'a pensé.

La position est l'autre moitié, et l'instruction en est la mauvaise unité. Une écriture et une garde
partagent une instruction aussi facilement qu'elles en occupent deux —
`else { percent = 100 - percent; ThrowIfNegative(percent); }` est une seule instruction qui porte les
deux — donc ce que le moteur interroge, ce sont les régions **terminées** quand la garde est évaluée.

**Il lit un seul ordre, et interroge entière toute autre construction.** L'ordre qu'il lit, c'est
l'enchaînement des instructions, plus le fait qu'atteindre l'une ou l'autre branche d'un `if` signifie
que sa condition s'est exécutée d'abord — et c'est cette seconde partie qui laisse intacte la règle du
`else` ci-dessus, une condition n'ayant au-dessus d'elle aucune région de sa propre instruction, donc
`if (v < 0) { throw … } else { v = -v; }` lit toujours `v < 0`. Tout le reste répond entier. Une boucle
réexécute son corps, donc une écriture que la source place sous la garde s'exécute au-dessus d'elle au
tour suivant ; un `finally` s'exécute après un `try` qui a écrit ; un `switch` évalue son expression de
contrôle avant la section qu'elle sélectionne ; un `using` sa ressource avant le corps qu'il délimite.

C'est la règle, et non un repli pour les formes que personne n'a listées ; la raison en est ce que le
moteur se met à croire quand ce n'en est pas une. Un parcours qui sait entrer dans certaines
constructions et ne rend rien pour les autres fait du **silence** la réponse pour tout ce qui n'est pas
listé, et le silence se lit *aucune écriture ne s'est exécutée* — la seule réponse qui transforme une
garde que le moteur ne peut pas placer en une garde qu'il émet. Les quatre constructions ci-dessus ont
été mesurées en train de faire exactement cela :
`while (v < 100) { ThrowIfGreaterThan(v, 50); v += 30; }` n'accepte aucune valeur tirée entre 51 et 99
et rejette 40, et `try { v = 100 - v; } finally { ThrowIfNegative(v); }` n'énonce rigoureusement rien
sur la valeur tirée, et pourtant les deux étaient lues comme des bornes sur elle. Interroger une région
qui est un *sur-ensemble* ne peut qu'ajouter des refus, jamais en retirer : la règle vaut donc pour les
constructions que cette page ne nomme pas, y compris celles que C# n'a pas encore.

**Le corps n'est pas là où le constructeur commence.** Un `: this(…)` ou un `: base(…)` s'exécute entier
avant la première instruction : ses arguments sont donc des régions terminées pour toute garde en
dessous — `: this(Normalise(ref value))` est une délégation ordinaire vers une surcharge plus large, et
elle a déjà remplacé la valeur tirée quand le premier `if` du corps est évalué. Une écriture
d'initialiseur échappe à cette question et fait l'objet d'une question à part : là où le modificateur
appartient à l'**argument** plutôt qu'à quelque chose en son sein, `: this(ref value, true)`, la région
analysée est l'identifiant nu qu'il porte et le compilateur rapporte le paramètre lu plutôt qu'écrit —
mesuré donnant `GreaterThanOrEqualTo(0)` sur une délégation qui avait déjà remplacé la valeur. Le
symbole de l'initialiseur est donc interrogé sur les paramètres qu'il reçoit par référence, et un
argument nommant le paramètre à l'une de ces positions compte comme une écriture. Posée au constructeur
appelé plutôt qu'aux mots-clés `ref` et `out`, pour la raison même qui envoie tout le reste de cette
question au compilateur ; et tout mode de passage autre que par valeur compte, `in` compris, car nommer
ceux qui peuvent écrire obligerait à rester juste sur tous ceux que le langage se donnera. Une forme de
constructeur qui ne déclare aucun corps propre — record positionnel, constructeur primaire — ne lit
aucune garde et est rapportée comme sans source (§6) : la question ne s'y pose pas.

Deux écritures échappent entièrement à ce parcours et sont refusées où qu'elles soient écrites. **Celle
qui se trouve dans une fonction locale ou un lambda** s'exécute quand on l'appelle et non là où elle est
déclarée — `Bump(); … void Bump() { v++; }` écrit en premier et lit en dernier — et le §9 nomme déjà
l'indirection que le tool ne suit pas ; c'est la même lacune vue de l'autre côté. Et **toute écriture
dans un corps portant un `goto`**, car un saut arrière place une écriture au-dessus d'une garde que la
source place au-dessous, et rien dans le texte ne le dit.

Le prix d'une interrogation entière, c'est la précision, et il est délibéré : une garde dans un `using`
ou un `lock` dont la construction n'écrit le paramètre qu'*après* elle est refusée alors qu'elle était
lisible. Un `try` ou un `switch` n'illustre plus ce prix, étant refusé par la question ci-dessous avant
que celle-ci soit atteinte. Refuser une contrainte coûte un marquage `unread guards` que son auteur lève une
fois ; en émettre une fausse coûte un generator dont le constructeur rejette chaque tirage, rapporté
comme inféré. Le graphe de flot de contrôle de Roslyn a été évalué pour cette question et n'a pas été
retenu, parce que la direction de son défaut est l'inverse — une arête qu'il ne porte pas se lit
*aucune écriture n'a tourné*, là où un construct que ce parcours ne modélise pas se lit *l'interroger
entier* ([ADR-0084](../adr/0084-place-a-guard-by-syntax-reach-not-a-control-flow-graph.fr.md)).

`ref` et `out` sur les paramètres **propres** du constructeur n'ont besoin d'aucune règle ici — le
§5.1 décline déjà un tel constructeur, puisque la fabrique émise ne saurait l'appeler.

L'ensemble reconnu est clos :

| Condition qui lève | Contrainte ajoutée |
|---|---|
| `p is null`, `p == null` | aucune — le generator ne retourne jamais `null` de toute façon |
| `string.IsNullOrEmpty(p)`, `string.IsNullOrWhiteSpace(p)`, `p.Length == 0`, `p.Length < 1` | `.NonEmpty()` |
| `p.Length > N` | `.WithMaxLength(N)` |
| `p.Length < N` | `.WithMinLength(N)` |
| `p.Length != N` | `.WithLength(N)` |
| `p <= 0` ; ou `p < 1` sur un type **intégral** | `.Positive()`, ou `.NonZero()` sur un type **non signé** |
| `p < 0` | `.GreaterThanOrEqualTo(0)` |
| `p >= 0` | `.Negative()` ; **non lue** sur un type **non signé**, où elle rejette toute valeur possible |
| `p == 0` | `.NonZero()` |
| `p > N` | `.LessThanOrEqualTo(N)` |
| `p < N` | `.GreaterThanOrEqualTo(N)` |
| `p == Guid.Empty` | `.NonEmpty()` |
| `!Enum.IsDefined(typeof(E), p)`, `!Enum.IsDefined(p)` | aucune — `Any.Enum<E>()` ne tire déjà que des membres déclarés, **là où `p` est de type `E`** |
| `p == E.Member` | `.DifferentFrom(E.Member)`, **là où `p` est de type `E`** |

`.NonEmpty()` couvre `IsNullOrWhiteSpace` aussi bien que `IsNullOrEmpty`, parce qu'un
`Any.String()` non contraint ne tire que des lettres et chiffres ASCII : un tirage non vide ne peut
jamais être blanc (§14.5).

**Un signe s'écrit dans le membre que porte le generator propre au paramètre.** Le §14.3 donne aux
familles non signées la surface signée *moins* `Positive` et `Negative` : écrire `.Positive()` pour
`p <= 0` sur un `byte` ou un `uint` émet donc un membre que la recherche abandonne ensuite — un
tirage que rien ne resserre, sous un fichier qui compile pourtant, et un generator qui tire la seule
valeur que la garde existe pour refuser. Zéro est le plancher d'un type non signé, donc *au-dessus
de zéro* est exactement *non nul* : `.NonZero()` est la même contrainte dans la seule orthographe
disponible, pas une plus lâche. `.Negative()` n'a pas d'équivalent — `p >= 0` rejette toute valeur
qu'un type non signé peut contenir — donc elle n'est pas écrite du tout et le paramètre est marqué
`unread guards`, le refus que mérite un tel domaine.

**Une garde d'exclusion d'énumération est lue elle aussi, et c'est la garde d'énumération la plus
courante qui soit** — `if (status == Status.None) { throw … }`. Roslyn rapporte un membre
d'énumération à zéro comme une simple constante **entière**, donc sans cette ligne la condition
tombait dans la ligne `p == 0` de la famille numérique et se lisait `.NonZero()` — un membre
qu'`AnyEnum<T>` ne porte pas, donc la recherche de membre (§5.2) l'abandonnait et le paramètre
rapportait `constraint unavailable` sur un tirage que rien ne resserrait. Un membre **non nul** ne
correspondait à aucune ligne numérique : il était marqué `unread guards` et bloquait le build du
développeur — le dénouement bruyant, celui que cette ligne convertit en contrainte lue. Les deux
moitiés échouaient différemment, et c'est ce qui fait que la ligne vaut mieux que l'une ou l'autre.
La même discipline d'identité du sujet que pour `Enum.IsDefined` s'applique : le membre
doit appartenir au type d'énumération **propre** au paramètre. La négation, `p != E.Member`, est un
invariant différent — elle lève à moins que la valeur **soit** ce membre, une fixation plutôt
qu'une exclusion — et n'est pas lue comme l'inverse de cette ligne.

Plusieurs conditions bornent l'arithmétique de la table elle-même, et toutes sont des refus plutôt
que des approximations. **Le `N` d'une ligne de taille doit se rendre en l'`int` que prend tout
membre de taille** (§14.3) : une borne se repliant sur `140.5`, ou hors de la portée d'`int`, n'est
pas une taille que le moteur peut écrire, et l'émettre telle quelle fait échouer la compilation du
développeur. **Elle doit aussi être une taille que le generator pourrait produire.** Tout membre de
taille refuse un argument au-delà d'un million (ADR-0076), donc une limite de corps à 1 Mio — une
règle métier ordinaire — n'est pas écrite : elle lèverait dans le constructeur sans paramètre émis,
là où aucun appel `With…` ne peut la rattraper. Et un **plancher** sur un set ou un dictionnaire
demande à la ligne d'élément autant de valeurs *distinctes*, donc un compte de cinq sur un enum à
trois membres est refusé pour la raison même que `JD016` le signale ; un plafond ne demande rien de
tel et ne répond qu'au plafond de production. **Une constante qui n'est pas un point de la droite numérique, ou qui sort de
`decimal`, n'est pas lue du tout** — `double` et `float` vont tous deux au-delà de `decimal`, et NaN
et les infinis ne sont pas des bornes. Dans les deux cas le paramètre garde son generator neutre et
est marqué `unread guards` (§9) — ce que reçoit aussi une garde `Enum.IsDefined` nommant un univers
autre que le type du paramètre : la justification de la ligne est que le generator ne tire déjà que
des membres déclarés, et cela ne tient que si le paramètre est du type de cet enum.

**Une garde de taille sur un paramètre collection relève de la famille `Count`, pas de la famille
`Length`.** Un generator de collection expose `NonEmpty`, `WithCount`, `WithMinCount` et
`WithMaxCount`, et aucun `WithLength` (§14.3). Donc `p.Length > N` sur un `T[]`, ou `p.Count > N`
sur une `List<T>`, devient `.WithMaxCount(N)` ; `p.Count != N` devient `.WithCount(N)`. Lire une
telle garde contre la famille des chaînes émettrait un membre qui ne se résout pas, et D4
l'abandonnerait **silencieusement** — une vraie contrainte perdue sans laisser de trace.
`.NonEmpty()` est le seul membre qui s'écrit pareil des deux côtés.

Les contraintes reconnues **se composent quand elles bornent des choses différentes, et sont
abandonnées quand elles se heurtent**. Deux gardes posant une borne inférieure et une borne
supérieure sont complémentaires — `.NonEmpty()` avec `.WithMaxLength(10)`, ou
`.GreaterThanOrEqualTo(0)` avec `.LessThanOrEqualTo(100)` — et les deux sont conservées. C'est
l'idiome d'intervalle borné ordinaire, écrit en deux gardes consécutives ; l'écarter rendrait la
lecture des gardes inutile pour le cas qu'elle rencontre le plus souvent. Les deux compositions ont
été vérifiées contre la bibliothèque (§17).

Deux gardes posant *la même* borne sont une **conjonction**, non une collision : les deux `if`
lèvent, donc une valeur doit satisfaire les deux, et la plus serrée est la seule chose qu'elles
puissent toutes deux vouloir dire. Elle survit et la plus lâche est abandonnée en silence — la
bibliothèque les replie exactement ainsi, donc émettre les deux écrirait un appel que `JD032`
signale comme mort.

Des bornes ne laissant **aucune valeur** sont inconciliables : toutes sont abandonnées et le
paramètre est signalé `guards not combined`. La bibliothèque rejette une telle chaîne par
`ConflictingAnyConstraintException`, et `JD016`, `JD023` et leurs semblables la signalent à la
compilation (§17), mais le moteur ne doit pas l'émettre pour autant — laquelle des gardes le
développeur voulait dire n'est pas à lui de le deviner. C'est de l'arithmétique d'intervalles sur
tout le `Bound` de la contrainte, et n'être que cela est le propos (ADR-0046) : une borne inférieure
au-dessus d'une supérieure, une taille **exacte** à côté d'une borne qui l'exclut, et un **signe**
contre une borne opposée sont la même question posée trois fois. `.Positive()` est un plancher à
zéro que zéro ne satisfait pas, donc `.Positive().LessThanOrEqualTo(0.5m)` compose et
`.Positive().LessThanOrEqualTo(-5)` non.

**Un raffinement de la table de base cède devant une garde.** Le `.NonEmpty()` de la ligne `string`
du §5.2 est l'opinion du moteur ; une garde est la déclaration du développeur. Là où les deux ne
peuvent tenir ensemble — un constructeur exigeant une chaîne vide — le raffinement est abandonné et
la garde tient, sans `guards not combined`, puisque rien du développeur n'a été concilié de force.
La même lecture l'absorbe là où ils se recouvrent seulement : un plancher à huit dit déjà non vide,
donc `.NonEmpty().WithMinLength(8)` énonce un invariant deux fois et `JD024` le dit.

**Un plancher et un plafond de la même famille sont émis comme l'intervalle qu'ils sont** —
`.WithLengthBetween(8, 20)`, `.WithCountBetween(2, 5)`, `.Between(0, 100)`. Non par obéissance à
`JD031`, qui signale l'écriture en deux bornes comme une information et rien de plus : le moteur
s'est fait dire un intervalle, donc écrire l'intervalle c'est écrire ce qu'il voulait dire. Seule
une paire portant des arguments se replie, ce qui laisse `.Positive()` de côté — il n'a rien à
mettre dans un appel d'intervalle. Chaque membre d'intervalle est cherché avant d'être écrit, comme
tous les autres (§13.1).

Aucune garde reconnue ne produit de contrainte de jeu de caractères ni de motif, donc ces axes ne se
présentent jamais.

**Les gardes regex ne sont délibérément pas lues.** `!Regex.IsMatch(p, "…")` a tout de la garde
idéale à traduire : la bibliothèque a `Any.StringMatching(...)`, et le motif est là, littéral. Elle
est hors de l'ensemble pour la v1.0, pour une raison qui se généralise.

La bibliothèque construit ses valeurs à partir du sous-ensemble *régulier* du langage des motifs —
lookarounds, backreferences, limites de mot et catégories Unicode sont en dehors, et un motif qui en
utilise lève `UnsupportedRegexException`. Quatre motifs de validation réalistes sur cinq ont été
rejetés à l'essai (§17) ; lookaheads et limites de mot sont le vocabulaire ordinaire d'un validateur
écrit à la main.

Pire, le rejet a lieu à la **construction**, pas au `Generate()`. Le constructeur sans paramètre
émis exécute toute la recette dans son initialiseur, donc `new AnyOrder()` lèverait avant que le
moindre `.WithReference(...)` ne puisse surcharger. Le type généré serait inutilisable, pas
seulement imprécis, et aucun appel que le développeur pourrait écrire ne le rattraperait — vérifié
(§17).

Et le moteur ne peut pas le savoir à l'avance. D9 lui interdit de référencer la bibliothèque, donc
il ne peut pas demander à son parser si un motif est supporté, et réimplémenter ce contrôle
dupliquerait un parser qu'il ne voit pas et en dériverait.

D'où une règle qui mérite d'être énoncée pour elle-même, puisque la ligne motif est la seule à
l'avoir jamais enfreinte : **le moteur n'émet jamais une expression dont la validité dépend d'une
valeur qu'il ne peut pas contrôler.** Toutes les autres lignes émettent un membre que D4 résout,
avec un argument qui est une constante de compilation du bon type. Lire les gardes regex est un
candidat v1.1 (§16) et suppose la question du sous-ensemble tranchée d'abord.

Quand deux lignes apparient une même condition, **la plus spécifique gagne**. `p < 1` sur un type
intégral relève de la ligne `.Positive()` ; sur `decimal`, `double` ou `float`, de la ligne
`.GreaterThanOrEqualTo(N)`, parce que `.Positive()` admettrait les valeurs entre zéro et un que la
garde rejette. C'est un tirage rare pour un `decimal` par ailleurs non contraint — mesuré à un sur
cinq mille — et fréquent dès que le paramètre porte une autre borne (§17). Exactement le profil d'un
défaut qui survit à un test superficiel.

**Où les contraintes s'attachent.** Une contrainte dérivée d'une garde appartient au generator du
type propre du paramètre, *avant* toute conversion ou composition. Un paramètre `int?` gardé par
`p <= 0` émet `Any.Int32().Positive().As(value => (int?)value)`, pas l'inverse ; un paramètre de
fabrique gardé dans le corps de celle-ci émet
`Any.String().NonEmpty().As(OrderReference.Create)`. Le saut `.As` vient toujours en dernier, parce
que c'est l'étape qui change le type.

Chaque contrainte ci-dessus reste soumise à D4. `.Positive()` sur un paramètre `uint` ne se résout
pas (§14.3) et est ignorée.

La lecture des gardes est aussi ce qui rend la composition par fabrique correcte plutôt que
nominale : `OrderReference.Create` garde sur `IsNullOrWhiteSpace`, donc le tool émet
`Any.String().NonEmpty().As(OrderReference.Create)` — une chaîne qui fonctionne — au lieu de
`Any.String().As(OrderReference.Create)`, mesurée levant `AnyGenerationException` **594 fois sur
10 000 tirages**, et 557 lors d'une reprise indépendante — environ une fois sur dix-sept, ce que
prédit un tirage non contraint sur les dix-sept longueurs de 0 à 16 (§17).

Cette seule mesure est la raison d'être de cette section ; D5 + D6 en expose l'argument et les
alternatives pesées contre lui.

**Une instruction qui lève est une garde, quelle que soit sa forme.** La seule chose qu'un `throw`
placé avant la première affectation à l'état ne peut pas être, c'est de la logique ordinaire : il
refuse de construire l'objet. Donc là où l'ensemble reconnu n'a pas su analyser la forme qui le
porte — un bloc qui journalise avant de lever, une condition hors de l'ensemble clos, une branche
`else if` dont l'atteignabilité dépend d'une branche précédente qui ne lève pas
inconditionnellement — les paramètres que cette instruction nomme sont marqués `unread guards`,
comme une condition que l'ensemble ne reconnaît pas. Ces formes tombaient auparavant à côté de la
branche des gardes reconnues et n'étaient signalées d'aucune façon : `if (v < 0) { Log(v); throw …
}` se lisait exactement comme un paramètre que personne n'avait contraint. Un paramètre nommé
seulement dans le `nameof` du message du `throw` ne compte pas — cela nomme le paramètre rejeté à
l'intention d'un lecteur plutôt que de tester quoi que ce soit, et toute garde réelle de cette forme
nomme aussi son sujet dans la condition.

**Une instruction de tête n'a pas besoin non plus d'être un `if` pour compter.** Une garde entièrement
déléguée à un helper — `Ensure.NotBlank(value);`, appelé tel quel, sans aucun `if` dans le
constructeur — lève depuis l'intérieur d'un appel que l'ensemble clos ci-dessus n'analyse pas, et
passait donc inaperçue : le paramètre se lisait exactement comme un paramètre sans garde, et le
generator neutre qu'il gardait pouvait tirer une valeur que le helper rejette à chaque construction
réelle. Une instruction précédant la première affectation à l'état et qui confie le paramètre à un
tel appel est marquée `unread guards` elle aussi, comme une condition que l'ensemble ne reconnaît
pas — et `nameof(...)` en est exempté, puisqu'il nomme le paramètre pour un message plutôt que
d'appeler quoi que ce soit.

**Le résultat de l'appel doit être jeté**, et ce seul test constitue toute la règle. Un appel dont
la valeur est *utilisée* produit quelque chose — `_name = value.Trim()`, `_tags = tags.ToList()` —
et normaliser une valeur ou copier une collection ne dit rien sur les valeurs admissibles. Un appel
dont la valeur est jetée a été fait pour son effet, et le seul effet qu'un appel sur un paramètre de
constructeur puisse avoir avant la première affectation est de le rejeter.

Le test est structurel plutôt qu'une liste de noms sous lesquels un validateur est censé s'écrire :
un ensemble de préfixes bénis est une supposition sur l'intention qu'aucun lecteur ne pourrait
reproduire, exactement le genre de mécanisme qu'ADR-0046 refuse. Cela rend aussi le marquage
indépendant de l'ordre des instructions, ce qu'une règle comptant les résultats utilisés n'était
pas : deux paramètres normalisés sur des lignes consécutives se lisent pareil, quel que soit celui
affecté en premier, là où le parcours s'arrêtait après la première affectation et épargnait le
second. Le coût est le cas miroir, nommé au §9 : une garde-helper qui *retourne* la valeur
vérifiée — `_name = Ensure.NotBlank(value);` — se lit comme une production et échappe au filet.

**Les gardes que l'ensemble connaît déjà se lisent dans les deux orthographes.**
`ArgumentNullException.ThrowIfNull(value)` et `if (value is null) { throw … }` énoncent un seul
invariant, tout comme `ArgumentException.ThrowIfNullOrEmpty(value)` / `ThrowIfNullOrWhiteSpace(value)`
et les conditions `string.IsNullOr…` ci-dessus. Seule l'orthographe ancienne était lue, si bien que
la moderne tombait sous la règle des appels et bloquait le build du développeur — au sujet d'une
chaîne pourtant déjà exactement juste, puisqu'un contrôle de nullité n'ajoute rien (ADR-0064 ne tire
jamais null) et qu'un contrôle de vacuité est le `NonEmpty` propre à la ligne. Lire une garde que
l'ensemble comprend comme une garde qu'il n'a pas su lire, c'est le pire des deux résultats : rien
n'est resserré, et rien ne compile non plus. Le premier argument doit **être** le paramètre, selon la
même discipline d'identité du sujet que gardent les lignes de comparaison.

**Les helpers arithmétiques sont lus eux aussi.** `ArgumentOutOfRangeException.ThrowIfNegative`,
`ThrowIfNegativeOrZero`, `ThrowIfZero`, `ThrowIfLessThan`, `ThrowIfGreaterThan`,
`ThrowIfLessThanOrEqual` et `ThrowIfGreaterThanOrEqual` correspondent aux mêmes lignes numériques
qu'une comparaison (§5.3 ci-dessus) : `ThrowIfNegative(value)` lève sur `value < 0`, donc zéro est
admissible — `GreaterThanOrEqualTo(0)`, pas `Positive()` — tandis que `ThrowIfNegativeOrZero(value)`
lève sur `value <= 0`, ce qui *est* `Positive()`. Cela élargit l'ensemble clos plutôt que de
reconnaître une seconde orthographe de ce qui s'y trouvait déjà, selon le suivi d'ADR-0082. La même
discipline d'identité du sujet s'y applique, et le second argument d'un helper à deux arguments doit
être une constante à la compilation, comme l'autre côté d'une comparaison.

**Un helper n'est lu que là où rien ne décide s'il s'exécute.** L'orthographe en `if` a toujours exigé
que la branche lève inconditionnellement avant que sa condition soit lue ; l'orthographe en appel avait
besoin de la même question, posée à l'instruction qui porte l'appel, et ne l'avait pas.
`if (strict) { ThrowIfNegative(value); }` lisait `GreaterThanOrEqualTo(0)` sur un constructeur dont les
appelants `strict: false` construisent volontiers avec un négatif — plus étroit que le domaine réellement
admis, rapporté comme inféré, et **silencieux**, puisque chaque tirage compile et construit encore : rien
n'envoyait le développeur y regarder. Le `if` n'est que l'orthographe sous laquelle la chose a été
signalée : `switch (value) { case 0: ThrowIfNegative(value); }` exécute le helper précisément là où il ne
peut pas lever, et un `catch` sur un `try` vide ne s'exécute jamais, et pourtant tous deux resserraient
le tirage de la même façon.

Un appel n'est donc lu que là où chaque construction entre lui et l'instruction remise à la lecture
exécute son corps à chaque fois — un bloc simple, un `using`, un `lock`, un bloc `checked` ou
`unchecked`, un bloc `unsafe`, un `finally`. Tout le reste est marqué `unread guards` : une boucle peut
ne pas exécuter son corps du tout, un `switch` choisit une section parmi plusieurs, un `catch` ne
s'exécute que si quelque chose a levé, un `try` peut se tenir sous un gestionnaire capable d'avaler le
rejet même que la garde énonce, et un corps de lambda s'exécute là où on l'appelle et non là où il est
écrit. Un helper dans un `else` reste lu, car la branche au-dessus lève inconditionnellement et toute
construction qui aboutit est passée par cet `else` — le raisonnement même qui lit une chaîne d'`else if`
une branche à la fois. Le comportement par défaut est le côté sûr de cette question, comme interroger
*entier* est le côté sûr de la question des écritures : une construction non listée coûte une contrainte
et ne peut jamais en émettre une fausse, ce qui laisse la liste courte au lieu de prétendre être complète.

Le refus a un coût qui mérite d'être nommé, parce que de vrais constructeurs s'écrivent ainsi :
`if (nickname is not null) { ThrowIfNullOrWhiteSpace(nickname); }` — valider ce qui est présent — est
refusé comme toute autre condition, rien ne permettant au moteur de la distinguer d'un `if (strict)`.
Le generator qu'il garde est le plus souvent celui qu'il aurait écrit de toute façon, le `NonEmpty` de
la ligne elle-même : le coût est alors une confirmation, pas une contrainte.

**La question se pose le long du corps autant qu'en le remontant.** Ce qui entoure une garde n'est que
la moitié de ce qui peut décider qu'elle s'exécute : une instruction au-dessus d'elle peut la sauter, et
aucun ancêtre de la garde ne le montre. `if (lenient) { kept = value; return; } ThrowIfNegative(value);`
n'entoure la garde de rien du tout, et pourtant `new Subject(lenient: true, value: -5)` construit très
bien tandis que la lecture émettait `GreaterThanOrEqualTo(0)`, inféré, sans rien à regarder. Une
instruction de tête capable d'envoyer l'exécution au-delà de celles qui la suivent met donc fin à la
lecture de toutes les gardes en dessous d'elle, et dans **les deux orthographes** : le même `return`
au-dessus d'une garde écrite en `if` a été mesuré lisant tout aussi faussement, d'où la question posée
au balayage de tête plutôt qu'à la règle des appels.

Quelles instructions peuvent sauter est demandé à l'analyse de flot de contrôle du compilateur plutôt
qu'à l'arbre, ce qui répond d'un coup à trois choses dont une liste d'orthographes aurait dû être juste
sur chacune. `return` et `goto` sont couverts ensemble, avec les sauts que personne n'a nommés. Un
`return` intérieur à un lambda ou à une fonction locale quitte *ce* corps-là et non le constructeur :
un helper ordinaire déclaré parmi les instructions de tête — qui en porte presque toujours un — ne
coûte donc rien. Et un `throw` n'est pas un saut : il refuse de construire l'objet, ce qui fait de lui
une garde et non un moyen de la contourner, et le compter refuserait toute garde écrite sous une autre.
Un saut *en dessous* d'une garde ne peut pas la sauter, et la laisse lue. Il s'agit de
`SemanticModel.AnalyzeControlFlow`, la requête par région voisine de celle de flot de données sur
laquelle cette section s'appuie déjà, et non du graphe de flot de contrôle qu'ADR-0084 écarte pour le
placement — et là où elle refuse de répondre, l'instruction compte comme sautant : son défaut pointe
donc du même côté qu'interroger une construction entière.

Un résidu est nommé plutôt que poursuivi : une condition que le compilateur pourrait prouver constante —
`if (true) { ThrowIfNegative(value); }` — est refusée comme toute autre, ce qui coûte une confirmation
au développeur.

### 5.4 Composition

**Un generator scaffoldé l'emporte.** Si la compilation contient un type nommé `Any{T}` implémentant
`IAny<T>` avec un constructeur public sans paramètre, le moteur émet `new Any{T}()`. C'est ainsi que
les agrégats se composent en cascade, et cela fonctionne que ce type ait été scaffoldé plus tôt ou
écrit à la main.

**Sinon, une fabrique statique.** Une méthode qualifie si elle est `public static`, retourne le type
du paramètre, prend exactement un paramètre, et se nomme `Create`, `From`, `Of` ou `Parse`. Si
plusieurs qualifient, `Create` gagne ; s'il en reste plusieurs, le paramètre est non résolu et la
console nomme les candidates. L'émission est `<generator du paramètre de la fabrique>.As(T.Create)`,
avec le §5.3 appliqué au corps de la fabrique elle-même.

Convention, pas attribut, pas configuration : un attribut supposerait de toucher au code de
production du développeur pour plaire à un outil de test, et un fichier de configuration casserait
la règle de conception 2.

### 5.5 Paramètres non résolus

La fabrique propre au paramètre retourne un identifiant qui n'existe pas, exactement là où sa
recette se trouverait sinon — le point d'appel dans le constructeur public reste un simple nom,
comme pour tout autre paramètre :

```csharp
    public AnyOrder()
        : this(reference: ReferenceFactory(),
               customer:  CustomerFactory(),
               quantity:  QuantityFactory(),
               ...) { }

    private static IAny<OrderReference> ReferenceFactory() {
        return Any.String().NonEmpty().As(OrderReference.Create);
    }

    private static IAny<Customer> CustomerFactory() {
        // TODO(dum): no generator inferred for 'Customer customer'.
        //   Scaffold one:  dum generate Customer
        //   or write one here, or replace it and always pass .WithCustomer(...) instead.
        return TODO_supply_a_generator_for_customer;
    }

    // ... une fabrique par paramètre ...
```

Le fichier ne compile pas tant que le développeur n'a pas agi. C'est le but (D6). Le message du
compilateur lui-même — *« The name 'TODO_supply_a_generator_for_customer' does not exist in the
current context »* — est l'instruction, et il apparaît dans l'IDE, dans la liste d'erreurs et en
intégration continue, à la ligne propre à `CustomerFactory`.

Les deux alternatives ont été écartées : une expression `throw` compile et reporte l'échec au premier
run de test, et omettre le paramètre rend `AnyOrder` silencieusement inutilisable. Le développeur
lance le tool et ouvre le fichier dans la même minute ; un soulignement rouge à la ligne exacte lui
coûte dix secondes, un échec à l'exécution une semaine plus tard lui coûte bien davantage.

### 5.6 Paramètres à vérifier

Une garde que le moteur ne peut pas cautionner — non lue du tout, ou lue puis abandonnée sans
certitude que l'abandon soit sûr (§5.3, §9) — bloque la compilation de la même façon, à une
différence près : un generator **a bien été** inféré ici, et il reste comme base de travail de la
fabrique plutôt que d'être jeté.

```csharp
    private static IAny<string> NameFactory() {
        // TODO(dum): 'string name' may be guarded by something dum could not read (§9).
        //   This is dum's best generator for the type; verify it honours the real invariant,
        //   or replace it, then delete the line below.
        _ = TODO_verify_the_generator_for_name;

        return Any.String().NonEmpty();
    }
```

L'identifiant sur la ligne écartée n'existe pas, le build échoue donc à cette ligne exacte, comme
au §5.5 — l'affectation à `_` est ce qui évite qu'un second `CS0201` sans rapport ne brouille ce
que le développeur doit lire. Le `return` en dessous est réel : supprimer une seule ligne laisse
exactement ce que dum aurait écrit en silence sinon, à garder ou à remplacer.

Un generator qui compile et tire une valeur que le vrai constructeur rejette encore est un échec
pire que celui qui ne compile jamais : il passe la run d'aujourd'hui et échoue plus tard,
indiscernable d'un test flaky pour qui le rencontre — le développeur a fait confiance au scaffold,
l'a committé, et l'invariant qu'il a manqué en silence resurgit ailleurs (ADR-0046). Émettre la
recette neutre sans un mot se lisait comme si le tool avait jugé l'abandon de la garde sûr ;
bloquer la compilation dit clairement qu'il n'a rien décidé.

---

## 6. Sortie console

Le récapitulatif console n'est pas décoratif : c'est le mécanisme qui maintient le tool honnête sur
ce qu'il a inféré et ce qu'il a deviné.

L'exécution ci-dessous porte sur le même `Order` qu'au §4.1, mais *avant* que `AnyCustomer` ne soit
scaffoldé — d'où l'unique paramètre resté ouvert. Scaffolder `Customer` puis relancer avec `--force`
le referme, et ce deux-temps est la façon prévue de traverser un graphe d'agrégats.

La deuxième ligne nomme la construction que `Generate()` fera, et ce n'est pas toujours un
constructeur : un type construit par la règle de factory du §5.1 imprime cet appel à la place —
`factory Email.Create(string)` — puisque c'est celui que le fichier émis écrit.

```console
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

La colonne de droite porte la provenance de chaque expression : vide pour la table de base, `guard`
quand le §5.3 l'a resserrée, `factory` quand le §5.4 l'a composée, `AnyX` quand un generator
scaffoldé a été réutilisé, `guards not combined` pour le cas de conflit du §5.3, `no source` quand le
corps du constructeur était indisponible et qu'aucune garde n'a pu être lue, `unread guards` quand
une instruction de tête lève ou appelle d'une façon que l'ensemble reconnu n'a pas appariée, ou à un
endroit dont le moteur ne peut pas répondre — sous une écriture du paramètre, ou sous quelque chose qui
décide s'il s'exécute —, `constraint unavailable` quand
une garde a été lue et comprise et que ce generator ne porte aucun membre pour l'exprimer, et
`unavailable` quand le generator existe dans la bibliothèque mais pas dans l'asset que ce projet
résout.

`guard` se calcule depuis les contraintes **appliquées**, jamais depuis les contraintes lues. La
distinction fait toute la valeur de la colonne : une garde dont le generator n'a pas le membre est
abandonnée par D4 — à raison, puisque l'alternative est une chaîne qui ne compile pas — et la
signaler `guard` affirme un invariant que rien n'a honoré. `constraint unavailable` est ce que cet
abandon dit à la place, et ce n'est pas `unavailable` : là c'est le *generator* du type qui manque,
ici le generator est exactement le bon et c'est une contrainte qui ne peut pas s'y exprimer.

Cette dernière valeur compte plus qu'il n'y paraît. Sans elle, la dégradation de D4 est
indiscernable d'une simple ignorance du tool : un paramètre `DateOnly` sur un projet downlevel se
lirait « non inféré », alors que la vérité est « inféré, mais `Any.DateOnly()` n'existe pas ici —
change de cible, ou écris-le toi-même ». Un mot transforme une impasse en instruction.

Un paramètre à vérifier (§5.6) clôt le récapitulatif de la même façon qu'un paramètre ouvert — le
fichier ne compilera pas — mais compté séparément, comme *N* **to verify** plutôt que *N*
**TODO** : un generator y a bien été inféré, et le compte le dit. Sa ligne porte le même mot,
jamais `TODO`, puisque la ligne et la ligne de clôture décrivent le même paramètre :

```console
  customer   Customer   —                        TODO
  name       string     Any.String().NonEmpty()  to verify, unread guards

✓ AnyOrder.cs — 5 of 6 parameters inferred, 1 TODO, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.
```

Les deux comptes se lisent dans l'ordre où un développeur agit dessus : fournir l'un, vérifier
l'autre.

**La provenance est une donnée, pas une sortie.** Le moteur la retourne dans son modèle de résultat
(§10.3) ; la CLI la rend. C'est ce qui rend le récapitulatif testable sans console.

Un point d'entrée (§4.5) clôt le récapitulatif par une seconde ligne à lui, nommant l'écriture qu'il
vient de rendre possible — la même règle encore, puisque l'écriture vient du modèle de résultat au
lieu d'être assemblée par la console :

```console
✓ AnyOrder.cs       — 6 of 6 parameters inferred.
✓ AnyOrder.Entry.cs — entry point Dummies.Order()
```

`--dry-run` affiche le même récapitulatif sur stderr et le fichier sur stdout. Avec un point
d'entrée il y a deux fichiers, affichés dans l'ordre où ils seraient écrits, generator d'abord ;
aucun séparateur n'est inventé entre eux, car chacun s'ouvre sur les trois lignes d'en-tête du §4.3
qui le nomment.

### 6.1 Le rapport machine *(v1.1)*

`--format json` remplace le récapitulatif par **un unique document JSON sur stdout**, pour l'appelant
qui est un script plutôt qu'un lecteur. Décision :
[ADR-0071](../adr/0071-report-a-run-as-data-without-moving-the-exit-codes.fr.md).

Il existe parce que le code de sortie ne peut pas porter ce que le §7 a tranché. Un fichier écrit
avec des paramètres ouverts est un **succès** — le build du développeur signale le reste, ce qui est
tout l'ADR-0060 — et c'est juste pour une personne et inutile pour un script qui scaffolde quarante
types en une invocation : `0` se lit pareil que tous les paramètres aient résolu ou qu'un tiers
d'entre eux non. `summary.openParameters` est ce nombre manquant, et les lignes par paramètre disent
pourquoi il vaut ce qu'il vaut.

**Un paramètre à vérifier est compté à part, dans `summary.parametersToVerify`.** Ce n'est pas un
paramètre ouvert : il porte une expression, donc sa ligne indique `resolved: true`, et son fichier
ne compile toujours pas (§5.6). Fondre les deux en un seul nombre ferait diverger ce nombre des
lignes qu'il résume — un script qui somme les `resolved: false` et un script qui lit le compteur
répondraient différemment à propos d'un même document. Chaque ligne énonce les deux faits,
`resolved` et `requiresVerification`, si bien que le résumé se vérifie contre les lignes au lieu
d'être cru sur parole.

**Les codes de sortie ne bougent pas.** Le §7 est un contrat publié, et une exécution qui a écrit ses
fichiers sort toujours en `0` quoi que dise le rapport. Ceci ajoute un canal ; cela n'en redéfinit
aucun.

**stdout porte le document et rien d'autre.** Le récapitulatif y est supprimé, puisque la prose d'un
lecteur rendrait le document inanalysable. Tout ce qui est écrit pour une personne — les refus, les
diagnostics du projet, l'avis de `--dry-run` — continue d'aller sur stderr exactement comme sous
`human`, de sorte que `2>/dev/null` laisse un tuyau propre.

**Un document par exécution, sans exception à retenir.** Une exécution arrêtée avant son premier
scaffold — pas de projet, un projet qui ne charge pas, `--entry-point any` en deçà de C# 14 — en
produit un aussi, dont le `refusal` nomme lequel c'était. Un contrat qui n'écrit parfois rien oblige
un script à distinguer une sortie vide d'une analyse en échec.

**`--dry-run` met le texte de chaque fichier dans le document**, puisque stdout n'est plus libre de le
porter. `path` et `text` sont les deux moitiés d'une même question et ne sont jamais répondues
ensemble : un fichier écrit porte où il est allé, un fichier de `--dry-run` ce qu'il aurait été.

Les mots de provenance sont ceux du récapitulatif (§6), lus dans une seule table plutôt qu'épelés une
seconde fois — deux rendus d'un même ensemble de faits, ce qui les empêche de dériver vers deux
réponses.

---

## 7. Modes d'échec et codes de sortie

| Situation | Sortie | Comportement |
|---|---|---|
| Fichier écrit, tout inféré | `0` | — |
| Fichier écrit, un ou plusieurs TODO ou paramètres à vérifier | `0` | L'écriture a réussi ; le build du développeur signale le reste. |
| `--dry-run` | `0` | Rien n'est écrit. |
| Type introuvable / ambigu | `1` | Candidats listés. |
| Fichier de sortie existant, sans `--force` | `1` | Nomme le fichier, suggère `--force`, avertit que les éditions seront perdues. |
| Aucun / plusieurs projets trouvés | `1` | Candidats listés, `--project` suggéré. |
| Le projet ne charge pas ou n'est pas restauré | `1` | Le diagnostic MSBuild, tel quel. |
| Le projet ne référence pas JustDummies | `1` | Rien ne peut être résolu (D4) ; le dit et suggère le package. |
| Rien ne construit le type cible (§5.1) | `1` | Nomme ce dont `Generate()` a besoin : un constructeur d'instance public passant tous ses paramètres par valeur. |
| Le type cible est abstrait | `1` | Il a des constructeurs et ne peut pas être instancié ; suggère un type concret qui en dérive. |
| Le type cible est générique, ou imbriqué dans un générique | `1` | Rien n'en fournit l'argument de type, donc le fichier émis ne pourrait pas le nommer. |
| Les membres `required` du type cible ne sont pas assignés par le constructeur retenu | `1` | Reporté au §16 ; nomme `[SetsRequiredMembers]`, scaffoldé comme n'importe quel autre constructeur. |
| `--entry-point any`, projet en deçà de C# 14 | `1` | Nomme la version que le projet a résolue, et `static:<Name>`. |
| `--entry-point static:Any` | `2` | Nomme ce qui cesserait de compiler, et renvoie vers `--entry-point any`. |
| `--entry-point` reçoit une valeur hors des trois | `2` | Liste les trois. |
| `--entry-point-namespace` sans point d'entrée à placer | `2` | Dit quelle option manque. |
| `--format` reçoit une valeur qui n'est ni `human` ni `json` | `2` | Nomme les deux. |
| `dum.json` illisible, ou fixant une clé non lue | `2` | Nomme la clé, et celles qui sont lues. |
| `Any{Type}` masque un type `JustDummies.Any*` | `0` | **Avertissement**, puis génération. |

Cette dernière ligne mérite sa note, et le contrôle derrière est plus étroit qu'il n'y paraît. La
bibliothèque déclare 40 noms de types publics `Any*`, mais **8 sont génériques** — `AnyList<T>`,
`AnySet<T>`, `AnyArray<T>`, `AnySequence<T>`, `AnyDictionary<K,V>`, `AnyOneOf<T>`, `AnyEnum<T>`,
`AnyCollection<…>`. L'arité fait partie de l'identité d'un type en C#, donc un `AnySet` scaffoldé
(arité 0) et le `AnySet<T>` de la bibliothèque **coexistent sans rien masquer** — vérifié. Un type
métier nommé `Set`, `List` ou `Sequence` est une fausse alerte.

Le vrai ensemble de collision, ce sont les **32 noms non génériques** (§14.2) : `AnyString`,
`AnyGuid`, `AnyUri`, `AnyPattern`, `AnyChar`, `AnyBoolean`, `AnyDateTime`, `AnyContext`,
`AnyDecimal`, `AnyInt32`, … Un type métier nommé `Pattern`, `Context` ou `Uri` scaffolde vers un nom
qui, dans son propre namespace, **masque silencieusement le type de la bibliothèque** pour tous les
fichiers de ce namespace : C# résout le namespace englobant avant tout `using`. Cela compile ; c'est
simplement faux plus tard — vérifié. Le tool avertit, nomme les deux types, et génère quand même ;
sous la règle de conception 4 le renommage est l'affaire du développeur, et la v1.1 lui en donne le
levier.

Le contrôle doit donc comparer l'arité, pas seulement le nom. Avertir sur les 40 crierait au loup
sur les huit qui ne peuvent pas entrer en collision.

Les quatre lignes de point d'entrée se répartissent sur deux codes, et le partage est celui que le §7
trace déjà. Seule la première est un échec de scaffolding : le tool a lu la ligne de commande, ouvert
le projet, et constaté qu'il ne peut pas compiler ce qui lui est demandé. Les trois autres sont des
lignes de commande que le tool n'a jamais réussi à exécuter, ce qui est `2`. La première est en outre
posée **une fois par exécution** plutôt qu'une fois par type, puisque c'est un fait sur le projet —
`dum generate Order Customer Invoice` l'affiche une fois et s'arrête.

Un scaffold est une unité de travail sur le disque. Là où un point d'entrée a été demandé, l'existence
de son fichier est vérifiée en même temps que celle du generator avant que l'un ou l'autre ne soit
écrit, de sorte qu'un `Any{Type}.Entry.cs` déjà présent refuse le scaffold entier plutôt que de
laisser `Any{Type}.cs` derrière lui. `--force` porte sur les deux, et perd les éditions du développeur
sur l'un comme sur l'autre par la même phrase.

Plusieurs arguments de type (`dum generate Order Customer Invoice`) sont traités indépendamment ; le
code de sortie est le pire d'entre eux, et un échec n'empêche pas l'écriture des autres.

---

## 8. Garanties

### 8.1 Déterminisme

Le même type analysé contre la même compilation produit un fichier **identique à l'octet près**, sur
n'importe quelle machine, sous n'importe quelle version du tool qui résout les mêmes membres. Rien
qui dépende du temps, du chemin, de la culture ou d'un ordre de hachage n'entre dans la sortie :
aucun horodatage, aucune version de tool, aucun chemin absolu, et toute énumération parcourue par
l'émetteur est ordonnée par déclaration.

Cela compte même sans verbe `check` : c'est ce qui rend un nouveau scaffold relisible comme un diff.

### 8.2 Reproductibilité

Le generator émis tire du contexte **ambiant**, parce que toutes les expressions qu'il émet viennent
de la façade statique `Any`, et que la source ambiante résout la frame `AsyncLocal` courante **au
moment du tirage**, pas à la construction (§14.5). Donc :

```csharp
AnyOrder recipe = new AnyOrder();          // construit hors du scope
Any.Reproducibly(() => {
    Order order = recipe.Generate();       // toujours épinglé par le seed du scope
});
```

est reproductible, tout comme le cas ordinaire où les deux se produisent dans le scope. Cela a été
vérifié (§17).

**`Any.WithSeed(seed)` est hors périmètre (D7).** Un `AnyContext` porte sa propre source aléatoire
fixe et n'est pas affecté par le scope ambiant, donc un generator construit à partir de `Any.*` ne
peut pas y tirer. Un développeur sur `WithSeed` fournit les generators de ce contexte paramètre par
paramètre via la surcharge `.With{Param}(IAny<TParam>)`, et la doc XML émise le dit en une phrase
(§4.1). Le raisonnement, et les alternatives pesées contre lui, sont dans D7.

L'émetteur ne produit jamais d'état statique, donc `JD009` et `JD020` n'ont rien sur quoi se
déclencher.

### 8.3 Aucune réflexion dans le code émis

Le fichier émis ne contient aucune réflexion — ce sont des appels de constructeur et des chaînes
fluides. La revendication *« no reflection »* de la bibliothèque porte sur ce qui s'exécute dans le
test du développeur, et elle tient.

Le **tool lui-même** est un programme de build et n'est soumis à aucune contrainte de ce genre ; il
utilise Roslyn, qui n'est de toute façon pas de la réflexion. Les deux questions sont indépendantes.

---

## 9. Non-objectifs de la v1.0

Nommés explicitement pour ne pas être pris pour des oublis.

* **Données réalistes.** Le tool hérite du périmètre de la bibliothèque : arbitraire-mais-valide,
  jamais plausible. Ni noms, ni emails, ni adresses.
* **Remplissage automatique de graphe d'objets.** La composition est d'un saut, via `Any{T}` ou une
  fabrique à un paramètre, limitée à une profondeur de 3. Au-delà, le développeur l'écrit.
* **Les invariants que le tool ne peut pas voir.** Le §5.3 lit un ensemble clos d'idiomes de garde.
  Là où le constructeur lève d'une façon que l'ensemble n'apparie pas — règle inter-paramètres,
  condition arithmétique, garde regex (§5.3) — le paramètre garde le generator neutre, le
  récapitulatif le marque `unread guards`, et **le fichier ne compile pas tant que le développeur
  n'a pas dit que le generator est le bon** (§5.6) : la recette est bien écrite, sous une ligne
  nommant un identifiant qui n'existe pas. Le même marquage atteint une garde entièrement déléguée à
  un helper appelé sur le paramètre lui-même (`Guard.Against.Null(value)`), même sans aucun `if`
  dans le corps (§5.3), dès lors que l'appel est fait pour son seul effet ; il atteint aussi toute
  instruction qui lève dans une forme que l'ensemble n'a pas su analyser du tout ; et il atteint une
  garde que le moteur ne peut pas placer au-dessus de toute écriture de son propre paramètre, et qui
  énoncerait sinon un invariant de la valeur calculée par le constructeur et non de celle que le
  generator tire ; et il atteint une garde dont le moteur ne peut pas montrer qu'elle s'exécute sur
  tout chemin — parce que quelque chose l'entoure qui en décide, ou parce qu'une instruction au-dessus
  d'elle peut la sauter — et qui énoncerait sinon un invariant des chemins qui l'atteignent et non du
  paramètre (§5.3). Deux formes lui échappent encore, et toutes deux sont silencieuses plutôt que simplement
  non lues — le tool n'y voit aucun rejet dont douter. Une garde-helper qui **retourne** la valeur
  vérifiée — `_name = Ensure.NotBlank(value);` — est indiscernable d'une normalisation, et la lire
  comme un doute reviendrait à lire `_name = value.Trim();` comme un doute aussi, ce qui bloque la
  compilation de constructeurs ne portant aucune garde. Et une garde atteinte seulement par un
  niveau d'indirection que le tool ne suit pas — une copie locale du paramètre (`var v = value;
  Validate(v);`), un lambda qui le capture, un appel atteint via un membre plutôt que par le nom
  propre du paramètre. Dans les deux cas, le tool ne peut toujours pas distinguer ce paramètre d'un
  paramètre non contraint, et il ne devine pas — c'est de ce résidu que parle ce non-objectif : non
  pas ce qui arrive une fois le doute établi, mais le doute que le tool ne voit jamais.
* **L'aller-retour.** Le tool ne relit jamais un fichier qu'il a écrit.
* **Membres `init` / `required`, construction par propriétés.** Constructeur et fabrique statique
  uniquement.
* **Tout ce qui relève de `--all`.** Arguments de type explicites seulement.

---

## 10. Architecture

### 10.1 Deux projets

| Projet | TFM | Rôle |
|---|---|---|
| `JustDummies.GenAny` | `netstandard2.0`, épinglé au plancher Roslyn (§13.2) | Le moteur. Résolution, lecture des gardes, composition, émission. |
| `JustDummies.Cli` | `net8.0`, `RollForward=Major` | La coquille. Commandes, chargement du projet, IO fichier, console. |

Sur le nom : le moteur existant du tool frère de ce dépôt s'appelle `GenDoc` — un nom de
**fonction**, pas un nom de pattern (`GenDoc` génère de la documentation). `GenAny` le suit
exactement : il génère les types `AnyX`, et `Any` est le nom central de la bibliothèque
(`Any.String()`, `IAny<T>`, `AnyOrder`). « Scaffolder » a été écarté comme nom de projet — il
nomme un rôle générique plutôt qu'un produit, et tous les frameworks en ont un. Le mot survit dans
la prose, où il décrit un **comportement** (§1) ; le projet est nommé d'après ce qu'il **produit**.

### 10.2 La frontière

**`JustDummies.GenAny` possède** la table de résolution (§5.2), la lecture des gardes (§5.3), la
composition et la reconnaissance de fabriques (§5.4), l'émetteur (§11.2) et la fonction de nommage
(§11.3).
Il dépend de `Microsoft.CodeAnalysis.CSharp` **uniquement** — pas de `Workspaces`, dont il n'a pas
besoin : la lecture des gardes veut un arbre syntaxique et un modèle sémantique, et l'émission est
de la construction de chaînes.

**Il ne fait aucune IO, n'écrit sur aucune console, et ne touche jamais MSBuild.** Ces trois
contraintes sont ce qui le maintient chargeable dans un hôte Roslyn.

**`JustDummies.Cli` possède** les définitions de commandes et de settings Spectre, la découverte de
projet, `MSBuildLocator` / `MSBuildWorkspace`, l'écriture de fichiers, la gestion de `--force` /
`--dry-run`, le rendu du récapitulatif console, et les codes de sortie du §7.

### 10.3 Le contrat entre les deux

Un point d'entrée, taillé pour que le futur consommateur IDE puisse l'appeler tel quel :

* **Entrée** — une `Compilation`, l'`ITypeSymbol` cible, et un enregistrement d'options portant la
  surcharge de namespace et le motif de nommage du type (§16).
* **Sortie** — un modèle de résultat, jamais une chaîne nue :
  * le nom de fichier et le texte source complet ;
  * une ligne par paramètre : nom, type affiché, expression émise (ou aucune), et provenance (§6) ;
  * les avertissements, comme le cas de masquage `Any*` du §7 ;
  * un indicateur « contient au moins un TODO » ;
  * **l'échec comme donnée, pas comme exception** — un type cible qui ne résout vers rien ou vers
    plusieurs candidats revient comme un résultat portant cette liste de candidats, de sorte que la
    CLI le projette sur les codes de sortie du §7 sans rien attraper. Le §11.1 place la résolution
    du type dans le moteur, donc le modèle doit porter cela ou la frontière fuit des exceptions.

La CLI rend ce modèle ; un code refactoring appliquerait le texte source et ignorerait le reste.
Rien dans le modèle n'est une chaîne destinée à une console.

### 10.4 Packaging

`JustDummies.Cli` est packagé comme le .NET tool (`PackAsTool`, `ToolCommandName=dum`,
`PackageId=JustDummies.Cli`). `JustDummies.GenAny` n'est **pas publié comme package propre** en
v1.0 : il voyage dans le package du tool comme dépendance managée ordinaire, exactement comme le
dépôt frère publie son moteur `GenDoc`. Le publier plus tard, quand un consommateur IDE existera,
est une décision purement additive.

Conséquence : aucun des deux projets ne porte de promesse de compatibilité d'API publique, donc
aucun ne prend de baseline d'API publique (§13.4).

**D9 s'applique aux deux.** Aucun des deux ne référence le package ni le projet `JustDummies`.
Chaque symbole JustDummies est résolu par nom de métadonnée contre la compilation du développeur,
exactement comme le font les analyzers de la bibliothèque. L'écart de version entre le tool et la
bibliothèque est donc structurellement impossible, et le package du tool ne doit déclarer aucune
dépendance `JustDummies` (§13.6).

---

## 11. Notes d'implémentation

### 11.1 Chaîne de traitement

1. `MSBuildLocator.RegisterDefaults()` — **avant de toucher au moindre type de workspace Roslyn**.
   Charger `MSBuildWorkspace` d'abord est la façon classique dont cela échoue, avec une
   `FileNotFoundException` sur `Microsoft.Build` qui ne nomme rien d'utile. (CLI uniquement.)
2. `MSBuildWorkspace.Create()`, ouvrir le projet, prendre sa `Compilation`. Les diagnostics du
   workspace sont remontés, pas avalés. (CLI uniquement.)
3. Passer la `Compilation` au moteur. Tout ce qui suit est `JustDummies.GenAny`.
4. Résoudre `JustDummies.Any`, ``JustDummies.IAny`1`` et `JustDummies.AnyExtensions` par nom de
   métadonnée. Absents → le moteur le signale et la CLI sort en `1` (§7).
5. Résoudre le type cible (§3.2), choisir le constructeur (§5.1).
6. Par paramètre : table de base (§5.2) → gardes (§5.3) → composition (§5.4) → non résolu (§5.5).
   Tout membre candidat est cherché dans la compilation avant d'être retenu (D4).
7. Émettre dans le modèle de résultat (§10.3).
8. La CLI écrit le fichier et rend le récapitulatif.

### 11.2 Émetteur

Un simple constructeur de chaînes sur un modèle ordonné, pas `SyntaxFactory`. La sortie doit être
lisible et correspondre à une mise en page écrite à la main — déclarations de champs alignées, types
explicites, accolades — et l'espacement normalisé par `SyntaxFactory` ne produit pas cela.
L'émetteur étant couvert par des tests à fichier de référence (§12), l'argument de fragilité en
faveur d'une API syntaxique ne s'applique pas.

### 11.3 Nommage

Faire passer le nom du type émis par **une seule** fonction,
`TypeNaming.GeneratorNameFor(ITypeSymbol, NamingOptions)`. La v1.1 (§16) devient alors une
modification de cette fonction plus une liaison d'options, pas un balayage. En v1.0
`NamingOptions` ne porte qu'un motif fixe, `Any{Type}`.

---

## 12. Plan de tests

**Moteur — `JustDummies.GenAny.UnitTests`** (le gros) :

* **Tests unitaires du résolveur.** Construire une `CSharpCompilation` en mémoire avec une référence
  sur le `JustDummies.dll` construit, et asserter la chaîne d'expression émise par paramètre.
  Rapide, sans MSBuild. Couvrir chaque ligne du §5.2, chaque ligne du §5.3, les deux chemins du
  §5.4 et le repli du §5.5. Inclure le cas non signé (`p <= 0` sur un `uint`), le cas nullable de
  type valeur, les issues de composition du §5.3 (bornes complémentaires conservées et repliées en
  intervalle, même côté replié sur la plus serrée, bornes n'admettant aucune valeur abandonnées, un
  raffinement cédant devant une garde), une garde de taille sur un paramètre **collection** (qui doit atteindre
  `WithMaxCount`, jamais `WithMaxLength`), et `p < 1` sur un paramètre intégral puis sur un
  `decimal` — les deux lignes qui ne diffèrent que par le type du paramètre. Ajouter un cas
  négatif : un constructeur gardé par `!Regex.IsMatch(...)` ne doit produire **aucune** contrainte
  de motif, pour que l'exclusion du §5.3 ne puisse pas être défaite par inadvertance.
* **Fichiers de référence de l'émetteur.** Un fichier approuvé par forme représentative : aucun
  paramètre, un paramètre, six paramètres, un TODO, une collision de nom, un record positionnel,
  une cible à fabrique statique. Le fichier sans paramètre épingle la forme dégénérée du §4.2 —
  émettre les deux constructeurs sans condition y donne un `CS0111`. Le fichier de collision doit
  utiliser un nom de bibliothèque **non générique** (`Pattern`, `Context`, `Uri`), puisqu'un nom
  générique ne peut pas entrer en collision (§7). Le fichier de point d'entrée du §4.5 en ajoute
  quatre : une racine statique, un membre d'extension, une racine déplacée dans un namespace à elle
  (le seul cas qui ouvre un `using`), et le namespace global, qui n'a aucune déclaration à copier.
  Ils sont compilés **avec le generator qu'ils atteignent**, puisque seul n'est un état dans lequel
  ni l'un ni l'autre ne se trouve jamais, et le plancher de langage est asserté des deux côtés : le
  membre d'extension doit échouer en deçà de C# 14, et la racine statique doit parser en C# 7.3.
* **Tests de compilation de la sortie.** Chaque fichier de référence est compilé contre
  `JustDummies.dll` **avec les analyzers JustDummies branchés**, et la compilation ne doit produire
  aucune erreur `CS*` ni aucun diagnostic `JD*` **de niveau avertissement ou au-dessus**. C'est le
  contrôle que D3 rend possible : le fichier n'étant pas marqué comme code généré, les analyzers
  tournent réellement dessus. Les règles informationnelles sont exclues à dessein : le tool remet le
  fichier au développeur (ADR-0056), si bien que `JD030` nommant une longueur que la chaîne émise
  laisse indéclarée est cette règle qui fait son travail sur un fichier dont l'auteur n'est pas
  encore arrivé — un point de départ, pas un défaut de ce qui a été émis. Un avertissement, lui, dit
  que le code émis est faux en propre, et c'est ce que ce contrôle prend en charge. Le harnais doit
  inclure un **fichier de contrôle avec une violation connue**, dont on asserte qu'elle se
  déclenche — sinon « aucun diagnostic » ne se distingue pas de « analyzers non chargés » (§17.2).
* **Le test sur le corpus gardé.** Le contrôle de compilation ci-dessus lit des fichiers golden, et
  tout paramètre de golden est sans garde ou gardé sur la vacuité — aucun fichier approuvé n'a donc
  jamais porté de paire de bornes, de compte au-delà des membres d'un enum, de taille au-dessus du
  plafond de production ni de signe contre une borne opposée, c'est-à-dire toute la composition du
  §5.3. Un corpus de **types de domaine gardés** est donc mené à travers le moteur et soumis à trois
  oracles : le fichier émis **compile**, il ne lève **aucun `JD*` de niveau avertissement ou
  au-dessus**, et le generator **se construit et tire** des valeurs que son propre domaine accepte.
  Le troisième est celui que les deux autres ne peuvent pas remplacer — une chaîne peut être légale,
  déclarable, silencieuse sous toutes les règles, et dire tout de même autre chose que les gardes, et
  seul le constructeur du domaine en décide. Une forme dont aucun generator ne peut satisfaire le
  domaine — une contradiction écrite par le développeur, une borne au-delà du plafond, un set voulant
  plus de valeurs distinctes que n'en porte sa ligne d'élément — répond à un quatrième : elle doit
  tout de même se construire, tout de même ne lever aucune règle, et le récapitulatif doit porter le
  refus.
* **Les règles informationnelles sont excusées nommément, jamais par sévérité.** L'exclusion en bloc
  ci-dessus est juste pour le contrôle auquel elle appartient et fausse comme règle générale, parce
  qu'elle raisonne sur l'auteur du fichier plutôt que sur la règle. `JD030` nomme une longueur que le
  domaine n'a jamais énoncée et le moteur n'en inventera pas une pour la faire taire — c'est un fait
  sur un fichier dont l'auteur n'est pas arrivé. `JD031` et `JD024` signalent ce que le **moteur a
  choisi** : deux bornes là où il voulait dire un intervalle, une contrainte qui ne resserre rien. Un
  scaffold sait ce qu'il voulait écrire, donc un diagnostic informationnel sur du code émis est une
  relecture de cette intention plutôt qu'un verdict sur elle — et un choix que le moteur ne peut pas
  défendre est un choix qu'il n'aurait pas dû faire. Le corpus nomme donc les règles informationnelles
  qu'il assume, et toute autre échoue jusqu'à ce que quelqu'un tranche.
* **Le test sur le code du dépôt.** Scaffolder les **vrais types du dépôt hôte**, compiler les
  résultats et générer une valeur depuis chacun. Le raisonnement est consigné dans la décision
  « faire tourner les analyzers sur notre propre code » (§13.7) : une règle et le snippet qui la
  teste, écrits par le même auteur, partagent la même idée fausse et passent ensemble ; du code
  écrit pour d'autres raisons, non. `ErrorCode.Create` du dépôt actuel est le cas canonique — il
  garde sur `IsNullOrWhiteSpace`, donc sans le §5.3 le code scaffoldé échoue environ une fois sur
  seize, ce qu'aucun fichier de référence ne révélerait. Dans un dépôt dépourvu de tels types,
  prendre n'importe quel value object validant doté d'une fabrique statique.
* **Test de sélection d'asset.** Scaffolder contre un consommateur de l'asset `netstandard2.0` et un
  consommateur de l'asset `net8.0` pour un type ayant un paramètre `DateOnly`, et asserter que le
  premier produit un TODO **marqué `unavailable`** — pas seulement un TODO — et le second
  `Any.DateOnly()`. C'est la preuve exécutable de D4 (§13.8).

**Coquille — `JustDummies.Cli.UnitTests`** : découverte de projet, gestion des options, codes de
sortie du §7, et rendu du récapitulatif depuis un modèle de résultat figé.

---

## 13. Ce que le dépôt hôte doit fournir

JustDummies a vocation à rejoindre son propre dépôt avant que ce tool soit construit. Cette section
énonce chaque dépendance envers l'hôte comme une **exigence**, avec la réalisation actuelle en
exemple. Si la bibliothèque a déménagé, rétablir tout cela là-bas ; ne pas construire le tool contre
l'infrastructure d'un autre dépôt.

### 13.1 Versions de packages épinglées

Pour les dépendances du tool. Nouvelles pour le tool :
`Microsoft.CodeAnalysis.Workspaces.MSBuild` et `Microsoft.Build.Locator` (CLI uniquement). Déjà
présente pour la bibliothèque et ses analyzers : `Microsoft.CodeAnalysis.CSharp`. *Réalisation
actuelle : gestion centralisée des packages dans `Directory.Packages.props`.*

Il en a fallu deux de plus que ce que cette section listait, et pour des raisons qui méritent d'être
écrites. `Microsoft.CodeAnalysis.CSharp.Workspaces`, parce que construire une compilation est un
service **de langage** et que `Workspaces.MSBuild` ne porte que les services indépendants du
langage : sans elle, le projet s'ouvre, ne signale aucune erreur, et répond `null` quand on lui
demande sa compilation. `Microsoft.Build.Framework`, avec `ExcludeAssets="runtime"` et
`PrivateAssets="all"`, parce que `Workspaces.MSBuild` en déposerait sinon une copie à côté de
l'outil — ce que `Microsoft.Build.Locator` refuse par construction (`MSBL001`), un outil qui charge
son propre assembly MSBuild au lieu de celui du SDK échouant à l'exécution d'une manière qui ne
nomme rien.

`Spectre.Console.Cli` n'était **pas** déjà présente, contrairement à ce que cette section supposait
lorsqu'elle a été écrite dans le dépôt d'origine : l'extraction l'a retirée, comme tout ce qu'aucun
projet ne référençait. Elle est revenue avec `JustDummies.Cli`, seul projet autorisé à la porter — la
§10.2 place les définitions de commandes dans la coquille et les interdit au moteur.

### 13.2 Une propriété de plancher Roslyn

`JustDummies.GenAny` doit compiler contre la **même
version minimale de Roslyn que le package d'analyzers**, et ne pas flotter au-dessus — un assembly
chargé par le compilateur d'un consommateur échoue silencieusement (`CS8032`) sur un hôte plus
ancien s'il a été construit contre un Roslyn plus récent. *Réalisation actuelle :
`RoslynFloorVersion` = `4.8.0`, posée une fois dans `Directory.Build.props` et appliquée avec
`VersionOverride`.* La CLI n'est **pas** liée par cela : elle héberge son propre compilateur.

### 13.3 Imbrication dans la solution

Si l'hôte utilise un `.sln`, ajouter les deux projets et les
deux projets de test à son `GlobalSection(NestedProjects)`, sous les dossiers de solution source et
tests. Un projet absent de cette section apparaît en vrac à la racine de la solution au lieu d'être
groupé avec ses frères. Cela a été manqué puis corrigé après coup à plusieurs reprises ; le vérifier
à chaque ajout de `.csproj`.

### 13.4 Exclusion de la baseline d'API publique

Ni `JustDummies.GenAny` ni `JustDummies.Cli`
n'adhèrent à la baseline d'API publique : les outils ne portent aucune promesse de compatibilité, et
l'analyzer signalerait toute leur surface comme non déclarée. *Réalisation actuelle : seules les
bibliothèques publiées importent `build/PublicApiBaseline.props`.*

### 13.5 Tests de mutation

Si l'hôte mesure la mutation sur les projets dont le code est publié ou
s'exécute, les deux projets qualifient. Donner à chacun sa propre configuration — le moteur est la
cible à forte valeur, la coquille non — et les enregistrer avec les autres. *Réalisation actuelle :
un JSON par projet sous `build/stryker/`, piloté par un flux dédié, consultatif par pull request et
imposé par un balayage hebdomadaire.*

### 13.6 Un train de publication pour le tool

Distinct de celui de la bibliothèque. Le tool ne
versionne pas en lockstep avec la bibliothèque (D9), donc il ne doit pas monter sur son train.
L'étape de packaging du train doit asserter que le `.nupkg` produit ne déclare **aucune dépendance
`JustDummies`** — la forme exécutable de D9. *Réalisation actuelle : `tools/packaging/pack.sh` avec
un train par famille de packages, et l'assertion propre au train `cli` à côté de celle de la
bibliothèque.*

Cette assertion a demandé une seconde moitié que cette section n'avait pas anticipée. Un outil .NET
embarque toute sa clôture de dépendances sous forme de **fichiers** dans `tools/<tfm>/any/`, si bien
que son nuspec ne déclare rien du tout : le contrôle des dépendances déclarées passe sur une liste
vide et ne prouve rien, pendant qu'un `JustDummies.dll` ajouté par un `ProjectReference` malencontreux
resterait dans la charge utile sans être vu. Le packaging asserte donc les deux : aucune dépendance
`JustDummies` dans le nuspec, et aucun `JustDummies.dll` dans le paquet. Mesuré, pas supposé —
ajouter la référence fait échouer le packaging sur le second contrôle et passer le premier.

### 13.7 Les analyzers doivent pouvoir tourner sur le code de l'hôte

Pour que le test sur le code
du dépôt (§12) puisse exister. *Réalisation actuelle : le projet d'analyzers est branché sur les
suites du dépôt lui-même, décision prise après avoir constaté que la suite unitaire des analyzers
n'attrapait pas cinq règles fausses que le passage sur du vrai code a attrapées immédiatement.*

### 13.8 Deux TFM consommateurs pour la bibliothèque packagée

Pour que le
test de sélection d'asset (§12) puisse exister : un consommateur en `net8.0` (résout l'asset
`net8.0`) et un en dessous (résout `netstandard2.0`). *Réalisation actuelle : un projet isolé hors
solution, multi-ciblé, consommant le `.nupkg` packagé depuis un flux local.*

### 13.9 Framework de tests

*Réalisation actuelle : `xunit.v3`, `NFluent`, `Verify.XunitV3` pour
les fichiers de référence, `NSubstitute`.* Tout équivalent convient ; les tests à fichier de
référence ont besoin d'une bibliothèque de snapshots.

### 13.10 Conventions de commit, de branche et de pull request

Et un processus ADR pour le §15.
*Réalisation actuelle : Conventional Commits avec une liste close de types et de scopes, imposée par
un hook et par la CI ; ADR sous `doc/handwritten/for-maintainers/adr/` où un agent rédige en
`Proposed` et le mainteneur accepte.*

---

## 14. Faits sur la bibliothèque dont dépend cette spécification

Tout ce qui suit a été lu dans la source de la bibliothèque. C'est inliné pour que ce document
puisse être implémenté sans ouvrir la bibliothèque, et pour qu'un lecteur futur puisse repérer
quelles affirmations sont porteuses. Le §14.7 donne la commande pour redériver chaque bloc.

### 14.1 Identité du package et frameworks cibles

* `PackageId` **`JustDummies`**, `TargetFrameworks` **`netstandard2.0;net8.0`**, `Nullable` activé,
  `LangVersion` latest.
* Les deux assets divergent : la branche `net8.0` porte en plus `DateOnly`, `TimeOnly`, `Int128`,
  `UInt128` et `Half`, gardés par `#if NET8_0_OR_GREATER`. Un consommateur sous `net8.0` résout
  l'asset `netstandard2.0` et ne les voit pas. C'est le fait que D4 existe pour absorber.
* Les analyzers voyagent **dans** ce package, sous `analyzers/dotnet/cs`, donc tout consommateur les
  reçoit automatiquement. C'est pour cela que le fichier émis est analysé du tout (D3).
* Un package compagnon adapte la bibliothèque à xUnit v3 (`[Reproducible]`) ; le tool n'interagit
  pas avec lui.

### 14.2 Points d'entrée

`JustDummies.Any` est une façade statique, répartie en fichiers partiels par famille. L'ensemble
complet des fabriques, toutes tirant du contexte aléatoire ambiant :

* **Primitifs** — `String()`, `Boolean()`, `Char()`, `Guid()`,
  `SByte()`, `Byte()`, `Int16()`, `UInt16()`, `Int32()`, `UInt32()`, `Int64()`, `UInt64()`,
  `Single()`, `Double()`, `Decimal()`,
  `TimeSpan()`, `DateTime()`, `DateTimeOffset()`,
  `Enum<TEnum>() where TEnum : struct, Enum`.
* **Asset `net8.0` uniquement** — `DateOnly()`, `TimeOnly()`, `Int128()`, `UInt128()`, `Half()`.
* **Motif** — `StringMatching(string)`, `StringMatching(Regex)`.
* **URI** — `Uri()`, puis un sélecteur de famille : `.Web()`, `.Ftp()`, `.Mailto()`, `.Relative()`,
  `.WebSocket()`.
* **Choix** — `OneOf<T>(params T[])`, `ElementOf<T>(IReadOnlyList<T>)`,
  `ElementOf<T>(IEnumerable<T>)`.
* **Collections** — `ListOf<T>`, `ArrayOf<T>`, `SequenceOf<T>`, `SetOf<T>` (avec comparateur
  optionnel), `DictionaryOf<TKey,TValue>` (avec comparateur de clés optionnel).
* **Composition** — `Combine` en arités 2 à 8, `PairOf`, `TripleOf`.
* **Reproductibilité** — `WithSeed(int)`, `UseSeed(int)`, `UseSeed(int, string)`,
  `Reproducibly(...)`, `ReproduciblyAsync(...)`.

Attention aux pièges de nommage : c'est **`Any.Boolean()`**, pas `Any.Bool()` ; et `double` se
projette sur **`Any.Double()`**, pas `Any.Decimal()`.

`AnyContext`, retourné par `Any.WithSeed(int)`, reflète les primitifs, le motif, l'URI et les points
d'entrée de choix comme méthodes **d'instance** tirant de sa propre source fixe. Il ne reflète
**pas** les points d'entrée de collection ni de composition. D7 le met hors périmètre.

La bibliothèque déclare **40 noms de types publics `Any*`** — 38 generators plus `AnyContext` et
`AnyGenerationException`. **8 sont génériques et 32 ne le sont pas**, et seuls les non génériques
peuvent être masqués par un `Any{Type}` scaffoldé ; c'est cet ensemble de 32 noms que
l'avertissement du §7 interroge. (`AnyCollection<…>`, la base abstraite des generators de
collection, est facile à manquer au comptage : elle est déclarée `public abstract class`, pas
`public sealed class`.)

### 14.3 Surfaces de contraintes utilisées par l'émetteur

| Famille de generator | Surface de contraintes disponible pour l'émetteur |
|---|---|
| `AnyString` | `NonEmpty`, `WithMinLength`, `WithMaxLength`, `WithLength`, `WithLengthBetween`, `StartingWith`, `EndingWith`, `Containing`, `Alpha`, `Numeric`, `AlphaNumeric`, `Punctuation`, `Printable`, `InUpperCase`, `InLowerCase`, `WithChars`, `OneOf`, `Except`, `DifferentFrom` |
| Entiers signés (`SByte`, `Int16`, `Int32`, `Int64`) | `Positive`, `Negative`, `NonZero`, `Zero`, `Between`, `GreaterThan(OrEqualTo)`, `LessThan(OrEqualTo)`, `MultipleOf`, `OneOf`, `Except`, `DifferentFrom` |
| **Entiers non signés** (`Byte`, `UInt16`, `UInt32`, `UInt64`) | les mêmes **moins `Positive` et `Negative`**, qu'un type non signé ne peut pas exprimer |
| `AnyDouble`, `AnySingle` | comme les entiers signés, moins `MultipleOf` |
| `AnyDecimal` | comme les entiers signés, moins `MultipleOf`, plus `WithScale` |
| `AnyGuid` | `NonEmpty`, `Empty`, `OneOf`, `Except`, `DifferentFrom` |
| `AnyBoolean` | `True`, `False`, `DifferentFrom` |
| `AnyEnum` | `AllowingCombinations`, `OneOf`, `Except`, `DifferentFrom` |
| Temporels (`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`) | `After(OrEqualTo)`, `Before(OrEqualTo)`, `Between`, `WithGranularity`, `OneOf`, `Except`, `DifferentFrom` |
| `AnyTimeSpan` | style temporel plus `Positive`, `Negative`, `NonZero`, `Zero` |
| Collections | `Empty`, `NonEmpty`, `WithCount`, `WithCountBetween`, `WithMinCount`, `WithMaxCount`, `Containing`, `ContainingAny` |

Deux lignes mordent. La ligne **non signée** est pourquoi D4 doit filtrer `.Positive()` plutôt que
laisser l'émetteur supposer une algèbre numérique uniforme. La ligne **collections** est pourquoi
une garde de taille doit atteindre la famille `Count` : il n'y a pas de `WithLength` sur un
generator de collection, donc lire une telle garde contre la famille des chaînes émet un membre qui
ne se résout jamais (§5.3).

La v1.0 n'utilise que les contraintes de taille, de signe et de borne. Les familles jeu de
caractères et motif sont listées parce que le §16 pourrait y revenir, pas parce que l'émetteur s'en
sert aujourd'hui.

### 14.4 Seams de composition

* `AnyExtensions.As<TSource,TResult>(this IAny<TSource>, Func<TSource,TResult>)` → `IAny<TResult>`.
  Un groupe de méthodes comme `OrderReference.Create` s'y lie directement. Quand la fabrique rejette
  la valeur générée, l'appel lève `AnyGenerationException`.
* `Any.Combine` (arités 2 à 8) → `IAny<TResult>`.
* Les generators de collection dérivent d'une base commune implémentant `IAny<TCollection>` :
  `ListOf` → `List<T>`, `ArrayOf` → `T[]`, `SequenceOf` → `IEnumerable<T>`, `SetOf` → `HashSet<T>`,
  `DictionaryOf` → `Dictionary<TKey,TValue>`.
* `NullableExtensions.OrNull<T>()` existe en deux formes, une pour les types valeur et une pour les
  types référence annotés. **D10 interdit d'émettre l'une ou l'autre.**

### 14.5 Invariants sémantiques dont dépend le code émis

Ces cinq-là sont ceux qui casseraient silencieusement le code émis s'ils changeaient. Chacun est
exercé au §17.

1. **La source ambiante se résout au moment du tirage.** Chaque fabrique `Any.*` capture une source
   ambiante singleton, et cette source lit la frame `AsyncLocal` courante à l'intérieur de
   `Generate()`, pas à la construction. C'est pour cela qu'une recette construite hors d'un scope de
   reproductibilité y rejoue quand même (§8.2).
2. **`IAny<out T>` est covariante.** D'où l'absence d'adaptateur pour les lignes d'interface de
   collection du §5.2 — et la nécessité d'un pour la ligne nullable de type valeur.
3. **Les generators sont des recettes immuables.** Chaque contrainte fluide retourne une nouvelle
   instance. D2 en hérite.
4. **`Any.String()` non contraint tire 0 à 1024 caractères dans tout l'ASCII** (ADR-0075,
   ADR-0076). Il peut retourner la chaîne vide, et il peut retourner du blanc et des caractères de
   contrôle. C'est la première moitié qui porte les §5.2 et §5.3 ; les mesures du §17 ont été
   prises avant ces deux records, quand le tirage allait de 0 à 16 lettres et chiffres.
5. **`Any.OneOf(value)` exige au moins une valeur, rejette les éléments `null`, et consomme un
   tirage.** Ces trois raisons sont pourquoi le §4.2 émet un `FixedValue<TValue>` privé à la place.

### 14.6 Inventaire des analyzers

32 identifiants de diagnostic sur 31 classes d'analyzer — `JD023` et `JD024` en partagent une.

| Plage | Catégorie | Sévérités |
|---|---|---|
| `JD001`–`JD004` | Reproducibility | toutes **Error** |
| `JD005` | Usage | **Error** |
| `JD006` | Usage | Warning |
| `JD007`–`JD010` | Reproducibility | Warning |
| `JD011` | Usage | **Désactivé par défaut** |
| `JD012`–`JD013` | Usage | Warning |
| `JD014`–`JD017` | Constraints | Warning |
| `JD018` | Reproducibility | Warning |
| `JD019` | Reproducibility | **Désactivé par défaut** |
| `JD020` | Reproducibility | Info |
| `JD021` | Reproducibility | Warning |
| `JD022` | Reproducibility | Info |
| `JD023` | Constraints | Warning |
| `JD024` | Constraints | Info |
| `JD025`–`JD026` | Constraints | Warning |
| `JD027`–`JD028` | Composition | Warning |
| `JD029`–`JD031` | Constraints | Info |
| `JD032` | Constraints | Warning |

Trois faits à leur sujet pilotent des décisions de ce document :

* **Les 32 appellent `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)`** — d'où D3.
* **Les règles `Usage` matchent tout type implémentant `IAny<T>`**, pas une liste de generators
  intégrés — d'où le second bénéfice de D2.
* **Les règles `Reproducibility` matchent les chaînes enracinées sur la façade statique `Any`**, et
  répondent délibérément « non » pour un generator atteint par un local, un champ ou un paramètre.
  `new AnyOrder().Generate()` leur est donc invisible ; c'est une limite connue et acceptée, pas un
  défaut que le tool peut corriger.

### 14.7 Comment redériver ces faits

Depuis la racine du dépôt de la bibliothèque :

```console
# 14.1  identité du package et clivage de TFM
grep -n "TargetFrameworks\|PackageId\|analyzers/dotnet/cs" JustDummies/JustDummies.csproj
grep -n "#if NET8_0_OR_GREATER" JustDummies/Any.Primitive.cs

# 14.2  points d'entrée, et le miroir AnyContext
grep -hn "public static" JustDummies/Any.*.cs
grep -n "public " JustDummies/AnyContext.cs
# Les noms de types AVEC leur arité. `abstract` compte — AnyCollection n'est pas sealed, et un
# motif n'acceptant que `sealed` sous-compte d'une unité. L'arité est ce dont le contrôle de
# masquage du §7 a besoin : 8 noms génériques ne peuvent pas entrer en collision, les 32 autres si.
grep -rhoP "^public (?:sealed |abstract )?class \KAny\w+(?:<[^>]*>)?" JustDummies/*.cs | sort -u

# 14.3  surfaces de contraintes
grep -oP "public AnyInt32 \K\w+(?=\()" JustDummies/AnyInt32.cs | sort -u
grep -oP "public AnyUInt32 \K\w+(?=\()" JustDummies/AnyUInt32.cs | sort -u   # noter : ni Positive ni Negative

# 14.4  seams de composition
grep -n "public static" JustDummies/AnyExtensions.cs JustDummies/NullableExtensions.cs

# 14.5  invariants — les docs XML les énoncent tous les cinq
sed -n '1,60p' JustDummies/IAny.cs
grep -n "AmbientRandomSource.Instance" JustDummies/Any.Primitive.cs | head -3

# 14.6  inventaire des analyzers et exemption du code généré
cat JustDummies.Analyzers/AnalyzerReleases.Unshipped.md
grep -rlc "ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)" JustDummies.Analyzers/*.cs | wc -l
```

Les chemins sont ceux du dépôt actuel ; les ajuster si la bibliothèque a déménagé.

---

## 15. Enregistrements de décision

Les douze décisions de §2 sont toutes architecturales : un mainteneur futur questionnerait chacune
d'elles, et chacune tiendrait inchangée si l'implémentation était réécrite. **Onze enregistrements**
les couvrent — D5 et D6 en partagent un.

Ils ont été tenus dans cette spécification tant que le dépôt qui devait les héberger n'existait
pas : JustDummies vivait encore dans `Reefact/first-class-errors`, et les y numéroter aurait attribué
des poignées que la migration aurait ensuite forcé à abandonner. Ce dépôt existe désormais — c'est
celui-ci — et les enregistrements sont entrés dans sa base d'ADR, chacun conservant la date
`Proposed:` avec laquelle il a été écrit et portant la date à laquelle le mainteneur l'a accepté.

| Décision | Enregistrement |
|---|---|
| **D1** | [ADR-0056](../adr/0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.fr.md) — Scaffolder le generator une fois et confier le fichier au développeur |
| **D2** | [ADR-0057](../adr/0057-make-the-emitted-generator-a-first-class-iany.fr.md) — Faire du generator émis un `IAny<T>` de plein droit |
| **D3** | [ADR-0058](../adr/0058-leave-the-scaffolded-file-open-to-the-analyzers.fr.md) — Laisser le fichier scaffoldé ouvert aux analyzers JustDummies |
| **D4** | [ADR-0059](../adr/0059-emit-only-members-resolved-in-the-target-compilation.fr.md) — N'émettre que des membres résolus dans la compilation cible |
| **D5 + D6** | [ADR-0060](../adr/0060-seed-generators-from-constructor-guards.fr.md) — Amorcer les generators sur les gardes du constructeur, et laisser le reste en erreur de compilation |
| **D7** | [ADR-0061](../adr/0061-draw-from-the-ambient-context-and-hold-no-state.fr.md) — Tirer du contexte ambiant et ne détenir aucun état |
| **D8** | [ADR-0062](../adr/0062-emit-the-generator-into-the-target-types-namespace.fr.md) — Émettre le generator dans le namespace du type cible |
| **D9** | [ADR-0063](../adr/0063-give-the-scaffolder-no-dependency-on-the-package.fr.md) — Ne donner au scaffolder aucune dépendance sur le package JustDummies |
| **D10** | [ADR-0064](../adr/0064-never-draw-null-for-a-nullable-parameter.fr.md) — Ne jamais tirer null pour un paramètre nullable |
| **D11** | [ADR-0065](../adr/0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.fr.md) — Garder le moteur de scaffolding chargeable par un hôte Roslyn |
| **D12** | [ADR-0070](../adr/0070-emit-an-entry-point-on-request-as-a-file-of-its-own.fr.md) — Émettre un point d'entrée à la demande, dans un fichier à lui |

Trois de ces enregistrements ont été écrits après le tableau des décisions, et la raison mérite
d'être conservée. D7, D8 et D10 ont chacun été jugés trop petits au départ — une limite de périmètre
déjà programmée pour réexamen, un namespace par défaut avec surcharge, une règle sur une seule
méthode de la bibliothèque. La taille était à chaque fois la mauvaise mesure ; le test est de savoir
si la décision survit à l'implémentation, et les trois y survivent.

Plus important encore, chacun s'est révélé porter ailleurs dans ce document une conséquence qui se
lit comme accidentelle tant que le raisonnement n'est pas écrit. D10 explique pourquoi §5.2 porte une
conversion explicite pour les types valeur nullables. D8 est la **cause unique** du risque
d'occultation de §7. D7 explique pourquoi le type émis n'a besoin d'aucune règle de cycle de vie, et
pourquoi deux analyzers de graine n'ont rien à y signaler. Un enregistrement qui empêche un nettoyage
plausible de réintroduire un défaut mérite sa place quelle que soit sa taille, et aucune de ces trois
conséquences ne va de soi dans la section où elle atterrit.

---

### Un suivi côté bibliothèque, pas un enregistrement de décision

`Any.Fixed<T>(value)` — un `IAny<T>` retournant une constante — permettrait à l'émetteur
d'abandonner le helper imbriqué `FixedValue<TValue>` du §4.2. `Any.OneOf(value)` remplit presque le
rôle mais refuse `null` et consomme un tirage (§14.5). C'est un ajout à l'API publique de la
bibliothèque plutôt qu'une décision sur le tool, donc cela relève de la base de décision de la
bibliothèque et n'est **pas** requis pour la v1.0.

---

## 16. Réservé pour la v1.1+

La v1.0 ne doit pas s'interdire ces évolutions ; le §11.3 est la contrainte qui garde la première
bon marché.

**Nommage.** `AnyOrder` → `OrderFactory`, ou tout autre motif. Forme :

```console
dum generate Order --name OrderFactory        # ce type uniquement
dum generate Order --pattern "{Type}Factory"  # cette exécution
```

plus un `dum.json` optionnel à la racine du projet pour un défaut à l'échelle du projet :

```json
{ "naming": { "pattern": "Any{Type}" } }
```

`{Type}` est le seul emplacement. Le motif par défaut reste `Any{Type}`, donc un projet existant ne
voit aucun changement. C'est aussi la réponse à l'avertissement de masquage du §7.

**Lire les gardes regex.** Laissé hors du §5.3 pour la v1.0 parce que la bibliothèque ne génère
qu'à partir du sous-ensemble régulier du langage des motifs, et qu'un motif non supporté lève à la
construction — ce qui rendrait le type émis entièrement inutilisable. Y revenir suppose la question
du sous-ensemble tranchée d'abord : soit le moteur valide un motif sans référencer la bibliothèque
(ce que D9 interdit aujourd'hui), soit la bibliothèque offre un moyen de le lui demander.

**Autres éléments reportés.** `--all` ; les membres `init` / `required` et la construction par
propriétés ; le support d'`AnyContext` (D7) ; un sélecteur `--ctor` quand plusieurs constructeurs se
disputent ; l'extension du §5.3 à une bibliothèque auxiliaire de type `Guard.Against` ; la
publication de `JustDummies.GenAny` comme package propre une fois qu'un consommateur IDE existe ; le
code refactoring IDE lui-même.

Délibérément **non** reportés — abandonnés : un verbe `check`, un mode source generator, et toute
forme de régénération ou de détection de dérive. D1 supprime le problème qu'ils résoudraient.

---

## 17. Vérifications

### 17.1 Ce qui a été contrôlé

Le fichier émis du §4.1 a été écrit à la main exactement comme spécifié — paramètre `int?`, helper
`FixedValue<TValue>` et `AnyCustomer` composé compris — puis compilé et exécuté contre le
`JustDummies.dll` construit depuis la source (asset `net8.0`), avec les analyzers JustDummies
branchés. Les résultats ci-dessous sont ce que le harnais a affiché.

| Affirmation | Où | Résultat |
|---|---|---|
| Le squelette spécifié compile tel quel | §4.1 | compile, 0 avertissement |
| Le chaînage `.WithX` fonctionne et ne perturbe pas une base partagée | D2, §4.2 | deux `.WithStatus` sur une même base restent indépendants |
| `AnyOrder` est accepté par les seams de composition de la bibliothèque | D2, §15 | `Any.ListOf`, `Any.PairOf` et `.As` l'acceptent tous |
| `.WithX(IAny<T>)` maintient la composition contrainte ouverte | §4.2 | `.WithReference(Any.String().StartingWith("ORD-").As(...))` donne `ORD-x9vDEd2` |
| Une recette construite **hors** d'un scope rejoue dedans | §8.2, §14.5 | deux exécutions `Any.Reproducibly(20260730, …)` ont produit des valeurs identiques |
| La chaîne dérivée des gardes ne lève jamais | §5.3 | 500 tirages à travers `OrderReference.Create`, aucune `AnyGenerationException` |
| La chaîne **sans** lecture des gardes lève par intermittence | §5.3 | **594 / 10 000** tirages ont levé, et **557 / 10 000** à la reprise contre une bibliothèque plus récente — environ 1 sur 17, conforme aux 588 que prédisent dix-sept longueurs équiprobables |
| La covariance des collections ne demande aucun adaptateur | §5.2, §14.5 | `Any.ListOf(...)` affecté à `IAny<IReadOnlyList<string>>` |
| Un nullable de type valeur **exige** bien le saut `.As` | §5.2 | `IAny<int>` n'est pas un `IAny<int?>` ; `.As(value => (int?)value)` compile |
| Les bornes complémentaires se composent | §5.3 | `.GreaterThanOrEqualTo(0).LessThanOrEqualTo(100)` et `.NonEmpty().WithMaxLength(10)` tirent tous deux |
| Les bornes contradictoires sont rejetées deux fois | §5.3 | `ConflictingAnyConstraintException` à l'exécution, et `JD023` à la **compilation** |
| Un generator de motif n'admet aucune autre contrainte de chaîne | §5.3 | `Any.StringMatching(...).NonEmpty()` ne compile pas — `CS1061`, `AnyPattern` n'a que `DifferentFrom`/`Except` |
| Les regex de validation réalistes sortent du sous-ensemble supporté | §5.3 | 4 sur 5 rejetées : lookahead, limite de mot, backreference, catégorie Unicode |
| Un motif non supporté lève à la **construction**, pas au `Generate()` | §5.3 | donc le constructeur sans paramètre émis lèverait avant qu'un `With…` puisse surcharger |
| Les generators de collection ne portent aucune contrainte de longueur | §5.3 | `AnyList<T>` expose `WithCount`, `WithCountBetween`, `WithMinCount`, `WithMaxCount` — pas de `WithLength` |
| **Chaque ligne du §5.2 compile** | §5.2 | 40 déclarations, chacune affectant l'expression émise à l'`IAny<T>` du paramètre — 0 erreur, 0 warning, nullable activé, warnings-as-errors |
| **Chaque ligne du §5.2 tient sa promesse** | §5.2 | 3 000 tirages par ligne scalaire : `NonEmpty` jamais vide, `Guid` jamais `Empty`, `Enum` uniquement des membres déclarés, `Uri().Web()` absolue http(s) |
| **Chaque mapping de garde du §5.3 est solide** | §5.3 | 17 mappings × 4 000 tirages : toute valeur tirée est une valeur que la garde d'origine accepterait |
| **Chaque fait du §14 redérivé contre une bibliothèque plus récente** | §14 | 29 commits amont plus tard — exceptions retravaillées, parser regex refactoré — les décomptes, l'inventaire des analyzers et le sous-ensemble regex tiennent toujours |
| Les formes record, fabrique statique et noms atypiques fonctionnent | §4.2, §5.1 | record positionnel, type à constructeur privé plus `Create`, et paramètres `_id` / `@class` compilent et génèrent |
| Un constructeur sans paramètre casse la forme standard | §4.2 | émettre les deux constructeurs leur donne une seule signature — `CS0111` |
| Un nom de bibliothèque générique ne peut pas être masqué | §7 | un `AnySet` scaffoldé et `JustDummies.AnySet<T>` coexistent ; l'arité fait partie de l'identité |
| Un nom non générique, si | §7 | `AnyPattern` dans le namespace de la cible résout vers le type scaffoldé, pas celui de la bibliothèque |
| Les paramètres `ref` / `out` cassent le site d'appel | §5.1 | `CS1620` ; `in` accepte un argument par valeur sans broncher |
| `FixedValue` accepte ce que `Any.OneOf` refuse | §4.2 | `FixedValue<string?>(null)` rend null ; `Any.OneOf<string>(null)` lève `ArgumentException` |
| `.Positive()` est incorrect pour une garde `p < 1` sur un decimal | §5.3 | 1 tirage sur 5 000 est passé sous 1 sans contrainte ; ~1 sur 5 dès qu'une autre borne resserre |
| La sortie scaffoldée ne lève aucun avertissement JD | D3, §12 | 0 diagnostic de niveau avertissement ou au-dessus sur les fichiers émis |
| Les analyzers étaient réellement chargés | D3 | un fichier de contrôle a levé `JD006` et `JD005` dans le même build |
| `<auto-generated/>` les éteint | D3, §15 | le même fichier de contrôle, ainsi marqué, en a levé **0** — l'erreur `JD005` comprise |

### 17.2 Comment le rejouer

Rien du harnais n'est exotique ; il vaut la peine d'être recréé chaque fois que la bibliothèque
déménage ou change de version.

1. Construire la bibliothèque et les analyzers en `Release` (branche `net8.0` pour la bibliothèque).
2. Créer un projet console `net8.0` jetable **hors** du dépôt, pour qu'aucune propriété de build à
   l'échelle du dépôt ne s'applique. Référencer le `JustDummies.dll` construit par un
   `<Reference>` / `<HintPath>`, et l'analyzer construit par
   `<Analyzer Include="…/JustDummies.Analyzers.dll" />`.
3. Ajouter le domaine du §4.1 (`Order`, `OrderReference` avec son `Create` gardé, `Customer`,
   `OrderStatus`) et les `AnyOrder.cs` / `AnyCustomer.cs` scaffoldés exactement comme le §4.1 les
   spécifie.
4. Ajouter un **fichier de contrôle** avec deux violations connues — une contrainte dont le résultat
   est jeté (`Any.String().NonEmpty();` comme instruction, `JD006`) et un generator dans une chaîne
   interpolée (`$"{Any.Int32()}"`, `JD005`). Compiler, et confirmer que **les deux se déclenchent**.
   Sans cette étape, « aucun diagnostic sur le fichier scaffoldé » ne se distingue pas de
   « l'analyzer n'a jamais été chargé » — piège dans lequel cette vérification est tombée au premier
   essai.
5. Préfixer ce même fichier de contrôle par `// <auto-generated/>` et recompiler : les deux
   diagnostics disparaissent et le build réussit. C'est la preuve de D3.
6. Lancer les assertions du §17.1. Pour la mesure, boucler
   `Any.String().As(OrderReference.Create).Generate()` 10 000 fois en comptant les
   `AnyGenerationException`.

Note d'exécution : si seul un runtime .NET plus récent est installé, la sortie `net8.0` s'exécute
quand même sous `DOTNET_ROLL_FORWARD=LatestMajor`.
