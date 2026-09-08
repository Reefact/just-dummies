# ADR-0098 | Offrir chaque `OneOf` sous les deux formes, params et séquence

🌍 🇬🇧 [English](0098-offer-every-oneof-in-both-shapes.md) · 🇫🇷 Français (ce fichier)

**Status:** Proposed
**Proposed:** 2026-09-08
**Decision Makers:** Reefact

## Contexte

`OneOf`/`Except`/`DifferentFrom` est le trio agnostique du type que portent les constructeurs typés.
Mesuré sur `main`, vingt-trois constructeurs exposent `OneOf`, et tous prennent `params T[]`. Un seul
— `AnyString` — prend en plus `IEnumerable<string>`.

Rien dans les chaînes ne rend un ensemble de valeurs plus susceptible d'arriver sous forme de séquence
qu'un ensemble de devises, de membres d'énumération ou d'identifiants. L'asymétrie n'a aucun
fondement consigné ; c'est simplement là que la surcharge séquence s'est trouvée nécessaire en
premier.

Autres faits qui encadrent le choix :

* Les points d'entrée génériques portent déjà les deux formes, délibérément. `Any.OneOf<T>` prend
  `params T[]` et `Any.ElementOf<T>` prend `IReadOnlyList<T>` **et** `IEnumerable<T>`, pour qu'un
  helper à `yield` ou une requête LINQ n'exige pas de `.ToList()` au site d'appel.
* `ElementOf` n'est **pas** la voie séquence d'un constructeur typé. Il renvoie `AnyOneOf<T>`, dont
  toute la surface est la paire d'exclusion : l'appelant qui le prend perd les contraintes propres au
  type. `Any.String().OneOf(list).WithLength(3)` et `Any.Decimal().OneOf(rates).GreaterThan(0m)` n'ont
  aucun équivalent en `ElementOf`.
* Les deux modes de défaillance diffèrent. Passer une collection détenue au `Any.OneOf<T>` générique
  compile et produit silencieusement un vivier d'un seul élément — le piège que `JD013` signale. La
  passer à un `OneOf` typé ne compile pas du tout : il n'y a aucun `T` à inférer, donc l'appelant
  écrit `.ToArray()` et passe à la suite.
* La résolution de surcharge ne déplace aucun appel existant. `T[]` est une meilleure cible de
  conversion que `IEnumerable<T>` : un argument tableau, une liste `params` et une expression de
  collection se lient toutes encore à la forme tableau. Les seuls appels que la seconde surcharge
  change sont ceux qui ne compilaient pas.
* Le `S3220` de Sonar se déclenche là où un appel à une méthode `params` pourrait aussi lier une
  surcharge à un paramètre, et c'est la mesure, non le raisonnement, qui dit jusqu'où. Sur les formes
  qu'emploient les suites, c'est étroit : une valeur unique ordinaire, une liste de valeurs, une
  variable tableau et une `List<T>` restent toutes silencieuses, tandis que trois formes le
  déclenchent — une expression de collection, un `null` nu, et une valeur unique dont le type est un
  paramètre de type non résolu. Sur une expression de collection, il contredit en outre `S3878`, qui
  demande de retirer le tableau.
* `IPoolInspection<T>` est porté par tout générateur admettant un ensemble de valeurs fourni par
  l'appelant ([ADR-0068](0068-carry-the-pool-inspection-wherever-a-caller-supplies-the-values.fr.md)),
  et une garde par réflexion échoue déjà quand un générateur expose `OneOf` sans elle. Aucune garde
  équivalente ne surveillait la forme de `OneOf` lui-même : c'est ainsi que l'asymétrie a survécu
  jusqu'à être trouvée par un audit plutôt que par la suite de tests.
* La bibliothèque est en `1.0.0-preview`. `PublicAPI.Shipped.txt` est vide par décision, donc rien
  d'automatique ne protège la surface d'une preview publiée — un retrait est bon marché face à
  l'outillage, et pas face aux utilisateurs des six previews déjà publiées.

## Décision

Tout générateur exposant `OneOf` l'offre sous les deux formes — `params T[]` et `IEnumerable<T>` —
avec le même contrat, la même validation et les mêmes conflits.

## Justification

**La surface enseigne une règle, mais le lecteur ne peut pas repérer l'exception.** Un appelant qui a
rencontré `OneOf(params …)` sur un constructeur s'attend raisonnablement à ce que le suivant accepte
l'ensemble qu'il détient déjà. Vingt-deux sur vingt-trois le refusent, et rien au site d'appel
n'indique lesquels. Chaque lecteur paie donc ce coût, à répétition, pour économiser vingt-deux
méthodes de quatre lignes écrites une seule fois.

**« Une séquence passe par `ElementOf` » est une règle défendable pour la voie générique, mais fausse
ici.** Elle tient là où le vivier constitue toute la spécification, car `AnyOneOf<T>` ne perd alors
rien. Sur un constructeur typé, en revanche, elle fait perdre les contraintes du type : l'appelant qui
voulait un tirage borné depuis un ensemble fourni ne dispose plus d'aucune écriture pour l'exprimer.
Consigner cette règle reviendrait à documenter une capacité que la bibliothèque n'a pas.

**La surcharge n'ajoute rien à ce que le générateur tente.**
L'[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) borne l'ambition :
solveurs, propagation, bornes élargies, cas rendus satisfaisables par chance. Une forme qui
matérialise une séquence puis délègue à la contrainte existante ne relève d'aucun de ces cas. Les
valeurs tirées, les conflits et les diagnostics restent ceux que la forme tableau produisait déjà :
la surface s'élargit, mais pas ce que la bibliothèque promet.

