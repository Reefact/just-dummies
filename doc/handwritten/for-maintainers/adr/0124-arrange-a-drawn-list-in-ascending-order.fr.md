# ADR-0124 | Ranger une liste tirée dans l'ordre croissant

🌍 🇬🇧 [English](0124-arrange-a-drawn-list-in-ascending-order.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

## Contexte

Une liste tirée vient dans n'importe quel ordre. Le code testé en exige souvent un : une courbe de charge
semi-horaire dont le constructeur refuse des horodatages qui ne croissent pas (essai UX 1, domaine 12), quatre
seuils de pertes catastrophe dans l'ordre croissant (essai UX 2, contexte 40). Les participants ont tiré des
valeurs distinctes et les ont triées dans `As(...)`, ce qui quitte le builder — plus d'effectif, plus de
`Containing`, plus de distinction après lui —, ou ont écrit un `IAny` de trente lignes. Un troisième cas, une
échelle de débits dont la résolution ne baisse jamais quand le débit monte (contexte 25), lie deux ordres.

L'issue #360 a décidé la surface : `Ordered()` et `OrderedBy(key)` sur les listes et les tableaux, croissants
seulement, la stricte croissance s'écrivant avec la distinction qui existe déjà.

## Décision

`Ordered()` et `OrderedBy(key)` rangent les éléments d'une liste ou d'un tableau dans l'ordre croissant au sens
large.

* Les éléments sont tirés, puis triés. L'ordre change l'arrangement, jamais le contenu : l'effectif, les valeurs
  contenues et la distinction tiennent exactement comme sans lui, où que l'ordre soit déclaré dans la chaîne, et le
  tri ne consomme aucun tirage.
* `Ordered()` n'existe que là où le type des éléments a un ordre naturel, `IComparable<T>` : c'est une méthode
  d'extension contrainte sur le type des éléments, donc ordonner des éléments sans ordre est une erreur de
  compilation. `OrderedBy(key)` exige que le type de la clé en ait un, par la même contrainte sur la clé.
* Strictement croissant s'écrit `Ordered().Distinct()` ou `OrderedBy(key).DistinctBy(key)` : une primitive
  chacun, qui se composent.
* Un ordre se déclare une fois. Le même ordre à nouveau ne change rien ; un ordre différent est refusé en nommant
  les deux. Deux ordres par clé ne sont le même qu'à travers la même instance de sélecteur, comme deux comparateurs
  d'égalité.
* Croissant seulement. Les ensembles et les dictionnaires n'ont pas d'ordre et n'ont pas le membre ; une séquence
  non plus.
* Deux ordres qui doivent monter ensemble sont deux listes ordonnées du même effectif, appariées dans l'ordre ; la
  page des collections montre la recette. Aucune règle entre éléments voisins n'est offerte.

## Justification

**Trier après le tirage est une construction, pas un filtre.** Chaque tirage donne une liste triée en une passe,
et toute autre contrainte porte sur le multiensemble des éléments, que le tri laisse intact. L'ordre compose donc
avec tout ce que la collection déclare déjà, sans coût pour la porte de cardinalité ni pour le retirage borné d'une
collection distincte.

**Une erreur de compilation vaut mieux qu'un refus à l'exécution.** Un membre d'`AnyList<T>` ne peut pas
contraindre `T` ; une méthode d'extension le peut. Écrire `Ordered()` sur des éléments sans ordre n'est alors pas
un échec à `Generate()` mais un appel qui n'existe pas, comme `Positive()` n'existe pas sur un entier non signé. Le
coût est un appel statique dans la chaîne ; les analyzers lisent une extension de la bibliothèque qui rend le
générateur qu'elle étend comme un maillon de la chaîne, donc les règles de chaîne continuent de voir chaque
contrainte autour.

**La stricte croissance est déjà la distinction.** Un drapeau `strict` dupliquerait ce que disent `Distinct()` et
`DistinctBy(key)`, et demanderait son propre raisonnement de cardinalité. Composer les deux garde un seul sens par
appel.

**Croissant seulement.** Un ordre décroissant est l'ordre croissant lu à l'envers ; les retours de terrain n'en
ont demandé aucun, et offrir les deux double la surface pour un renversement que le test peut écrire.

**Aucune règle entre voisins.** « L'élément i + 1 satisfait f par rapport à l'élément i » est de la propagation de
contraintes, qu'[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) tient hors de la
bibliothèque. Le cas des deux échelles s'exprime par construction avec ce qui existe, et le registre consigne la
règle générale comme non adoptée.

## Alternatives envisagées

### Continuer à trier dans `As(...)`

Envisagé parce que cela fonctionne déjà. Rejeté parce que cela quitte le builder : plus rien ne peut se déclarer
après, et l'intention — cette liste est ordonnée — se lit comme un détail d'implémentation d'une projection.

### Un `Ordered()` d'instance vérifié à l'exécution

Envisagé parce que chaque maillon reste un appel d'instance. Rejeté parce que cela change une erreur de type en
échec à `Generate()`, pour une propriété que le compilateur sait décider.

### Un paramètre `strict`

Envisagé dans la première version de l'issue. Rejeté parce que la distinction le dit déjà, et que les deux se
composent.

### Trier aussi les ensembles et les dictionnaires

Envisagé par symétrie. Rejeté parce que leurs types de résultat n'ont pas d'ordre à garder.

## Conséquences

### Positives

* Une liste ou un tableau ordonné tient en un appel, en gardant le builder : effectif, valeurs contenues et
  distinction s'appliquent toujours.
* Ordonner des éléments sans ordre ne compile pas.
* Le parcours de chaîne des analyzers suit une extension de la bibliothèque qui rend son propre générateur, donc un
  futur membre contraint peut s'ajouter de la même façon.

### Négatives

* `Ordered()` est une extension statique, trouvée par `using JustDummies;`, à la différence de ses voisins sur le
  builder.
* Une clé nullable — `OrderedBy(r => r.ClosedAt)` avec un `DateTime?` — ne satisfait pas la contrainte et demande
  la valeur qu'elle contient.

### Risques

* `Ordered()` sur des chaînes suit l'ordre naturel de `string`, sensible à la culture. Atténuation : l'ordre est
  celui du type, le même que donne `List<T>.Sort()` ; un test qui a besoin d'un ordre ordinal trie par `OrderedBy`
  selon une clé qu'il choisit.

## Actions de suivi

* Aucune.

## Références

* Issue [#360](https://github.com/Reefact/just-dummies/issues/360) — la décision et la recette que porte cet
  enregistrement.
* [ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md) — la collection distincte avec
  laquelle l'ordre compose.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — pourquoi aucune règle entre voisins.
* La page des collections — [`collections.fr.md`](../../for-users/generators/collections.fr.md).