**Une uniformité que seule la convention maintient finit par dériver.** L'écart a été trouvé en
comparant la surface déclarée de chaque générateur à celle des autres, et non par un test qui échoue :
la garde d'algèbre existante compare des *noms* de méthodes, elle ne pouvait donc pas voir qu'un
générateur portait une surcharge que les autres n'avaient pas. Une règle qui mérite d'être énoncée
mérite d'être vérifiée par réflexion, ce qui couvre aussi la prochaine famille ajoutée.

**Le coût `S3220` est trop limité pour peser contre la décision, et il a été mesuré plutôt que
supposé.** La règle ne touche pas les appels ordinaires. Le dépôt rencontre deux des trois formes
concernées, une fois chacune, et les résout en nommant explicitement la surcharge voulue. La seule
qu'un utilisateur écrirait plausiblement est l'expression de collection, où Sonar se contredit
lui-même et où passer directement les valeurs satisfait les deux règles. Renoncer à l'uniformité pour
épargner un analyseur tiers sur un cas aussi limité serait un mauvais arbitrage.

## Alternatives envisagées

### Retirer la surcharge d'`AnyString`, et énoncer qu'une séquence passe par `ElementOf`

Envisagée parce que c'est la surface la plus étroite, qu'elle demande une phrase plutôt que
vingt-deux méthodes, et qu'elle s'aligne sur l'habitude de la base : borner plutôt qu'élargir.

Rejetée parce que la règle qu'elle énoncerait n'est pas vraie de cette bibliothèque : `ElementOf`
renvoie un générateur ne portant que la paire d'exclusion, donc `Any.String().OneOf(list).WithLength(3)`
perdrait sa seule écriture. C'est en outre un retrait d'une surface publiée sur six previews, payé par
les appelants pour qu'une règle se lise bien.

### Laisser `AnyString` en exception documentée

Envisagée parce qu'elle est gratuite, et qu'une exception écrite n'est plus un piège.

Rejetée parce qu'il n'y a rien à écrire. Une exception a besoin d'un fondement, et le fondement ici
serait « c'est là que le besoin est apparu en premier » — ce qui explique l'histoire et ne justifie
rien. Consigner cela comme un choix de conception apprendrait au lecteur que les asymétries de la
bibliothèque sont délibérées, ce qui est précisément l'inférence qui rend la suivante plus difficile à
repérer.

### N'ajouter la forme séquence que là où un cas d'usage est apparu

Envisagée comme la voie incrémentale : l'ajouter aux constructeurs numériques maintenant, laisser les
temporels et les identifiants jusqu'à ce qu'on la demande.

Rejetée parce qu'elle recrée le défaut qu'elle corrige, un constructeur à la fois, et laisse la
surface dans un état qu'aucune règle ne décrit. L'uniformité *est* le livrable ; un sous-ensemble
d'uniformité n'en est pas une version réduite.

## Conséquences

### Positives

* Un ensemble déjà détenu comme liste, résultat LINQ ou valeurs d'une fixture atteint directement
  n'importe quel constructeur typé, les contraintes propres au type restant disponibles pour le
  resserrer.
* La règle — tout `OneOf` prend les deux formes — est vérifiable, tenue par une garde par réflexion
  qui couvre les générateurs pas encore écrits.
* Aucun site d'appel existant ne change de sens : les appels que la nouvelle surcharge affecte sont
  ceux qui ne compilaient pas.

### Négatives

* Vingt-deux surcharges publiques sur une surface qui approche la 1.0, chacune un membre à maintenir
  documenté et présent dans la baseline.
* Trois formes d'appel lèvent désormais `S3220` là où elles ne le faisaient pas — une expression de
  collection, un `null` nu, et une valeur unique d'un paramètre de type non résolu — et sur la
  première Sonar se contredit avec `S3878`. Les appels ordinaires ne sont pas touchés. Le dépôt a
  rencontré deux des trois, dans un test chacune.
* Sur `AnyChar`, la forme séquence admet une `string`, puisqu'une `string` est un `IEnumerable<char>`.
  Elle se lit comme les caractères qu'elle énumère, ce qui est la lecture utile, mais c'est une
  écriture que la forme `params` refusait et que la documentation doit désormais porter.

### Risques

* **La garde d'uniformité énonce une règle plus large que cette décision.** Elle exige les deux formes
  de chaque `OneOf` : un futur générateur ayant une vraie raison de n'en offrir qu'une échouerait à un
  test plutôt que de rencontrer une exception documentée. C'est la direction voulue — l'exception doit
  coûter un argument — mais c'est un test avec lequel il faudra un jour discuter plutôt qu'obéir.

## Actions de suivi

* Aucune. Les surcharges, les entrées de baseline, la garde et la documentation utilisateur
  atterrissent ensemble.

## Références

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — la borne à l'aune de
  laquelle cet élargissement est mesuré, et pourquoi elle ne s'applique pas à une forme de site
  d'appel.
* [ADR-0068](0068-carry-the-pool-inspection-wherever-a-caller-supplies-the-values.fr.md) — la règle
  jumelle sur qui porte un vivier fourni par l'appelant.
* [ADR-0011](0011-draw-arbitrary-values-from-an-explicit-top-level-pool.fr.md) — le vivier de premier
  niveau dont les formes d'`ElementOf` servent ici de contraste.
* [Issue #185](https://github.com/Reefact/just-dummies/issues/185) — l'élément d'audit qui a signalé
  l'asymétrie.
